# A09:2021 · Fallos de Registro y Monitoreo de Seguridad (Security Logging and Monitoring Failures)

> **Estado:** ✅ — Logging estructurado en JSON con source generators `[LoggerMessage]`, eventos de auditoría para toda operación sensible (login, refresh, borrado de usuario, alta/renombrado de tenant), readiness por dependencia en `/health/ready`, y la regla firme de **nunca registrar secretos, contraseñas ni tokens**.

## Qué exige OWASP
Registrar los eventos de seguridad relevantes (inicios de sesión válidos y fallidos, control de acceso, fallos del servidor) en un formato consumible por herramientas de monitoreo, con contexto suficiente para detectar y responder a incidentes — pero **sin** filtrar datos sensibles en los propios logs. Exponer señales de salud/disponibilidad que permitan alertar cuando una dependencia cae.

## Cómo lo cumple DAMA

### Logging estructurado con source generators `[LoggerMessage]`
Los eventos de log se declaran como métodos `partial` decorados con `[LoggerMessage]`, de modo que el compilador genera el código de logging (alto rendimiento, sin boxing, con `EventId` estable por evento). Cada operación sensible tiene su propio identificador numérico, lo que permite filtrar y alertar por `EventId` en el agregador.

`apps/Auth/Backend/Logging/LogEvents.cs:17-19` y `:53-59`:

```csharp
[LoggerMessage(EventId = 3001, Level = LogLevel.Information,
    Message = "Login succeeded for user {UserId} on tenant {TenantId}")]
public static partial void LoginSucceeded(ILogger logger, Guid userId, Guid tenantId);
...
[LoggerMessage(EventId = 3010, Level = LogLevel.Warning,
    Message = "Refresh token reuse detected for user {UserId}; all sessions revoked")]
public static partial void RefreshTokenReuseDetected(ILogger logger, Guid userId);
```

### Auditoría de operaciones sensibles (ampliada recientemente)
El logging de Auth se amplió con un conjunto de eventos de auditoría que cubren todo el ciclo de identidad. Los eventos existen en `LogEvents.cs` y **se invocan** en los servicios:

- **Login** — `LoginSucceeded` (3001), `LoginFailedUserNotFound` (3002), `LoginFailedInvalidPassword` (3003), `LoginBlockedAccountLocked` (3006), `PasswordHashUpgraded` (3007) en `apps/Auth/Backend/Services/Concrete/Users/AuthenticationService.cs` (`:55`, `:63`, `:72`, `:87`, `:93`).
- **Refresh / logout** — `RefreshTokenReuseDetected` (3010), `TokenRefreshed` (3009), `UserLoggedOut` (3011) en `apps/Auth/Backend/Services/Concrete/Users/RefreshService.cs:56`, `:75`, `:89`.
- **Directorio** — `UserDeleted` (3008) en `apps/Auth/Backend/Services/Concrete/Users/UserDirectoryService.cs:80`.
- **Tenants** — `TenantCreated` (5003), `TenantRenamed` (5004) en `apps/Auth/Backend/Services/Concrete/Tenants/TenantService.cs:41`, `:50`.

Ejemplo de la cadena login fallido → bloqueo de cuenta, `AuthenticationService.cs:61-73`:

```csharp
if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
{
    LogEvents.LoginBlockedAccountLocked(_logger, user.Id);
    return new LoginOutcome.AccountLocked();
}

PasswordVerificationResult verification =
    _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
if (verification == PasswordVerificationResult.Failed)
{
    await _userDao.RegisterFailedLoginAttemptAsync(user.Id);
    LogEvents.LoginFailedInvalidPassword(_logger, request.Username);
    return new LoginOutcome.InvalidCredentials();
}
```

La detección de reuso de refresh token (señal de robo de credenciales) registra el evento y revoca todas las sesiones del usuario — exactamente el tipo de evento de seguridad que OWASP espera ver auditado.

### Auditoría de la integración externa (Payment)
Payment registra los eventos de seguridad de su frontera con Todotix en `apps/Payment/Backend/Logging/LogEvents.cs`: callback con firma inválida `TodotixCallbackInvalidSignature` (5001), respuesta que no deserializa `TodotixConsultDebtDeserializationFailed` (5002), deuda no pagada `TodotixConsultDebtUnpaid` (5003), y fallos de prueba de credenciales (5004/5005). `:81-83`:

```csharp
[LoggerMessage(EventId = 5001, Level = LogLevel.Warning,
    Message = "Rejected Todotix callback with invalid signature for transaction {TransactionId}")]
public static partial void TodotixCallbackInvalidSignature(ILogger logger, Guid transactionId);
```

Invocado en el endpoint de callback, `apps/Payment/Backend/Controllers/QrPaymentController.cs:128-132`:

```csharp
if (!_callbackSignature.Verify(transactionId.ToString("D"), signature ?? string.Empty))
{
    LogEvents.TodotixCallbackInvalidSignature(_logger, transactionId);
    return Ok();
}
```

### Logs en JSON estructurado
Todos los servicios emiten logs en JSON (no texto plano), consumibles por un agregador. `apps/Auth/Backend/appsettings.json:9-16`:

```json
"Console": {
  "FormatterName": "json",
  "FormatterOptions": {
    "IncludeScopes": true,
    "UseUtcTimestamp": true,
    "TimestampFormat": "yyyy-MM-ddTHH:mm:ss.fffZ "
  }
}
```

`IncludeScopes: true` hace que el ámbito de correlación de request (módulo `RequestCorrelation`, ver `apps/CLAUDE.md`) viaje en cada línea, así que los eventos de un mismo request se pueden agrupar. Un `dotnet run` local emite el mismo JSON; para logs legibles a mano se exporta `Logging__Console__FormatterName=simple`.

### Readiness por dependencia: `/health/ready`
Cada backend mapea dos endpoints en `apps/*/Backend/Modules/HealthCheckModule.cs`: `GET /health` (liveness superficial, `Predicate = _ => false`, sin checks) para que el orquestador sepa que el proceso responde, y `GET /health/ready` (readiness profundo) que ejecuta un check por dependencia real y nombra en la respuesta cuál está caída. `apps/Auth/Backend/Modules/HealthCheckModule.cs:34-39`:

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
    ResponseWriter = ReadinessResponseWriter.WriteAsync
})
.AllowAnonymous();
```

Los checks viven en `apps/*/Backend/ExternalCheck/` (`DatabaseHealthCheck`, `RabbitMqHealthCheck`, `GrpcPeerHealthCheck`), registrados bajo el nombre convención `"{Servicio}-{Dependencia}"` (p.ej. `AuthService-Database`, `AttendanceService-CourseManagementGrpc`). Cobertura: Auth/CourseManagement = Database + RabbitMq; Attendance = + CourseManagementGrpc; Payment = + AuthGrpc; Credentials sólo `/health` (sin dependencias externas). Un 503 de `/health/ready` indica exactamente qué dependencia falló, que es la señal de monitoreo que OWASP pide.

### No se registran secretos, contraseñas ni tokens
**Punto clave de diseño.** Los métodos `[LoggerMessage]` sólo aceptan identificadores no sensibles: `Guid userId`, `Guid tenantId`, `string userName`, `string role`, `int error`. Nunca se pasan `Password`, `PasswordHash`, `AccessToken`, `RefreshToken`, `Appkey` ni la firma del callback. En `AuthenticationService.LoginAsync` el `accessToken` y el `refreshToken` se generan y se devuelven, pero **jamás se loguean**; el evento `LoginSucceeded` sólo lleva `userId`/`tenantId`. Lo mismo en Payment: el callback inválido registra el `transactionId`, no la firma ni el `Appkey` del tenant.

## Flujo de los componentes

```
Operación sensible (login / refresh / delete user / tenant create-rename / callback Todotix)
  └─ Servicio invoca LogEvents.<Evento>(logger, <ids no sensibles>)
       └─ source generator [LoggerMessage] (EventId estable) emite el log
            └─ Console formatter = json  (+ scope de correlación)
                 └─► stdout ──► agregador / monitoreo  (filtrable por EventId)

Monitoreo de disponibilidad
  GET /health          (liveness, sin checks)        ── orquestador: ¿proceso vivo?
  GET /health/ready    (un check por dependencia)    ── 503 nombra la dependencia caída
                       Database / RabbitMq / gRPC peer

Regla transversal: ningún Password / Token / Appkey / firma entra en un log.
```

Diagrama FossFLOW: rectángulo **"A09 · Logging & Monitoring Failures"** en `extra/fossflow/diagrams/owasp-web-top-10.json`, nodos `LogEvents source-gen`, `JSON structured logs` y `Health /ready checks`.

## Verificación
- `grep -n "EventId = 30\|EventId = 50" apps/Auth/Backend/Logging/LogEvents.cs` → lista los eventos de auditoría de identidad y tenant.
- Call sites: `grep -rn "LogEvents\." apps/Auth/Backend/Services/` → confirma que cada evento se invoca (login, refresh, delete, tenant).
- Sin secretos en logs: `grep -rniE "logger.*(password|passwordhash|accesstoken|refreshtoken|appkey|secret)" apps/*/Backend --include=*.cs` → sin coincidencias en mensajes de log.
- JSON: arrancar cualquier servicio y comprobar que stdout emite objetos JSON con `EventId`, `Message` y el scope de correlación.
- Readiness: `curl -i http://localhost:<puerto>/health/ready` con una dependencia caída → 503 con la lista de checks y el nombre del que falló.

## Notas / brechas conocidas
- El stack confía en `stdout` + el agregador del orquestador (Dokploy/Docker) para retención y alerta; no hay un SIEM ni reglas de alerta versionadas en el repo — el envío y la correlación se configuran en infraestructura, fuera de código.
- No hay rate-limit ni umbral de bloqueo configurado *en el logging*; el anti-fuerza-bruta vive en el gateway nginx (ver A04/A07), y los eventos `LoginFailed*` / `LoginBlockedAccountLocked` son la señal que ese control produce.
- Credentials, al no tener dependencias externas, sólo expone `/health` (sin `/health/ready`); es intencional, no una omisión de monitoreo.
