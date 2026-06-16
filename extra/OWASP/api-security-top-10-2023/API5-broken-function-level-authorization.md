# API5 · Broken Function Level Authorization (API Security Top 10 2023)

> **Estado:** ✅ — Cada función se gatea por rol con `[Authorize(Roles)]` sobre una base deny-by-default, y las funciones premium suman gating por nivel de suscripción con `[RequiresServiceTier]`.

## Qué exige OWASP

BFLA ocurre cuando un usuario puede invocar funciones (endpoints, operaciones administrativas o de pago) para las que no está autorizado, normalmente porque la API confía en que el cliente oculte la acción o porque el control por rol es inconsistente. OWASP exige negar por defecto y verificar, **en el servidor y por cada función**, que el rol/plan del llamante la habilita.

## Cómo lo cumple DAMA

### Gating por rol con deny-by-default

La autorización a nivel de función parte del `FallbackPolicy` (deny-by-default, ver `../web-top-10-2021/A01-broken-access-control.md`): sin atributo, la función exige al menos autenticación. Encima, cada acción declara el rol que la habilita.

`apps/Auth/Backend/Controllers/TenantController.cs:25` — funciones de operador global reservadas a `Admin`:

```csharp
[Authorize(Roles = UserRoles.Admin)]
[HttpGet]
public async Task<ActionResult<List<TenantDto>>> GetAll()
```

`apps/Payment/Backend/Controllers/QrPaymentController.cs:43` — funciones de pago QR reservadas a `Student`:

```csharp
[Authorize(Roles = UserRoles.Student)]
[RequiresServiceTier(3)]
[HttpPost("{templateId:guid}")]
public async Task<ActionResult> CreateDebt(Guid templateId, CreateQrDebtDto dto)
```

Los roles son constantes en `apps/Auth/Backend/Security/UserRoles.cs:3` (`Admin`/`Client`/`Teacher`/`Student`), nunca literales.

### Gating por nivel de suscripción: `[RequiresServiceTier]`

Más allá del rol, las funciones premium exigen un nivel mínimo de la pirámide de servicios contratada por el tenant. El filtro lee el claim `IndexCoreServicesPyramid` y la expiración de la suscripción, ambos del token firmado.

`apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:19`

```csharp
public void OnAuthorization(AuthorizationFilterContext context)
{
    IClaimContext claimContext = context.HttpContext.RequestServices.GetRequiredService<IClaimContext>();
    if (ResolveEffectiveIndex(claimContext) < _minimumIndex)
    {
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Insufficient subscription tier.",
            ...
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
```

El nivel efectivo es 0 si la suscripción venció — la expiración se evalúa contra el claim, no contra un flag mutable por el cliente. `apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:36`:

```csharp
private static int ResolveEffectiveIndex(IClaimContext claimContext)
{
    try
    {
        return DateTime.UtcNow < claimContext.SubscriptionExpiresAt
            ? claimContext.IndexCoreServicesPyramid
            : 0;
    }
    catch (MissingClaimException)
    {
        return 0;
    }
}
```

El atributo es combinable con el rol y aplicable a método o clase (`AttributeUsage(... AllowMultiple = true)`, `apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:9`). Ejemplos de uso:

- A nivel de método en `apps/Auth/Backend/Controllers/AuthController.cs:46` (`[RequiresServiceTier(2)]` sobre el directorio de usuarios) y en cada acción de `apps/Payment/Backend/Controllers/QrPaymentController.cs:44`.
- A nivel de **clase** en `apps/Payment/Backend/Controllers/TodotixCredentialController.cs:15` (`[RequiresServiceTier(3)]`), que aplica a todas sus acciones, combinándose con el `[Authorize(Roles = ...)]` de cada una.

## Flujo de los componentes

La cadena que protege una función:

```
request (Authorization: Bearer <jwt>)
   │
   ▼  UseAuthentication()  ── JwtBearer valida RS256/iss/aud/exp
   │
   ▼  UseAuthorization() + FallbackPolicy  ── deny-by-default → 401 si anónimo
   │
   ▼  [Authorize(Roles = ...)]  ── ¿el rol del claim habilita la función? → 403
   │
   ▼  [RequiresServiceTier(n)]  ── ¿IndexCoreServicesPyramid ≥ n y suscripción vigente? → 403
   │
   ▼  acción del controlador
```

Las dos comprobaciones son ortogonales: el rol decide *qué tipo de usuario* puede invocar; el tier decide *qué plan* lo desbloquea. Una función premium para `Student` exige ambas (`[Authorize(Roles = UserRoles.Student)]` + `[RequiresServiceTier(3)]`).

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API5 · Broken Function Level Authorization** que agrupa: **[Authorize(Roles)]**, **RequiresServiceTier** y **FallbackPolicy** (con **Tier gating (suscripcion)** como nodo de apoyo).

## Verificación

- `apps/Payment/Test/` ejercita el comportamiento de `RequiresServiceTierAttribute` (tier insuficiente ⇒ 403; suscripción vencida ⇒ nivel efectivo 0).
- `apps/Auth/Test/` cubre el gating por rol de `TenantController`/`AuthController`.
- Manual con Bruno (`api-endpoints/`): invocar una función `Admin` con token de `Client` ⇒ 403; invocar una función con `[RequiresServiceTier(3)]` con un token cuyo `IndexCoreServicesPyramid` sea menor ⇒ 403 con `ProblemDetails` "Insufficient subscription tier."

## Notas / brechas conocidas

- `[RequiresServiceTier]` confía en el claim `IndexCoreServicesPyramid` del token. Tras un cambio de plan, el nivel real sólo se refresca cuando se emite un token nuevo (login/refresh): hasta que el access token expira, el nivel del claim puede quedar momentáneamente desfasado. La expiración de suscripción sí se evalúa en cada petición contra `SubscriptionExpiresAt`.
- El filtro vive en Payment y Auth (las funciones con gating por tier). No es transversal a los cinco servicios; un endpoint premium nuevo debe declarar el atributo explícitamente.
- El rate-limiting/anti-fuerza-bruta de estas funciones vive en el gateway nginx, no por servicio (ver `A01`/diagrama, nodo **Rate-limit nginx**).
