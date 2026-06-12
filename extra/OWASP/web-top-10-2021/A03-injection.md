# A03:2021 · Injection (Web Application Top 10 2021)

> **Estado:** ✅ — Todo acceso a datos pasa por *stored procedures* parametrizados o queries con parámetros nombrados; la entrada se valida automáticamente con un filtro MVC global. No existe SQL dinámico, concatenación ni ORM con interpolación de cadenas.

## Qué exige OWASP

Una inyección ocurre cuando datos no confiables del usuario se interpretan como código o comandos (SQL, NoSQL, OS, LDAP…). OWASP exige separar datos de instrucciones: usar APIs parametrizadas (consultas preparadas / *stored procedures* sin SQL dinámico), validar la entrada en el servidor con una *allowlist*, y escapar cualquier residuo. El control clave es que el atacante nunca pueda alterar la estructura de la consulta.

## Cómo lo cumple DAMA

### 1. DAOs parametrizados — nunca concatenan entrada del usuario

Cada acceso a MySQL se hace con `MySqlCommand` y `Parameters.AddWithValue`; los valores viajan como parámetros enlazados, no como texto interpolado en la sentencia. El único `INSERT` literal del `UserDao` usa placeholders `@…`:

`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:64`
```csharp
const string sql = "INSERT INTO User (Id, UserName, PasswordHash, Role) " +
                   "VALUES (@Id, @UserName, @PasswordHash, @Role);";
MySqlCommand com = new MySqlCommand(sql, _connection, sqlTransaction);
com.Parameters.AddWithValue("@Id", user.Id);
com.Parameters.AddWithValue("@UserName", user.UserName);
```

No hay `string.Format`, interpolación `$"..."` ni `+` con datos de petición dentro de ninguna sentencia SQL: el texto SQL es siempre una constante y los valores siempre parámetros.

### 2. Stored procedures con `CommandType.StoredProcedure`

Las lecturas/escrituras de negocio invocan *stored procedures* por nombre, pasando los argumentos como parámetros de entrada. El nombre del SP es una constante del código (jamás se arma con datos del usuario):

`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:249`
```csharp
MySqlCommand command = new MySqlCommand("RegisterFailedLoginAttempt", _connection)
{
    CommandType = CommandType.StoredProcedure
};
command.Parameters.AddWithValue("@userId", userId.ToString());
command.Parameters["@userId"].Direction = ParameterDirection.Input;
```

### 3. Los SP no construyen SQL dinámico

Los procedimientos en `infrastructure/environments/<svc>/init.sql` son SQL estático: sin `PREPARE`/`EXECUTE`, sin `CONCAT` para armar consultas, sin `EXEC`. La estructura de la consulta es fija y solo los valores son variables. Ejemplo:

`infrastructure/environments/auth/init.sql:119`
```sql
CREATE PROCEDURE GetUsersByRoleForTenantPaged(
    IN tenantId VARCHAR(36),
    IN userRole VARCHAR(50),
    IN pageOffset INT,
    IN pageSize INT
)
BEGIN
    SELECT u.Id, u.UserName
    FROM User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    WHERE td.TenantId = tenantId
        AND u.Role = userRole
        AND u.IsDeleted = 0
    ORDER BY u.UserName ASC, u.Id ASC
    LIMIT pageSize OFFSET pageOffset;
END //
```

**Convención anti-colisión (relevante para inyección lógica):** los parámetros `camelCase` (`tenantId`) colisionarían con columnas `PascalCase` (`TenantId`) — `WHERE TenantId = tenantId` sería siempre verdadero y filtraría de más. Por eso todo `WHERE` califica la columna con el **alias de tabla**: `td.TenantId = tenantId` (línea 133). Esto evita que el filtro de tenant se neutralice silenciosamente — un fallo de aislamiento, no de inyección clásica, pero la misma disciplina de no mezclar identificadores.

### 4. Validación de entrada automática (FluentValidation, filtro global)

Antes de llegar al DAO, todo DTO de entrada se valida en un único punto: un `IAsyncActionFilter` MVC registrado globalmente resuelve el `IValidator<>` de cada argumento de la acción, ejecuta `ValidateAsync` y cortocircuita con un 400 ante el primer fallo. Los controladores **no** inyectan validadores ni llaman `ValidateAsync`.

`apps/Auth/Backend/Filters/FluentValidationActionFilter.cs:40`
```csharp
IValidationContext validationContext = BuildValidationContext(argument, ruleSetNames);
ValidationResult validationResult = await validator.ValidateAsync(validationContext);
if (!validationResult.IsValid)
{
    actionContext.Result = new BadRequestObjectResult(validationResult.Errors[0].ErrorMessage);
    return;
}
```

Registro global del filtro en `apps/Auth/Backend/Modules/MvcModule.cs` (`AddControllers(options => options.Filters.Add<FluentValidationActionFilter>())`); los validadores concretos viven en `apps/*/Backend/Validators/` y se descubren con `AddValidatorsFromAssemblyContaining<Program>()`. Es una *allowlist* por DTO: longitudes, formatos y campos permitidos se declaran por tipo.

### 5. Sin ORM con concatenación

No hay Entity Framework ni un ORM que traduzca expresiones a SQL interpolado. El acceso a datos se apoya en las clases base `MySQL*Dao` del paquete `JuanCarlosHS.SQLDaosPackage` (consumido por NuGet), que solo exponen el patrón `MySqlCommand` + parámetros. No existe superficie para inyección por *string building*.

## Flujo de los componentes

Rectángulo **A03 Injection** del diagrama FossFLOW `extra/fossflow/diagrams/owasp-web-top-10.json` (nodos `FluentValidation filter` → `DAO parametrizado` → `Stored Procedures`):

```
Petición HTTP (DTO)
   │
   ▼
[FluentValidation filter]  ── inválido ──▶ 400 BadRequest (primer error)
   │ válido
   ▼
Controller → Service/Handler → [DAO parametrizado]
   │  MySqlCommand + Parameters.AddWithValue (@param)
   ▼
[Stored Procedures]  init.sql, SQL estático, sin PREPARE/CONCAT
   │  alias de tabla en WHERE (td.TenantId = tenantId)
   ▼
MySQL
```

1. El DTO entra por el controlador; el **FluentValidation filter** lo valida (allowlist por tipo) antes de cualquier lógica.
2. El servicio/handler delega en el **DAO parametrizado**, que enlaza cada valor como `@param` — nunca concatena.
3. El DAO invoca un **stored procedure** (o un `INSERT` con placeholders) cuyo texto es constante; los SP de `init.sql` no generan SQL dinámico y califican columnas con alias de tabla.

## Verificación

- Buscar concatenación/interpolación en SQL (debe dar vacío de casos reales):
  ```bash
  grep -rnE '"\s*\+\s*\w|SELECT.*\$\{|FORMAT\(|CONCAT\(|PREPARE |EXECUTE ' \
    apps/*/Backend/DB infrastructure/environments/*/init.sql
  ```
- Confirmar que todo DAO usa parámetros:
  ```bash
  grep -rln "Parameters.AddWithValue" apps/*/Backend/DB/Daos/Concrete | sort
  ```
- Confirmar el filtro de validación registrado por servicio:
  ```bash
  grep -rn "FluentValidationActionFilter" apps/*/Backend/Modules/MvcModule.cs
  ```
- Tests: las suites de Auth/Attendance/CourseManagement/Payment ejercitan los validadores y los DAOs contra `mysql:9` (Testcontainers).

## Notas / brechas conocidas

- **Credentials no tiene DTOs de entrada ni DB**, por lo que su `MvcModule` no registra el filtro de validación y no tiene DAOs — no es una brecha, es ausencia de superficie (servicio *stateless* de solo-claims).
- La protección depende de que los SP nuevos sigan siendo SQL estático. **Regla a mantener:** toda consulta sobre `User` (y agregados con tenant) se expresa como un SP nuevo con sufijo `*ForTenant` y alias de tabla en el `WHERE`, nunca como SQL ad-hoc en el DAO (patrón #8 de `apps/CLAUDE.md`).
- `AddWithValue` infiere el tipo del parámetro; para columnas donde el tipo importe (p. ej. comparaciones de fecha de alto volumen) podría preferirse `Add` con `MySqlDbType` explícito. No es un riesgo de inyección, sí una optimización pendiente.
