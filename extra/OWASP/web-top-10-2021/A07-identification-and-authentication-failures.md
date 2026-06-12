# A07:2021 · Fallos de Identificación y Autenticación (Identification and Authentication Failures)

> **Estado:** ✅ — Login con bloqueo de cuenta, contraseñas PBKDF2 con re-hash transparente, JWT RS256 validados estrictamente, refresh tokens rotados con detección de reuso y rate-limit de fuerza bruta en el gateway.

## Qué exige OWASP
Confirmar la identidad del usuario y gestionar la sesión de forma robusta: contraseñas almacenadas con un hash fuerte y salado, defensa contra ataques automatizados (credential stuffing / fuerza bruta), bloqueo de cuenta ante intentos fallidos, tokens de sesión de alta entropía que se invalidan correctamente, y validación íntegra de las credenciales de sesión (firma, emisor, audiencia, expiración).

## Cómo lo cumple DAMA

### Hashing de contraseña con PBKDF2 (210 000 iteraciones)
El hash de contraseña usa el `PasswordHasher<User>` de ASP.NET Core Identity (PBKDF2-HMAC-SHA256, salt aleatorio por contraseña, formato versionado v3) con el conteo de iteraciones elevado a 210 000.

`apps/Auth/Backend/Modules/PasswordHashingModule.cs:9` y `:15-19`:

```csharp
private const int Pbkdf2IterationCount = 210_000;
...
services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = Pbkdf2IterationCount;
});
services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
```

### Re-hash transparente al iniciar sesión
Si el hash almacenado se generó con parámetros obsoletos, `VerifyHashedPassword` devuelve `SuccessRehashNeeded`; en ese caso se re-calcula el hash con los parámetros actuales y se persiste, sin fricción para el usuario.

`apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs:83-88`:

```csharp
if (verification == PasswordVerificationResult.SuccessRehashNeeded)
{
    string upgradedHash = _passwordHasher.HashPassword(user, request.Password);
    await _userDao.UpdatePasswordHashAsync(user.Id, upgradedHash, scope);
    LogEvents.PasswordHashUpgraded(_logger, user.Id);
}
```

### Bloqueo de cuenta ante intentos fallidos (5 intentos / 900 s → 423)
La tabla `User` lleva `FailedLoginAttempts` y `LockedUntil`. Antes de verificar la contraseña, si la cuenta está bloqueada y el bloqueo aún no expiró, el login devuelve `AccountLocked` sin tocar el hash.

`apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs:61-65`:

```csharp
if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
{
    LogEvents.LoginBlockedAccountLocked(_logger, user.Id);
    return new LoginOutcome.AccountLocked();
}
```

Cada contraseña incorrecta incrementa el contador; al alcanzar el umbral se fija `LockedUntil`. Constantes en `apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:19-20`:

```csharp
private const int MaxFailedLoginAttempts = 5;
private const int LockoutSeconds = 900;
```

El incremento+bloqueo es atómico en el procedimiento almacenado `RegisterFailedLoginAttempt` (`infrastructure/environments/auth/init.sql:78-93`):

```sql
UPDATE User u
SET u.FailedLoginAttempts = u.FailedLoginAttempts + 1
WHERE u.Id = userId;

UPDATE User u
SET u.LockedUntil = UTC_TIMESTAMP(6) + INTERVAL lockoutSeconds SECOND,
    u.FailedLoginAttempts = 0
WHERE u.Id = userId
    AND u.FailedLoginAttempts >= maxAttempts;
```

Un login exitoso resetea el contador y limpia `LockedUntil` (`ResetFailedLoginAttempts`, `init.sql:97-103`), invocado dentro de la misma transacción que emite el refresh token (`AuthenticationService.cs:89`).

El controlador mapea el resultado bloqueado a **HTTP 423 Locked**. `apps/Auth/Backend/Controllers/AuthController.cs:151-152`:

```csharp
LoginOutcome.AccountLocked => StatusCode(StatusCodes.Status423Locked,
    "Cuenta bloqueada temporalmente por intentos fallidos. Intente más tarde."),
```

El tipo de resultado es una unión discriminada cerrada (`apps/Auth/Backend/Results/Users/LoginOutcome.cs:9-13`): `Success`, `InvalidCredentials`, `AccountLocked`. La respuesta de credenciales inválidas es genérica (no distingue "usuario inexistente" de "contraseña incorrecta"), evitando enumeración de usuarios.

### Tokens de acceso JWT firmados con RS256
El access token se firma con clave RSA privada (RS256), no con un secreto simétrico compartido. Auth es el único emisor.

`apps/Auth/Backend/Security/JwtTokenSigner.cs:23-27`:

```csharp
_rsa = RSA.Create();
_rsa.ImportFromPem(privateKeyPem);

RsaSecurityKey securityKey = new RsaSecurityKey(_rsa);
Credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
```

### Validación estricta del JWT en cada servicio
Cada backend valida emisor, audiencia, vigencia y firma con la clave pública RSA; `MapInboundClaims = false` evita el renombrado automático de claims.

`apps/Auth/Backend/Modules/JwtAuthenticationModule.cs:33-44`:

```csharp
bearerOptions.MapInboundClaims = false;
bearerOptions.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwtOptions.Issuer,
    ValidateAudience = true,
    ValidAudience = jwtOptions.Audience,
    ValidateLifetime = true,
    IssuerSigningKey = LoadPublicKey(jwtOptions.PublicKey),
    ValidateIssuerSigningKey = true,
    ...
};
```

### Refresh tokens: alta entropía, almacenados como hash, rotación + detección de reuso
El refresh token es de 32 bytes aleatorios (`RandomNumberGenerator`), se entrega en claro una sola vez al cliente y en la base solo se guarda su SHA-256 (nunca el valor en claro).

`apps/Auth/Backend/Security/RefreshTokenGenerator.cs:14`, `:25` y `:38-42`:

```csharp
private const int TokenByteLength = 32;
...
string rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
...
public string ComputeHash(string rawToken)
{
    byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

Cada refresh **rota**: revoca el token presentado y emite uno nuevo. Si se presenta un token ya revocado (reuso, señal de robo), se revocan **todas** las sesiones del usuario. `apps/Auth/Backend/Services/Concrete/Users/RefreshService.cs:51-58` y `:67-70`:

```csharp
if (stored.Token.RevokedAt is not null)
{
    await using ITransactionScope reuseScope = await _unitOfWork.BeginAsync();
    await _refreshTokenWriteDao.RevokeAllForUserAsync(stored.Token.UserId, reuseScope);
    await reuseScope.CommitAsync();
    LogEvents.RefreshTokenReuseDetected(_logger, stored.Token.UserId);
    return null;
}
...
await _refreshTokenWriteDao.RevokeAsync(stored.Token.Id, scope);
await _refreshTokenWriteDao.CreateAsync(issued.Entity, scope);
```

El logout revoca todas las sesiones del usuario (`RefreshService.cs:84-90`). La columna `TokenHash` tiene índice único (`infrastructure/environments/auth/init.sql:50`).

### Rate-limit de fuerza bruta en el gateway
El throttling vive en nginx, no en cada servicio. El login está limitado a 5 peticiones por minuto por IP.

`infrastructure/environments/api-gateway/nginx.conf:73` y `:146-150`:

```nginx
limit_req_zone $binary_remote_addr zone=login:10m    rate=5r/m;
...
location = /api/auth/login {
    limit_req zone=login burst=2 nodelay;
    limit_req_status 429;
    proxy_pass http://auth;
}
```

## Flujo de los componentes

```
POST /api/auth/login
  └─ nginx zone=login (5 r/m por IP) ──► 429 si se excede
       │
       ▼
  AuthenticationService.LoginAsync
   1. ReadUserWithTenantByUserNameAsync(username)
        └─ no existe ──► InvalidCredentials (401, mensaje genérico)
   2. ¿user.LockedUntil > UtcNow?
        └─ sí ──► AccountLocked ──► 423 Locked
   3. VerifyHashedPassword(hash, password)
        ├─ Failed ──► RegisterFailedLoginAttempt
        │              └─ ¿FailedLoginAttempts >= 5? ──► fija LockedUntil (+900 s)
        │              └─ InvalidCredentials (401)
        ├─ SuccessRehashNeeded ──► HashPassword + UpdatePasswordHash (en transacción)
        └─ Success
   4. ResetFailedLoginAttempts + emitir access token (RS256) + refresh token (32B, SHA-256) ──► commit
   5. ──► 200 { AccessToken, RefreshToken }
```

Diagrama FossFLOW: rectángulo **"A07 · Identification & Auth Failures"** en `extra/fossflow/diagrams/owasp-web-top-10.json`, con los nodos `JWT validation params`, `Account Lockout 423`, `Refresh rotation + reuse` y `Rate-limit login (nginx)`.

## Verificación
- Suite de Auth: `cd apps/Auth/Test && dotnet test --filter "FullyQualifiedName~Login"` y `~Refresh`.
- Bloqueo: 5 POST consecutivos a `/api/auth/login` con contraseña incorrecta para un usuario válido → el 6.º (y los siguientes durante 900 s) devuelven **423** aunque la contraseña sea correcta. Colecciones Bruno en `api-endpoints/`.
- Rate-limit: ráfaga de >5 logins/min desde una IP → **429** desde nginx antes de llegar al backend.
- Reuso de refresh: usar dos veces el mismo refresh token → la segunda devuelve 401 y revoca todas las sesiones (`LogEvents.RefreshTokenReuseDetected`).

## Notas / brechas conocidas
- No hay MFA ni política de complejidad/longitud de contraseña más allá de la validación FluentValidation del DTO de registro; el endurecimiento se centra en almacenamiento, bloqueo y anti-fuerza-bruta.
- El bloqueo es por cuenta (`LockedUntil`), no por IP; la defensa por IP la aporta el rate-limit de nginx. Ambos se complementan pero un atacante distribuido podría sortear el límite por-IP.
- El reset de `FailedLoginAttempts` tras un bloqueo se hace en el propio SP (`RegisterFailedLoginAttempt` pone el contador a 0 al bloquear), de modo que tras expirar `LockedUntil` la cuenta arranca con contador limpio.
