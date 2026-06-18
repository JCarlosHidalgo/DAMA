# A07:2021 · Fallos de Identificación y Autenticación (Identification and Authentication Failures)

> **Estado:** ✅ — Login con bloqueo de cuenta, contraseñas PBKDF2 con re-cálculo transparente del hash, JWT RS256 validados estrictamente, refresh tokens rotados con detección de reuso y limitación de tasa de peticiones de fuerza bruta en la puerta de enlace.

## Introducción

Esta ficha documenta cómo DAMA atiende los **fallos de identificación y autenticación** (A07), el riesgo de confirmar la identidad del usuario y gestionar la sesión de forma deficiente. El documento recorre la evidencia técnica de la autenticación robusta: el hashing de contraseñas con PBKDF2 (210 000 iteraciones) y su re-cálculo transparente al iniciar sesión, el bloqueo de cuenta atómico ante intentos fallidos, los access tokens JWT firmados con RS256 y validados estrictamente en cada servicio, los refresh tokens de alta entropía con rotación y detección de reuso, y la limitación de tasa anti-fuerza-bruta en la puerta de enlace. Cierra con el flujo del login, los comandos de verificación y las brechas conocidas (ausencia de MFA y de política de complejidad de contraseña).

## Qué exige OWASP
Confirmar la identidad del usuario y gestionar la sesión de forma robusta: contraseñas almacenadas con un hash fuerte y salado, defensa contra ataques automatizados (relleno de credenciales / fuerza bruta), bloqueo de cuenta ante intentos fallidos, tokens de sesión de alta entropía que se invalidan correctamente, y validación íntegra de las credenciales de sesión (firma, emisor, audiencia, expiración).

## Cómo lo cumple DAMA

### Hashing de contraseña con PBKDF2 (210 000 iteraciones)
El hash de contraseña usa el `PasswordHasher<User>` de ASP.NET Core Identity (PBKDF2-HMAC-SHA256, salt aleatorio por contraseña, formato versionado v3) con el conteo de iteraciones elevado a 210 000. El módulo configura `PasswordHasherOptions.IterationCount` con esa constante y registra `IPasswordHasher<User>` como singleton (`apps/Auth/Backend/Modules/PasswordHashingModule.cs:9` y `:15-19`).

### Re-cálculo transparente del hash al iniciar sesión
Si el hash almacenado se generó con parámetros obsoletos, `VerifyHashedPassword` devuelve `SuccessRehashNeeded`; en ese caso se re-calcula el hash con los parámetros actuales, se persiste mediante `UpdatePasswordHashAsync` y se registra el evento `PasswordHashUpgraded`, sin fricción para el usuario (`apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs:83-88`).

### Bloqueo de cuenta ante intentos fallidos (5 intentos / 900 s → 423)
La tabla `User` lleva `FailedLoginAttempts` y `LockedUntil`. Antes de verificar la contraseña, si la cuenta está bloqueada y el bloqueo aún no expiró, el login devuelve `AccountLocked` sin tocar el hash (`apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs:61-65`).

Cada contraseña incorrecta incrementa el contador; al alcanzar el umbral se fija `LockedUntil`. Las constantes son `MaxFailedLoginAttempts = 5` y `LockoutSeconds = 900` (`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:19-20`).

El incremento y el bloqueo son atómicos en el procedimiento almacenado `RegisterFailedLoginAttempt`: una sentencia incrementa `FailedLoginAttempts` para el usuario, y otra fija `LockedUntil` a la hora actual más el intervalo de bloqueo y reinicia el contador a cero sólo cuando el número de intentos alcanza el umbral (`infrastructure/environments/auth/init.sql:78-93`).

Un login exitoso resetea el contador y limpia `LockedUntil` (`ResetFailedLoginAttempts`, `init.sql:97-103`), invocado dentro de la misma transacción que emite el refresh token (`AuthenticationService.cs:89`).

El controlador mapea el resultado bloqueado a **HTTP 423 Locked**, con un mensaje que informa del bloqueo temporal por intentos fallidos (`apps/Auth/Backend/Controllers/AuthController.cs:151-152`).

El tipo de resultado es una unión discriminada cerrada con los casos `Success`, `InvalidCredentials` y `AccountLocked` (`apps/Auth/Backend/Results/Users/LoginOutcome.cs:9-13`). La respuesta de credenciales inválidas es genérica (no distingue "usuario inexistente" de "contraseña incorrecta"), evitando enumeración de usuarios.

### Tokens de acceso JWT firmados con RS256
El access token se firma con clave RSA privada (RS256), no con un secreto simétrico compartido. La clase importa la clave privada desde PEM y construye unas `SigningCredentials` con `RsaSha256`. Auth es el único emisor (`apps/Auth/Backend/Security/JwtTokenSigner.cs:23-27`).

### Validación estricta del JWT en cada servicio
Cada backend valida emisor, audiencia, vigencia y firma con la clave pública RSA; `MapInboundClaims = false` evita el renombrado automático de claims. Los `TokenValidationParameters` activan `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` y `ValidateIssuerSigningKey`, con la clave pública cargada vía `LoadPublicKey` (`apps/Auth/Backend/Modules/JwtAuthenticationModule.cs:33-44`).

### Refresh tokens: alta entropía, almacenados como hash, rotación + detección de reuso
El refresh token es de 32 bytes aleatorios (`RandomNumberGenerator`), se entrega en claro una sola vez al cliente y en la base sólo se guarda su SHA-256 (nunca el valor en claro). La longitud del token está fijada en `TokenByteLength = 32`, el valor en claro se genera codificando los bytes aleatorios en Base64Url, y `ComputeHash` produce el SHA-256 en hexadecimal minúsculas que se persiste (`apps/Auth/Backend/Security/RefreshTokenGenerator.cs:14`, `:25` y `:38-42`).

Cada refresh **rota**: revoca el token presentado y emite uno nuevo. Si se presenta un token ya revocado (reuso, señal de robo), se abre una transacción que revoca **todas** las sesiones del usuario vía `RevokeAllForUserAsync`, se registra `RefreshTokenReuseDetected` y se aborta la operación; en el camino normal se revoca el token presentado y se crea el nuevo (`apps/Auth/Backend/Services/Concrete/Users/RefreshService.cs:51-58` y `:67-70`).

El logout revoca todas las sesiones del usuario (`RefreshService.cs:84-90`). La columna `TokenHash` tiene índice único (`infrastructure/environments/auth/init.sql:50`).

### Limitación de tasa de peticiones de fuerza bruta en la puerta de enlace
La limitación de tasa vive en nginx, no en cada servicio. Se define una zona `login` con tasa de 5 peticiones por minuto por IP, y el `location` exacto `/api/auth/login` aplica esa zona con `burst=2 nodelay`, responde 429 al exceder el límite y reenvía al upstream `auth` (`infrastructure/environments/api-gateway/nginx.conf:73` y `:146-150`).

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

Diagrama FossFlow: rectángulo **"A07 · Identification & Auth Failures"** en `extra/graphics/diagrams/owasp-web-top-10.json`, con los nodos `JWT validation params`, `Account Lockout 423`, `Refresh rotation + reuse` y `Rate-limit login (nginx)`.

## Verificación
- Suite de Auth: `cd apps/Auth/Test && dotnet test --filter "FullyQualifiedName~Login"` y `~Refresh`.
- Bloqueo: 5 POST consecutivos a `/api/auth/login` con contraseña incorrecta para un usuario válido → el 6.º (y los siguientes durante 900 s) devuelven **423** aunque la contraseña sea correcta. Colecciones Bruno en `api-endpoints/`.
- Rate-limit: ráfaga de >5 logins/min desde una IP → **429** desde nginx antes de llegar al backend.
- Reuso de refresh: usar dos veces el mismo refresh token → la segunda devuelve 401 y revoca todas las sesiones (`LogEvents.RefreshTokenReuseDetected`).

## Notas y brechas conocidas
- No hay MFA ni política de complejidad/longitud de contraseña más allá de la validación FluentValidation del DTO de registro; el endurecimiento se centra en almacenamiento, bloqueo y anti-fuerza-bruta.
- El bloqueo es por cuenta (`LockedUntil`), no por IP; la defensa por IP la aporta el rate-limit de nginx. Ambos se complementan pero un atacante distribuido podría sortear el límite por-IP.
- El reset de `FailedLoginAttempts` tras un bloqueo se hace en el propio SP (`RegisterFailedLoginAttempt` pone el contador a 0 al bloquear), de modo que tras expirar `LockedUntil` la cuenta arranca con contador limpio.
