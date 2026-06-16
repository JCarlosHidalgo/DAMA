# API2:2023 · Autenticación Rota (Broken Authentication)

> **Estado:** ✅ — Emisión/validación de JWT RS256 con parámetros estrictos, bloqueo de cuenta (423), rotación de refresh tokens con detección de reuso y rate-limit de login en el gateway.

## Qué exige OWASP
Los mecanismos de autenticación de la API deben resistir credential stuffing y fuerza bruta, almacenar credenciales con hashes fuertes, emitir tokens con firma verificable (sin algoritmos débiles ni `alg:none`), validar siempre firma/emisor/audiencia/expiración, y permitir invalidar tokens. No deben filtrar si un usuario existe ni aceptar tokens fabricados.

## Cómo lo cumple DAMA

### Emisión de JWT con RS256 (firma asimétrica)
Auth es el único emisor de tokens y firma con clave RSA **privada**; el resto de servicios nunca tiene la privada, solo la pública. Esto descarta `alg:none` y la confusión de algoritmo simétrico/asimétrico.

`apps/Auth/Backend/Security/JwtTokenSigner.cs:23-27`:

```csharp
_rsa = RSA.Create();
_rsa.ImportFromPem(privateKeyPem);

RsaSecurityKey securityKey = new RsaSecurityKey(_rsa);
Credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
```

La clave privada solo se inyecta en Auth (env `JWT_PRIVATE_KEY_B64`), y `SecretsValidationModule` (Order -100) la valida al arranque, fallando rápido si falta o no decodifica. `apps/Auth/Backend/Modules/SecretsValidationModule.cs:12-15`:

```csharp
SecretsValidation.RequireRsaPrivateKey(
    configuration["AppSettings:PrivateKey"], "JWT_PRIVATE_KEY_B64");
SecretsValidation.RequireRsaPublicKey(
    configuration["AppSettings:PublicKey"], "JWT_PUBLIC_KEY_B64");
```

### Validación estricta del token en cada API
Cada backend valida emisor, audiencia, vigencia y firma; `MapInboundClaims = false` desactiva el remapeo de claims, de modo que los nombres de claim (`AuthClaims`) se leen tal cual se emiten.

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

El token que emite Auth lleva un `aud` por cada servicio (`JwtAccessTokenGenerator.cs:46-49`), de modo que pasa el chequeo de audiencia única de cada API y valida en todas. `UseAuthentication()` se cablea explícitamente en los cinco servicios.

### Resistencia a fuerza bruta: bloqueo de cuenta (423)
Tras 5 contraseñas incorrectas la cuenta se bloquea 900 s; durante ese tiempo el login devuelve **423 Locked** aun con la contraseña correcta.

`apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs:61-65` y `:69-74`:

```csharp
if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
{
    LogEvents.LoginBlockedAccountLocked(_logger, user.Id);
    return new LoginOutcome.AccountLocked();
}
...
if (verification == PasswordVerificationResult.Failed)
{
    await _userDao.RegisterFailedLoginAttemptAsync(user.Id);
    LogEvents.LoginFailedInvalidPassword(_logger, request.Username);
    return new LoginOutcome.InvalidCredentials();
}
```

Umbral y ventana (`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:19-20`): `MaxFailedLoginAttempts = 5`, `LockoutSeconds = 900`. El incremento+bloqueo es atómico en el SP `RegisterFailedLoginAttempt` (`infrastructure/environments/auth/init.sql:78-93`). Mapeo a 423 en `apps/Auth/Backend/Controllers/AuthController.cs:151-152`.

La respuesta de credenciales inválidas no distingue entre usuario inexistente y contraseña errónea (mismo `InvalidCredentials` → 401 con mensaje genérico), evitando enumeración de cuentas vía la API.

### Credenciales almacenadas con PBKDF2 + re-hash
PBKDF2-HMAC-SHA256 con 210 000 iteraciones (`apps/Auth/Backend/Modules/PasswordHashingModule.cs:9`), con upgrade transparente del hash en login cuando los parámetros son obsoletos (`AuthenticationService.cs:83-88`).

### Tokens de sesión invalidables: rotación de refresh + detección de reuso
El refresh token es de 32 bytes aleatorios y solo se almacena su SHA-256. Cada uso rota el token (revoca + emite). Reusar un token ya revocado revoca **todas** las sesiones del usuario.

`apps/Auth/Backend/Services/Concrete/Users/RefreshService.cs:51-58`:

```csharp
if (stored.Token.RevokedAt is not null)
{
    await using ITransactionScope reuseScope = await _unitOfWork.BeginAsync();
    await _refreshTokenWriteDao.RevokeAllForUserAsync(stored.Token.UserId, reuseScope);
    await reuseScope.CommitAsync();
    LogEvents.RefreshTokenReuseDetected(_logger, stored.Token.UserId);
    return null;
}
```

Generación en `apps/Auth/Backend/Security/RefreshTokenGenerator.cs:25` (`RandomNumberGenerator.GetBytes(32)`) y `:38-42` (SHA-256). Logout: `RefreshService.cs:84-90` revoca todo.

### Rate-limit del endpoint de login en el gateway
`infrastructure/environments/api-gateway/nginx.conf:73` y `:146-150`: zona `login` a 5 r/m por IP sobre `location = /api/auth/login`, respondiendo **429** al excederse — antes de tocar el backend.

## Flujo de los componentes

```
Cliente API ──► POST /api/auth/login
  └─ nginx zone=login (5 r/m) ──► 429 si excede
       ▼
  AuthenticationService.LoginAsync
   leer usuario ──► (inexistente) InvalidCredentials/401 genérico
        │
        ▼ ¿LockedUntil > ahora? ── sí ──► AccountLocked/423
        ▼ verificar PBKDF2
          ├─ Failed ──► RegisterFailedLoginAttempt (¿>=5? bloquea 900s) ──► 401
          ├─ SuccessRehashNeeded ──► re-hash + UpdatePasswordHash
          └─ Success ──► ResetFailedLoginAttempts
                          + access token RS256 (validado por cada API: iss/aud/exp/firma)
                          + refresh token (32B, guardado como SHA-256)
                          ──► 200
  Uso posterior:
   POST /api/auth/refresh ──► rota token; si ya estaba revocado ──► revoca TODAS las sesiones
```

Diagrama FossFLOW: rectángulo **"API2 · Broken Authentication"** en `extra/graphics/diagrams/owasp-api-top-10.json`, nodos `RS256 JWT`, `Account Lockout`, `Refresh reuse detection` y `Rate-limit login`.

## Verificación
- `cd apps/Auth/Test && dotnet test --filter "FullyQualifiedName~Login"` / `~Refresh`.
- Token forjado / `alg:none`: cualquier servicio rechaza con 401 (firma RSA no válida, `ValidateIssuerSigningKey = true`).
- Bloqueo: 5 logins fallidos → 423 durante 900 s (verificable con Bruno en `api-endpoints/`).
- Reuso de refresh: segundo uso del mismo token → 401 + revocación global.
- Rate-limit: >5 logins/min por IP → 429 desde el gateway.

## Notas / brechas conocidas
- Sin MFA. El factor único es contraseña + bloqueo + rate-limit.
- La llamada gRPC interna Payment→Auth (`x-subscription-secret`) usa un secreto compartido en metadata, no un JWT de usuario, por originarse en un `BackgroundService` sin `HttpContext`; está fuera del flujo de autenticación de usuario y cubierta por TLS interno (ver A08 / API8).
- El access token no es revocable individualmente antes de su expiración (es stateless); la revocación efectiva opera sobre los refresh tokens. Mantener la vida del access token corta limita la ventana.
