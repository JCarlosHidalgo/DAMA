# API1 · Autorización a nivel de objeto rota (Broken Object Level Authorization, API Security Top 10 2023)

> **Estado:** ✅ — Todo acceso a un objeto se filtra por el `tenantId` del token dentro del `WHERE` del DAO, y los recursos por-estudiante suman un chequeo de propiedad explícito.

## Qué exige OWASP

La autorización a nivel de objeto rota (antes IDOR) es el riesgo número uno de la lista de APIs: el atacante manipula el `id` de un objeto en la ruta o el cuerpo para leer u operar recursos que no le pertenecen. OWASP exige validar, **en cada acceso a cada objeto**, que el usuario autenticado tiene derecho sobre *ese* objeto concreto — no basta con que esté autenticado.

## Cómo lo cumple DAMA

### El `tenantId` viene del token, nunca del cliente

El identificador de academia no se acepta como parámetro de la petición: se lee del claim firmado vía `IClaimContext`. El cliente no puede falsearlo sin romper la firma RS256.

`apps/Auth/Backend/Claims/IClaimContext.cs:5` expone `Guid TenantId { get; }`; la implementación lo lee del `HttpContext.User` (`apps/Auth/Backend/Claims/ClaimContext.cs:24`) y lanza `MissingClaimException` si falta o es inválido (`:62`). Así, el filtro de academia siempre parte de un valor confiable.

### El filtro por academia vive en el `WHERE` del DAO

La consulta por id siempre exige `id` **y** `tenantId` juntos en SQL. Un objeto de otra academia simplemente no aparece (devuelve `null` → 404), nunca 403 que confirme su existencia.

El método `GetByIdForTenantAsync` del DAO de pagos QR exitosos (`apps/Payment/Backend/DB/Daos/Concrete/Single/QrPayments/SuccessQrPaymentDao.cs:151`) construye un `SELECT ... FROM SuccessQrPayment WHERE Id = @paymentId AND TenantId = @tenantId`, parametrizando ambos valores (id de pago e id de academia) como parámetros de consulta, sin concatenarlos en el texto SQL.

El servicio invoca el DAO pasando la academia del claim, no de la petición. En `GetDebtStatusAsync` (`apps/Payment/Backend/Services/Concrete/QrPayments/QrPaymentQueryService.cs:54`) el `tenantId` se toma de `IClaimContext.TenantId` y se pasa tanto al DAO de pagos pendientes como al de pagos exitosos; si ninguno devuelve resultado, retorna `NotFound`.

El mismo patrón aparece en `apps/Payment/Backend/Services/Concrete/Subscriptions/SubscriptionQueryService.cs:37` (`GetByIdForTenantAsync(tenantId, paymentId)` para pagos de suscripción) y en CourseManagement: en `GetCourseById` (`apps/CourseManagement/Backend/Application/Courses/GetCourseById.cs:29`) la consulta del curso pasa `_claimContext.TenantId` junto al id pedido y retorna `NotFound` cuando el curso es nulo.

### Chequeo de propiedad para recursos por-estudiante

Dentro de una academia, un `Student` no puede leer los datos de asistencia de otro estudiante. El chequeo combina rol e identidad del token en una extensión sobre `IClaimContext`.

El método de extensión `IsStudentAccessingOtherStudent` (`apps/Attendance/Backend/Services/Concrete/ClaimContextExtensions.cs:8`) devuelve `false` cuando el rol no es `Student` (es decir, no bloquea a otros roles) y, cuando sí lo es, devuelve `true` solo si el `UserId` del token difiere del estudiante solicitado.

El servicio lo usa para cortar con un outcome `Forbidden` antes de tocar la base de datos: en `ScheduledClassService` (`apps/Attendance/Backend/Services/Concrete/Attendance/ScheduledClassService.cs:44`), si la extensión detecta a un estudiante pidiendo datos de otro, retorna `Forbidden` sin consultar.

Se aplica también en `apps/Attendance/Backend/Services/Concrete/Attendance/UniqueClassService.cs:42` y `apps/Attendance/Backend/Services/Concrete/Remain/RemainClassReader.cs:21`. Un `Client`/`Teacher` (rol distinto de `Student`) pasa el chequeo y puede consultar a cualquier estudiante de su propia academia — el filtro de academia del DAO sigue acotando el alcance.

## Flujo de los componentes

```
petición con id en la ruta (p.ej. GET /api/payment/qr/{id}/status)
   │
   ▼  IClaimContext.TenantId  ── claim firmado, no parámetro del cliente
   │
   ▼  servicio: GetByIdForTenantAsync(tenantId, id)
   │
   ▼  DAO: SELECT ... WHERE Id = @id AND TenantId = @tenantId
   │       → objeto de otra academia ⇒ null ⇒ NotFound (404), no se filtra existencia
   │
   ▼  (recursos por-estudiante) IsStudentAccessingOtherStudent(studentId)
           → Student pidiendo a otro Student ⇒ Forbidden (403)
```

La defensa es en dos capas: el `WHERE ... AND TenantId` impide el cruce **entre** academias, y el chequeo de propiedad impide el cruce **entre estudiantes** dentro de una academia.

En el diagrama FossFlow `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API1 · Broken Object Level Authorization** que agrupa: **IClaimContext (tenant)**, **GetByIdForTenant** y **Ownership check Student**.

## Verificación

- `apps/Payment/Test/` y `apps/Attendance/Test/` cubren los servicios de consulta. El patrón de mock de la extensión `IsStudentAccessingOtherStudent` (que no se puede `Mock.Setup` por ser método de extensión) está documentado en `apps/Attendance/Test/Services/Concrete/Attendance/ScheduledClassServiceTests.cs`: se configuran `Role` y `UserId` del mock de `IClaimContext`.
- `apps/CourseManagement/Test/` valida `GetCourseByIdHandler` (Found/NotFound).
- Verificación manual con Bruno: pedir un `id` de un recurso de otra academia debe devolver 404; pedir como `Student` los datos de otro estudiante, 403.

## Notas y brechas conocidas

- El cruce entre academias se reporta como **404 NotFound**, no 403, por diseño (no confirmar la existencia de objetos ajenos). Es la elección segura; tenerlo presente al depurar.
- El filtro de academia es responsabilidad de cada DAO. Una consulta nueva que omita `AND TenantId = @tenantId` rompería la autorización a nivel de objeto silenciosamente; la convención es que todo método de lectura/escritura sobre objetos con dueño lleve sufijo `*ForTenant` y reciba la academia del `IClaimContext`. No hay un filtro global transversal que lo garantice automáticamente — es disciplina por DAO.
- La colisión histórica «param `tenantId` camelCase vs columna `TenantId`» en stored procedures (que volvía el `WHERE` siempre verdadero) está resuelta calificando con alias de tabla; vigilar al añadir SPs nuevos.
