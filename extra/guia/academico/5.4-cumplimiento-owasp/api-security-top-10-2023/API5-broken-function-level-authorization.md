# API5 · Broken Function Level Authorization (API Security Top 10 2023)

> **Estado:** ✅ — Cada función se gatea por rol con `[Authorize(Roles)]` sobre una base denegada por defecto, y las funciones premium suman gating por nivel de suscripción con `[RequiresServiceTier]`.

## Qué exige OWASP

BFLA ocurre cuando un usuario puede invocar funciones (puntos de acceso, operaciones administrativas o de pago) para las que no está autorizado, normalmente porque la API confía en que el cliente oculte la acción o porque el control por rol es inconsistente. OWASP exige negar por defecto y verificar, **en el servidor y por cada función**, que el rol/plan del llamante la habilita.

## Cómo lo cumple DAMA

### Gating por rol con denegado por defecto

La autorización a nivel de función parte del `FallbackPolicy` (denegado por defecto, ver `../web-top-10-2021/A01-broken-access-control.md`): sin atributo, la función exige al menos autenticación. Encima, cada acción declara el rol que la habilita.

Las funciones de operador global se reservan a `Admin` decorando la acción con `[Authorize(Roles = UserRoles.Admin)]`; así, por ejemplo, el listado completo de academias en `apps/Auth/Backend/Controllers/TenantController.cs:25` solo es accesible para ese rol.

Las funciones de pago QR se reservan a `Student` combinando `[Authorize(Roles = UserRoles.Student)]` con `[RequiresServiceTier(3)]` sobre la acción que crea la deuda, en `apps/Payment/Backend/Controllers/QrPaymentController.cs:43`.

Los roles son constantes en `apps/Auth/Backend/Security/UserRoles.cs:3` (`Admin`/`Client`/`Teacher`/`Student`), nunca literales.

### Gating por nivel de suscripción: `[RequiresServiceTier]`

Más allá del rol, las funciones premium exigen un nivel mínimo de la pirámide de servicios contratada por la academia. El filtro lee el claim `IndexCoreServicesPyramid` y la expiración de la suscripción, ambos del token firmado.

En `OnAuthorization`, el filtro resuelve el `IClaimContext` desde el contenedor de la petición y, si el nivel efectivo del llamante queda por debajo del mínimo requerido, corta la ejecución devolviendo un `403 Forbidden` con un `ProblemDetails` titulado "Insufficient subscription tier." (`apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:19`).

El nivel efectivo es 0 si la suscripción venció — la expiración se evalúa contra el claim, no contra un indicador mutable por el cliente. El método que lo resuelve compara `DateTime.UtcNow` con `SubscriptionExpiresAt`: si la suscripción sigue vigente devuelve `IndexCoreServicesPyramid`, y en caso contrario (o si falta el claim, vía `MissingClaimException`) devuelve 0 (`apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:36`).

El atributo es combinable con el rol y aplicable a método o clase (declarado con `AttributeUsage(... AllowMultiple = true)`, `apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:9`). Ejemplos de uso:

- A nivel de método en `apps/Auth/Backend/Controllers/AuthController.cs:46` (`[RequiresServiceTier(2)]` sobre el directorio de usuarios) y en cada acción de `apps/Payment/Backend/Controllers/QrPaymentController.cs:44`.
- A nivel de **clase** en `apps/Payment/Backend/Controllers/TodotixCredentialController.cs:15` (`[RequiresServiceTier(3)]`), que aplica a todas sus acciones, combinándose con el `[Authorize(Roles = ...)]` de cada una.

## Flujo de los componentes

La cadena que protege una función:

```
petición (Authorization: Bearer <jwt>)
   │
   ▼  UseAuthentication()  ── JwtBearer valida RS256/iss/aud/exp
   │
   ▼  UseAuthorization() + FallbackPolicy  ── denegado por defecto → 401 si anónimo
   │
   ▼  [Authorize(Roles = ...)]  ── ¿el rol del claim habilita la función? → 403
   │
   ▼  [RequiresServiceTier(n)]  ── ¿IndexCoreServicesPyramid ≥ n y suscripción vigente? → 403
   │
   ▼  acción del controlador
```

Las dos comprobaciones son ortogonales: el rol decide *qué tipo de usuario* puede invocar; el tier decide *qué plan* lo desbloquea. Una función premium para `Student` exige ambas (`[Authorize(Roles = UserRoles.Student)]` + `[RequiresServiceTier(3)]`).

En el diagrama FossFlow `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API5 · Broken Function Level Authorization** que agrupa: **[Authorize(Roles)]**, **RequiresServiceTier** y **FallbackPolicy** (con **Tier gating (suscripcion)** como nodo de apoyo).

## Verificación

- `apps/Payment/Test/` ejercita el comportamiento de `RequiresServiceTierAttribute` (tier insuficiente ⇒ 403; suscripción vencida ⇒ nivel efectivo 0).
- `apps/Auth/Test/` cubre el gating por rol de `TenantController`/`AuthController`.
- Manual con Bruno (`api-endpoints/`): invocar una función `Admin` con token de `Client` ⇒ 403; invocar una función con `[RequiresServiceTier(3)]` con un token cuyo `IndexCoreServicesPyramid` sea menor ⇒ 403 con `ProblemDetails` "Insufficient subscription tier."

## Notas y brechas conocidas

- `[RequiresServiceTier]` confía en el claim `IndexCoreServicesPyramid` del token. Tras un cambio de plan, el nivel real sólo se refresca cuando se emite un token nuevo (login/refresh): hasta que el access token expira, el nivel del claim puede quedar momentáneamente desfasado. La expiración de suscripción sí se evalúa en cada petición contra `SubscriptionExpiresAt`.
- El filtro vive en Payment y Auth (las funciones con gating por tier). No es transversal a los cinco servicios; un punto de acceso premium nuevo debe declarar el atributo explícitamente.
- La limitación de tasa de peticiones/anti-fuerza-bruta de estas funciones vive en la puerta de enlace nginx, no por servicio (ver `A01`/diagrama, nodo **Rate-limit nginx**).
