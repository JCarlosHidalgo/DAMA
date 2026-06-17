# A01 · Broken Access Control (Web Top 10 2021)

> **Estado:** ✅ — Toda la superficie HTTP de los cinco backends es *denegada por defecto* vía `FallbackPolicy`, y cada punto de acceso accesible exige rol explícito o se abre con `[AllowAnonymous]` de forma deliberada.

## Qué exige OWASP

El control de acceso roto ocurre cuando un usuario puede actuar fuera de los permisos previstos: acceder a funciones de otro rol, elevar privilegios o ver/operar recursos ajenos. OWASP recomienda **denegar por defecto** (salvo recursos públicos), centralizar la verificación de autorización en el servidor y nunca confiar en que el cliente oculte las acciones. Es el riesgo #1 de la lista 2021.

## Cómo lo cumple DAMA

### Denegado por defecto con `FallbackPolicy`

Los cinco servicios (Auth, Attendance, CourseManagement, Payment, Credentials) registran una política de respaldo que exige usuario autenticado para **cualquier** punto de acceso que no declare su propia política. La configuración añade autorización al contenedor de servicios y fija el `FallbackPolicy` a un `AuthorizationPolicyBuilder` que requiere usuario autenticado (`apps/Auth/Backend/Modules/AuthorizationModule.cs:12`). Si un controlador olvida `[Authorize]`, sigue cerrado.

El módulo implementa `IServiceModule, IAppModule` y cablea `app.UseAuthorization()` en `Configure` (`apps/Auth/Backend/Modules/AuthorizationModule.cs:22`). Los cinco archivos `apps/<Svc>/Backend/Modules/AuthorizationModule.cs` son idénticos en este punto.

### Autenticación JWT RS256 antes de autorizar

El middleware de autenticación valida la firma asimétrica (clave pública RSA), emisor, audiencia y vigencia antes de que la política de autorización corra. Los `TokenValidationParameters` activan la validación de emisor (`ValidIssuer`), audiencia (`ValidAudience`) y vigencia (`ValidateLifetime`), cargan la clave pública RSA como `IssuerSigningKey` y mapean los claims de nombre y de rol a las constantes de `AuthClaims` (`apps/Auth/Backend/Modules/JwtAuthenticationModule.cs:34`).

`UseAuthentication()` se cablea explícitamente en los cinco servicios (`apps/Auth/Backend/Modules/JwtAuthenticationModule.cs:51`), sin depender del middleware auto-añadido por `WebApplication`.

### `[Authorize(Roles = ...)]` por punto de acceso

Sobre la base denegado por defecto, cada acción declara el rol que la habilita usando las constantes de `apps/Auth/Backend/Security/UserRoles.cs:3` (`Admin`/`Client`/`Teacher`/`Student`), nunca literales.

Las operaciones globales sobre academias quedan reservadas a `Admin`: la acción `GetAll` de listado lleva `[Authorize(Roles = UserRoles.Admin)]` (`apps/Auth/Backend/Controllers/TenantController.cs:25`).

El directorio y registro de usuarios es de `Client` (`apps/Auth/Backend/Controllers/AuthController.cs:45`). Sólo `login`/`refresh` se abren con `[AllowAnonymous]` (`apps/Auth/Backend/Controllers/AuthController.cs:143` y `:158`), y `logout` exige sólo autenticación (`:170`).

### Excepciones públicas declaradas, no implícitas

Las rutas que deben ser anónimas lo marcan explícitamente, de modo que la apertura es auditable:

- Sondas de disponibilidad: `apps/Auth/Backend/Modules/HealthCheckModule.cs:32` (`/health`) y `:39` (`/health/ready`) usan `.AllowAnonymous()` para que el orquestador sondee sin token.
- gRPC `TenantSubscription`: `apps/Auth/Backend/Modules/GrpcServerModule.cs:17` lo abre con `.AllowAnonymous()` porque su autenticación es un secreto compartido en metadata, no un JWT de usuario.
- Callback de Todotix: `apps/Payment/Backend/Controllers/QrPaymentController.cs:121` (`[AllowAnonymous]`) porque el origen es un webhook externo; se valida por firma HMAC en su lugar (`:128`).

## Flujo de los componentes

La cadena de una petición HTTP autenticada:

```
petición (Authorization: Bearer <jwt>)
   │
   ▼  UseAuthentication()  ── JwtBearer: valida RS256, emisor, audiencia, vigencia
   │       (JwtAuthenticationModule, IAppModule.Order = 30)
   ▼  UseAuthorization()   ── FallbackPolicy: ¿usuario autenticado?
   │       (AuthorizationModule, IAppModule.Order = 40)  → 401 si no
   ▼  [Authorize(Roles = ...)] del punto de acceso  → 403 si el rol no calza
   │
   ▼  acción del controlador → IClaimContext lee academia/usuario/rol
```

Si un punto de acceso no declara política, el `FallbackPolicy` igual exige autenticación (denegado por defecto); si declara `[Authorize(Roles)]`, suma el filtro de rol; si declara `[AllowAnonymous]`, la apertura es explícita y rastreable.

En el diagrama FossFlow `extra/graphics/diagrams/owasp-web-top-10.json`, este ítem es el rectángulo **A01 · Broken Access Control** que agrupa: **JwtBearer middleware**, **FallbackPolicy deny-by-default**, **[Authorize(Roles)]**, **IClaimContext**, **Tenant-filter DAO** y **JWT validation params**.

## Verificación

- `apps/Auth/Test/` (116 pruebas), `apps/Attendance/Test/` (103), `apps/CourseManagement/Test/` (193), `apps/Payment/Test/` (186) cubren los flujos de controlador. Ejecutar por servicio:

  ```bash
  cd apps/Auth/Test && dotnet test
  ```
- Comprobar el comportamiento denegado por defecto manualmente con Bruno (`api-endpoints/`): cualquier petición sin `Authorization` a un punto de acceso no-anónimo debe devolver 401; con rol incorrecto, 403.
- Revisar que ningún `AuthorizationModule.cs` haya perdido el `FallbackPolicy`:

  ```bash
  grep -n FallbackPolicy apps/*/Backend/Modules/AuthorizationModule.cs
  ```
  debe arrojar los cinco.

## Notas y brechas conocidas

- El `FallbackPolicy` sólo exige *autenticación*, no rol: un punto de acceso sin `[Authorize(Roles)]` queda abierto a cualquier usuario autenticado. La defensa real por-rol depende de que cada acción declare su rol; está cubierto hoy, pero un punto de acceso nuevo sin atributo de rol sería accesible a todo token válido (no a anónimos).
- La autorización a nivel de objeto (que un `Client` no toque la academia de otro, que un `Student` no lea datos de otro) **no** la cubre `[Authorize]`; vive en la capa de consulta filtrada por academia y en los chequeos de propiedad. Ver `API1-broken-object-level-authorization.md`.
