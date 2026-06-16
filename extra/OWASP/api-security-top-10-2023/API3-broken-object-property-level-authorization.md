# API3 · Broken Object Property Level Authorization (API Security Top 10 2023)

> **Estado:** ✅ — Los DTOs están segregados por interfaz, los campos sensibles (rol, ids, timestamps) los fija el servidor en builders y nunca el cuerpo, y todo input pasa por un filtro de validación FluentValidation.

## Qué exige OWASP

BOPLA combina *mass assignment* (el cliente escribe propiedades que no debería, p. ej. `role`, `tenantId`, `isAdmin`) y *excessive data exposure* (la respuesta filtra propiedades que el usuario no debería ver). OWASP exige autorizar el acceso a nivel de **propiedad**: validar exactamente qué campos puede leer y escribir cada consumidor, no aceptar/devolver objetos completos a ciegas.

## Cómo lo cumple DAMA

### El cuerpo de la petición no puede fijar campos sensibles (sin mass-assignment)

El DTO de entrada expone **sólo** lo que el cliente puede escribir. El rol, el id y el hash de contraseña los pone el servidor en el builder, no el cuerpo.

El DTO de registro lleva únicamente credenciales — no hay propiedad `Role` ni `Id` que enviar. `apps/Auth/Backend/Dtos/Users/Input/RegisterCredentialsDto.cs:3`:

```csharp
public class RegisterCredentialsDto : ICredentialsPayload
{
    public required string Username { get; set; } = string.Empty;
    public required string Password { get; set; } = string.Empty;
}
```

El **rol lo decide el endpoint**, pasándolo como parámetro al servicio, no leyéndolo del cuerpo. `apps/Auth/Backend/Controllers/AuthController.cs:64`:

```csharp
RegisterUserOutcome outcome = await _userRegistrationService.RegisterAsync(request, UserRole.Teacher);
```

Y el builder fija explícitamente cada campo de la entidad — `Id` generado en servidor, `PasswordHash` calculado, `Role` desde el parámetro, no desde el DTO. `apps/Auth/Backend/Builders/UserEntityBuilder.cs:18`:

```csharp
public User BuildUser(ICredentialsPayload request, UserRole role)
{
    User user = new User();
    string hashedPassword = _passwordHasher.HashPassword(user, request.Password);
    user.Id = Guid.NewGuid();
    user.UserName = request.Username;
    user.PasswordHash = hashedPassword;
    user.Role = role.Value;
    return user;
}
```

El builder recibe la **interfaz** `ICredentialsPayload` (`apps/Auth/Backend/Dtos/Users/Input/ICredentialsPayload.cs:3`), que sólo expone `Username` y `Password` — aun si el DTO concreto creciera con más propiedades, el builder no puede leerlas.

### DTOs segregados por interfaz (ISP): cada consumidor ve sólo su slice

El patrón es transversal. Las interfaces `I*Payload`/`I*Data` acotan el subconjunto escribible que comparten varios DTOs, de modo que un builder o handler depende sólo de lo que realmente lee:

- `apps/Auth/Backend/Dtos/Users/Input/ICredentialsPayload.cs` (Username/Password) sobre Register y Login.
- `apps/CourseManagement/Backend/Dtos/Courses/ICourseData.cs` y `apps/CourseManagement/Backend/Dtos/Scheduleds/IScheduledClassPayload.cs` sobre los DTOs Create/Update/Get.
- `apps/Payment/Backend/Dtos/DebtTemplates/IDebtTemplateData.cs` sobre los DTOs de plantilla.

ASP.NET no liga interfaces desde `[FromBody]`, así que el controlador mantiene el tipo concreto en la firma y la interfaz vive por debajo (servicio/builder). El patrón completo está descrito en `apps/CLAUDE.md` (pattern #13, "DTOs son interface-segregated, no god-shaped").

### Validación de entrada como única puerta

Todo argumento de acción con un `IValidator<>` registrado se valida antes de ejecutar la acción; el primer fallo corta con 400. Los controladores no inyectan validadores ni llaman `ValidateAsync`.

`apps/Auth/Backend/Filters/FluentValidationActionFilter.cs:22`

```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
{
    ...
    foreach (object? argument in actionContext.ActionArguments.Values)
    {
        ...
        Type validatorInterfaceType = typeof(IValidator<>).MakeGenericType(argument.GetType());
        IValidator? validator = (IValidator?)_serviceProvider.GetService(validatorInterfaceType);
        if (validator is null) { continue; }
        ...
        if (!validationResult.IsValid)
        {
            actionContext.Result = new BadRequestObjectResult(validationResult.Errors[0].ErrorMessage);
            return;
        }
    }
    await next();
}
```

El validador concreto restringe forma y contenido de cada propiedad escribible. `apps/Auth/Backend/Validators/Users/RegisterCredentialsDtoValidator.cs:17` acota longitud y regex de `Username`/`Password`:

```csharp
RuleFor(x => x.Username)
    .MinimumLength(MinUsernameLength).WithMessage(InvalidUsernameMessage)
    .MaximumLength(MaxUsernameLength).WithMessage(InvalidUsernameMessage)
    .Matches(UsernameRegex).WithMessage(InvalidUsernameMessage);
```

Cada split de DTO trae su propio validador concreto (Register vs Login tienen `RegisterCredentialsDtoValidator` y `LoginCredentialsDtoValidator`), de modo que cada flujo valida sólo sus propiedades.

## Flujo de los componentes

```
request body (JSON)
   │
   ▼  [FromBody] → DTO concreto (sólo propiedades escribibles; sin Role/Id/Hash)
   │
   ▼  FluentValidationActionFilter  ── resuelve IValidator<DTO>, valida, 400 al primer fallo
   │
   ▼  controlador pasa el DTO al servicio; el ROL/identidad los fija el endpoint, no el cuerpo
   │
   ▼  Builder (ICredentialsPayload)  ── fija Id = Guid.NewGuid(), Role = parámetro, Hash = calculado
   │       el builder lee la INTERFAZ → no puede asignar propiedades fuera del slice
   │
   ▼  entidad persistida con exactamente los campos que el servidor autoriza
```

La defensa contra mass-assignment es estructural: el cliente no tiene dónde poner un campo privilegiado (el DTO no lo expone) y el builder lo fija él mismo. La defensa contra exposición excesiva es la separación request/response DTO + AutoMapper a proyecciones de lectura controladas.

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API3 · Broken Object Property Level Authorization** que agrupa: **DTOs ISP (req/resp)**, **Builders (sin mass-assign)** y **FluentValidation** (con **Response validation** como nodo de apoyo del lado de la salida).

## Verificación

- `apps/Auth/Test/` cubre `RegisterCredentialsDtoValidator`/`LoginCredentialsDtoValidator` y el flujo de registro (el rol queda determinado por el endpoint, no por el cuerpo).
- `dotnet test` en cada `apps/<Svc>/Test/` valida que los handlers/builders construyen entidades con los campos esperados.
- Manual con Bruno: enviar en el cuerpo de `POST /api/auth/register/student` una propiedad extra `"role": "Admin"` no tiene efecto — el DTO no la liga y el builder fija `Role` desde `UserRole.Student`.

## Notas / brechas conocidas

- La protección contra mass-assignment depende de que los DTOs de entrada no expongan campos privilegiados y de que los builders fijen esos campos. Un DTO nuevo que añadiera por descuido una propiedad sensible escribible **sí** sería ligado por `[FromBody]`; la disciplina es: campos que decide el servidor → parámetro del endpoint + builder, nunca propiedad del DTO de entrada (pattern #13 y #10 en `apps/CLAUDE.md`).
- No hay un mecanismo de "ignore-list" de propiedades a nivel de framework; la seguridad es por construcción (DTO mínimo + builder explícito), no por configuración de binder.
- La división request/response evita exposición excesiva, pero es responsabilidad del Profile de AutoMapper no proyectar campos internos; revisar los Profiles al añadir DTOs de salida.
