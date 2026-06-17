# 3.3.3.7 Diagramado del servicio Auth

> El servicio Auth es el **emisor exclusivo de tokens JWT** de la plataforma DAMA y el único que
> gestiona usuarios, academias (tenants) y roles en la base de datos. Expone una API REST para
> autenticación y directorio de usuarios, publica el evento `student.registered` hacia RabbitMQ
> mediante el patrón outbox, y sirve el **servidor gRPC `TenantSubscription`** que el servicio
> Payment consume para actualizar el nivel de suscripción de una academia. Todos los grafos citados
> los genera **Doxygen** desde el código (`UML_LOOK`, `GRAPHICAL_HIERARCHY`, `COLLABORATION_GRAPH`);
> aquí solo se titulan las figuras y se explican las **relaciones** que muestran, sin describir
> métodos.
>
> **Generar las figuras:** `cd extra/graphics && docker compose --profile docs run --rm doxygen`
> (salida en `extra/graphics/out/doxygen/html/`).

---

## a) Jerarquía gráfica

Auth es el servicio más completo del monorepo: combina autenticación con firma RSA, directorio
multitenant de usuarios, producción de eventos de dominio, un servidor gRPC y mantenimiento de
suscripciones mediante trabajos en segundo plano. Su código se organiza por **namespaces** en los
que cada carpeta corresponde a un rol estructural bien definido:

- `Backend.Claims` — **abstracción de claims**: lectura tipada de los datos de identidad y academia
  del token.
- `Backend.Dtos` — **datos de entrada y salida**: contratos de la API agrupados por agregado (Users,
  Tenants) y por dirección (Input/Output), con interfaces ISP sobre los DTOs compartidos.
- `Backend.Entities` — **modelo persistente**: las seis entidades que se mapean a la base de datos
  de Auth.
- `Backend.Events` — **eventos de dominio**: el contrato genérico de evento y el POCO del evento
  `student.registered`.
- `Backend.Results` — **uniones discriminadas de resultado**: los tipos cerrados que los servicios
  devuelven a los controladores en lugar de excepciones o valores nulos.
- `Backend.Transporters.Entities` — **transportadores internos**: registros de solo lectura que
  combinan varias entidades en una sola proyección, usados entre capa de datos y capa de servicio.
- `Backend.DB.Daos.Abstract` — **interfaces ISP de acceso a datos**: contratos estrechos por
  consumidor sobre las tablas de usuario, academia, token y outbox.
- `Backend.DB.Daos.Concrete` — **implementaciones de acceso a datos**: los objetos de acceso a datos
  concretos que heredan de las clases base del paquete externo `SQLDaosPackage`.
- `Backend.DB.Injectors` — **inyectores de semilla**: cargan los datos de prueba desde archivos CSV
  durante el arranque controlado por la variable de entorno `SEED_DB`.
- `Backend.DB.Utils` — **utilidades de conexión y sembrado**: el conector de cadena de conexión y el
  coordinador de truncado e inyección.
- `Backend.Pagination` — **paginación**: el calculador de páginas y el DTO de consulta paginada.
- `Backend.Builders` — **constructores de entidades y vistas**: interfaces ISP y sus implementaciones
  que centralizan la fabricación de entidades, DTOs de salida y eventos de outbox.
- `Backend.Services.Abstract` — **contratos de servicio**: las interfaces estrechas de los servicios
  de negocio y de la infraestructura de mensajería y firma JWT.
- `Backend.Services.Concrete` — **implementaciones de servicio**: la lógica de negocio de
  autenticación, refresco de token, directorio de usuarios, registro y actualización de suscripción.
- `Backend.Security` — **seguridad y JWT**: los generadores de tokens de acceso y de refresco, el
  firmante RSA, las constantes de claims y roles, y el filtro de autorización por nivel de
  suscripción.
- `Backend.Grpc.Services` — **servidor gRPC**: la implementación del servicio `TenantSubscription`
  que recibe llamadas autenticadas de Payment.
- `Backend.Filters` — **filtros de MVC**: el filtro de validación automática de FluentValidation y
  el atributo de selección de conjunto de reglas.
- `Backend.Validators` — **validadores de entrada**: una clase por cada DTO de entrada, registradas
  automáticamente por FluentValidation.
- `Backend.ExternalCheck` — **sondas de disponibilidad**: las comprobaciones de base de datos y
  RabbitMQ expuestas en `/health/ready`, junto con el enumerado de dependencias, el convenio de
  nomenclatura y el escritor de respuesta de disponibilidad.
- `Backend.Messaging` — **publicador de RabbitMQ**: el publicador singleton que entrega los eventos
  del outbox al intercambio de tópicos `dama.events`.
- `Backend.Workers` — **trabajos en segundo plano**: el publicador del outbox, el limpiador de
  eventos publicados y el limpiador de suscripciones vencidas.
- `Backend.Options` — **opciones tipadas**: los objetos de configuración enlazados desde variables
  de entorno en el arranque.
- `Backend.Logging` — **eventos de log estructurado**: la clase estática parcial de mensajes de log
  compilados en tiempo de compilación.
- `Backend.Modules` — **composición por módulos**: los contratos `IServiceModule` e `IAppModule`,
  el anfitrión `ModuleHost` y todos los módulos concretos que registran y configuran el servicio.

**No aplican** los siguientes grupos que sí existen en otros servicios:

- No hay **CQRS-lite** (manejadores de comandos y consultas): Auth usa el patrón `Services/`
  convencional, no el patrón `Application/` de CourseManagement.
- No hay **clientes gRPC salientes** (`Grpc/Interceptors/`, `Modules/GrpcClientsModule`): Auth es
  servidor gRPC, no cliente.
- No hay **consumidores de eventos de RabbitMQ** (`processed_events`, `BackgroundService` de
  consumo): Auth solo produce el evento `student.registered`; no consume eventos de otros servicios.

A continuación, un título de figura por grupo estructural y la función del grupo:

> **Figura: Jerarquía gráfica de la abstracción de claims (`IClaimContext`, `ClaimContext`, `MissingClaimException`)**

Expone al resto del servicio los datos de identidad y academia del token de forma tipada y con fallo
rápido: las ocho propiedades lazy incluyen, además de las seis base comunes a los cinco servicios
(`TenantId`, `TenantName`, `TenantTimezone`, `UserId`, `UserName`, `Role`), dos exclusivas de Auth
(`IndexCoreServicesPyramid`, `SubscriptionExpiresAt`) necesarias para el filtro `RequiresServiceTierAttribute`.

> **Figura: Jerarquía gráfica de los datos de entrada de usuarios (`ICredentialsPayload`, `LoginCredentialsDto`, `RegisterCredentialsDto`, `RefreshTokenRequestDto`, `UpdateUsernameDto`, `UserSearchQueryDto`)**

Define los contratos de la API para las operaciones de usuario: la interfaz compartida
`ICredentialsPayload` abstrae el subconjunto `Username`/`Password` que `LoginCredentialsDto` y
`RegisterCredentialsDto` tienen en común, siguiendo el principio de segregación de interfaces; los
demás DTOs son tipos independientes para sus flujos específicos.

> **Figura: Jerarquía gráfica de los datos de entrada de academias (`CreateTenantDto`, `UpdateTenantNameDto`, `UpdateTenantTimezoneDto`)**

Define los contratos de la API para las operaciones de gestión de academias; cada DTO es un tipo
propio porque corresponde a un flujo con su propio validador.

> **Figura: Jerarquía gráfica de los datos de salida (`TokenResponseDto`, `UserListItemDto`, `PagedUsersResponseDto`, `TenantDto`, `TenantTierCountDto`)**

Define la forma de los objetos que la API devuelve al cliente: credenciales de sesión, listas
paginadas de usuarios, proyecciones de academia y distribución de niveles de suscripción.

> **Figura: Jerarquía gráfica del DTO de paginación (`PaginationQueryDto`)**

DTO de consulta de entrada que transporta el índice de página solicitado; su posición bajo
`Backend.Pagination` lo separa de los DTOs de dominio.

> **Figura: Jerarquía gráfica del modelo persistente (`User`, `UserRole`, `Tenant`, `TenantDomain`, `TenantAllowedServices`, `RefreshToken`, `OutboxEvent`)**

Las siete clases que representan las tablas de la base de datos de Auth: usuarios con bloqueo de
cuenta, el tipo de valor que encapsula los roles disponibles, la academia, la tabla de unión
muchos-a-muchos usuario-academia, el estado de suscripción de la academia, los tokens de refresco
rotantes y la tabla de outbox de eventos publicados a RabbitMQ.

> **Figura: Jerarquía gráfica de los eventos de dominio (`IDomainEvent`, `StudentRegisteredEvent`, `StudentRegisteredEventData`)**

Define el contrato de evento de dominio y su única instancia concreta: el evento que Auth emite
cuando un estudiante se registra, con su carga de datos anidada como registro separado.

> **Figura: Jerarquía gráfica de los resultados discriminados de usuarios (`LoginOutcome`, `RegisterUserOutcome`, `DeleteUserOutcome`, `RenameUserOutcome`)**

Tipos cerrados que los servicios de usuario devuelven en lugar de excepciones o nulos: cada uno es
un registro abstracto con casos sellados anidados que el controlador empareja exhaustivamente.

> **Figura: Jerarquía gráfica de los resultados discriminados de academias (`UpdateTenantNameOutcome`, `UpdateTenantTimezoneOutcome`)**

Tipos cerrados análogos a los anteriores, para las operaciones de renombrado y cambio de zona
horaria de academias.

> **Figura: Jerarquía gráfica de los transportadores internos (`UserWithTenant`, `IssuedRefreshToken`, `RefreshTokenWithOwner`)**

Registros inmutables que agrupan varias entidades en una sola proyección para transferir datos
entre la capa de acceso a datos y la capa de servicio sin exponer el `MySqlConnection` ni los
tipos de NuGet fuera de la capa de datos.

> **Figura: Jerarquía gráfica de las interfaces ISP de acceso a datos de usuarios (`IUserAuthenticationDao`, `IUserRegistrationDao`, `IUserDirectoryDao`)**

Tres interfaces estrechas por consumidor sobre la misma tabla `User`: el servicio de autenticación
solo ve las operaciones de verificación de credenciales y bloqueo de cuenta; el de registro solo ve
la creación transaccional; el de directorio solo ve las lecturas y escrituras paginadas por tenant.

> **Figura: Jerarquía gráfica de las interfaces ISP de acceso a datos de academias y tokens (`ITenantDao`, `ITenantAllowedServicesDao`, `ITenantDomainDao`, `IRefreshTokenReadDao`, `IRefreshTokenWriteDao`, `IOutboxEventDao`)**

Contratos de acceso a datos para las demás tablas: la academia con sus operaciones de listado y
actualización, los servicios permitidos con su operación de restablecimiento masivo, la tabla de
unión usuario-academia, la lectura y escritura segregadas del token de refresco, y el outbox de
eventos.

> **Figura: Jerarquía gráfica de los objetos de acceso a datos concretos (`UserDao`, `TenantDao`, `TenantAllowedServicesDao`, `TenantDomainDao`, `RefreshTokenDao`, `OutboxEventDao`)**

Las implementaciones concretas que heredan de las clases base del paquete `SQLDaosPackage`
(`MySQLSingleDao<T>`, `MySQLTwoForeignDao<T>`) e implementan las interfaces ISP correspondientes;
`OutboxEventDao` no hereda de una clase base del paquete sino que lo usa como utilidad estática.

> **Figura: Jerarquía gráfica de los inyectores de semilla (`TenantDataInjector`, `TenantAllowedServicesDataInjector`, `UserDataInjector`, `TenantDomainDataInjector`)**

Familia de clases que heredan de `DataInjector` (del paquete externo `SQLDaosPackage`) y encapsulan
cada uno el comando `LOAD DATA INFILE` para una tabla; los orquesta `DBInjector` durante el arranque
de siembra.

> **Figura: Jerarquía gráfica de las utilidades de base de datos (`DBConnector`, `DBInjector`)**

Clases estáticas de infraestructura de datos: `DBConnector` resuelve la cadena de conexión
(entorno primero, luego `dbsettings.json`) y `DBInjector` coordina el truncado de tablas y la
inyección de datos de semilla.

> **Figura: Jerarquía gráfica de la utilidad de paginación (`PageCalculator`)**

Clase estática que calcula el índice de página efectivo, el máximo y el desplazamiento SQL a partir
del total de registros, el índice solicitado y el tamaño de página.

> **Figura: Jerarquía gráfica de los constructores (`IUserEntityBuilder`, `UserEntityBuilder`, `IUserViewBuilder`, `UserViewBuilder`, `ITenantBuilder`, `TenantBuilder`, `IStudentRegisteredEventBuilder`, `StudentRegisteredEventBuilder`)**

Cuatro pares interfaz-implementación que centralizan la fabricación de objetos con efectos
colaterales (generación de identificadores, cifrado de contraseña, serialización de payload):
el constructor de entidades de usuario (inyecta `IPasswordHasher<User>`, consume `ICredentialsPayload`),
el constructor de vistas de usuario (sin dependencias, produce DTOs de listado y paginado), el
constructor de academias (produce `Tenant` y `TenantDto`), y el constructor de eventos del outbox
(serializa `StudentRegisteredEvent` como JSON dentro de `OutboxEvent`).

> **Figura: Jerarquía gráfica de los contratos de servicio (`IAuthenticationService`, `IRefreshService`, `IUserRegistrationService`, `IUserDirectoryService`, `ITenantService`, `ITenantSubscriptionUpdater`, `IEventPublisher`, `IJwtTokenSigner`)**

Contratos de la capa de servicio y de la infraestructura de mensajería y seguridad: los cuatro
contratos de casos de uso de usuario, el de gestión de academias, el de actualización de
suscripción (invocado desde el servidor gRPC), el publicador de eventos hacia RabbitMQ y el
firmante RSA de tokens.

> **Figura: Jerarquía gráfica de la capa de servicio (`AuthenticationService`, `RefreshService`, `UserRegistrationService`, `UserDirectoryService`, `TenantService`, `TenantSubscriptionUpdater`)**

Las seis implementaciones concretas de la lógica de negocio: autenticación con bloqueo de cuenta y
actualización progresiva del hash PBKDF2, refresco de token con detección de reutilización,
registro de usuario con inserción de outbox en la misma transacción, directorio multitenant con
borrado lógico, gestión de academias y actualización atómica del nivel de suscripción.

> **Figura: Jerarquía gráfica de los componentes de seguridad JWT (`JwtTokenSigner`, `JwtAccessTokenGenerator`, `RefreshTokenGenerator`, `IAccessTokenGenerator`, `IRefreshTokenGenerator`, `AuthClaims`, `UserRoles`, `RequiresServiceTierAttribute`)**

El núcleo de seguridad exclusivo de Auth: el firmante RSA que importa la clave privada PEM,
el generador de tokens de acceso que construye el JWT con todos los claims incluyendo las
audiencias múltiples, el generador de tokens de refresco aleatorios con hash SHA-256, las
interfaces de sus contratos, las clases estáticas de constantes de claim y rol, y el filtro de
autorización por nivel de suscripción de la pirámide de servicios.

> **Figura: Jerarquía gráfica del servidor gRPC (`TenantSubscriptionGrpcService`)**

La implementación del servicio `TenantSubscription` definido en `grpc-contracts`: autentica
llamadas entrantes de Payment mediante el secreto compartido en la cabecera `x-subscription-secret`
(comparación en tiempo constante) y delega la actualización del nivel de suscripción en
`ITenantSubscriptionUpdater`.

> **Figura: Jerarquía gráfica de los filtros de MVC (`FluentValidationActionFilter`, `RuleSetAttribute`)**

El filtro global que ejecuta automáticamente los validadores FluentValidation antes de cada acción,
y el atributo que permite indicar qué conjunto de reglas aplicar en acciones específicas.

> **Figura: Jerarquía gráfica de los validadores (`PaginationQueryDtoValidator`, `LoginCredentialsDtoValidator`, `RegisterCredentialsDtoValidator`, `RefreshTokenRequestDtoValidator`, `UpdateUsernameDtoValidator`, `UserSearchQueryDtoValidator`, `CreateTenantDtoValidator`, `UpdateTenantNameDtoValidator`, `UpdateTenantTimezoneDtoValidator`)**

Familia de nueve validadores FluentValidation, uno por cada DTO de entrada: todos extienden
`AbstractValidator<T>` del paquete externo FluentValidation y son descubiertos y registrados
automáticamente por `ValidationModule` vía `AddValidatorsFromAssemblyContaining<Program>()`.

> **Figura: Jerarquía gráfica de las comprobaciones de disponibilidad (`DatabaseHealthCheck`, `RabbitMqHealthCheck`, `ExternalDependency`, `ExternalCheckNaming`, `ReadinessResponseWriter`)**

Las dos comprobaciones de dependencias externas (base de datos y RabbitMQ) que implementan la
interfaz externa `IHealthCheck`, el enumerado que nombra las dependencias, el convenio de
nomenclatura de las sondas y el escritor de respuesta JSON para el punto de acceso `/health/ready`.

> **Figura: Jerarquía gráfica del publicador de RabbitMQ (`RabbitMqEventPublisher`)**

El publicador singleton que implementa `IEventPublisher` y `IAsyncDisposable`: gestiona una
conexión perezosa al broker con inicialización protegida por semáforo, declara el intercambio de
tópicos duradero `dama.events` en el primer uso y publica con confirmaciones del publicador
habilitadas.

> **Figura: Jerarquía gráfica de los trabajos en segundo plano (`OutboxPublisher`, `OutboxJanitor`, `SubscriptionExpiryJanitor`)**

Tres trabajos `BackgroundService`: el publicador del outbox que lee lotes de eventos pendientes y
los entrega a RabbitMQ, el limpiador que elimina eventos publicados con más de siete días de
antigüedad, y el limpiador de suscripciones que restablece a nivel cero las academias con
suscripción vencida.

> **Figura: Jerarquía gráfica de las opciones tipadas (`JwtOptions`, `RabbitMqOptions`, `SubscriptionGrpcOptions`)**

Los tres objetos de configuración del servicio enlazados desde variables de entorno o secciones de
`appsettings.json`: `JwtOptions` incluye claves RSA pública y privada, emisor, audiencias y
tiempos de vida de ambos tokens; `RabbitMqOptions` recoge las credenciales del broker; y
`SubscriptionGrpcOptions` contiene el secreto compartido del servidor gRPC.

> **Figura: Jerarquía gráfica del log estructurado (`LogEvents`)**

Clase estática parcial con los mensajes de log compilados por el generador de fuentes de
`LoggerMessage`: centraliza todos los eventos de diagnóstico (conexión RabbitMQ, ciclo de vida del
outbox, autenticación, operaciones de usuario, academias y gRPC) en un único punto de registro sin
cadenas de interpolación.

> **Figura: Jerarquía gráfica de la composición de módulos (`IServiceModule`, `IAppModule`, `ModuleHost`, y los módulos concretos)**

El mismo patrón de arranque modular que Credentials: dos contratos, un anfitrión que los descubre
por reflexión y dieciocho módulos concretos que lo implementan, más `DatabaseSeeder` que se
ejecuta antes del arranque cuando `SEED_DB=true`.

---

## b) Diagramas de herencia y colaboración

Una entrada por cada clase/interfaz **implementada** en Auth. Las clases/interfaces externas (del
framework .NET, ASP.NET Core, NuGet o del paquete `grpc-contracts` generado por Grpc.Tools) se
**referencian** desde las viñetas, sin entrada propia.

### Abstracción de claims

1. **`IClaimContext`** — contrato que define la lectura tipada de los ocho claims del token: los
   seis base comunes a todos los servicios más `IndexCoreServicesPyramid` y `SubscriptionExpiresAt`,
   exclusivos de Auth porque es el emisor que los incluye en el JWT.

   > **Figura: Diagrama de herencia para `IClaimContext`**
   - Es una interfaz **raíz**: no hereda de ninguna otra.

   > **Figura: Diagrama de colaboración para `IClaimContext`**
   - Sus propiedades son de tipos primitivos del lenguaje (`Guid`, `string`, `int`, `DateTime`), por
     lo que el grafo no muestra dependencia con tipos implementados; es un contrato puro.

2. **`ClaimContext`** — implementación que lee cada claim del usuario autenticado con memorización
   en la primera lectura y fallo rápido ante ausencia o formato incorrecto.

   > **Figura: Diagrama de herencia para `ClaimContext`**
   - Implementa la interfaz implementada `IClaimContext`.

   > **Figura: Diagrama de colaboración para `ClaimContext`**
   - Recibe por inyección de dependencias la interfaz externa `IHttpContextAccessor` para acceder
     a los claims del principal autenticado.
   - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim que lee.
   - Construye y lanza la clase implementada `MissingClaimException` cuando un claim falta o no
     puede parsearse al tipo esperado.

3. **`MissingClaimException`** — excepción específica que señala un claim requerido ausente o
   malformado en el token.

   > **Figura: Diagrama de herencia para `MissingClaimException`**
   - Hereda de la clase externa `System.Exception`.

   > **Figura: Diagrama de colaboración para `MissingClaimException`**
   - La construye y lanza la clase implementada `ClaimContext`.
   - La captura la clase implementada `RequiresServiceTierAttribute` para tratar la ausencia del
     claim de nivel de suscripción como nivel cero.

### Datos de entrada — usuarios

4. **`ICredentialsPayload`** — contrato de los campos compartidos por las operaciones de login y
   registro: el nombre de usuario y la contraseña en texto claro.

   > **Figura: Diagrama de herencia para `ICredentialsPayload`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `ICredentialsPayload`**
   - La implementan las clases implementadas `LoginCredentialsDto` y `RegisterCredentialsDto`.
   - La consume la interfaz implementada `IUserEntityBuilder` en su operación de construcción, de
     modo que el constructor solo accede al subconjunto `Username`/`Password` sin ver el tipo
     concreto.

5. **`LoginCredentialsDto`** — DTO de entrada para la operación de inicio de sesión.

   > **Figura: Diagrama de herencia para `LoginCredentialsDto`**
   - Implementa la interfaz implementada `ICredentialsPayload`.

   > **Figura: Diagrama de colaboración para `LoginCredentialsDto`**
   - La recibe el controlador implementado `AuthController` en la acción de login.
   - La valida la clase implementada `LoginCredentialsDtoValidator`.
   - La consume la interfaz implementada `IAuthenticationService`.

6. **`RegisterCredentialsDto`** — DTO de entrada para el registro de profesores y estudiantes.

   > **Figura: Diagrama de herencia para `RegisterCredentialsDto`**
   - Implementa la interfaz implementada `ICredentialsPayload`.

   > **Figura: Diagrama de colaboración para `RegisterCredentialsDto`**
   - La recibe el controlador implementado `AuthController` en las acciones de registro.
   - La valida la clase implementada `RegisterCredentialsDtoValidator`.
   - La consume la interfaz implementada `IUserRegistrationService`, que la delega a
     `IUserEntityBuilder` a través de `ICredentialsPayload`.

7. **`RefreshTokenRequestDto`** — DTO de entrada que transporta el token de refresco opaco.

   > **Figura: Diagrama de herencia para `RefreshTokenRequestDto`**
   - Clase **raíz**: no hereda de ninguna otra ni implementa interfaces.

   > **Figura: Diagrama de colaboración para `RefreshTokenRequestDto`**
   - La recibe el controlador implementado `AuthController` en la acción de refresco.
   - La valida la clase implementada `RefreshTokenRequestDtoValidator`.
   - La consume la interfaz implementada `IRefreshService`.

8. **`UpdateUsernameDto`** — DTO de entrada para renombrar un usuario.

   > **Figura: Diagrama de herencia para `UpdateUsernameDto`**
   - Clase **raíz**.

   > **Figura: Diagrama de colaboración para `UpdateUsernameDto`**
   - La recibe el controlador implementado `AuthController`.
   - La valida la clase implementada `UpdateUsernameDtoValidator`.
   - La consume la interfaz implementada `IUserDirectoryService`.

9. **`UserSearchQueryDto`** — DTO de entrada de consulta para buscar un estudiante por nombre exacto.

   > **Figura: Diagrama de herencia para `UserSearchQueryDto`**
   - Clase **raíz**.

   > **Figura: Diagrama de colaboración para `UserSearchQueryDto`**
   - La recibe el controlador implementado `AuthController` como parámetro de consulta.
   - La valida la clase implementada `UserSearchQueryDtoValidator`.

### Datos de entrada — academias

10. **`CreateTenantDto`** — DTO de entrada para crear una nueva academia.

    > **Figura: Diagrama de herencia para `CreateTenantDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `CreateTenantDto`**
    - La recibe el controlador implementado `TenantController`.
    - La valida la clase implementada `CreateTenantDtoValidator`.
    - La consume la interfaz implementada `ITenantService`.

11. **`UpdateTenantNameDto`** — DTO de entrada para renombrar una academia.

    > **Figura: Diagrama de herencia para `UpdateTenantNameDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `UpdateTenantNameDto`**
    - La recibe el controlador implementado `TenantController`.
    - La valida la clase implementada `UpdateTenantNameDtoValidator`.

12. **`UpdateTenantTimezoneDto`** — DTO de entrada para actualizar la zona horaria de la academia.

    > **Figura: Diagrama de herencia para `UpdateTenantTimezoneDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `UpdateTenantTimezoneDto`**
    - La recibe el controlador implementado `TenantController`.
    - La valida la clase implementada `UpdateTenantTimezoneDtoValidator`.

### Datos de salida

13. **`TokenResponseDto`** — objeto de salida que agrupa el token de acceso JWT y el token de
    refresco opaco emitidos al autenticar o refrescar una sesión.

    > **Figura: Diagrama de herencia para `TokenResponseDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `TokenResponseDto`**
    - La construye la clase implementada `AuthenticationService` y la clase implementada
      `RefreshService`.
    - La encapsula el caso sellado `LoginOutcome.Success` como propiedad.
    - La devuelve el controlador implementado `AuthController`.

14. **`UserListItemDto`** — proyección mínima de usuario (identificador y nombre de usuario) para
    listados paginados y búsquedas.

    > **Figura: Diagrama de herencia para `UserListItemDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `UserListItemDto`**
    - La construye la clase implementada `UserViewBuilder`.
    - La devuelven la interfaz implementada `IUserViewBuilder` y el controlador implementado
      `AuthController`.

15. **`PagedUsersResponseDto`** — envoltura paginada de una lista de `UserListItemDto` con índice
    de página actual y máximo.

    > **Figura: Diagrama de herencia para `PagedUsersResponseDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `PagedUsersResponseDto`**
    - La construye la clase implementada `UserViewBuilder` a partir de listas de la entidad
      implementada `User`.
    - La devuelve el controlador implementado `AuthController`.

16. **`TenantDto`** — proyección de academia para su devolución en la API: identificador, nombre y
    zona horaria.

    > **Figura: Diagrama de herencia para `TenantDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `TenantDto`**
    - La construye la clase implementada `TenantBuilder`.
    - La devuelven la interfaz implementada `ITenantService` y el controlador implementado
      `TenantController`.

17. **`TenantTierCountDto`** — DTO de salida con la distribución de academias por nivel de
    suscripción.

    > **Figura: Diagrama de herencia para `TenantTierCountDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `TenantTierCountDto`**
    - La construye la clase implementada `TenantService` a partir del registro implementado
      `TenantTierCountRow`.
    - La devuelve el controlador implementado `TenantController`.

### DTO de paginación

18. **`PaginationQueryDto`** — parámetro de consulta que transporta el índice de página solicitado.

    > **Figura: Diagrama de herencia para `PaginationQueryDto`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `PaginationQueryDto`**
    - La recibe el controlador implementado `AuthController` en los puntos de acceso de listado.
    - La valida la clase implementada `PaginationQueryDtoValidator`.
    - La consume la clase implementada `UserDirectoryService`, que extrae `PageIndex` y lo delega a
      la clase implementada `PageCalculator`.

### Modelo persistente

19. **`User`** — entidad persistente del usuario: credenciales, rol, indicador de borrado lógico y
    datos de bloqueo de cuenta por intentos fallidos.

    > **Figura: Diagrama de herencia para `User`**
    - Implementa la interfaz externa `IEntity` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para `User`**
    - La persiste la clase implementada `UserDao`.
    - La recibe la interfaz externa `IPasswordHasher<User>` para la operación de verificación y
      actualización de hash.
    - La construye la clase implementada `UserEntityBuilder`.
    - La consume la clase implementada `UserViewBuilder` para producir `UserListItemDto`.
    - La encapsula el registro implementado `UserWithTenant`.

20. **`UserRole`** — tipo de valor de dominio que encapsula el rol como cadena validada; expone
    tres instancias estáticas (`Student`, `Teacher`, `Client`) y un método de fábrica.

    > **Figura: Diagrama de herencia para `UserRole`**
    - Implementa la interfaz genérica externa `IEquatable<UserRole>`.

    > **Figura: Diagrama de colaboración para `UserRole`**
    - Referencia las constantes de la clase implementada `UserRoles` para inicializar sus instancias
      estáticas.
    - La consume el controlador implementado `AuthController` y la clase implementada
      `UserRegistrationService` para indicar el rol al registrar un usuario.

21. **`Tenant`** — entidad persistente de la academia: identificador, nombre y zona horaria.

    > **Figura: Diagrama de herencia para `Tenant`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `Tenant`**
    - La persiste la clase implementada `TenantDao`.
    - La construye la clase implementada `TenantBuilder`.
    - La encapsula el registro implementado `UserWithTenant`.
    - La consume la clase implementada `JwtAccessTokenGenerator` para incluir los claims de academia
      en el token.

22. **`TenantDomain`** — entidad de unión muchos-a-muchos entre `User` y `Tenant`: registra la
    pertenencia de un usuario a una academia.

    > **Figura: Diagrama de herencia para `TenantDomain`**
    - Implementa la interfaz externa `ITwoForeignEntity` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para `TenantDomain`**
    - La persiste la clase implementada `TenantDomainDao`.
    - La construye la clase implementada `UserEntityBuilder` al registrar un usuario.

23. **`TenantAllowedServices`** — entidad persistente del estado de suscripción de una academia:
    nivel de la pirámide de servicios y fecha de vencimiento.

    > **Figura: Diagrama de herencia para `TenantAllowedServices`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `TenantAllowedServices`**
    - La persiste la clase implementada `TenantAllowedServicesDao`.
    - La construye la clase implementada `TenantSubscriptionUpdater` con los datos recibidos vía
      gRPC y la persiste con un upsert.
    - La consume la clase implementada `JwtAccessTokenGenerator` para incluir el nivel de
      suscripción y la fecha de vencimiento en el JWT.

24. **`RefreshToken`** — entidad persistente del token de refresco rotante: hash SHA-256 del token
    opaco, identificador del usuario propietario, fechas de creación, vencimiento y revocación.

    > **Figura: Diagrama de herencia para `RefreshToken`**
    - Clase **raíz**: no hereda de ninguna otra ni implementa interfaces.

    > **Figura: Diagrama de colaboración para `RefreshToken`**
    - La construye la clase implementada `RefreshTokenGenerator` al emitir un nuevo token.
    - La persiste y revoca la clase implementada `RefreshTokenDao`.
    - La encapsula el registro implementado `RefreshTokenWithOwner` como resultado de una lectura
      por hash.

25. **`OutboxEvent`** — entidad persistente del outbox de eventos: todos los campos del esquema
    canónico (`Id`, `AggregateType`, `AggregateId`, `EventType`, `RoutingKey`, `Payload`,
    `OccurredAt`, `PublishedAt`, `LeasedUntil`, `Attempts`, `LastError`).

    > **Figura: Diagrama de herencia para `OutboxEvent`**
    - Implementa la interfaz externa `IOutboxEvent` (del paquete `DAMA.Software.MySqlOutbox`) y la
      interfaz externa `IEntity` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para `OutboxEvent`**
    - La construye la clase implementada `StudentRegisteredEventBuilder`, serializando el evento de
      dominio como JSON en el campo `Payload`.
    - La inserta en la base de datos la clase implementada `OutboxEventDao` dentro de la transacción
      de registro de usuario.
    - La consumen las clases implementadas `OutboxPublisher` (para arrendar y publicar en RabbitMQ)
      y `OutboxJanitor` (para eliminar publicados antiguos).
    - La publica en RabbitMQ la clase implementada `RabbitMqEventPublisher`.

### Eventos de dominio

26. **`IDomainEvent`** — contrato de los cuatro campos que todo evento de dominio debe exponer:
    `EventId`, `EventType`, `OccurredAt` y `AggregateId`.

    > **Figura: Diagrama de herencia para `IDomainEvent`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IDomainEvent`**
    - La implementa el registro implementado `StudentRegisteredEvent`.

27. **`StudentRegisteredEvent`** — registro de evento de dominio que `StudentRegisteredEventBuilder`
    serializa como carga del outbox cuando se registra un estudiante.

    > **Figura: Diagrama de herencia para `StudentRegisteredEvent`**
    - Es un registro sellado que implementa la interfaz implementada `IDomainEvent`.

    > **Figura: Diagrama de colaboración para `StudentRegisteredEvent`**
    - Lo construye la clase implementada `StudentRegisteredEventBuilder`.
    - Contiene como propiedad el registro implementado `StudentRegisteredEventData`.

28. **`StudentRegisteredEventData`** — registro de datos anidado dentro de `StudentRegisteredEvent`
    con los campos que el consumidor Attendance necesita: identificador y nombre del estudiante,
    identificador de la academia y fecha de registro.

    > **Figura: Diagrama de herencia para `StudentRegisteredEventData`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `StudentRegisteredEventData`**
    - Lo construye la clase implementada `StudentRegisteredEventBuilder`.
    - Lo contiene el registro implementado `StudentRegisteredEvent`.

### Resultados discriminados — usuarios

29. **`LoginOutcome`** — unión discriminada del resultado del inicio de sesión: `Success` (con
    `TokenResponseDto`), `InvalidCredentials` o `AccountLocked`.

    > **Figura: Diagrama de herencia para `LoginOutcome`**
    - Registro abstracto **raíz** con tres casos sellados anidados que lo extienden.

    > **Figura: Diagrama de colaboración para `LoginOutcome`**
    - Lo produce la clase implementada `AuthenticationService`.
    - Lo empareja por patrón el controlador implementado `AuthController`.
    - El caso `Success` encapsula la clase implementada `TokenResponseDto`.

30. **`RegisterUserOutcome`** — unión discriminada del resultado del registro de usuario: `Created`
    o `DuplicateName`.

    > **Figura: Diagrama de herencia para `RegisterUserOutcome`**
    - Registro abstracto **raíz** con dos casos sellados anidados.

    > **Figura: Diagrama de colaboración para `RegisterUserOutcome`**
    - Lo produce la clase implementada `UserRegistrationService`.
    - Lo empareja por patrón el controlador implementado `AuthController`.

31. **`DeleteUserOutcome`** — unión discriminada del resultado de la eliminación de usuario:
    `Deleted`, `NotFound`, `SelfDeleteForbidden` o `ClientDeleteForbidden`.

    > **Figura: Diagrama de herencia para `DeleteUserOutcome`**
    - Registro abstracto **raíz** con cuatro casos sellados anidados.

    > **Figura: Diagrama de colaboración para `DeleteUserOutcome`**
    - Lo produce la clase implementada `UserDirectoryService`.
    - Lo empareja por patrón el controlador implementado `AuthController`.

32. **`RenameUserOutcome`** — unión discriminada del resultado de renombrar un usuario: `Renamed`,
    `NotFound` o `DuplicateName`.

    > **Figura: Diagrama de herencia para `RenameUserOutcome`**
    - Registro abstracto **raíz** con tres casos sellados anidados.

    > **Figura: Diagrama de colaboración para `RenameUserOutcome`**
    - Lo produce la clase implementada `UserDirectoryService`.
    - Lo empareja por patrón el controlador implementado `AuthController`.

### Resultados discriminados — academias

33. **`UpdateTenantNameOutcome`** — unión discriminada del resultado de renombrar una academia:
    `Updated` o `NotFound`.

    > **Figura: Diagrama de herencia para `UpdateTenantNameOutcome`**
    - Registro abstracto **raíz** con dos casos sellados anidados.

    > **Figura: Diagrama de colaboración para `UpdateTenantNameOutcome`**
    - Lo produce la clase implementada `TenantService`.
    - Lo empareja por patrón el controlador implementado `TenantController`.

34. **`UpdateTenantTimezoneOutcome`** — unión discriminada del resultado de actualizar la zona
    horaria: `Updated`, `Forbidden` o `NotFound`.

    > **Figura: Diagrama de herencia para `UpdateTenantTimezoneOutcome`**
    - Registro abstracto **raíz** con tres casos sellados anidados.

    > **Figura: Diagrama de colaboración para `UpdateTenantTimezoneOutcome`**
    - Lo produce la clase implementada `TenantService`.
    - Lo empareja por patrón el controlador implementado `TenantController`.

### Transportadores internos

35. **`UserWithTenant`** — registro inmutable que agrupa una entidad `User` y una entidad `Tenant`
    leídas juntas desde la base de datos en una única consulta de autenticación.

    > **Figura: Diagrama de herencia para `UserWithTenant`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `UserWithTenant`**
    - Lo construye la clase implementada `UserDao` como resultado de la consulta de login.
    - Lo consume la clase implementada `AuthenticationService` para extraer el usuario y la academia
      antes de emitir el token.
    - Lo encapsula el registro implementado `RefreshTokenWithOwner`.

36. **`IssuedRefreshToken`** — registro inmutable que agrupa el token opaco en texto claro (para
    devolverlo al cliente) y la entidad `RefreshToken` (para persistirla en base de datos).

    > **Figura: Diagrama de herencia para `IssuedRefreshToken`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `IssuedRefreshToken`**
    - Lo construye la clase implementada `RefreshTokenGenerator`.
    - Lo consumen las clases implementadas `AuthenticationService` y `RefreshService` para separar
      el token opaco (enviado al cliente) del hash almacenado.

37. **`RefreshTokenWithOwner`** — registro inmutable que agrupa una entidad `RefreshToken` y el
    `UserWithTenant` propietario, leídos juntos en una consulta de refresco.

    > **Figura: Diagrama de herencia para `RefreshTokenWithOwner`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `RefreshTokenWithOwner`**
    - Lo construye la clase implementada `RefreshTokenDao` como resultado de la búsqueda por hash.
    - Lo consume la clase implementada `RefreshService` para detectar reutilización de token y para
      emitir un nuevo par de tokens.

### Registro de proyección auxiliar del objeto de acceso a datos de academias

38. **`TenantTierCountRow`** — registro de solo lectura que transporta una fila de la consulta de
    distribución de academias por nivel de suscripción: nivel entero y conteo de academias.

    > **Figura: Diagrama de herencia para `TenantTierCountRow`**
    - Es un registro de estructura de solo lectura **raíz** (`readonly record struct`).

    > **Figura: Diagrama de colaboración para `TenantTierCountRow`**
    - Lo construye la clase implementada `TenantDao` al mapear los resultados de la consulta.
    - Lo consume la clase implementada `TenantService` para proyectarlo a `TenantTierCountDto`.

### Interfaces ISP de acceso a datos — usuarios

39. **`IUserAuthenticationDao`** — contrato de las operaciones de acceso a datos exclusivas de la
    autenticación: lectura del usuario con su academia por nombre de usuario, registro de intento
    fallido, restablecimiento de intentos y actualización del hash de contraseña.

    > **Figura: Diagrama de herencia para `IUserAuthenticationDao`**
    - Es una interfaz **raíz** (no extiende `ISingleDao<T>` deliberadamente, por ISP).

    > **Figura: Diagrama de colaboración para `IUserAuthenticationDao`**
    - La implementa la clase implementada `UserDao`.
    - La consume la clase implementada `AuthenticationService`.
    - Sus operaciones trabajan con las entidades implementadas `User` y el transportador implementado
      `UserWithTenant`.

40. **`IUserRegistrationDao`** — contrato de la operación de creación transaccional de usuario:
    inserción que devuelve `false` ante nombre duplicado (excepción MySQL 1062).

    > **Figura: Diagrama de herencia para `IUserRegistrationDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserRegistrationDao`**
    - La implementa la clase implementada `UserDao`.
    - La consume la clase implementada `UserRegistrationService`.
    - Su operación recibe la entidad implementada `User` y un `ITransactionContext` externo.

41. **`IUserDirectoryDao`** — contrato de las operaciones del directorio de usuarios para un tenant:
    listado paginado por rol, conteo, búsqueda por nombre exacto, lectura por identificador, borrado
    lógico y renombrado con detección de duplicados.

    > **Figura: Diagrama de herencia para `IUserDirectoryDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserDirectoryDao`**
    - La implementa la clase implementada `UserDao`.
    - La consume la clase implementada `UserDirectoryService`.
    - Sus operaciones trabajan con la entidad implementada `User`.

### Interfaces ISP de acceso a datos — academias y tokens

42. **`ITenantDao`** — contrato del objeto de acceso a datos de academias: lectura total, creación,
    actualización de nombre y zona horaria, y distribución por nivel de suscripción.

    > **Figura: Diagrama de herencia para `ITenantDao`**
    - Extiende la interfaz externa `ISingleDao<Tenant>` del paquete `SQLDaosPackage`.

    > **Figura: Diagrama de colaboración para `ITenantDao`**
    - La implementa la clase implementada `TenantDao`.
    - La consume la clase implementada `TenantService`.
    - Sus operaciones trabajan con la entidad implementada `Tenant` y el registro implementado
      `TenantTierCountRow`.

43. **`ITenantAllowedServicesDao`** — contrato de las operaciones de estado de suscripción de
    academias: lectura por identificador de academia, upsert transaccional y restablecimiento masivo
    de las suscripciones vencidas.

    > **Figura: Diagrama de herencia para `ITenantAllowedServicesDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ITenantAllowedServicesDao`**
    - La implementa la clase implementada `TenantAllowedServicesDao`.
    - La consumen las clases implementadas `AuthenticationService`, `RefreshService`,
      `TenantSubscriptionUpdater` y el trabajo implementado `SubscriptionExpiryJanitor`.
    - Sus operaciones trabajan con la entidad implementada `TenantAllowedServices`.

44. **`ITenantDomainDao`** — contrato de la operación de creación de la relación usuario-academia.

    > **Figura: Diagrama de herencia para `ITenantDomainDao`**
    - Extiende la interfaz externa `ITwoForeignDao<TenantDomain>` del paquete `SQLDaosPackage`.

    > **Figura: Diagrama de colaboración para `ITenantDomainDao`**
    - La implementa la clase implementada `TenantDomainDao`.
    - La consume la clase implementada `UserRegistrationService`.
    - Su operación trabaja con la entidad implementada `TenantDomain` y un `ITransactionContext`
      externo.

45. **`IRefreshTokenReadDao`** — contrato de la lectura de un token de refresco por su hash,
    segregada de las escrituras para que `RefreshService` solo inyecte lo que necesita.

    > **Figura: Diagrama de herencia para `IRefreshTokenReadDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IRefreshTokenReadDao`**
    - La implementa la clase implementada `RefreshTokenDao`.
    - La consume la clase implementada `RefreshService`.
    - Su operación devuelve el transportador implementado `RefreshTokenWithOwner`.

46. **`IRefreshTokenWriteDao`** — contrato de las escrituras sobre tokens de refresco: creación,
    revocación individual y revocación masiva por usuario.

    > **Figura: Diagrama de herencia para `IRefreshTokenWriteDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IRefreshTokenWriteDao`**
    - La implementa la clase implementada `RefreshTokenDao`.
    - La consumen las clases implementadas `AuthenticationService` y `RefreshService`.
    - Sus operaciones trabajan con la entidad implementada `RefreshToken` y reciben
      `ITransactionContext` externo.

47. **`IOutboxEventDao`** — contrato de las operaciones del outbox de eventos: inserción
    transaccional, arrendamiento de lotes pendientes, marcado de publicados, registro de fallos y
    eliminación de publicados antiguos.

    > **Figura: Diagrama de herencia para `IOutboxEventDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IOutboxEventDao`**
    - La implementa la clase implementada `OutboxEventDao`.
    - La consumen las clases implementadas `UserRegistrationService` (inserción), `OutboxPublisher`
      (arrendamiento y marcado) y `OutboxJanitor` (eliminación).
    - Sus operaciones trabajan con la entidad implementada `OutboxEvent`.

### Objetos de acceso a datos concretos

48. **`UserDao`** — objeto de acceso a datos concreto de la tabla `User`: implementa las tres
    interfaces ISP de usuario (`IUserAuthenticationDao`, `IUserRegistrationDao`,
    `IUserDirectoryDao`) desde una única clase, lo que Scrutor registra automáticamente contra cada
    una de ellas.

    > **Figura: Diagrama de herencia para `UserDao`**
    - Hereda de la clase externa `MySQLSingleDao<User>` del paquete `SQLDaosPackage`.
    - Implementa las interfaces implementadas `IUserAuthenticationDao`, `IUserRegistrationDao` e
      `IUserDirectoryDao`.

    > **Figura: Diagrama de colaboración para `UserDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Trabaja con las entidades implementadas `User` y `Tenant` y el transportador implementado
      `UserWithTenant`.
    - Usa la clase auxiliar externa `MySqlTransactionContextAccessor` para extraer la transacción
      nativa de la abstracción `ITransactionContext`.

49. **`TenantDao`** — objeto de acceso a datos concreto de la tabla `Tenant`: implementa `ITenantDao`
    (que extiende `ISingleDao<Tenant>`) con operaciones vía procedimientos almacenados para todas
    las consultas de negocio.

    > **Figura: Diagrama de herencia para `TenantDao`**
    - Hereda de la clase externa `MySQLSingleDao<Tenant>`.
    - Implementa la interfaz implementada `ITenantDao`.

    > **Figura: Diagrama de colaboración para `TenantDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `Tenant` y el registro implementado
      `TenantTierCountRow`.

50. **`TenantAllowedServicesDao`** — objeto de acceso a datos concreto para la tabla de servicios
    permitidos: no hereda de clase base del paquete (sus operaciones son todas custom vía
    procedimientos almacenados).

    > **Figura: Diagrama de herencia para `TenantAllowedServicesDao`**
    - Implementa la interfaz implementada `ITenantAllowedServicesDao`.

    > **Figura: Diagrama de colaboración para `TenantAllowedServicesDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `TenantAllowedServices`.
    - Usa la clase auxiliar externa `MySqlTransactionContextAccessor` en la operación de upsert.

51. **`TenantDomainDao`** — objeto de acceso a datos concreto de la tabla de unión `TenantDomain`:
    implementa `ITenantDomainDao` (que extiende `ITwoForeignDao<TenantDomain>`) con una única
    operación de creación transaccional.

    > **Figura: Diagrama de herencia para `TenantDomainDao`**
    - Hereda de la clase externa `MySQLTwoForeignDao<TenantDomain>`.
    - Implementa la interfaz implementada `ITenantDomainDao`.

    > **Figura: Diagrama de colaboración para `TenantDomainDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Produce la entidad implementada `TenantDomain` a partir de la lectura y la consume en la
      escritura.

52. **`RefreshTokenDao`** — objeto de acceso a datos concreto de la tabla `RefreshToken`: implementa
    las dos interfaces ISP de lectura y escritura de token de refresco desde una única clase.

    > **Figura: Diagrama de herencia para `RefreshTokenDao`**
    - Implementa las interfaces implementadas `IRefreshTokenWriteDao` e `IRefreshTokenReadDao`.

    > **Figura: Diagrama de colaboración para `RefreshTokenDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Trabaja con la entidad implementada `RefreshToken` y los transportadores implementados
      `UserWithTenant` y `RefreshTokenWithOwner`.
    - Usa la clase auxiliar externa `MySqlTransactionContextAccessor` en las operaciones
      transaccionales.

53. **`OutboxEventDao`** — objeto de acceso a datos concreto de la tabla `outbox_events`: implementa
    `IOutboxEventDao` usando utilidades del paquete `DAMA.Software.MySqlOutbox` para el arrendamiento
    de lotes y el registro de fallos.

    > **Figura: Diagrama de herencia para `OutboxEventDao`**
    - Implementa la interfaz implementada `IOutboxEventDao`.

    > **Figura: Diagrama de colaboración para `OutboxEventDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `OutboxEvent`.
    - Usa las utilidades externas `MySqlOutboxLeaseHelper` y `OutboxLeaseDescriptor<OutboxEvent>`
      del paquete `DAMA.Software.MySqlOutbox`.

### Inyectores de semilla

54. **Familia `TenantDataInjector`, `TenantAllowedServicesDataInjector`, `UserDataInjector`, `TenantDomainDataInjector`** — familia de cuatro inyectores de datos de semilla, uno por cada tabla
    que el sembrado carga desde archivos CSV. Todos comparten la misma relación estructural:
    heredan de la clase externa `DataInjector` (del paquete `SQLDaosPackage`) e inicializan en el
    constructor el comando `LOAD DATA INFILE` específico de su tabla.

    > **Figura: Diagrama de herencia para `TenantDataInjector` / `TenantAllowedServicesDataInjector` / `UserDataInjector` / `TenantDomainDataInjector`**
    - Cada uno hereda de la clase externa `DataInjector`.

    > **Figura: Diagrama de colaboración para `TenantDataInjector` / `TenantAllowedServicesDataInjector` / `UserDataInjector` / `TenantDomainDataInjector`**
    - Los orquesta y ejecuta la clase implementada `DBInjector` a través de la interfaz externa
      `IDataInjector`.

### Utilidades de base de datos

55. **`DBConnector`** — clase estática que resuelve la cadena de conexión desde la variable de
    entorno `DB_CONNECTION_STRING` o, como respaldo, desde `dbsettings.json`.

    > **Figura: Diagrama de herencia para `DBConnector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBConnector`**
    - La consumen las clases implementadas `PersistenceModule` y `DatabaseHealthCheck` para obtener
      la cadena de conexión.

56. **`DBInjector`** — clase estática que coordina el sembrado: trunca todas las tablas llamando al
    procedimiento almacenado `TruncateAllTables` y luego ejecuta los cuatro inyectores en orden.

    > **Figura: Diagrama de herencia para `DBInjector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBInjector`**
    - Lo invoca la clase implementada `DatabaseSeeder`.
    - Construye y ejecuta la familia de inyectores implementados
      (`TenantDataInjector`, `TenantAllowedServicesDataInjector`, `UserDataInjector`,
      `TenantDomainDataInjector`).

### Paginación

57. **`PageCalculator`** — clase estática que calcula el índice de página efectivo, el índice máximo
    y el desplazamiento SQL a partir del total de registros, el índice solicitado y el tamaño de
    página.

    > **Figura: Diagrama de herencia para `PageCalculator`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `PageCalculator`**
    - La consume la clase implementada `UserDirectoryService`.

### Constructores

58. **`IUserEntityBuilder`** — contrato de la fabricación de entidades de usuario: construcción de
    un `User` con hash de contraseña y de un `TenantDomain` a partir de las claves foráneas.

    > **Figura: Diagrama de herencia para `IUserEntityBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserEntityBuilder`**
    - La implementa la clase implementada `UserEntityBuilder`.
    - La consume la clase implementada `UserRegistrationService`.
    - Su operación de construcción recibe la interfaz implementada `ICredentialsPayload` (no el tipo
      concreto), siguiendo el principio de segregación de interfaces.

59. **`UserEntityBuilder`** — implementación que construye entidades de usuario con `Guid.NewGuid()`
    y `IPasswordHasher<User>`, y la entidad de unión `TenantDomain`.

    > **Figura: Diagrama de herencia para `UserEntityBuilder`**
    - Implementa la interfaz implementada `IUserEntityBuilder`.

    > **Figura: Diagrama de colaboración para `UserEntityBuilder`**
    - Recibe por inyección de dependencias la interfaz externa `IPasswordHasher<User>` para cifrar
      la contraseña.
    - Consume la interfaz implementada `ICredentialsPayload` y la clase implementada `UserRole`.
    - Construye y devuelve las entidades implementadas `User` y `TenantDomain`.

60. **`IUserViewBuilder`** — contrato de la fabricación de vistas de usuario: proyección de un
    usuario a `UserListItemDto` y construcción de la respuesta paginada.

    > **Figura: Diagrama de herencia para `IUserViewBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserViewBuilder`**
    - La implementa la clase implementada `UserViewBuilder`.
    - La consume la clase implementada `UserDirectoryService`.

61. **`UserViewBuilder`** — implementación sin dependencias que proyecta entidades `User` a DTOs de
    lista y de respuesta paginada.

    > **Figura: Diagrama de herencia para `UserViewBuilder`**
    - Implementa la interfaz implementada `IUserViewBuilder`.

    > **Figura: Diagrama de colaboración para `UserViewBuilder`**
    - Consume la entidad implementada `User`.
    - Construye y devuelve las clases implementadas `UserListItemDto` y `PagedUsersResponseDto`.

62. **`ITenantBuilder`** — contrato de la fabricación de academias: construcción de la entidad
    `Tenant` con zona horaria predeterminada y proyección a `TenantDto`.

    > **Figura: Diagrama de herencia para `ITenantBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ITenantBuilder`**
    - La implementa la clase implementada `TenantBuilder`.
    - La consume la clase implementada `TenantService`.

63. **`TenantBuilder`** — implementación que construye la entidad `Tenant` con `Guid.NewGuid()` y
    la zona horaria predeterminada, y la proyecta a `TenantDto`.

    > **Figura: Diagrama de herencia para `TenantBuilder`**
    - Implementa la interfaz implementada `ITenantBuilder`.

    > **Figura: Diagrama de colaboración para `TenantBuilder`**
    - Consume la entidad implementada `Tenant` y la clase implementada `CreateTenantDto`.
    - Construye y devuelve las clases implementadas `Tenant` y `TenantDto`.

64. **`IStudentRegisteredEventBuilder`** — contrato de la fabricación del evento de outbox
    `student.registered`: construye un `OutboxEvent` serializado a partir de un `User` y un
    identificador de academia.

    > **Figura: Diagrama de herencia para `IStudentRegisteredEventBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IStudentRegisteredEventBuilder`**
    - La implementa la clase implementada `StudentRegisteredEventBuilder`.
    - La consume la clase implementada `UserRegistrationService`.

65. **`StudentRegisteredEventBuilder`** — implementación sellada que construye el `OutboxEvent` de
    registro de estudiante: crea el registro de dominio `StudentRegisteredEvent` con su carga
    `StudentRegisteredEventData`, lo serializa como JSON y lo envuelve en `OutboxEvent` con el
    identificador, tipo, clave de enrutamiento y metadatos canónicos del outbox.

    > **Figura: Diagrama de herencia para `StudentRegisteredEventBuilder`**
    - Implementa la interfaz implementada `IStudentRegisteredEventBuilder`.

    > **Figura: Diagrama de colaboración para `StudentRegisteredEventBuilder`**
    - Construye los registros implementados `StudentRegisteredEvent` y `StudentRegisteredEventData`.
    - Construye y devuelve la entidad implementada `OutboxEvent`.

### Contratos de servicio

66. **`IAuthenticationService`** — contrato del caso de uso de inicio de sesión.

    > **Figura: Diagrama de herencia para `IAuthenticationService`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IAuthenticationService`**
    - La implementa la clase implementada `AuthenticationService`.
    - La consume el controlador implementado `AuthController`.
    - Su operación devuelve el resultado discriminado implementado `LoginOutcome`.

67. **`IRefreshService`** — contrato de los casos de uso de refresco de token y cierre de sesión.

    > **Figura: Diagrama de herencia para `IRefreshService`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IRefreshService`**
    - La implementa la clase implementada `RefreshService`.
    - La consume el controlador implementado `AuthController`.
    - Su operación de refresco devuelve `TokenResponseDto` nulable; la de cierre de sesión es `void`.

68. **`IUserRegistrationService`** — contrato del caso de uso de registro de usuarios.

    > **Figura: Diagrama de herencia para `IUserRegistrationService`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserRegistrationService`**
    - La implementa la clase implementada `UserRegistrationService`.
    - La consume el controlador implementado `AuthController`.
    - Su operación devuelve el resultado discriminado implementado `RegisterUserOutcome`.

69. **`IUserDirectoryService`** — contrato de los casos de uso del directorio de usuarios: listados
    paginados, búsqueda, renombrado, eliminación y búsqueda por nombre exacto.

    > **Figura: Diagrama de herencia para `IUserDirectoryService`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUserDirectoryService`**
    - La implementa la clase implementada `UserDirectoryService`.
    - La consume el controlador implementado `AuthController`.
    - Sus operaciones devuelven los resultados discriminados implementados `DeleteUserOutcome` y
      `RenameUserOutcome`, y los DTOs de salida `PagedUsersResponseDto` y `UserListItemDto`.

70. **`ITenantService`** — contrato de los casos de uso de gestión de academias: listado total,
    distribución por nivel, creación, renombrado y actualización de zona horaria.

    > **Figura: Diagrama de herencia para `ITenantService`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ITenantService`**
    - La implementa la clase implementada `TenantService`.
    - La consume el controlador implementado `TenantController`.
    - Sus operaciones de actualización devuelven los resultados discriminados implementados
      `UpdateTenantNameOutcome` y `UpdateTenantTimezoneOutcome`.

71. **`ITenantSubscriptionUpdater`** — contrato de la actualización atómica del nivel y la fecha de
    vencimiento de la suscripción de una academia.

    > **Figura: Diagrama de herencia para `ITenantSubscriptionUpdater`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ITenantSubscriptionUpdater`**
    - La implementa la clase implementada `TenantSubscriptionUpdater`.
    - La consume la clase implementada `TenantSubscriptionGrpcService`.

72. **`IEventPublisher`** — contrato de la publicación de un evento del outbox hacia RabbitMQ.

    > **Figura: Diagrama de herencia para `IEventPublisher`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IEventPublisher`**
    - La implementa la clase implementada `RabbitMqEventPublisher`.
    - La consume el trabajo implementado `OutboxPublisher`.
    - Su operación recibe la entidad implementada `OutboxEvent`.

73. **`IJwtTokenSigner`** — contrato del firmante RSA que expone las credenciales de firma para que
    `JwtAccessTokenGenerator` las use sin depender directamente de `JwtTokenSigner`.

    > **Figura: Diagrama de herencia para `IJwtTokenSigner`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IJwtTokenSigner`**
    - La implementa la clase implementada `JwtTokenSigner`.
    - La consume la clase implementada `JwtAccessTokenGenerator`.

### Capa de servicio

74. **`AuthenticationService`** — implementación del caso de uso de inicio de sesión: lee el usuario
    con su academia, verifica la contraseña mediante `IPasswordHasher<User>`, aplica bloqueo de
    cuenta por intentos fallidos, actualiza el hash si el algoritmo lo requiere, persiste el nuevo
    token de refresco y emite ambos tokens en la misma transacción.

    > **Figura: Diagrama de herencia para `AuthenticationService`**
    - Implementa la interfaz implementada `IAuthenticationService`.

    > **Figura: Diagrama de colaboración para `AuthenticationService`**
    - Recibe por inyección de dependencias: `IUserAuthenticationDao`, `ITenantAllowedServicesDao`,
      la interfaz externa `IPasswordHasher<User>`, `IAccessTokenGenerator`, `IRefreshTokenGenerator`,
      `IRefreshTokenWriteDao` y `IUnitOfWork` (externo).
    - Consume los transportadores implementados `UserWithTenant` e `IssuedRefreshToken`.
    - Produce el resultado discriminado implementado `LoginOutcome`.

75. **`RefreshService`** — implementación de los casos de uso de refresco de token y cierre de
    sesión: detecta reutilización de token revocando todas las sesiones del usuario afectado, rota
    el token de refresco dentro de una transacción y emite un nuevo par de tokens.

    > **Figura: Diagrama de herencia para `RefreshService`**
    - Implementa la interfaz implementada `IRefreshService`.

    > **Figura: Diagrama de colaboración para `RefreshService`**
    - Recibe por inyección de dependencias: `IRefreshTokenReadDao`, `IRefreshTokenWriteDao`,
      `ITenantAllowedServicesDao`, `IAccessTokenGenerator`, `IRefreshTokenGenerator` e `IUnitOfWork`
      (externo).
    - Consume el transportador implementado `RefreshTokenWithOwner`.
    - Produce y devuelve la clase implementada `TokenResponseDto`.

76. **`UserRegistrationService`** — implementación del caso de uso de registro de usuarios: construye
    la entidad `User` mediante el builder, persiste el usuario, la relación `TenantDomain` y el
    evento de outbox en la misma transacción; para estudiantes además inserta el evento de dominio.

    > **Figura: Diagrama de herencia para `UserRegistrationService`**
    - Implementa la interfaz implementada `IUserRegistrationService`.

    > **Figura: Diagrama de colaboración para `UserRegistrationService`**
    - Recibe por inyección de dependencias: `IUserRegistrationDao`, `ITenantDomainDao`,
      `IOutboxEventDao`, `IUnitOfWork` (externo), `IClaimContext`,
      `IStudentRegisteredEventBuilder` e `IUserEntityBuilder`.
    - Produce el resultado discriminado implementado `RegisterUserOutcome`.

77. **`UserDirectoryService`** — implementación de los casos de uso del directorio de usuarios:
    listados paginados usando `PageCalculator`, búsqueda por nombre exacto, renombrado con
    detección de duplicados y borrado lógico con verificación de permisos.

    > **Figura: Diagrama de herencia para `UserDirectoryService`**
    - Implementa la interfaz implementada `IUserDirectoryService`.

    > **Figura: Diagrama de colaboración para `UserDirectoryService`**
    - Recibe por inyección de dependencias: `IUserDirectoryDao`, `IClaimContext` e `IUserViewBuilder`.
    - Usa la clase implementada `PageCalculator` y las constantes de la clase implementada
      `UserRoles`.
    - Produce los resultados discriminados implementados `DeleteUserOutcome` y `RenameUserOutcome`,
      y los DTOs implementados `PagedUsersResponseDto` y `UserListItemDto`.

78. **`TenantService`** — implementación de los casos de uso de gestión de academias: lectura total,
    distribución por nivel, creación con zona horaria predeterminada, renombrado y actualización de
    zona horaria con verificación de que el llamante opera sobre su propia academia.

    > **Figura: Diagrama de herencia para `TenantService`**
    - Implementa la interfaz implementada `ITenantService`.

    > **Figura: Diagrama de colaboración para `TenantService`**
    - Recibe por inyección de dependencias: `ITenantDao`, `IClaimContext` e `ITenantBuilder`.
    - Produce los resultados discriminados implementados `UpdateTenantNameOutcome` y
      `UpdateTenantTimezoneOutcome`, y los DTOs implementados `TenantDto` y `TenantTierCountDto`.

79. **`TenantSubscriptionUpdater`** — implementación del caso de uso de actualización de suscripción:
    construye la entidad `TenantAllowedServices` con el nuevo nivel y fecha de vencimiento y la
    persiste con un upsert dentro de una transacción.

    > **Figura: Diagrama de herencia para `TenantSubscriptionUpdater`**
    - Implementa la interfaz implementada `ITenantSubscriptionUpdater`.

    > **Figura: Diagrama de colaboración para `TenantSubscriptionUpdater`**
    - Recibe por inyección de dependencias: `ITenantAllowedServicesDao` e `IUnitOfWork` (externo).
    - Construye y persiste la entidad implementada `TenantAllowedServices`.

### Seguridad y JWT

80. **`IAccessTokenGenerator`** — contrato del generador de tokens de acceso JWT.

    > **Figura: Diagrama de herencia para `IAccessTokenGenerator`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IAccessTokenGenerator`**
    - La implementa la clase implementada `JwtAccessTokenGenerator`.
    - La consumen las clases implementadas `AuthenticationService` y `RefreshService`.
    - Su operación recibe las entidades implementadas `User`, `Tenant` y `TenantAllowedServices`.

81. **`IRefreshTokenGenerator`** — contrato del generador de tokens de refresco.

    > **Figura: Diagrama de herencia para `IRefreshTokenGenerator`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IRefreshTokenGenerator`**
    - La implementa la clase implementada `RefreshTokenGenerator`.
    - La consumen las clases implementadas `AuthenticationService` y `RefreshService`.
    - Sus operaciones producen el transportador implementado `IssuedRefreshToken`.

82. **`JwtAccessTokenGenerator`** — generador del token de acceso JWT firmado con RS256: incluye
    los ocho claims propios de la plataforma más las audiencias múltiples separadas por coma,
    firmados con las credenciales del `IJwtTokenSigner`.

    > **Figura: Diagrama de herencia para `JwtAccessTokenGenerator`**
    - Implementa la interfaz implementada `IAccessTokenGenerator`.

    > **Figura: Diagrama de colaboración para `JwtAccessTokenGenerator`**
    - Recibe por inyección de dependencias `IOptions<JwtOptions>` (externo) e `IJwtTokenSigner`.
    - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim emitido.
    - Consume las entidades implementadas `User`, `Tenant` y `TenantAllowedServices`.
    - Usa el tipo externo `JwtSecurityToken` y `JwtSecurityTokenHandler` para construir y serializar
      el token.

83. **`JwtTokenSigner`** — firmante singleton que importa la clave privada RSA PEM (en Base64)
    desde `JwtOptions`, crea las `SigningCredentials` RS256 y las expone a través de `IJwtTokenSigner`;
    implementa `IDisposable` para liberar el objeto `RSA`.

    > **Figura: Diagrama de herencia para `JwtTokenSigner`**
    - Implementa las interfaces implementadas `IJwtTokenSigner` y la interfaz externa `IDisposable`.

    > **Figura: Diagrama de colaboración para `JwtTokenSigner`**
    - Recibe por inyección de dependencias `IOptions<JwtOptions>` (externo).
    - Usa el tipo externo `RSA` (de `System.Security.Cryptography`) para importar la clave.
    - Produce y expone el tipo externo `SigningCredentials` (de `Microsoft.IdentityModel.Tokens`).

84. **`RefreshTokenGenerator`** — generador de tokens de refresco: emite bytes aleatorios
    codificados en Base64Url como token opaco, calcula su hash SHA-256 para almacenamiento y
    construye la entidad `RefreshToken`.

    > **Figura: Diagrama de herencia para `RefreshTokenGenerator`**
    - Implementa la interfaz implementada `IRefreshTokenGenerator`.

    > **Figura: Diagrama de colaboración para `RefreshTokenGenerator`**
    - Recibe por inyección de dependencias `IOptions<JwtOptions>` (externo) para el tiempo de vida
      del token de refresco.
    - Construye y devuelve el transportador implementado `IssuedRefreshToken` y la entidad
      implementada `RefreshToken`.

85. **`AuthClaims`** — clase estática con las constantes de los nombres de los ocho claims del token
    emitido por Auth.

    > **Figura: Diagrama de herencia para `AuthClaims`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `AuthClaims`**
    - La consumen las clases implementadas `ClaimContext` (lectura de claims),
      `JwtAccessTokenGenerator` (emisión de claims) y `JwtAuthenticationModule` (configuración del
      esquema de autenticación).

86. **`UserRoles`** — clase estática con los nombres de los cuatro roles del sistema (`Admin`,
    `Client`, `Teacher`, `Student`) y sus combinaciones compuestas para atributos de autorización.

    > **Figura: Diagrama de herencia para `UserRoles`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `UserRoles`**
    - La consumen los controladores implementados `AuthController` y `TenantController` en sus
      atributos de autorización.
    - La consume la clase implementada `UserRole` para inicializar sus instancias estáticas.
    - La consume la clase implementada `UserDirectoryService` para comparar el rol del usuario
      objetivo.

87. **`RequiresServiceTierAttribute`** — atributo de autorización personalizado que actúa como
    filtro de MVC: comprueba que el nivel de suscripción activo del llamante sea mayor o igual al
    mínimo requerido; si el claim de nivel o de vencimiento falta, asume nivel cero.

    > **Figura: Diagrama de herencia para `RequiresServiceTierAttribute`**
    - Hereda de la clase externa `System.Attribute`.
    - Implementa la interfaz externa `IAuthorizationFilter` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `RequiresServiceTierAttribute`**
    - Resuelve por inyección desde el contenedor la interfaz implementada `IClaimContext`.
    - Captura la clase implementada `MissingClaimException` para tratar claims ausentes como nivel
      cero.

### Servidor gRPC

88. **`TenantSubscriptionGrpcService`** — servidor gRPC que implementa el método
    `UpdateTenantSubscription` del contrato generado `TenantSubscription.TenantSubscriptionBase`:
    autentica la llamada entrante de Payment comparando en tiempo constante el secreto del encabezado
    `x-subscription-secret`, valida el formato del identificador de academia y delega en
    `ITenantSubscriptionUpdater`.

    > **Figura: Diagrama de herencia para `TenantSubscriptionGrpcService`**
    - Hereda de la clase externa generada por Grpc.Tools `TenantSubscription.TenantSubscriptionBase`
      (del paquete `DAMA.Software.GrpcContracts`).

    > **Figura: Diagrama de colaboración para `TenantSubscriptionGrpcService`**
    - Recibe por inyección de dependencias: `ITenantSubscriptionUpdater` e
      `IOptions<SubscriptionGrpcOptions>` (externo).
    - Usa el tipo externo `CryptographicOperations.FixedTimeEquals` para la comparación del secreto.

### Filtros de MVC

89. **`FluentValidationActionFilter`** — filtro de acción asíncrono global que ejecuta
    automáticamente, antes de cada acción, el validador FluentValidation correspondiente a cada
    argumento de la acción; si la validación falla, interrumpe con una respuesta 400 y el primer
    mensaje de error.

    > **Figura: Diagrama de herencia para `FluentValidationActionFilter`**
    - Implementa la interfaz externa `IAsyncActionFilter` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `FluentValidationActionFilter`**
    - Recibe por inyección de dependencias el tipo externo `IServiceProvider`.
    - Lee el atributo implementado `RuleSetAttribute` desde la reflexión de la acción para activar
      subconjuntos de reglas.

90. **`RuleSetAttribute`** — atributo de marcado de acción que indica a `FluentValidationActionFilter`
    qué conjunto de reglas FluentValidation activar para esa acción específica.

    > **Figura: Diagrama de herencia para `RuleSetAttribute`**
    - Hereda de la clase externa `System.Attribute`.

    > **Figura: Diagrama de colaboración para `RuleSetAttribute`**
    - Lo lee la clase implementada `FluentValidationActionFilter` mediante reflexión.

### Validadores

91. **Familia de validadores (`PaginationQueryDtoValidator`, `LoginCredentialsDtoValidator`, `RegisterCredentialsDtoValidator`, `RefreshTokenRequestDtoValidator`, `UpdateUsernameDtoValidator`, `UserSearchQueryDtoValidator`, `CreateTenantDtoValidator`, `UpdateTenantNameDtoValidator`, `UpdateTenantTimezoneDtoValidator`)** — familia de nueve validadores FluentValidation,
    uno por cada DTO de entrada del servicio. Todos comparten la misma relación estructural: cada
    uno extiende la clase externa `AbstractValidator<T>` donde `T` es el DTO que valida, y son
    descubiertos y registrados automáticamente por `ValidationModule` mediante
    `AddValidatorsFromAssemblyContaining<Program>()`. El filtro implementado
    `FluentValidationActionFilter` los resuelve en tiempo de ejecución a través de la interfaz
    externa `IValidator<T>`.

    > **Figura: Diagrama de herencia para cada validador de la familia**
    - Cada uno hereda de la clase externa `AbstractValidator<T>` correspondiente a su DTO.

    > **Figura: Diagrama de colaboración para cada validador de la familia**
    - El filtro implementado `FluentValidationActionFilter` los resuelve y ejecuta.
    - Los validadores de credenciales (`LoginCredentialsDtoValidator`,
      `RegisterCredentialsDtoValidator`) exponen la constante de mensaje de error inválido que el
      controlador implementado `AuthController` referencia directamente.

### Comprobaciones de disponibilidad

92. **`DatabaseHealthCheck`** — comprobación de disponibilidad de la base de datos: abre una
    conexión dedicada usando `DBConnector` y ejecuta `SELECT 1`.

    > **Figura: Diagrama de herencia para `DatabaseHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck` (de ASP.NET Core).

    > **Figura: Diagrama de colaboración para `DatabaseHealthCheck`**
    - Usa la clase implementada `DBConnector` para obtener la cadena de conexión.

93. **`RabbitMqHealthCheck`** — comprobación de disponibilidad del broker: abre una conexión AMQP
    usando las opciones de `RabbitMqOptions` y la descarta inmediatamente.

    > **Figura: Diagrama de herencia para `RabbitMqHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck`.

    > **Figura: Diagrama de colaboración para `RabbitMqHealthCheck`**
    - Recibe por inyección de dependencias `IOptions<RabbitMqOptions>` (externo).

94. **`ExternalDependency`** — enumerado de las dos dependencias externas del servicio: `Database` y
    `RabbitMq`.

    > **Figura: Diagrama de herencia para `ExternalDependency`**
    - Enumerado **raíz** (hereda implícitamente de `System.Enum`).

    > **Figura: Diagrama de colaboración para `ExternalDependency`**
    - Lo consume la clase implementada `ExternalCheckNaming` para generar el nombre convencional de
      cada sonda.
    - Lo consume el módulo implementado `HealthCheckModule` para registrar las comprobaciones.

95. **`ExternalCheckNaming`** — clase estática que genera el nombre convencional de cada sonda de
    disponibilidad (`AuthService-Database`, `AuthService-RabbitMq`).

    > **Figura: Diagrama de herencia para `ExternalCheckNaming`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ExternalCheckNaming`**
    - Consume el enumerado implementado `ExternalDependency`.
    - La consume el módulo implementado `HealthCheckModule` para registrar cada comprobación bajo
      su nombre convencional.

96. **`ReadinessResponseWriter`** — clase estática que serializa el `HealthReport` de ASP.NET Core
    como JSON con el estado total, la duración y la lista de sondas con sus estados individuales y
    mensajes de error.

    > **Figura: Diagrama de herencia para `ReadinessResponseWriter`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ReadinessResponseWriter`**
    - La configura el módulo implementado `HealthCheckModule` como delegado de escritura de
      respuesta para el punto de acceso `/health/ready`.

### Publicador de RabbitMQ

97. **`RabbitMqEventPublisher`** — publicador singleton de eventos a RabbitMQ: gestiona una
    conexión perezosa protegida por `SemaphoreSlim`, declara el intercambio de tópicos duradero
    `dama.events` en el primer uso, publica con confirmaciones del publicador habilitadas y libera
    los recursos al apagarse.

    > **Figura: Diagrama de herencia para `RabbitMqEventPublisher`**
    - Implementa la interfaz implementada `IEventPublisher` y la interfaz externa `IAsyncDisposable`.

    > **Figura: Diagrama de colaboración para `RabbitMqEventPublisher`**
    - Recibe por inyección de dependencias `IOptions<RabbitMqOptions>` (externo).
    - Consume la entidad implementada `OutboxEvent` para extraer la clave de enrutamiento, el
      identificador de mensaje y la carga serializada.
    - Usa los tipos externos `ConnectionFactory`, `IConnection` e `IChannel` del paquete
      `RabbitMQ.Client`.

### Trabajos en segundo plano

98. **`OutboxPublisher`** — trabajo `BackgroundService` que en cada ciclo resuelve un alcance de
    servicio, arrienda hasta cien eventos pendientes del outbox y los publica en RabbitMQ en
    paralelo; marca los publicados como tales y registra los fallos con el mensaje de error.

    > **Figura: Diagrama de herencia para `OutboxPublisher`**
    - Hereda de la clase externa `BackgroundService` (de ASP.NET Core).

    > **Figura: Diagrama de colaboración para `OutboxPublisher`**
    - Recibe por inyección de dependencias `IServiceProvider` (externo) e `IEventPublisher`.
    - Resuelve `IOutboxEventDao` desde un alcance creado por ciclo.
    - Consume y publica la entidad implementada `OutboxEvent`.

99. **`OutboxJanitor`** — trabajo `BackgroundService` que cada veinticuatro horas elimina de la
    tabla `outbox_events` los eventos publicados con más de siete días de antigüedad.

    > **Figura: Diagrama de herencia para `OutboxJanitor`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `OutboxJanitor`**
    - Recibe por inyección de dependencias `IServiceProvider` (externo).
    - Resuelve `IOutboxEventDao` desde un alcance creado por ciclo.

100. **`SubscriptionExpiryJanitor`** — trabajo `BackgroundService` que cada minuto restablece a
     nivel cero las entradas de `TenantAllowedServices` con la fecha de vencimiento pasada, de modo
     que ningún token nuevo incluya un nivel de suscripción activo para academias vencidas.

     > **Figura: Diagrama de herencia para `SubscriptionExpiryJanitor`**
     - Hereda de la clase externa `BackgroundService`.

     > **Figura: Diagrama de colaboración para `SubscriptionExpiryJanitor`**
     - Recibe por inyección de dependencias `IServiceProvider` (externo).
     - Resuelve `ITenantAllowedServicesDao` desde un alcance creado por ciclo.

### Opciones tipadas

101. **`JwtOptions`** — objeto de configuración enlazado desde la sección `AppSettings`: claves RSA
     pública y privada (Base64), emisor, audiencia de validación, lista de audiencias de emisión y
     tiempos de vida del token de acceso y del token de refresco. Es el único `*Options` del
     monorepo que incluye `PrivateKey`.

     > **Figura: Diagrama de herencia para `JwtOptions`**
     - Clase sellada **raíz**.

     > **Figura: Diagrama de colaboración para `JwtOptions`**
     - Lo enlaza y valida el módulo implementado `OptionsModule`.
     - Lo consumen las clases implementadas `JwtTokenSigner`, `JwtAccessTokenGenerator`,
       `RefreshTokenGenerator` y el módulo implementado `JwtAuthenticationModule`.

102. **`RabbitMqOptions`** — objeto de configuración enlazado desde variables de entorno
     (`RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`) con validación de
     anotaciones de datos en el arranque.

     > **Figura: Diagrama de herencia para `RabbitMqOptions`**
     - Clase sellada **raíz**.

     > **Figura: Diagrama de colaboración para `RabbitMqOptions`**
     - Lo enlaza y valida el módulo implementado `OptionsModule`.
     - Lo consumen las clases implementadas `RabbitMqEventPublisher` y `RabbitMqHealthCheck`.

103. **`SubscriptionGrpcOptions`** — objeto de configuración con el secreto compartido del servidor
     gRPC (`SUBSCRIPTION_GRPC_SECRET`), validado en el arranque.

     > **Figura: Diagrama de herencia para `SubscriptionGrpcOptions`**
     - Clase sellada **raíz**.

     > **Figura: Diagrama de colaboración para `SubscriptionGrpcOptions`**
     - Lo enlaza y valida el módulo implementado `OptionsModule`.
     - Lo consume la clase implementada `TenantSubscriptionGrpcService`.

### Logging estructurado

104. **`LogEvents`** — clase estática parcial que centraliza todos los mensajes de log compilados en
     tiempo de compilación con `LoggerMessage`: autenticación, outbox, gestión de usuarios y academias
     y servidor gRPC.

     > **Figura: Diagrama de herencia para `LogEvents`**
     - Clase estática **raíz**.

     > **Figura: Diagrama de colaboración para `LogEvents`**
     - La consumen las clases implementadas `AuthenticationService`, `UserRegistrationService`,
       `UserDirectoryService`, `TenantService`, `OutboxPublisher`, `RabbitMqEventPublisher` y
       `TenantSubscriptionGrpcService`.

### Composición de módulos

105. **`IServiceModule`** — contrato de un módulo que **registra servicios** en el contenedor de
     inyección de dependencias durante el arranque.

     > **Figura: Diagrama de herencia para `IServiceModule`**
     - Es una interfaz **raíz**.

     > **Figura: Diagrama de colaboración para `IServiceModule`**
     - Su operación de registro recibe los tipos externos `IServiceCollection` e `IConfiguration`.
     - La implementan todos los módulos de registro del servicio (entradas 108 a 122).

106. **`IAppModule`** — contrato de un módulo que **configura la canalización** de la aplicación.

     > **Figura: Diagrama de herencia para `IAppModule`**
     - Es una interfaz **raíz**.

     > **Figura: Diagrama de colaboración para `IAppModule`**
     - Su operación de configuración recibe el tipo externo `WebApplication`.
     - La implementan los módulos que intervienen en la fase de aplicación.

107. **`ModuleHost`** — anfitrión estático que descubre los módulos por reflexión sobre
     `Backend.Modules` y los ejecuta ordenados por su propiedad `Order`.

     > **Figura: Diagrama de herencia para `ModuleHost`**
     - Clase estática **raíz**.

     > **Figura: Diagrama de colaboración para `ModuleHost`**
     - Descubre y orquesta las interfaces implementadas `IServiceModule` e `IAppModule`.
     - Recibe los tipos externos `WebApplicationBuilder` (fase de registro) y `WebApplication`
       (fase de configuración).

108. **`DatabaseSeeder`** — clase estática que, cuando la variable de entorno `SEED_DB=true`,
     abre una conexión dedicada, trunca todas las tablas y carga los datos de semilla antes del
     arranque del servicio web.

     > **Figura: Diagrama de herencia para `DatabaseSeeder`**
     - Clase estática **raíz**.

     > **Figura: Diagrama de colaboración para `DatabaseSeeder`**
     - Lo invoca `Program` antes de `WebApplication.CreateBuilder`.
     - Usa la clase implementada `DBConnector` para la cadena de conexión y la clase implementada
       `DBInjector` para el truncado e inyección.

109. **`SecretsValidationModule`** — módulo de orden -100 que valida, antes que cualquier otro, que
     la clave privada RSA y la clave pública RSA sean válidas (fallo rápido).

     > **Figura: Diagrama de herencia para `SecretsValidationModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `SecretsValidationModule`**
     - Usa la clase auxiliar interna implementada `SecretsValidation` que importa y exporta las
       claves con el tipo externo `RSA`.
     - Lee los secretos del tipo externo `IConfiguration`.

110. **`OptionsModule`** — módulo de orden 10 que enlaza las tres opciones tipadas del servicio
     (`JwtOptions`, `RabbitMqOptions`, `SubscriptionGrpcOptions`) con validación de anotaciones de
     datos en el arranque.

     > **Figura: Diagrama de herencia para `OptionsModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `OptionsModule`**
     - Enlaza las clases implementadas `JwtOptions`, `RabbitMqOptions` y `SubscriptionGrpcOptions`
       desde `IConfiguration`.

111. **`RequestCorrelationModule`** — módulo de orden 12 que inyecta en la canalización un
     middleware que propaga o genera el identificador de correlación `X-Correlation-Id` y lo añade
     al alcance de log estructurado.

     > **Figura: Diagrama de herencia para `RequestCorrelationModule`**
     - Implementa la interfaz implementada `IAppModule`.

     > **Figura: Diagrama de colaboración para `RequestCorrelationModule`**
     - Activa un middleware inline sobre el tipo externo `WebApplication`.

112. **`ForwardedHeadersModule`** — módulo de orden 20/20 que registra y activa el middleware de
     cabeceras reenviadas del gateway (esquema, anfitrión y dirección de origen reales).

     > **Figura: Diagrama de herencia para `ForwardedHeadersModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `ForwardedHeadersModule`**
     - Configura el tipo externo de opciones de cabeceras reenviadas y activa su middleware.

113. **`HttpContextModule`** — módulo de orden 30 que registra el acceso al contexto de la
     petición HTTP.

     > **Figura: Diagrama de herencia para `HttpContextModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `HttpContextModule`**
     - Registra la interfaz externa `IHttpContextAccessor`, de la que depende la clase implementada
       `ClaimContext`.

114. **`ClaimsLogScopeModule`** — módulo de orden 35 que inyecta un middleware que, para peticiones
     autenticadas, añade `TenantId`, `UserId` y `Role` al alcance de log estructurado de cada
     petición.

     > **Figura: Diagrama de herencia para `ClaimsLogScopeModule`**
     - Implementa la interfaz implementada `IAppModule`.

     > **Figura: Diagrama de colaboración para `ClaimsLogScopeModule`**
     - Activa un middleware inline que usa las constantes de la clase implementada `AuthClaims` para
       leer los claims del principal autenticado.

115. **`PersistenceModule`** — módulo de orden 40 que registra `MySqlConnection` como scoped (una
     conexión por petición) y `IUnitOfWork` como scoped.

     > **Figura: Diagrama de herencia para `PersistenceModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `PersistenceModule`**
     - Usa la clase implementada `DBConnector` para la cadena de conexión.
     - Registra la interfaz externa `IUnitOfWork` contra la clase externa `MySqlUnitOfWork` del
       paquete `DAMA.Software.MySqlUnitOfWork`.

116. **`PasswordHashingModule`** — módulo de orden 45 que configura el número de iteraciones
     PBKDF2 y registra `IPasswordHasher<User>` como singleton.

     > **Figura: Diagrama de herencia para `PasswordHashingModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `PasswordHashingModule`**
     - Registra la interfaz externa `IPasswordHasher<User>` contra la clase externa `PasswordHasher<User>`.
     - Consume la entidad implementada `User` como parámetro de tipo genérico.

117. **`JwtAuthenticationModule`** — módulo de orden 50/30 que registra `IJwtTokenSigner`,
     `IAccessTokenGenerator` y `IRefreshTokenGenerator` como singletons, configura la validación
     del portador JWT con la clave pública RSA y, en la fase de aplicación, activa `UseAuthentication`.

     > **Figura: Diagrama de herencia para `JwtAuthenticationModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `JwtAuthenticationModule`**
     - Registra las clases implementadas `JwtTokenSigner`, `JwtAccessTokenGenerator` y
       `RefreshTokenGenerator`.
     - Usa las constantes de la clase implementada `AuthClaims` para configurar
       `NameClaimType` y `RoleClaimType`.
     - Carga la clave pública RSA con el tipo externo `RSA` para `TokenValidationParameters`.

118. **`ValidationModule`** — módulo de orden 70 que registra todos los validadores FluentValidation
     del ensamblado.

     > **Figura: Diagrama de herencia para `ValidationModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `ValidationModule`**
     - Registra la familia de clases implementadas validadoras mediante
       `AddValidatorsFromAssemblyContaining<Program>()`.

119. **`AutoRegisteredServicesModule`** — módulo de orden 80 que usa Scrutor para registrar
     automáticamente, por prefijo de espacio de nombres, los servicios concretos, los objetos de
     acceso a datos concretos, los builders y la implementación de claims.

     > **Figura: Diagrama de herencia para `AutoRegisteredServicesModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `AutoRegisteredServicesModule`**
     - Registra todas las clases de los namespaces `Backend.Services.Concrete`,
       `Backend.DB.Daos.Concrete`, `Backend.Claims` y `Backend.Builders` contra sus interfaces,
       incluyendo la clase implementada `ClaimContext` contra `IClaimContext`.

120. **`OutboxProducerModule`** — módulo de orden 93 que registra `RabbitMqEventPublisher` como
     singleton y los dos trabajos del outbox como servicios alojados.

     > **Figura: Diagrama de herencia para `OutboxProducerModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `OutboxProducerModule`**
     - Registra la clase implementada `RabbitMqEventPublisher` contra `IEventPublisher`.
     - Registra los trabajos implementados `OutboxPublisher` y `OutboxJanitor` como
       `IHostedService`.

121. **`SubscriptionMaintenanceModule`** — módulo de orden 95 que registra el limpiador de
     suscripciones vencidas como servicio alojado.

     > **Figura: Diagrama de herencia para `SubscriptionMaintenanceModule`**
     - Implementa la interfaz implementada `IServiceModule`.

     > **Figura: Diagrama de colaboración para `SubscriptionMaintenanceModule`**
     - Registra el trabajo implementado `SubscriptionExpiryJanitor` como `IHostedService`.

122. **`GrpcServerModule`** — módulo de orden 100/110 que registra el servidor gRPC y, en la fase
     de aplicación, mapea el servicio `TenantSubscriptionGrpcService` como anónimo (la autenticación
     es por secreto compartido en cabecera gRPC, no por portador JWT).

     > **Figura: Diagrama de herencia para `GrpcServerModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `GrpcServerModule`**
     - En la fase de configuración, mapea la clase implementada `TenantSubscriptionGrpcService`.

123. **`MvcModule`** — módulo de orden 200/100 que registra los controladores con el filtro global
     de validación y, en la fase de aplicación, mapea los controladores.

     > **Figura: Diagrama de herencia para `MvcModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `MvcModule`**
     - Registra la clase implementada `FluentValidationActionFilter` como filtro global de MVC.

124. **`AuthorizationModule`** — módulo de orden 40/40 que define la política de autorización de
     denegar por defecto (requiere usuario autenticado) y activa el middleware de autorización.

     > **Figura: Diagrama de herencia para `AuthorizationModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `AuthorizationModule`**
     - Construye la política con el tipo externo `AuthorizationPolicyBuilder`.

125. **`ProblemDetailsModule`** — módulo de orden 210/200 que normaliza las respuestas de error y
     activa el manejador de excepciones de ASP.NET Core.

     > **Figura: Diagrama de herencia para `ProblemDetailsModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `ProblemDetailsModule`**
     - Registra el servicio externo de detalles de problema y activa su middleware.

126. **`HealthCheckModule`** — módulo de orden 220/5 que registra las dos comprobaciones de
     dependencias externas y mapea los dos puntos de acceso de disponibilidad: `/health` (liveness,
     sin comprobaciones) y `/health/ready` (readiness, solo sondas etiquetadas con `"ready"`).

     > **Figura: Diagrama de herencia para `HealthCheckModule`**
     - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

     > **Figura: Diagrama de colaboración para `HealthCheckModule`**
     - Registra las clases implementadas `DatabaseHealthCheck` y `RabbitMqHealthCheck` usando los
       nombres generados por `ExternalCheckNaming` con el enumerado `ExternalDependency`.
     - Usa la clase implementada `ReadinessResponseWriter` como delegado de escritura de respuesta.

> **Nota sobre `SecretsValidation`.** Es una clase auxiliar **interna** y estática que acompaña a
> `SecretsValidationModule` (entrada 109); valida claves RSA con el tipo externo
> `System.Security.Cryptography.RSA`. Doxygen la grafica junto a su módulo en el diagrama de
> colaboración de la entrada 109.

---

## Comandos de demostración

```bash
# Tipos implementados en Auth (lo que Doxygen diagrama)
find apps/Auth/Backend -name "*.cs" -not -path "*/obj/*" | sort

# Relaciones de herencia (qué implementa cada clase / interfaz)
grep -rn "class .*:\|interface " apps/Auth/Backend --include=*.cs | grep -v "/obj/"

# Generar los grafos de jerarquía, herencia y colaboración del servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
#   salida: extra/graphics/out/doxygen/html/
```
