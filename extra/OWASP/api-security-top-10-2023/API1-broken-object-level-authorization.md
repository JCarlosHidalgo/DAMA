# API1 · Broken Object Level Authorization (API Security Top 10 2023)

> **Estado:** ✅ — Todo acceso a un objeto se filtra por el `tenantId` del token dentro del `WHERE` del DAO, y los recursos por-estudiante suman un chequeo de propiedad explícito.

## Qué exige OWASP

BOLA (antes IDOR) es el riesgo #1 de la lista de APIs: el atacante manipula el `id` de un objeto en la ruta o el cuerpo para leer/operar recursos que no le pertenecen. OWASP exige validar, **en cada acceso a cada objeto**, que el usuario autenticado tiene derecho sobre *ese* objeto concreto — no basta con que esté autenticado.

## Cómo lo cumple DAMA

### El `tenantId` viene del token, nunca del cliente

El identificador de tenant no se acepta como parámetro de la petición: se lee del claim firmado vía `IClaimContext`. El cliente no puede falsearlo sin romper la firma RS256.

`apps/Auth/Backend/Claims/IClaimContext.cs:5` expone `Guid TenantId { get; }`; la implementación lo lee del `HttpContext.User` (`apps/Auth/Backend/Claims/ClaimContext.cs:24`) y lanza `MissingClaimException` si falta o es inválido (`:62`). Así, el filtro de tenant siempre parte de un valor confiable.

### El filtro por tenant vive en el `WHERE` del DAO

La consulta por id siempre exige `id` **y** `tenantId` juntos en SQL. Un objeto de otro tenant simplemente no aparece (devuelve `null` → 404), nunca 403 que confirme su existencia.

`apps/Payment/Backend/DB/Daos/Concrete/Single/QrPayments/SuccessQrPaymentDao.cs:151`

```csharp
public async Task<SuccessQrPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId)
{
    ...
    const string sql = "SELECT Id, TenantId, StudentId, ClassQuantity, Cost, Currency, PaidAt " +
                       "FROM SuccessQrPayment WHERE Id = @paymentId AND TenantId = @tenantId;";
    ...
    selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
    selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
```

El servicio invoca el DAO pasando el tenant del claim, no de la petición. `apps/Payment/Backend/Services/Concrete/QrPayments/QrPaymentQueryService.cs:54`:

```csharp
public async Task<GetQrDebtStatusOutcome> GetDebtStatusAsync(Guid paymentId)
{
    Guid tenantId = _claimContext.TenantId;
    PendingQrPayment? pending = await _pendingQrPaymentDao.GetByIdForTenantAsync(tenantId, paymentId);
    ...
    SuccessQrPayment? success = await _successQrPaymentDao.GetByIdForTenantAsync(tenantId, paymentId);
    if (success != null) { ... }
    return new GetQrDebtStatusOutcome.NotFound();
}
```

El mismo patrón en `apps/Payment/Backend/Services/Concrete/Subscriptions/SubscriptionQueryService.cs:37` (`GetByIdForTenantAsync(tenantId, paymentId)` para pagos de suscripción) y en CourseManagement: `apps/CourseManagement/Backend/Application/Courses/GetCourseById.cs:29`:

```csharp
Course? course = await _courseDao.GetByIdForTenantAsync(_claimContext.TenantId, query.CourseId);
if (course == null)
{
    return new GetCourseByIdResult.NotFound();
}
```

### Chequeo de propiedad para recursos por-estudiante

Dentro de un tenant, un `Student` no puede leer los datos de attendance de otro estudiante. El chequeo combina rol + identidad del token en una extensión sobre `IClaimContext`.

`apps/Attendance/Backend/Services/Concrete/ClaimContextExtensions.cs:8`

```csharp
public static bool IsStudentAccessingOtherStudent(this IClaimContext claimContext, Guid requestedStudentId)
{
    if (claimContext.Role != UserRoles.Student)
    {
        return false;
    }
    return claimContext.UserId != requestedStudentId;
}
```

Uso en el servicio, que corta con un outcome `Forbidden` antes de tocar la BD. `apps/Attendance/Backend/Services/Concrete/Attendance/ScheduledClassService.cs:44`:

```csharp
if (claimContext.IsStudentAccessingOtherStudent(studentId))
{
    return new GetScheduledByStudentOutcome.Forbidden();
}
```

Se aplica también en `apps/Attendance/Backend/Services/Concrete/Attendance/UniqueClassService.cs:42` y `apps/Attendance/Backend/Services/Concrete/Remain/RemainClassReader.cs:21`. Un `Client`/`Teacher` (rol distinto de `Student`) pasa el chequeo y puede consultar a cualquier estudiante de su propio tenant — el filtro de tenant del DAO sigue acotando el alcance.

## Flujo de los componentes

```
request con id en la ruta (p.ej. GET /api/payment/qr/{id}/status)
   │
   ▼  IClaimContext.TenantId  ── claim firmado, no parámetro del cliente
   │
   ▼  service: GetByIdForTenantAsync(tenantId, id)
   │
   ▼  DAO: SELECT ... WHERE Id = @id AND TenantId = @tenantId
   │       → objeto de otro tenant ⇒ null ⇒ NotFound (404), no se filtra existencia
   │
   ▼  (recursos por-estudiante) IsStudentAccessingOtherStudent(studentId)
           → Student pidiendo a otro Student ⇒ Forbidden (403)
```

La defensa es en dos capas: el `WHERE ... AND TenantId` impide el cruce **entre** tenants, y el chequeo de propiedad impide el cruce **entre estudiantes** dentro de un tenant.

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API1 · Broken Object Level Authorization** que agrupa: **IClaimContext (tenant)**, **GetByIdForTenant** y **Ownership check Student**.

## Verificación

- `apps/Payment/Test/` y `apps/Attendance/Test/` cubren los servicios de consulta. El patrón de mock de la extensión `IsStudentAccessingOtherStudent` (que no se puede `Mock.Setup` por ser método de extensión) está documentado en `apps/Attendance/Test/Services/Concrete/Attendance/ScheduledClassServiceTests.cs`: se configuran `Role` y `UserId` del mock de `IClaimContext`.
- `apps/CourseManagement/Test/` valida `GetCourseByIdHandler` (Found/NotFound).
- Verificación manual con Bruno: pedir un `id` de un recurso de otro tenant debe devolver 404; pedir como `Student` los datos de otro estudiante, 403.

## Notas / brechas conocidas

- El cruce entre tenants se reporta como **404 NotFound**, no 403, por diseño (no confirmar la existencia de objetos ajenos). Es la elección segura; tenerlo presente al depurar.
- El filtro de tenant es responsabilidad de cada DAO. Una consulta nueva que omita `AND TenantId = @tenantId` rompería BOLA silenciosamente; la convención es que todo método de lectura/escritura sobre objetos con dueño lleve sufijo `*ForTenant` y reciba el tenant del `IClaimContext`. No hay un filtro global transversal que lo garantice automáticamente — es disciplina por DAO.
- La colisión histórica «param `tenantId` camelCase vs columna `TenantId`» en stored procedures (que volvía el `WHERE` siempre verdadero) está resuelta calificando con alias de tabla; vigilar al añadir SPs nuevos.
