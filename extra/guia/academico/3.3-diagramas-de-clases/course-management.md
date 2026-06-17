# 3.3.3.8 Diagramado del servicio CourseManagement

> CourseManagement es el servicio con **mayor superficie de dominio** del monorepo: gestiona cursos,
> grupos de clase, clases periódicas (Scheduleds) y clases únicas (Uniques), publica eventos de
> dominio hacia RabbitMQ mediante el patrón outbox, garantiza idempotencia en las creaciones y
> actúa como **servidor gRPC del contrato `ClassExistence`** que el servicio Attendance consume sobre
> TLS mutuo. Su rasgo más distintivo respecto a los demás
> servicios es la **capa de aplicación con mediador propio** (`Application/`), que desacopla los
> controladores de la lógica de negocio a través de contratos `ICommandHandler<TCommand, TResult>` e
> `IQueryHandler<TQuery, TResult>`, organizados por agregado. Todos los grafos citados los genera
> **Doxygen** desde el código (`UML_LOOK`, `GRAPHICAL_HIERARCHY`, `COLLABORATION_GRAPH`); aquí solo
> se titulan las figuras y se explican las **relaciones** que muestran, sin describir métodos.
>
> **Generar las figuras:** `cd extra/graphics && docker compose --profile docs run --rm doxygen`
> (salida en `extra/graphics/out/doxygen/html/`).

---

## a) Jerarquía gráfica

CourseManagement organiza su código en **namespaces** donde cada carpeta corresponde a un rol
estructural preciso. A diferencia de los servicios de tipo `Services/`, aquí la lógica de negocio
reside en la **capa de aplicación** (`Application/`): manejadores de comandos y consultas
descubiertos por el contenedor y despachos por los controladores a través de los contratos del
mediador. Los agregados de clases usan **nomenclatura en plural sin sufijo** (`Scheduleds`,
`Uniques`) tal como lo establece la convención de este proyecto. El servicio es servidor gRPC de
dos contratos, usa AutoMapper para la proyección de entidades a DTOs, y delega las inserciones
idempotentes a una infraestructura de aplicación (`Application/Infrastructure/`) antes de escribir
en la base de datos.

Los grupos estructurales del servicio son:

- `Backend.Application.Mediator` — los **contratos del mediador propio**: las interfaces genéricas
  de manejador de comandos y manejador de consultas que desacoplan los controladores de la lógica
  de caso de uso.
- `Backend.Application.Courses`, `Backend.Application.Groups`, `Backend.Application.Scheduleds`,
  `Backend.Application.Uniques`, `Backend.Application.Schedules` — los **manejadores de caso de
  uso por agregado**: cada archivo aloja un par (mensaje + manejador) que implementa el contrato del
  mediador correspondiente.
- `Backend.Application.Infrastructure` — la **infraestructura de aplicación**: el coordinador
  genérico de creación de clases, el ejecutor de transacciones idempotentes, sus interfaces y sus
  resultados discriminados internos.
- `Backend.AutoMapperProfiles` — los **perfiles de AutoMapper**: configuraciones de proyección de
  entidad a DTO, uno por agregado.
- `Backend.Builders` — los **constructores de entidades y eventos**: cuatro pares
  interfaz-implementación que centralizan la fabricación de entidades y de eventos de outbox.
- `Backend.Claims` — la **abstracción de claims**: lectura tipada del token con fallo rápido.
- `Backend.Controllers` — los **cuatro controladores de la API REST**: uno por agregado principal
  (Course, ClassGroup, ScheduledClass, UniqueClass), que despachan a los manejadores del mediador.
- `Backend.DB.Daos.Abstract.Single` — las **interfaces de acceso a datos**: contratos ISP por
  agregado sobre las tablas Course, ClassGroup, ScheduledClass, UniqueClass, CourseIdempotency y
  OutboxEvent.
- `Backend.DB.Daos.Concrete.Single` — las **implementaciones de acceso a datos**: los objetos de
  acceso a datos concretos que heredan de las clases base del paquete externo `SQLDaosPackage`.
- `Backend.DB.Injectors` — los **inyectores de semilla**: siete clases que cargan datos desde CSV
  con el patrón `LOAD DATA INFILE`.
- `Backend.DB.Utils` — las **utilidades de base de datos**: el conector de cadena de conexión y el
  coordinador de siembra.
- `Backend.Dtos` — los **datos de entrada y salida**: contratos de la API agrupados por agregado y
  dirección, con interfaces ISP sobre los DTOs compartidos.
- `Backend.Entities` — el **modelo persistente**: nueve clases y registros que mapean las tablas y
  los transportadores de solo lectura.
- `Backend.Events` — los **eventos de dominio**: los dos eventos que CourseManagement publica
  (`CourseDeleted`, `ClassDeleted`) con sus respectivas cargas anidadas.
- `Backend.ExternalCheck` — las **sondas de disponibilidad**: las comprobaciones de base de datos y
  RabbitMQ, el enumerado de dependencias, el convenio de nomenclatura y el escritor de respuesta.
- `Backend.Filters` — los **filtros de MVC**: el filtro de validación automática de FluentValidation
  y el atributo de selección de conjunto de reglas.
- `Backend.Grpc.Services` — el **servidor gRPC**: `ClassExistenceGrpcService`, que implementa el
  contrato `ClassExistence` del paquete `DAMA.Software.GrpcContracts`.
- `Backend.Logging` — el **log estructurado compilado**: la clase estática parcial de mensajes
  generados en tiempo de compilación.
- `Backend.Mapping` — la **utilidad de mapeo JSON**: el analizador de la columna `Teachers`
  almacenada como JSON en la base de datos.
- `Backend.Messaging` — el **publicador de RabbitMQ**: el publicador singleton del outbox.
- `Backend.Modules` — la **composición por módulos**: los contratos `IServiceModule` e `IAppModule`,
  el anfitrión `ModuleHost` y diecinueve módulos concretos.
- `Backend.Options` — las **opciones tipadas**: el objeto de configuración de RabbitMQ enlazado
  desde variables de entorno.
- `Backend.Results` — las **uniones discriminadas de resultado**: tipos cerrados que los manejadores
  devuelven a los controladores, más el meta-registro de existencia de clase para gRPC.
- `Backend.Security` — los **componentes de seguridad**: las constantes de claims y roles, y el
  filtro de autorización por nivel de suscripción.
- `Backend.Validators` — los **validadores de entrada**: una clase por DTO de entrada, descubiertos
  automáticamente por FluentValidation.
- `Backend.Workers` — los **trabajos en segundo plano**: el publicador del outbox y el limpiador de
  eventos publicados.

**No aplican** los siguientes grupos que sí existen en otros servicios:

- No hay **capa de servicio convencional** (`Services/Abstract`, `Services/Concrete`): CourseManagement
  usa el patrón `Application/` con manejadores de mediador, que cumple la misma función de encapsular
  la lógica de negocio pero de forma más granular.
- No hay **consumidor de eventos de RabbitMQ** (`Messaging/Consumer`, `BackgroundService` de
  consumo): CourseManagement solo produce eventos (`CourseDeleted`, `ClassDeleted`); no consume.
- No hay **cliente gRPC saliente**: CourseManagement es servidor gRPC, no cliente; los contratos
  generados por `Grpc.Tools` se usan como clases base del servidor.
- No hay **paginación** (`Pagination/`): las listas que devuelve CourseManagement son completas por
  tenant, sin desplazamiento paginado.

A continuación, un título de figura por grupo estructural y la función del grupo:

> **Figura: Jerarquía gráfica de los contratos del mediador (`ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`)**

Define el contrato genérico que los controladores usan para despachar casos de uso: cualquier clase
que implemente `ICommandHandler<TCommand, TResult>` o `IQueryHandler<TQuery, TResult>` puede ser
inyectada directamente y despachada sin necesidad de un bus de eventos de terceros.

> **Figura: Jerarquía gráfica de los manejadores de caso de uso por agregado (familia `*Handler` y mensajes `*Command`/`*Query`)**

Veintitrés pares (mensaje + manejador) que implementan el mediador: cinco por el agregado Courses
(crear, listar, obtener por identificador, actualizar, verificar existencia), cuatro por Groups
(crear, listar, listar por profesor, actualizar, eliminar), cinco por Scheduleds (crear, actualizar,
eliminar, buscar, transferir), cinco por Uniques (crear, actualizar, eliminar, buscar, transferir)
y cuatro por Schedules (horario de curso, de profesor, de tenant, más el ensamblador de horario y
el resolvedor de semana). Cada manejador inyecta los Daos que necesita, el `IClaimContext` y, cuando
aplica, un constructor o el coordinador de creación idempotente.

> **Figura: Jerarquía gráfica de la infraestructura de aplicación (`IClassCreationCoordinator<TEntity>`, `ClassCreationCoordinator<TEntity>`, `IClassAggregateWriter<TEntity>`, `IIdempotentTransactionExecutor`, `IdempotentTransactionExecutor`, `ClassCreationOutcome<TEntity>`, `IdempotentInsertOutcome<TEntity>`)**

Abstrae el protocolo de creación idempotente de clases: el coordinador genérico verifica la
existencia del curso padre, delega en el ejecutor de transacciones la inserción con la clave de
idempotencia y, si la referencia ya fue procesada, recupera la entidad previa. Los resultados
discriminados internos (`ClassCreationOutcome<TEntity>`, `IdempotentInsertOutcome<TEntity>`)
comunican el desenlace al manejador que los invocó.

> **Figura: Jerarquía gráfica de los perfiles de AutoMapper (`CourseProfile`, `ScheduledClassProfile`, `UniqueClassProfile`, `ClassGroupProfile`, `ClassTeacherProfile`)**

Registra las proyecciones entidad → DTO que los manejadores usan para construir los objetos de
salida de la API sin lógica de mapeo en línea.

> **Figura: Jerarquía gráfica de los constructores (`ICourseBuilder`, `CourseBuilder`, `IClassGroupBuilder`, `ClassGroupBuilder`, `IClassBuilder`, `ClassBuilder`, `ICourseEventBuilder`, `CourseEventBuilder`)**

Cuatro pares interfaz-implementación que centralizan la fabricación de entidades y eventos con
efectos colaterales (generación de identificadores, serialización de payload): el constructor de
cursos, el de grupos, el de clases (que crea tanto `ScheduledClass` como `UniqueClass`) y el
constructor de eventos de outbox (que serializa `CourseDeletedEvent` y `ClassDeletedEvent`).

> **Figura: Jerarquía gráfica de la abstracción de claims (`IClaimContext`, `ClaimContext`, `MissingClaimException`)**

Expone al resto del servicio los ocho datos de identidad del token con fallo rápido: los seis base
comunes más `IndexCoreServicesPyramid` y `SubscriptionExpiresAt`, estos dos necesarios para el
filtro `RequiresServiceTierAttribute`.

> **Figura: Jerarquía gráfica de los controladores (`CourseController`, `ClassGroupController`, `ScheduledClassController`, `UniqueClassController`)**

Los cuatro puntos de entrada REST del servicio: cada uno inyecta directamente los manejadores del
mediador que necesita y delega en ellos toda la lógica; la autorización por rol y nivel de
suscripción se aplica en los atributos de cada acción.

> **Figura: Jerarquía gráfica de las interfaces ISP de acceso a datos (`ICourseDao`, `IClassGroupDao`, `IScheduledClassDao`, `IUniqueClassDao`, `ICourseIdempotencyDao`, `IOutboxEventDao`)**

Contratos estrechos sobre las seis tablas del servicio: `ICourseDao` e `IClassGroupDao` extienden
`ISingleDao<T>` del paquete externo; `IScheduledClassDao` e `IUniqueClassDao` además implementan
`IClassAggregateWriter<TEntity>` de la infraestructura de aplicación; `ICourseIdempotencyDao` y
`IOutboxEventDao` son interfaces raíz sin herencia de paquete.

> **Figura: Jerarquía gráfica de los objetos de acceso a datos concretos (`CourseDao`, `ClassGroupDao`, `ScheduledClassDao`, `UniqueClassDao`, `CourseIdempotencyDao`, `OutboxEventDao`)**

Las seis implementaciones concretas que heredan de las clases base del paquete `SQLDaosPackage`
(`MySQLSingleDao<T>`, `MySQLBaseDao<T>`) e implementan sus respectivas interfaces ISP;
`OutboxEventDao` no hereda de clase base del paquete y usa las utilidades externas
`MySqlOutboxLeaseHelper` y `OutboxLeaseDescriptor<OutboxEvent>` del paquete `DAMA.Software.MySqlOutbox`.

> **Figura: Jerarquía gráfica de los inyectores de semilla (familia `*Injector`)**

Siete clases que heredan de la clase externa `DataInjector` (del paquete `SQLDaosPackage`) y
encapsulan cada uno el comando `LOAD DATA INFILE` para su tabla CSV correspondiente: cursos, grupos,
clases periódicas, profesores de clases periódicas, clases únicas, profesores de clases únicas y
registros de idempotencia.

> **Figura: Jerarquía gráfica de las utilidades de base de datos (`DBConnector`, `DBInjector`)**

Clases estáticas de infraestructura de datos: `DBConnector` resuelve la cadena de conexión desde
la variable de entorno `DB_CONNECTION_STRING` y `DBInjector` coordina el truncado de tablas y la
inyección de datos de semilla en el orden correcto.

> **Figura: Jerarquía gráfica de las interfaces ISP de DTOs de clases (`ICourseData`, `IScheduledClassPayload`, `IUniqueClassPayload`)**

Tres interfaces raíz que abstraen el subconjunto de campos compartidos entre los DTOs de creación
y actualización de sus respectivos agregados, permitiendo que los constructores operen sobre el
contrato en lugar del tipo concreto.

> **Figura: Jerarquía gráfica del DTO compartido de profesores y los DTOs de entrada/salida por agregado**

Familia de DTOs que definen los contratos de entrada y salida de la API: `ClassTeacherDto`
(compartido entre Scheduleds y Uniques), y los DTOs específicos de Courses, Groups, Scheduleds,
Uniques y Schedules, organizados en subcarpetas `Input/` y `Output/`.

> **Figura: Jerarquía gráfica del modelo persistente (`Course`, `ClassGroup`, `ScheduledClass`, `UniqueClass`, `ClassTeacher`, `CourseIdempotency`, `OutboxEvent`, `ScheduledClassAttendanceControl`, `UniqueClassAttendanceControl`, `ScheduledClassUpdate`, `UniqueClassUpdate`)**

Once tipos que representan las tablas y los registros de transferencia de la base de datos:
`Course`, `ClassGroup`, `ScheduledClass` y `UniqueClass` implementan la interfaz externa `IEntity`
del paquete `SQLDaosPackage`; `OutboxEvent` implementa además `IOutboxEvent` del paquete
`DAMA.Software.MySqlOutbox`; los registros de control de asistencia y los registros de actualización
son transportadores de solo lectura que usan los Daos y los manejadores.

> **Figura: Jerarquía gráfica de los eventos de dominio (`CourseDeletedEvent`, `CourseDeletedEventData`, `ClassDeletedEvent`, `ClassDeletedEventData`)**

Los cuatro registros sellados que `CourseEventBuilder` serializa como carga del outbox: dos eventos
con sus datos anidados, uno al eliminar un curso (incluye la lista de identificadores de clases
eliminadas) y otro al eliminar una clase individual.

> **Figura: Jerarquía gráfica de las comprobaciones de disponibilidad (`DatabaseHealthCheck`, `RabbitMqHealthCheck`, `ExternalDependency`, `ExternalCheckNaming`, `ReadinessResponseWriter`)**

Las dos comprobaciones de dependencias externas, el enumerado que nombra las dependencias, el
convenio de nomenclatura de las sondas y el escritor de respuesta JSON para el punto de acceso
`/health/ready`.

> **Figura: Jerarquía gráfica de los filtros de MVC (`FluentValidationActionFilter`, `RuleSetAttribute`)**

El filtro global que ejecuta automáticamente los validadores FluentValidation antes de cada acción,
y el atributo que permite indicar qué conjunto de reglas aplicar en acciones específicas.

> **Figura: Jerarquía gráfica del servidor gRPC (`ClassExistenceGrpcService`)**

La implementación del contrato gRPC que Attendance consume sobre TLS: `ClassExistenceGrpcService`
verifica la existencia de clases periódicas y únicas despachando a los manejadores `FindScheduledClassHandler`
y `FindUniqueClassHandler`. Hereda de la clase base generada por `Grpc.Tools` del paquete
`DAMA.Software.GrpcContracts`.

> **Figura: Jerarquía gráfica del log estructurado compilado (`LogEvents`)**

Clase estática parcial con los mensajes de log compilados por el generador de fuentes de
`LoggerMessage`: centraliza los eventos de diagnóstico de RabbitMQ y del ciclo de vida del outbox.

> **Figura: Jerarquía gráfica de la utilidad de mapeo JSON (`ClassTeachersJsonParser`)**

Clase estática que deserializa la columna `Teachers` almacenada como JSON en las tablas
`ScheduledClass` y `UniqueClass` hacia una lista de entidades `ClassTeacher`; los Daos la
invocan al mapear el lector de la base de datos.

> **Figura: Jerarquía gráfica del publicador de RabbitMQ (`IEventPublisher`, `RabbitMqEventPublisher`)**

El contrato y el publicador singleton que implementa `IEventPublisher` e `IAsyncDisposable`: gestiona
una conexión perezosa al broker con inicialización protegida por semáforo, declara el intercambio de
tópicos duradero `dama.events` en el primer uso y publica con confirmaciones del publicador habilitadas.

> **Figura: Jerarquía gráfica de la composición de módulos (`IServiceModule`, `IAppModule`, `ModuleHost`, y los módulos concretos)**

El mismo patrón de arranque modular que los demás servicios: dos contratos, un anfitrión que los
descubre por reflexión y diecinueve módulos concretos, incluyendo `GrpcServerModule` (exclusivo de
este servicio), `OpenGenericHandlersModule` (registra el coordinador genérico abierto),
`AutoMapperModule` y `OutboxProducerModule`, más `DatabaseSeeder` que se ejecuta antes del arranque
cuando `SEED_DB=true`.

> **Figura: Jerarquía gráfica de las opciones tipadas (`RabbitMqOptions`)**

El objeto de configuración de RabbitMQ enlazado desde variables de entorno en el arranque, con
validación de campos requeridos vía anotaciones de datos.

> **Figura: Jerarquía gráfica de los resultados discriminados por agregado (familia `*Result`)**

Veintitrés tipos cerrados que los manejadores devuelven a los controladores en lugar de excepciones
o valores nulos, más `ClassExistenceMeta` (registro de metadatos de existencia de clase usado por
los servicios gRPC): organizados por agregado en subcarpetas `Courses/`, `Groups/`, `Scheduleds/`,
`Uniques/` y `Schedules/`.

> **Figura: Jerarquía gráfica de los componentes de seguridad (`AuthClaims`, `UserRoles`, `RequiresServiceTierAttribute`)**

Las constantes de nombres de claims y roles, y el filtro de autorización por nivel de suscripción
de la pirámide de servicios: `RequiresServiceTierAttribute` implementa `IAuthorizationFilter` y lee
`IndexCoreServicesPyramid` y `SubscriptionExpiresAt` del `IClaimContext`.

> **Figura: Jerarquía gráfica de los validadores (familia `*DtoValidator` y extensiones)**

Familia de doce validadores FluentValidation, uno por cada DTO de entrada, más la extensión
compartida `TeacherListRuleExtensions` y el validador compartido `ClassTeacherDtoValidator`: todos
extienden `AbstractValidator<T>` del paquete externo FluentValidation y son descubiertos y
registrados automáticamente por `ValidationModule`.

> **Figura: Jerarquía gráfica de los trabajos en segundo plano (`OutboxPublisher`, `OutboxJanitor`)**

Dos trabajos `BackgroundService`: el publicador del outbox que arrienda lotes de eventos pendientes
y los entrega a RabbitMQ en paralelo, y el limpiador que elimina eventos publicados con más de siete
días de antigüedad.

---

## b) Diagramas de herencia y colaboración

Una entrada por cada clase/interfaz **implementada** en CourseManagement. Las clases/interfaces
externas (del framework .NET, ASP.NET Core, NuGet o del paquete `grpc-contracts` generado por
`Grpc.Tools`) se **referencian** desde las viñetas, sin entrada propia. Las familias estructuralmente
idénticas se consolidan en una entrada de familia.

### Contratos del mediador

1. **`ICommandHandler<TCommand, TResult>`** — contrato genérico de un manejador de comando: recibe
   un mensaje de mutación y devuelve un resultado tipado.

   > **Figura: Diagrama de herencia para `ICommandHandler<TCommand, TResult>`**
   - Es una interfaz genérica **raíz**: no hereda de ninguna otra.

   > **Figura: Diagrama de colaboración para `ICommandHandler<TCommand, TResult>`**
   - La implementan todos los manejadores de comando del servicio (`CreateCourseHandler`,
     `DeleteCourseHandler`, `UpdateCourseHandler`, `CreateClassGroupHandler`, `UpdateClassGroupHandler`,
     `DeleteClassGroupHandler`, `CreateScheduledClassHandler`, `UpdateScheduledClassHandler`,
     `DeleteScheduledClassHandler`, `TransferScheduledClassHandler`, `CreateUniqueClassHandler`,
     `UpdateUniqueClassHandler`, `DeleteUniqueClassHandler`, `TransferUniqueClassHandler`).
   - Los controladores implementados la inyectan y la despachan para cada acción de escritura.

2. **`IQueryHandler<TQuery, TResult>`** — contrato genérico de un manejador de consulta: recibe un
   mensaje de lectura y devuelve un resultado tipado.

   > **Figura: Diagrama de herencia para `IQueryHandler<TQuery, TResult>`**
   - Es una interfaz genérica **raíz**.

   > **Figura: Diagrama de colaboración para `IQueryHandler<TQuery, TResult>`**
   - La implementan todos los manejadores de consulta del servicio (`ListCoursesHandler`,
     `GetCourseByIdHandler`, `ListClassGroupsHandler`,
     `ListTeacherClassGroupsHandler`, `FindScheduledClassHandler`, `FindUniqueClassHandler`,
     `GetCourseScheduleHandler`, `GetTeacherScheduleHandler`, `GetTenantScheduleHandler`).
   - Los controladores implementados y los servicios gRPC implementados la inyectan para despachar
     lecturas.

### Manejadores de caso de uso — Courses

3. **Familia `CreateCourseCommand` / `CreateCourseHandler`, `ListCoursesQuery` / `ListCoursesHandler`, `GetCourseByIdQuery` / `GetCourseByIdHandler`, `UpdateCourseCommand` / `UpdateCourseHandler`, `DeleteCourseCommand` / `DeleteCourseHandler`** — cinco pares (mensaje + manejador) que cubren el ciclo completo del agregado Course: crear con idempotencia, listar por tenant, obtener por identificador, renombrar y eliminar en transacción (cascada a clases hijas). Todos comparten la misma estructura relacional:

   > **Figura: Diagrama de herencia para `CreateCourseHandler` / `ListCoursesHandler` / `GetCourseByIdHandler` / `UpdateCourseHandler` / `DeleteCourseHandler`**
   - Cada manejador de comando implementa la interfaz implementada `ICommandHandler<TCommand, TResult>`
     con sus tipos concretos; cada manejador de consulta implementa `IQueryHandler<TQuery, TResult>`.

   > **Figura: Diagrama de colaboración para la familia de manejadores de Courses**
   - Reciben por inyección de dependencias la interfaz implementada `ICourseDao`.
   - `CreateCourseHandler` recibe además la interfaz implementada `IIdempotentTransactionExecutor`,
     la interfaz implementada `ICourseBuilder` y la interfaz externa `IMapper` (AutoMapper).
   - `DeleteCourseHandler` recibe además la interfaz implementada `ICourseEventBuilder` y la
     interfaz implementada `IOutboxEventDao`, para insertar el evento `CourseDeleted` en el outbox
     dentro de la misma transacción.
   - Todos leen el identificador del tenant con la interfaz implementada `IClaimContext`.
   - Devuelven los registros implementados de `Results/Courses/` correspondientes.

### Manejadores de caso de uso — Groups

4. **Familia `CreateClassGroupCommand` / `CreateClassGroupHandler`, `ListClassGroupsQuery` / `ListClassGroupsHandler`, `ListTeacherClassGroupsQuery` / `ListTeacherClassGroupsHandler`, `UpdateClassGroupCommand` / `UpdateClassGroupHandler`, `DeleteClassGroupCommand` / `DeleteClassGroupHandler`** — cinco pares que cubren el ciclo del agregado ClassGroup: crear, listar por tenant, listar por profesor, renombrar y eliminar (solo si el grupo está vacío de clases).

   > **Figura: Diagrama de herencia para la familia de manejadores de Groups**
   - Cada manejador de comando implementa `ICommandHandler<TCommand, TResult>`; cada manejador de
     consulta implementa `IQueryHandler<TQuery, TResult>`.

   > **Figura: Diagrama de colaboración para la familia de manejadores de Groups**
   - Reciben por inyección la interfaz implementada `IClassGroupDao`.
   - `CreateClassGroupHandler` recibe además la interfaz implementada `IClassGroupBuilder` y la
     interfaz externa `IMapper`.
   - `ListClassGroupsHandler` y `ListTeacherClassGroupsHandler` reciben la interfaz externa `IMapper`.
   - Todos leen el identificador del tenant (y del usuario, para el listado por profesor) con la
     interfaz implementada `IClaimContext`.
   - Devuelven los registros implementados de `Results/Groups/` correspondientes.

### Manejadores de caso de uso — Scheduleds

5. **Familia `CreateScheduledClassCommand` / `CreateScheduledClassHandler`, `UpdateScheduledClassCommand` / `UpdateScheduledClassHandler`, `DeleteScheduledClassCommand` / `DeleteScheduledClassHandler`, `FindScheduledClassQuery` / `FindScheduledClassHandler`, `TransferScheduledClassCommand` / `TransferScheduledClassHandler`** — cinco pares que cubren el ciclo de las clases periódicas: crear con idempotencia y coordinación del agregado, actualizar con verificación de solapamiento, eliminar publicando evento al outbox, buscar por identificador y fecha (para gRPC) y transferir a otro grupo.

   > **Figura: Diagrama de herencia para la familia de manejadores de Scheduleds**
   - Cada manejador de comando implementa `ICommandHandler<TCommand, TResult>`; el manejador de
     consulta implementa `IQueryHandler<TQuery, TResult>`.

   > **Figura: Diagrama de colaboración para la familia de manejadores de Scheduleds**
   - Reciben por inyección las interfaces implementadas `IScheduledClassDao` y `IClassGroupDao`.
   - `CreateScheduledClassHandler` recibe además la interfaz implementada
     `IClassCreationCoordinator<ScheduledClass>`, la interfaz implementada `IClassBuilder` y la
     interfaz externa `IMapper`.
   - `DeleteScheduledClassHandler` recibe además la interfaz implementada `ICourseEventBuilder` y
     la interfaz implementada `IOutboxEventDao`.
   - Todos leen el identificador del tenant con la interfaz implementada `IClaimContext`.
   - Devuelven los registros implementados de `Results/Scheduleds/` correspondientes.

### Manejadores de caso de uso — Uniques

6. **Familia `CreateUniqueClassCommand` / `CreateUniqueClassHandler`, `UpdateUniqueClassCommand` / `UpdateUniqueClassHandler`, `DeleteUniqueClassCommand` / `DeleteUniqueClassHandler`, `FindUniqueClassQuery` / `FindUniqueClassHandler`, `TransferUniqueClassCommand` / `TransferUniqueClassHandler`** — cinco pares estructuralmente análogos a los de Scheduleds pero sobre la tabla `UniqueClass`: la diferencia de dominio reside en que las clases únicas tienen una fecha concreta en lugar de un día de semana recurrente.

   > **Figura: Diagrama de herencia para la familia de manejadores de Uniques**
   - Cada manejador de comando implementa `ICommandHandler<TCommand, TResult>`; el manejador de
     consulta implementa `IQueryHandler<TQuery, TResult>`.

   > **Figura: Diagrama de colaboración para la familia de manejadores de Uniques**
   - Reciben por inyección las interfaces implementadas `IUniqueClassDao` y `IClassGroupDao`.
   - `CreateUniqueClassHandler` recibe además la interfaz implementada
     `IClassCreationCoordinator<UniqueClass>`, la interfaz implementada `IClassBuilder` y la
     interfaz externa `IMapper`.
   - `DeleteUniqueClassHandler` recibe además la interfaz implementada `ICourseEventBuilder` y la
     interfaz implementada `IOutboxEventDao`.
   - Todos leen el identificador del tenant con la interfaz implementada `IClaimContext`.
   - Devuelven los registros implementados de `Results/Uniques/` correspondientes.

### Manejadores de caso de uso — Schedules

7. **`GetCourseScheduleQuery` / `GetCourseScheduleHandler`** — par que obtiene el horario semanal
   de un curso: combina las clases periódicas fijas y las clases únicas de la semana apuntada.

   > **Figura: Diagrama de herencia para `GetCourseScheduleHandler`**
   - Implementa la interfaz implementada `IQueryHandler<GetCourseScheduleQuery, GetCourseScheduleResult>`.

   > **Figura: Diagrama de colaboración para `GetCourseScheduleHandler`**
   - Recibe por inyección las interfaces implementadas `IScheduledClassDao`, `IUniqueClassDao` y
     `IScheduleAssembler`.
   - Devuelve el registro implementado `GetCourseScheduleResult.Found`.

8. **`GetTeacherScheduleQuery` / `GetTeacherScheduleHandler`** — par que obtiene el horario semanal
   de un profesor: filtra por el identificador de usuario extraído del token.

   > **Figura: Diagrama de herencia para `GetTeacherScheduleHandler`**
   - Implementa la interfaz implementada `IQueryHandler<GetTeacherScheduleQuery, GetTeacherScheduleResult>`.

   > **Figura: Diagrama de colaboración para `GetTeacherScheduleHandler`**
   - Recibe por inyección las interfaces implementadas `IScheduledClassDao`, `IUniqueClassDao`,
     `IScheduleAssembler` y `IClaimContext`.
   - Devuelve el registro implementado `GetTeacherScheduleResult.Found`.

9. **`GetTenantScheduleQuery` / `GetTenantScheduleHandler`** — par que obtiene el horario semanal
   completo del tenant para la semana apuntada.

   > **Figura: Diagrama de herencia para `GetTenantScheduleHandler`**
   - Implementa la interfaz implementada `IQueryHandler<GetTenantScheduleQuery, GetTenantScheduleResult>`.

   > **Figura: Diagrama de colaboración para `GetTenantScheduleHandler`**
   - Recibe por inyección las interfaces implementadas `IScheduledClassDao`, `IUniqueClassDao`,
     `IScheduleAssembler` y `IClaimContext`.
   - Devuelve el registro implementado `GetTenantScheduleResult.Found`.

### Infraestructura de aplicación

10. **`IClassCreationCoordinator<TEntity>`** — contrato genérico del coordinador de creación de
    clases: verifica que el curso padre exista y delega en el ejecutor idempotente la inserción
    transaccional del agregado de clase.

    > **Figura: Diagrama de herencia para `IClassCreationCoordinator<TEntity>`**
    - Es una interfaz genérica **raíz**.

    > **Figura: Diagrama de colaboración para `IClassCreationCoordinator<TEntity>`**
    - La implementa la clase implementada `ClassCreationCoordinator<TEntity>`.
    - La consumen los manejadores `CreateScheduledClassHandler` y `CreateUniqueClassHandler`.
    - Su operación devuelve el registro implementado `ClassCreationOutcome<TEntity>`.

11. **`ClassCreationCoordinator<TEntity>`** — coordinador genérico que orquesta la verificación
    del curso padre, la inserción idempotente de la entidad de clase y la inserción de los
    profesores asociados.

    > **Figura: Diagrama de herencia para `ClassCreationCoordinator<TEntity>`**
    - Implementa la interfaz implementada `IClassCreationCoordinator<TEntity>`.

    > **Figura: Diagrama de colaboración para `ClassCreationCoordinator<TEntity>`**
    - Recibe por inyección las interfaces implementadas `ICourseDao`, `IIdempotentTransactionExecutor`
      y `IClassAggregateWriter<TEntity>`.
    - Produce y devuelve el registro implementado `ClassCreationOutcome<TEntity>`.

12. **`IClassAggregateWriter<TEntity>`** — contrato de las tres operaciones de escritura que un
    DAO de clase debe exponer para ser orquestado por el coordinador: crear la entidad, insertar
    un profesor asociado y recuperarla por identificador.

    > **Figura: Diagrama de herencia para `IClassAggregateWriter<TEntity>`**
    - Es una interfaz genérica **raíz**.

    > **Figura: Diagrama de colaboración para `IClassAggregateWriter<TEntity>`**
    - La implementan las interfaces implementadas `IScheduledClassDao` e `IUniqueClassDao` (y por
      tanto sus Daos concretos), que así se vuelven compatibles con el coordinador genérico.
    - La consume la clase implementada `ClassCreationCoordinator<TEntity>`.

13. **`IIdempotentTransactionExecutor`** — contrato del ejecutor de transacciones idempotentes:
    registra la referencia externa, ejecuta la función de inserción dentro de una transacción y,
    en caso de referencia duplicada, recupera la entidad previamente insertada.

    > **Figura: Diagrama de herencia para `IIdempotentTransactionExecutor`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IIdempotentTransactionExecutor`**
    - La implementa la clase implementada `IdempotentTransactionExecutor`.
    - La consumen `CreateCourseHandler` y la clase implementada `ClassCreationCoordinator<TEntity>`.
    - Su operación devuelve el registro implementado `IdempotentInsertOutcome<TEntity>`.

14. **`IdempotentTransactionExecutor`** — ejecutor que abre una transacción con la interfaz
    externa `IUnitOfWork`, intenta registrar la referencia de idempotencia y, si la inserción tiene
    éxito, confirma la transacción; si ya existe, devuelve la entidad previa.

    > **Figura: Diagrama de herencia para `IdempotentTransactionExecutor`**
    - Implementa la interfaz implementada `IIdempotentTransactionExecutor`.

    > **Figura: Diagrama de colaboración para `IdempotentTransactionExecutor`**
    - Recibe por inyección la interfaz externa `IUnitOfWork` (del paquete `DAMA.Software.MySqlUnitOfWork`)
      y la interfaz implementada `ICourseIdempotencyDao`.
    - Produce y devuelve el registro implementado `IdempotentInsertOutcome<TEntity>`.
    - Construye la entidad implementada `CourseIdempotency` para registrar la referencia.

15. **`ClassCreationOutcome<TEntity>`** — unión discriminada interna del resultado de la
    coordinación de creación de clase: `Created` (inserción nueva), `Replayed` (referencia ya
    procesada) o `CourseMissing` (el curso padre no existe).

    > **Figura: Diagrama de herencia para `ClassCreationOutcome<TEntity>`**
    - Registro abstracto genérico **raíz** con tres casos sellados anidados.

    > **Figura: Diagrama de colaboración para `ClassCreationOutcome<TEntity>`**
    - Lo produce la clase implementada `ClassCreationCoordinator<TEntity>`.
    - Lo emparejan por patrón los manejadores `CreateScheduledClassHandler` y
      `CreateUniqueClassHandler`.
    - El caso `Created` y el caso `Replayed` encapsulan la entidad de tipo `TEntity`.

16. **`IdempotentInsertOutcome<TEntity>`** — unión discriminada interna del resultado de la
    transacción idempotente: `Inserted` (nueva inserción), `Replayed` (referencia duplicada
    recuperada) o `InsertFailed` (la inserción devolvió nulo).

    > **Figura: Diagrama de herencia para `IdempotentInsertOutcome<TEntity>`**
    - Registro abstracto genérico **raíz** con tres casos sellados anidados.

    > **Figura: Diagrama de colaboración para `IdempotentInsertOutcome<TEntity>`**
    - Lo produce la clase implementada `IdempotentTransactionExecutor`.
    - Lo empareja por patrón `ClassCreationCoordinator<TEntity>` para traducirlo a
      `ClassCreationOutcome<TEntity>`, y `CreateCourseHandler` para traducirlo a
      `CreateCourseResult`.

### Perfiles de AutoMapper

17. **Familia `CourseProfile`, `ScheduledClassProfile`, `UniqueClassProfile`, `ClassGroupProfile`, `ClassTeacherProfile`** — cinco perfiles de AutoMapper, cada uno hereda de la clase externa `Profile` y registra las proyecciones de entidad a DTO y, en el caso de `ClassTeacherProfile`, también la proyección inversa (`ClassTeacherDto` → `ClassTeacher`). Todos son estructuralmente idénticos en su relación.

    > **Figura: Diagrama de herencia para `CourseProfile` / `ScheduledClassProfile` / `UniqueClassProfile` / `ClassGroupProfile` / `ClassTeacherProfile`**
    - Cada uno hereda de la clase externa `Profile` (de AutoMapper).

    > **Figura: Diagrama de colaboración para la familia de perfiles**
    - Los registra la clase implementada `AutoMapperModule` en el contenedor de servicios.
    - Los consume la interfaz externa `IMapper` inyectada en los manejadores que producen DTOs
      de salida.

### Constructores

18. **`ICourseBuilder`** — contrato de la fabricación de la entidad `Course` a partir de los datos
    de entrada y el identificador de tenant.

    > **Figura: Diagrama de herencia para `ICourseBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ICourseBuilder`**
    - La implementa la clase implementada `CourseBuilder`.
    - La consume `CreateCourseHandler`.
    - Su operación produce la entidad implementada `Course`.

19. **`CourseBuilder`** — implementación que construye un `Course` con identificador nuevo generado.

    > **Figura: Diagrama de herencia para `CourseBuilder`**
    - Implementa la interfaz implementada `ICourseBuilder`.

    > **Figura: Diagrama de colaboración para `CourseBuilder`**
    - Consume la interfaz implementada `ICourseData` (que `CreateCourseDto` y `UpdateCourseDto`
      implementan) para acceder al nombre sin ver el tipo concreto.
    - Produce la entidad implementada `Course`.

20. **`IClassGroupBuilder`** — contrato de la fabricación de la entidad `ClassGroup`.

    > **Figura: Diagrama de herencia para `IClassGroupBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IClassGroupBuilder`**
    - La implementa la clase implementada `ClassGroupBuilder`.
    - La consume `CreateClassGroupHandler`.

21. **`ClassGroupBuilder`** — implementación que construye un `ClassGroup` con identificador nuevo.

    > **Figura: Diagrama de herencia para `ClassGroupBuilder`**
    - Implementa la interfaz implementada `IClassGroupBuilder`.

    > **Figura: Diagrama de colaboración para `ClassGroupBuilder`**
    - Produce la entidad implementada `ClassGroup`.

22. **`IClassBuilder`** — contrato unificado de la fabricación de las dos variantes de clase:
    `ScheduledClass` y `UniqueClass`.

    > **Figura: Diagrama de herencia para `IClassBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IClassBuilder`**
    - La implementa la clase implementada `ClassBuilder`.
    - La consumen `CreateScheduledClassHandler` y `CreateUniqueClassHandler`.
    - Sus operaciones consumen las interfaces implementadas `IScheduledClassPayload` e
      `IUniqueClassPayload` para acceder a los campos compartidos de cada variante.

23. **`ClassBuilder`** — implementación que construye `ScheduledClass` y `UniqueClass` con
    identificadores nuevos e inicializa la lista de profesores.

    > **Figura: Diagrama de herencia para `ClassBuilder`**
    - Implementa la interfaz implementada `IClassBuilder`.

    > **Figura: Diagrama de colaboración para `ClassBuilder`**
    - Produce las entidades implementadas `ScheduledClass` y `UniqueClass`.
    - Recibe listas de la entidad implementada `ClassTeacher` como parámetro.

24. **`ICourseEventBuilder`** — contrato de la fabricación de los dos eventos de dominio que
    CourseManagement publica: eliminación de curso y eliminación de clase.

    > **Figura: Diagrama de herencia para `ICourseEventBuilder`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ICourseEventBuilder`**
    - La implementa la clase implementada `CourseEventBuilder`.
    - La consumen `DeleteCourseHandler`, `DeleteScheduledClassHandler` y `DeleteUniqueClassHandler`.
    - Sus operaciones producen la entidad implementada `OutboxEvent`.

25. **`CourseEventBuilder`** — implementación que serializa los eventos de dominio como JSON dentro
    de la entidad `OutboxEvent`, lista para ser insertada en la tabla del outbox.

    > **Figura: Diagrama de herencia para `CourseEventBuilder`**
    - Implementa la interfaz implementada `ICourseEventBuilder`.

    > **Figura: Diagrama de colaboración para `CourseEventBuilder`**
    - Construye y serializa los registros implementados `CourseDeletedEvent` y `ClassDeletedEvent`
      (y sus respectivos registros de datos `CourseDeletedEventData`, `ClassDeletedEventData`) como
      JSON en el campo `Payload`.
    - Produce la entidad implementada `OutboxEvent`.

### Abstracción de claims

26. **`IClaimContext`** — contrato que define la lectura tipada de los ocho claims del token: los
    seis base comunes a todos los servicios (`TenantId`, `TenantName`, `TenantTimezone`, `UserId`,
    `UserName`, `Role`) más `IndexCoreServicesPyramid` y `SubscriptionExpiresAt`, necesarios para
    el filtro `RequiresServiceTierAttribute`.

    > **Figura: Diagrama de herencia para `IClaimContext`**
    - Es una interfaz **raíz**: no hereda de ninguna otra.

    > **Figura: Diagrama de colaboración para `IClaimContext`**
    - Sus propiedades son de tipos primitivos del lenguaje (`Guid`, `string`, `int`, `DateTime`),
      por lo que el grafo no muestra dependencia con tipos implementados; es un contrato puro.

27. **`ClaimContext`** — implementación que lee cada claim del usuario autenticado con memorización
    en la primera lectura y fallo rápido ante ausencia o formato incorrecto.

    > **Figura: Diagrama de herencia para `ClaimContext`**
    - Implementa la interfaz implementada `IClaimContext`.

    > **Figura: Diagrama de colaboración para `ClaimContext`**
    - Recibe por inyección de dependencias la interfaz externa `IHttpContextAccessor` para acceder
      a los claims del principal autenticado.
    - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim que lee.
    - Construye y lanza la clase implementada `MissingClaimException` cuando un claim falta o no
      puede analizarse al tipo esperado.

28. **`MissingClaimException`** — excepción específica que señala un claim requerido ausente o
    malformado en el token.

    > **Figura: Diagrama de herencia para `MissingClaimException`**
    - Hereda de la clase externa `System.Exception`.

    > **Figura: Diagrama de colaboración para `MissingClaimException`**
    - La construye y lanza la clase implementada `ClaimContext`.
    - La captura la clase implementada `RequiresServiceTierAttribute` para tratar la ausencia del
      claim de nivel de suscripción como nivel cero.

### Controladores de la API

29. **`CourseController`** — expone los puntos de acceso REST del agregado Course: listar, crear,
    obtener por identificador, actualizar y eliminar.

    > **Figura: Diagrama de herencia para `CourseController`**
    - Hereda de la clase externa `ControllerBase` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `CourseController`**
    - Recibe por inyección los manejadores implementados de Courses a través de las interfaces
      `ICommandHandler` e `IQueryHandler` con sus tipos concretos.
    - Empareja por patrón los registros implementados de `Results/Courses/` para devolver el
      código de estado HTTP apropiado.

30. **`ClassGroupController`** — expone los puntos de acceso REST del agregado ClassGroup: listar,
    listar por profesor, crear, actualizar y eliminar.

    > **Figura: Diagrama de herencia para `ClassGroupController`**
    - Hereda de la clase externa `ControllerBase`.

    > **Figura: Diagrama de colaboración para `ClassGroupController`**
    - Recibe por inyección los manejadores implementados de Groups.
    - Empareja los registros implementados de `Results/Groups/`.

31. **`ScheduledClassController`** — expone los puntos de acceso REST del agregado ScheduledClass:
    crear, actualizar, eliminar y transferir a otro grupo; y el horario del curso, del profesor y
    del tenant.

    > **Figura: Diagrama de herencia para `ScheduledClassController`**
    - Hereda de la clase externa `ControllerBase`.

    > **Figura: Diagrama de colaboración para `ScheduledClassController`**
    - Recibe por inyección los manejadores implementados de Scheduleds y Schedules.
    - Usa la clase implementada `WeekResolver` para calcular la semana actual del tenant antes de
      despachar las consultas de horario.
    - Empareja los registros implementados de `Results/Scheduleds/` y `Results/Schedules/`.

32. **`UniqueClassController`** — expone los puntos de acceso REST del agregado UniqueClass:
    crear, actualizar, eliminar y transferir a otro grupo.

    > **Figura: Diagrama de herencia para `UniqueClassController`**
    - Hereda de la clase externa `ControllerBase`.

    > **Figura: Diagrama de colaboración para `UniqueClassController`**
    - Recibe por inyección los manejadores implementados de Uniques.
    - Empareja los registros implementados de `Results/Uniques/`.

### Interfaces ISP de acceso a datos

33. **`ICourseDao`** — contrato de las operaciones sobre la tabla Course: crear (con y sin
    transacción), listar por tenant, obtener por identificador y tenant, verificar existencia,
    actualizar nombre y eliminar en transacción (cascada a clases hijas).

    > **Figura: Diagrama de herencia para `ICourseDao`**
    - Extiende la interfaz externa `ISingleDao<Course>` del paquete `SQLDaosPackage`.

    > **Figura: Diagrama de colaboración para `ICourseDao`**
    - La implementa la clase implementada `CourseDao`.
    - La consumen `CreateCourseHandler`, `ListCoursesHandler`, `GetCourseByIdHandler`,
      `UpdateCourseHandler`, `DeleteCourseHandler` y la clase implementada
      `ClassCreationCoordinator<TEntity>` (esta última verifica la existencia del curso antes de crear
      una clase).
    - Sus operaciones trabajan con la entidad implementada `Course`.

34. **`IClassGroupDao`** — contrato de las operaciones sobre la tabla ClassGroup: crear por tenant,
    actualizar, eliminar si está vacío, listar por tenant, listar por profesor y verificar
    existencia.

    > **Figura: Diagrama de herencia para `IClassGroupDao`**
    - Extiende la interfaz externa `ISingleDao<ClassGroup>` del paquete `SQLDaosPackage`.

    > **Figura: Diagrama de colaboración para `IClassGroupDao`**
    - La implementa la clase implementada `ClassGroupDao`.
    - La consumen los manejadores de Groups y los manejadores que crean o transfieren clases.
    - Sus operaciones trabajan con la entidad implementada `ClassGroup`.

35. **`IScheduledClassDao`** — contrato de las operaciones sobre la tabla ScheduledClass: las
    operaciones de `IClassAggregateWriter<ScheduledClass>`, más reemplazar profesores, listar por
    curso, por tenant, por profesor, buscar existencia con metadatos, actualizar, verificar
    solapamiento, transferir, eliminar y listar identificadores por curso en transacción.

    > **Figura: Diagrama de herencia para `IScheduledClassDao`**
    - Extiende la interfaz externa `ISingleDao<ScheduledClass>` del paquete `SQLDaosPackage`.
    - Extiende la interfaz implementada `IClassAggregateWriter<ScheduledClass>`.

    > **Figura: Diagrama de colaboración para `IScheduledClassDao`**
    - La implementa la clase implementada `ScheduledClassDao`.
    - La consumen los manejadores de Scheduleds, los manejadores de Schedules y la clase
      implementada `ClassExistenceGrpcService`.
    - Sus operaciones trabajan con la entidad implementada `ScheduledClass`, la entidad
      implementada `ClassTeacher` y el registro implementado `ClassExistenceMeta`.

36. **`IUniqueClassDao`** — contrato análogo a `IScheduledClassDao` pero sobre la tabla UniqueClass:
    las operaciones de `IClassAggregateWriter<UniqueClass>` más las específicas de la variante
    única (listar por semana, por profesor en semana, verificar solapamiento por fecha, etc.).

    > **Figura: Diagrama de herencia para `IUniqueClassDao`**
    - Extiende la interfaz externa `ISingleDao<UniqueClass>` del paquete `SQLDaosPackage`.
    - Extiende la interfaz implementada `IClassAggregateWriter<UniqueClass>`.

    > **Figura: Diagrama de colaboración para `IUniqueClassDao`**
    - La implementa la clase implementada `UniqueClassDao`.
    - La consumen los manejadores de Uniques, los manejadores de Schedules y la clase
      implementada `ClassExistenceGrpcService`.
    - Sus operaciones trabajan con la entidad implementada `UniqueClass`, la entidad implementada
      `ClassTeacher` y el registro implementado `ClassExistenceMeta`.

37. **`ICourseIdempotencyDao`** — contrato de las dos operaciones del libro mayor de idempotencia:
    intentar registrar una nueva entrada (devuelve falso ante duplicado) y recuperar una entrada
    por referencia externa.

    > **Figura: Diagrama de herencia para `ICourseIdempotencyDao`**
    - Es una interfaz **raíz** (no extiende `ISingleDao<T>` deliberadamente, por ISP).

    > **Figura: Diagrama de colaboración para `ICourseIdempotencyDao`**
    - La implementa la clase implementada `CourseIdempotencyDao`.
    - La consume la clase implementada `IdempotentTransactionExecutor`.
    - Sus operaciones trabajan con la entidad implementada `CourseIdempotency`.

38. **`IOutboxEventDao`** — contrato de las operaciones del outbox de eventos: inserción
    transaccional, arrendamiento de lotes pendientes, marcado de publicados, registro de fallos y
    eliminación de publicados antiguos.

    > **Figura: Diagrama de herencia para `IOutboxEventDao`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IOutboxEventDao`**
    - La implementa la clase implementada `OutboxEventDao`.
    - La consumen los manejadores de eliminación (`DeleteCourseHandler`,
      `DeleteScheduledClassHandler`, `DeleteUniqueClassHandler`) para insertar eventos, la clase
      implementada `OutboxPublisher` para arrendar y marcar, y la clase implementada `OutboxJanitor`
      para eliminar publicados.
    - Sus operaciones trabajan con la entidad implementada `OutboxEvent`.

### Objetos de acceso a datos concretos

39. **`CourseDao`** — objeto de acceso a datos concreto de la tabla `Course`: implementa `ICourseDao`
    con procedimientos almacenados para las consultas de negocio y sentencias directas para las
    escrituras transaccionales.

    > **Figura: Diagrama de herencia para `CourseDao`**
    - Hereda de la clase externa `MySQLSingleDao<Course>` del paquete `SQLDaosPackage`.
    - Implementa la interfaz implementada `ICourseDao`.

    > **Figura: Diagrama de colaboración para `CourseDao`**
    - Recibe por inyección de dependencias la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `Course`.
    - Usa la clase auxiliar externa `MySqlTransactionContextAccessor` para extraer la transacción
      nativa de la abstracción `ITransactionContext`.

40. **`ClassGroupDao`** — objeto de acceso a datos concreto de la tabla `ClassGroup`: implementa
    `IClassGroupDao` con procedimientos almacenados.

    > **Figura: Diagrama de herencia para `ClassGroupDao`**
    - Hereda de la clase externa `MySQLSingleDao<ClassGroup>`.
    - Implementa la interfaz implementada `IClassGroupDao`.

    > **Figura: Diagrama de colaboración para `ClassGroupDao`**
    - Recibe por inyección la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `ClassGroup`.

41. **`ScheduledClassDao`** — objeto de acceso a datos concreto de la tabla `ScheduledClass`:
    implementa `IScheduledClassDao` (que a su vez extiende `IClassAggregateWriter<ScheduledClass>`)
    con procedimientos almacenados y sentencias directas para las operaciones transaccionales.

    > **Figura: Diagrama de herencia para `ScheduledClassDao`**
    - Hereda de la clase externa `MySQLSingleDao<ScheduledClass>`.
    - Implementa la interfaz implementada `IScheduledClassDao`.

    > **Figura: Diagrama de colaboración para `ScheduledClassDao`**
    - Recibe por inyección la conexión externa `MySqlConnection`.
    - Produce y consume las entidades implementadas `ScheduledClass` y `ClassTeacher`.
    - Usa la clase auxiliar externa `MySqlTransactionContextAccessor`.
    - Usa la clase estática implementada `ClassTeachersJsonParser` para deserializar la columna
      `Teachers` almacenada como JSON.
    - Produce el registro implementado `ClassExistenceMeta` como resultado de la operación de
      búsqueda por existencia.

42. **`UniqueClassDao`** — objeto de acceso a datos concreto de la tabla `UniqueClass`: análogo a
    `ScheduledClassDao` pero sobre la variante de clase única.

    > **Figura: Diagrama de herencia para `UniqueClassDao`**
    - Hereda de la clase externa `MySQLSingleDao<UniqueClass>`.
    - Implementa la interfaz implementada `IUniqueClassDao`.

    > **Figura: Diagrama de colaboración para `UniqueClassDao`**
    - Recibe por inyección la conexión externa `MySqlConnection`.
    - Produce y consume las entidades implementadas `UniqueClass` y `ClassTeacher`.
    - Usa `MySqlTransactionContextAccessor` y la clase estática implementada
      `ClassTeachersJsonParser`.
    - Produce el registro implementado `ClassExistenceMeta`.

43. **`CourseIdempotencyDao`** — objeto de acceso a datos concreto de la tabla `CourseIdempotency`:
    intenta insertar con detección de duplicado por clave primaria compuesta (trampa MySQL 1062) y
    recupera por referencia externa.

    > **Figura: Diagrama de herencia para `CourseIdempotencyDao`**
    - Hereda de la clase externa `MySQLBaseDao<CourseIdempotency>` del paquete `SQLDaosPackage`.
    - Implementa la interfaz implementada `ICourseIdempotencyDao`.

    > **Figura: Diagrama de colaboración para `CourseIdempotencyDao`**
    - Recibe por inyección la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `CourseIdempotency`.
    - Usa `MySqlTransactionContextAccessor` para la inserción transaccional.

44. **`OutboxEventDao`** — objeto de acceso a datos concreto de la tabla `outbox_events`: implementa
    `IOutboxEventDao` usando las utilidades del paquete `DAMA.Software.MySqlOutbox` para el
    arrendamiento de lotes y el registro de fallos.

    > **Figura: Diagrama de herencia para `OutboxEventDao`**
    - Implementa la interfaz implementada `IOutboxEventDao`.
    - No hereda de una clase base del paquete `SQLDaosPackage`: sus operaciones son todas
      sentencias directas o delegadas al paquete externo de outbox.

    > **Figura: Diagrama de colaboración para `OutboxEventDao`**
    - Recibe por inyección la conexión externa `MySqlConnection`.
    - Produce y consume la entidad implementada `OutboxEvent`.
    - Usa las utilidades externas `MySqlOutboxLeaseHelper` y `OutboxLeaseDescriptor<OutboxEvent>`
      del paquete `DAMA.Software.MySqlOutbox`.

### Inyectores de semilla

45. **Familia `CourseInjector`, `ClassGroupInjector`, `ScheduledClassInjector`, `ScheduledClassTeacherInjector`, `UniqueClassInjector`, `UniqueClassTeacherInjector`, `CourseIdempotencyInjector`** — siete inyectores de datos de semilla, uno por cada tabla o relación que el sembrado carga desde archivos CSV. Todos comparten la misma relación estructural.

    > **Figura: Diagrama de herencia para `CourseInjector` / `ClassGroupInjector` / `ScheduledClassInjector` / `ScheduledClassTeacherInjector` / `UniqueClassInjector` / `UniqueClassTeacherInjector` / `CourseIdempotencyInjector`**
    - Cada uno hereda de la clase externa `DataInjector` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para la familia de inyectores**
    - Los orquesta y ejecuta la clase implementada `DBInjector` a través de la interfaz externa
      `IDataInjector`.

### Utilidades de base de datos

46. **`DBConnector`** — clase estática que resuelve la cadena de conexión desde la variable de
    entorno `DB_CONNECTION_STRING` o, como respaldo, desde `dbsettings.json`.

    > **Figura: Diagrama de herencia para `DBConnector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBConnector`**
    - La consumen la clase implementada `PersistenceModule` (para registrar la conexión en el
      contenedor) y la clase implementada `DatabaseHealthCheck` (para verificar la disponibilidad
      de la base de datos).

47. **`DBInjector`** — clase estática que coordina el truncado de tablas y la inyección de datos
    de semilla en el orden correcto para CourseManagement.

    > **Figura: Diagrama de herencia para `DBInjector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBInjector`**
    - La ejecuta la clase implementada `DatabaseSeeder` durante el arranque cuando `SEED_DB=true`.
    - Invoca los siete inyectores de la familia a través de la interfaz externa `IDataInjector`.

### Interfaces ISP de DTOs

48. **`ICourseData`** — contrato del campo compartido por los DTOs de creación y actualización de
    curso: el nombre del curso.

    > **Figura: Diagrama de herencia para `ICourseData`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `ICourseData`**
    - La implementan las clases implementadas `CreateCourseDto` y `UpdateCourseDto`.
    - La consume la interfaz implementada `ICourseBuilder` para operar sin ver el tipo concreto.

49. **`IScheduledClassPayload`** — contrato de los campos compartidos entre creación y actualización
    de clases periódicas: día de semana, límite de estudiantes, hora de inicio y hora de fin.

    > **Figura: Diagrama de herencia para `IScheduledClassPayload`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IScheduledClassPayload`**
    - La implementan las clases implementadas `CreateScheduledClassDto` y `UpdateScheduledClassDto`.
    - La consume la interfaz implementada `IClassBuilder` para construir un `ScheduledClass`.

50. **`IUniqueClassPayload`** — contrato análogo para clases únicas: fecha, límite de estudiantes,
    hora de inicio y hora de fin.

    > **Figura: Diagrama de herencia para `IUniqueClassPayload`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IUniqueClassPayload`**
    - La implementan las clases implementadas `CreateUniqueClassDto` y `UpdateUniqueClassDto`.
    - La consume la interfaz implementada `IClassBuilder` para construir un `UniqueClass`.

### Datos de transferencia

51. **`ClassTeacherDto`** — DTO compartido que representa un profesor asignado a una clase: par
    de identificador y nombre de usuario.

    > **Figura: Diagrama de herencia para `ClassTeacherDto`**
    - Clase **raíz**: no hereda de ninguna otra ni implementa interfaces.

    > **Figura: Diagrama de colaboración para `ClassTeacherDto`**
    - La incluyen las interfaces implementadas `IScheduledClassPayload` e `IUniqueClassPayload`
      como lista en los DTOs de entrada de clases.
    - La proyecta la clase externa `IMapper` a la entidad implementada `ClassTeacher` y viceversa
      (perfil implementado `ClassTeacherProfile`).

52. **Familia de DTOs de entrada y salida por agregado (`CreateCourseDto`, `UpdateCourseDto`, `GetCourseDto`, `CreateClassGroupDto`, `UpdateClassGroupDto`, `TransferClassDto`, `GetClassGroupDto`, `CreateScheduledClassDto`, `UpdateScheduledClassDto`, `GetScheduledClassDto`, `CourseScheduleParametersDto`, `WeekPointerDto`, `GetCourseScheduleDto`, `CreateUniqueClassDto`, `UpdateUniqueClassDto`, `GetUniqueClassDto`)** — dieciséis DTOs organizados bajo `Dtos/{Courses,Groups,Scheduleds,Schedules,Uniques}/{Input,Output}/`. Los DTOs de entrada son recibidos por los controladores y validados por los validadores; los de salida son producidos por AutoMapper desde las entidades. `TransferClassDto` es compartido entre Scheduleds y Uniques. Todos son clases o registros **raíz** sin herencia propia, salvo los que implementan sus interfaces ISP de payload.

    > **Figura: Diagrama de herencia para la familia de DTOs de entrada**
    - `CreateCourseDto` y `UpdateCourseDto` implementan la interfaz implementada `ICourseData`.
    - `CreateScheduledClassDto` y `UpdateScheduledClassDto` implementan la interfaz implementada
      `IScheduledClassPayload`.
    - `CreateUniqueClassDto` y `UpdateUniqueClassDto` implementan la interfaz implementada
      `IUniqueClassPayload`.
    - Los demás DTOs son clases **raíz** sin herencia.

    > **Figura: Diagrama de colaboración para la familia de DTOs de entrada**
    - Los DTOs de entrada los reciben los controladores implementados y los validan los validadores
      implementados.
    - Los DTOs de salida los construye la interfaz externa `IMapper` desde las entidades
      implementadas correspondientes.
    - Los DTOs de salida los devuelven los manejadores implementados envueltos en sus registros de
      resultado.

### Modelo persistente

53. **`Course`** — entidad persistente del curso: identificador, nombre y referencia de tenant.

    > **Figura: Diagrama de herencia para `Course`**
    - Implementa la interfaz externa `IEntity` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para `Course`**
    - La persiste la clase implementada `CourseDao`.
    - La construye la clase implementada `CourseBuilder`.
    - La proyecta la interfaz externa `IMapper` hacia la clase implementada `GetCourseDto`.

54. **`ClassGroup`** — entidad persistente del grupo de clase: identificador, nombre y referencia
    de tenant.

    > **Figura: Diagrama de herencia para `ClassGroup`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `ClassGroup`**
    - La persiste la clase implementada `ClassGroupDao`.
    - La construye la clase implementada `ClassGroupBuilder`.
    - La proyecta la interfaz externa `IMapper` hacia la clase implementada `GetClassGroupDto`.

55. **`ScheduledClass`** — entidad persistente de la clase periódica: identificador, día de semana,
    límite de estudiantes, horario, y referencias a curso, grupo y tenant; con propiedades no
    persistidas `GroupName` y `Teachers`.

    > **Figura: Diagrama de herencia para `ScheduledClass`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `ScheduledClass`**
    - La persiste la clase implementada `ScheduledClassDao`.
    - La construye la clase implementada `ClassBuilder`.
    - La proyecta la interfaz externa `IMapper` hacia la clase implementada `GetScheduledClassDto`.
    - La contiene la clase implementada `ScheduledClassDao` como propiedad de la lista `Teachers`
      de tipo `ClassTeacher`.

56. **`UniqueClass`** — entidad persistente de la clase única: identificador, fecha, límite de
    estudiantes, horario, y referencias a curso, grupo y tenant; con propiedades no persistidas
    `GroupName` y `Teachers`.

    > **Figura: Diagrama de herencia para `UniqueClass`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `UniqueClass`**
    - La persiste la clase implementada `UniqueClassDao`.
    - La construye la clase implementada `ClassBuilder`.
    - La proyecta la interfaz externa `IMapper` hacia la clase implementada `GetUniqueClassDto`.

57. **`ClassTeacher`** — entidad de datos que representa la asignación de un profesor a una clase:
    par de identificador y nombre; no es una entidad con tabla propia sino un objeto de valor
    persistido como columna JSON en las tablas de clase.

    > **Figura: Diagrama de herencia para `ClassTeacher`**
    - Clase **raíz**: no hereda de ninguna otra ni implementa interfaces.

    > **Figura: Diagrama de colaboración para `ClassTeacher`**
    - La deserializa la clase estática implementada `ClassTeachersJsonParser` desde la columna JSON
      de los Daos.
    - La construyen los manejadores de creación y actualización de clases a partir de la lista de
      `ClassTeacherDto` proyectada por AutoMapper.
    - La proyecta la interfaz externa `IMapper` hacia la clase implementada `ClassTeacherDto`.

58. **`CourseIdempotency`** — entidad del libro mayor de idempotencia: clave compuesta por tenant e
    identificador externo, tipo de entidad, identificador interno asignado y fecha de procesamiento.

    > **Figura: Diagrama de herencia para `CourseIdempotency`**
    - Clase **raíz**: no implementa `IEntity` porque su clave primaria es compuesta y no sigue el
      patrón de identificador único.

    > **Figura: Diagrama de colaboración para `CourseIdempotency`**
    - La persiste la clase implementada `CourseIdempotencyDao`.
    - La construye la clase implementada `IdempotentTransactionExecutor` al registrar una nueva
      referencia.

59. **`OutboxEvent`** — entidad persistente del outbox de eventos: todos los campos del esquema
    canónico (`Id`, `AggregateType`, `AggregateId`, `EventType`, `RoutingKey`, `Payload`,
    `OccurredAt`, `PublishedAt`, `LeasedUntil`, `Attempts`, `LastError`).

    > **Figura: Diagrama de herencia para `OutboxEvent`**
    - Implementa la interfaz externa `IOutboxEvent` (del paquete `DAMA.Software.MySqlOutbox`) y la
      interfaz externa `IEntity` (del paquete `SQLDaosPackage`).

    > **Figura: Diagrama de colaboración para `OutboxEvent`**
    - La construye la clase implementada `CourseEventBuilder`, serializando el evento de dominio
      como JSON en el campo `Payload`.
    - La inserta en la base de datos la clase implementada `OutboxEventDao` dentro de la
      transacción de eliminación.
    - La consumen la clase implementada `OutboxPublisher` (arrendar y publicar) y la clase
      implementada `OutboxJanitor` (eliminar publicados antiguos).
    - La publica en RabbitMQ la clase implementada `RabbitMqEventPublisher`.

60. **`ScheduledClassAttendanceControl`** — clase de datos que el DAO de clases periódicas usa como
    proyección de la tabla de control de asistencia: identificador de clase y fecha, identificador
    y nombre del estudiante.

    > **Figura: Diagrama de herencia para `ScheduledClassAttendanceControl`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `ScheduledClassAttendanceControl`**
    - La construye la clase implementada `ScheduledClassDao` al mapear resultados de consultas de
      control de asistencia; la consume el servicio de Attendance (externo, por gRPC).

61. **`UniqueClassAttendanceControl`** — clase de datos análoga para clases únicas: identificador
    de clase, identificador y nombre del estudiante (sin fecha, porque las clases únicas ya la
    tienen en la entidad).

    > **Figura: Diagrama de herencia para `UniqueClassAttendanceControl`**
    - Clase **raíz**.

    > **Figura: Diagrama de colaboración para `UniqueClassAttendanceControl`**
    - La construye la clase implementada `UniqueClassDao`; la consume el servicio de Attendance
      externo por gRPC.

62. **`ScheduledClassUpdate`** — registro inmutable que transporta los campos actualizables de una
    clase periódica: identificador, día de semana, límite, horario de inicio y fin.

    > **Figura: Diagrama de herencia para `ScheduledClassUpdate`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `ScheduledClassUpdate`**
    - Lo construye el manejador `UpdateScheduledClassHandler` a partir del DTO de entrada.
    - Lo consume la interfaz implementada `IScheduledClassDao` en su operación de actualización.

63. **`UniqueClassUpdate`** — registro inmutable análogo para clases únicas: identificador, fecha,
    límite, horario de inicio y fin.

    > **Figura: Diagrama de herencia para `UniqueClassUpdate`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `UniqueClassUpdate`**
    - Lo construye el manejador `UpdateUniqueClassHandler`.
    - Lo consume la interfaz implementada `IUniqueClassDao`.

### Eventos de dominio

64. **`CourseDeletedEvent`** — registro de evento de dominio que `CourseEventBuilder` serializa
    como carga del outbox cuando se elimina un curso.

    > **Figura: Diagrama de herencia para `CourseDeletedEvent`**
    - Es un registro sellado **raíz** (CourseManagement no define un contrato `IDomainEvent`
      explícito; la interfaz canónica la podría añadir si se estandariza en el futuro).

    > **Figura: Diagrama de colaboración para `CourseDeletedEvent`**
    - Lo construye la clase implementada `CourseEventBuilder`.
    - Contiene como propiedad el registro implementado `CourseDeletedEventData`.

65. **`CourseDeletedEventData`** — registro de datos anidado dentro de `CourseDeletedEvent`: el
    identificador del curso, el identificador del tenant y la lista de identificadores de las
    clases que se eliminaron junto con él.

    > **Figura: Diagrama de herencia para `CourseDeletedEventData`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `CourseDeletedEventData`**
    - Lo construye la clase implementada `CourseEventBuilder`.
    - Lo contiene el registro implementado `CourseDeletedEvent`.

66. **`ClassDeletedEvent`** — registro de evento de dominio que `CourseEventBuilder` serializa
    cuando se elimina una clase individual (periódica o única).

    > **Figura: Diagrama de herencia para `ClassDeletedEvent`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `ClassDeletedEvent`**
    - Lo construye la clase implementada `CourseEventBuilder`.
    - Contiene como propiedad el registro implementado `ClassDeletedEventData`.

67. **`ClassDeletedEventData`** — registro de datos anidado dentro de `ClassDeletedEvent`: el
    identificador de la clase y el identificador del tenant.

    > **Figura: Diagrama de herencia para `ClassDeletedEventData`**
    - Es un registro sellado **raíz**.

    > **Figura: Diagrama de colaboración para `ClassDeletedEventData`**
    - Lo construye la clase implementada `CourseEventBuilder`.
    - Lo contiene el registro implementado `ClassDeletedEvent`.

### Comprobaciones de disponibilidad

68. **`DatabaseHealthCheck`** — comprobación que abre una conexión MySQL y ejecuta `SELECT 1` para
    verificar que la base de datos responde.

    > **Figura: Diagrama de herencia para `DatabaseHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck` (de ASP.NET Core).

    > **Figura: Diagrama de colaboración para `DatabaseHealthCheck`**
    - Usa la clase estática implementada `DBConnector` para obtener la cadena de conexión.
    - El módulo implementado `HealthCheckModule` la registra con el nombre producido por la clase
      implementada `ExternalCheckNaming`.

69. **`RabbitMqHealthCheck`** — comprobación que intenta conectar al broker RabbitMQ para verificar
    su disponibilidad.

    > **Figura: Diagrama de herencia para `RabbitMqHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck`.

    > **Figura: Diagrama de colaboración para `RabbitMqHealthCheck`**
    - Recibe por inyección la interfaz externa `IOptions<RabbitMqOptions>` para leer las
      credenciales del broker.
    - El módulo implementado `HealthCheckModule` la registra con el nombre producido por la clase
      implementada `ExternalCheckNaming`.

70. **`ExternalDependency`** — enumerado que nombra las dos dependencias externas verificadas:
    `Database` y `RabbitMq`.

    > **Figura: Diagrama de herencia para `ExternalDependency`**
    - Es un enumerado **raíz**.

    > **Figura: Diagrama de colaboración para `ExternalDependency`**
    - Lo consume la clase implementada `ExternalCheckNaming` para componer el nombre de cada sonda.

71. **`ExternalCheckNaming`** — clase estática que compone el nombre de cada sonda de disponibilidad
    anteponiendo el nombre del servicio (`CourseManagementService`).

    > **Figura: Diagrama de herencia para `ExternalCheckNaming`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ExternalCheckNaming`**
    - La consume el módulo implementado `HealthCheckModule` para registrar cada comprobación con su
      nombre canónico.
    - Usa el enumerado implementado `ExternalDependency`.

72. **`ReadinessResponseWriter`** — clase estática que escribe la respuesta JSON del punto de
    acceso `/health/ready` con el estado y los detalles de cada comprobación.

    > **Figura: Diagrama de herencia para `ReadinessResponseWriter`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ReadinessResponseWriter`**
    - La registra el módulo implementado `HealthCheckModule` como escritor de respuesta del
      punto de acceso de disponibilidad.

### Filtros de MVC

73. **`FluentValidationActionFilter`** — filtro global que, antes de cada acción de controlador,
    localiza el validador FluentValidation correspondiente al DTO de entrada y devuelve un error
    de validación estructurado si falla.

    > **Figura: Diagrama de herencia para `FluentValidationActionFilter`**
    - Implementa la interfaz externa `IAsyncActionFilter` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `FluentValidationActionFilter`**
    - Recibe por inyección la interfaz externa `IServiceProvider` para resolver los validadores
      en tiempo de ejecución.
    - Trabaja con el atributo implementado `RuleSetAttribute` para seleccionar el conjunto de
      reglas cuando el método de acción lo especifica.

74. **`RuleSetAttribute`** — atributo de anotación que permite marcar un método de controlador con
    los nombres de los conjuntos de reglas FluentValidation que deben aplicarse.

    > **Figura: Diagrama de herencia para `RuleSetAttribute`**
    - Hereda de la clase externa `System.Attribute`.

    > **Figura: Diagrama de colaboración para `RuleSetAttribute`**
    - Lo lee la clase implementada `FluentValidationActionFilter` mediante reflexión sobre el
      descriptor de la acción.

### Servidores gRPC

75. **`ClassExistenceGrpcService`** — servidor del contrato gRPC `ClassExistence`: recibe llamadas
    de Attendance para verificar si existe una clase periódica (por identificador y fecha) o una
    clase única (por identificador), y devuelve los metadatos de horario.

    > **Figura: Diagrama de herencia para `ClassExistenceGrpcService`**
    - Hereda de la clase externa generada `ClassExistence.ClassExistenceBase` (del paquete
      `DAMA.Software.GrpcContracts`, emitida por `Grpc.Tools`).

    > **Figura: Diagrama de colaboración para `ClassExistenceGrpcService`**
    - Recibe por inyección primaria las interfaces implementadas
      `IQueryHandler<FindScheduledClassQuery, FindScheduledClassResult>` y
      `IQueryHandler<FindUniqueClassQuery, FindUniqueClassResult>`.
    - Empareja los registros implementados `FindScheduledClassResult` y `FindUniqueClassResult`
      para componer la respuesta gRPC externa `ClassExistsResponse`.

### Log estructurado

76. **`LogEvents`** — clase estática parcial con los mensajes de log compilados por el generador
    de fuentes de `LoggerMessage`: centraliza los cuatro eventos de diagnóstico del outbox y la
    conexión a RabbitMQ.

    > **Figura: Diagrama de herencia para `LogEvents`**
    - Clase estática parcial **raíz**.

    > **Figura: Diagrama de colaboración para `LogEvents`**
    - La consumen las clases implementadas `RabbitMqEventPublisher`, `OutboxPublisher` y
      `OutboxJanitor` para emitir sus eventos de diagnóstico.

### Utilidad de mapeo JSON

77. **`ClassTeachersJsonParser`** — clase estática que deserializa la columna `Teachers` (guardada
    como JSON en la base de datos) hacia una lista de entidades `ClassTeacher`.

    > **Figura: Diagrama de herencia para `ClassTeachersJsonParser`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ClassTeachersJsonParser`**
    - La consumen las clases implementadas `ScheduledClassDao` y `UniqueClassDao` al mapear cada
      fila leída del lector de la base de datos.
    - Produce listas de la entidad implementada `ClassTeacher`.

### Publicador de RabbitMQ

78. **`IEventPublisher`** — contrato del publicador de eventos al broker RabbitMQ.

    > **Figura: Diagrama de herencia para `IEventPublisher`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IEventPublisher`**
    - La implementa la clase implementada `RabbitMqEventPublisher`.
    - La consume la clase implementada `OutboxPublisher`.
    - Su operación recibe la entidad implementada `OutboxEvent`.

79. **`RabbitMqEventPublisher`** — publicador singleton que implementa `IEventPublisher` e
    `IAsyncDisposable`: gestiona una conexión perezosa al broker con inicialización protegida por
    semáforo, declara el intercambio de tópicos duradero `dama.events` y publica con confirmaciones
    del publicador habilitadas.

    > **Figura: Diagrama de herencia para `RabbitMqEventPublisher`**
    - Implementa la interfaz implementada `IEventPublisher`.
    - Implementa la interfaz externa `IAsyncDisposable`.

    > **Figura: Diagrama de colaboración para `RabbitMqEventPublisher`**
    - Recibe por inyección la interfaz externa `IOptions<RabbitMqOptions>` y la interfaz externa
      `ILogger<RabbitMqEventPublisher>`.
    - Usa las clases externas del paquete `RabbitMQ.Client` para la conexión y publicación.
    - Emite eventos de diagnóstico a través de la clase implementada `LogEvents`.
    - Publica la entidad implementada `OutboxEvent`.

### Opciones tipadas

80. **`RabbitMqOptions`** — objeto de configuración de RabbitMQ enlazado desde variables de entorno:
    anfitrión, puerto, usuario y contraseña, con validación de campos requeridos.

    > **Figura: Diagrama de herencia para `RabbitMqOptions`**
    - Clase **raíz** (sin herencia de clase base de opciones).

    > **Figura: Diagrama de colaboración para `RabbitMqOptions`**
    - La registra el módulo implementado `OptionsModule` con validación en el arranque.
    - La consumen la clase implementada `RabbitMqEventPublisher` y la clase implementada
      `RabbitMqHealthCheck` a través de la interfaz externa `IOptions<RabbitMqOptions>`.

### Resultados discriminados

81. **Familia de resultados discriminados de Courses (`CreateCourseResult`, `GetCourseByIdResult`, `ListCoursesResult`, `UpdateCourseResult`, `DeleteCourseResult`)** — cinco uniones discriminadas que los manejadores de Courses devuelven a `CourseController`. Todos son registros abstractos con casos sellados anidados.

    > **Figura: Diagrama de herencia para la familia de resultados de Courses**
    - Cada uno es un registro abstracto **raíz** con sus casos sellados anidados que lo extienden.

    > **Figura: Diagrama de colaboración para la familia de resultados de Courses**
    - Los producen los manejadores implementados de Courses.
    - Los empareja por patrón el controlador implementado `CourseController`.
    - `CreateCourseResult.Created` y `CreateCourseResult.ReplayedFromIdempotency` encapsulan la
      clase implementada `GetCourseDto`; `GetCourseByIdResult.Found` y `UpdateCourseResult.Updated`
      también.

82. **Familia de resultados discriminados de Groups (`CreateClassGroupResult`, `ListClassGroupsResult`, `UpdateClassGroupResult`, `DeleteClassGroupResult`)** — cuatro uniones discriminadas que los manejadores de Groups devuelven a `ClassGroupController`.

    > **Figura: Diagrama de herencia para la familia de resultados de Groups**
    - Cada uno es un registro abstracto **raíz** con sus casos sellados anidados.

    > **Figura: Diagrama de colaboración para la familia de resultados de Groups**
    - Los producen los manejadores implementados de Groups.
    - Los empareja por patrón el controlador implementado `ClassGroupController`.

83. **Familia de resultados discriminados de Scheduleds (`CreateScheduledClassResult`, `UpdateScheduledClassResult`, `DeleteScheduledClassResult`, `FindScheduledClassResult`, `TransferScheduledClassResult`)** — cinco uniones discriminadas que los manejadores de Scheduleds devuelven a `ScheduledClassController` y al servidor gRPC `ClassExistenceGrpcService`.

    > **Figura: Diagrama de herencia para la familia de resultados de Scheduleds**
    - Cada uno es un registro abstracto **raíz** con sus casos sellados anidados.

    > **Figura: Diagrama de colaboración para la familia de resultados de Scheduleds**
    - Los producen los manejadores implementados de Scheduleds.
    - Los empareja por patrón el controlador implementado `ScheduledClassController` y la clase
      implementada `ClassExistenceGrpcService`.
    - `FindScheduledClassResult.Found` encapsula el registro implementado `ClassExistenceMeta`.

84. **Familia de resultados discriminados de Uniques (`CreateUniqueClassResult`, `UpdateUniqueClassResult`, `DeleteUniqueClassResult`, `FindUniqueClassResult`, `TransferUniqueClassResult`)** — cinco uniones discriminadas análogas a las de Scheduleds pero para `UniqueClassController` y `ClassExistenceGrpcService`.

    > **Figura: Diagrama de herencia para la familia de resultados de Uniques**
    - Cada uno es un registro abstracto **raíz** con sus casos sellados anidados.

    > **Figura: Diagrama de colaboración para la familia de resultados de Uniques**
    - Los producen los manejadores implementados de Uniques.
    - Los empareja por patrón el controlador implementado `UniqueClassController` y la clase
      implementada `ClassExistenceGrpcService`.

85. **Familia de resultados discriminados de Schedules (`GetCourseScheduleResult`, `GetTeacherScheduleResult`, `GetTenantScheduleResult`)** — tres uniones discriminadas que devuelven el horario ensamblado.

    > **Figura: Diagrama de herencia para la familia de resultados de Schedules**
    - Cada uno es un registro abstracto **raíz** con un único caso sellado `Found`.

    > **Figura: Diagrama de colaboración para la familia de resultados de Schedules**
    - Los producen los manejadores implementados de Schedules.
    - Los empareja por patrón el controlador implementado `ScheduledClassController`.
    - El caso `Found` encapsula la clase implementada `GetCourseScheduleDto`.

86. **`ClassExistenceMeta`** — registro de solo lectura que transporta los metadatos de existencia
    de una clase: hora de inicio, hora de fin, fecha opcional y límite de estudiantes; lo usan los
    servicios gRPC para componer su respuesta.

    > **Figura: Diagrama de herencia para `ClassExistenceMeta`**
    - Es un registro **raíz**.

    > **Figura: Diagrama de colaboración para `ClassExistenceMeta`**
    - Lo producen las clases implementadas `ScheduledClassDao` y `UniqueClassDao` como resultado
      de la operación de búsqueda por existencia.
    - Lo encapsulan los casos `FindScheduledClassResult.Found` y `FindUniqueClassResult.Found`.
    - Lo consumen las clases implementadas `ClassExistenceGrpcService` para componer la respuesta
      gRPC externa `ClassExistsResponse`.

### Ensamblador y resolvedor de horario

87. **`IScheduleAssembler`** — contrato del ensamblador de horario semanal: combina clases
    periódicas y clases únicas en un único DTO de horario.

    > **Figura: Diagrama de herencia para `IScheduleAssembler`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IScheduleAssembler`**
    - La implementa la clase implementada `ScheduleAssembler`.
    - La consumen los manejadores `GetCourseScheduleHandler`, `GetTeacherScheduleHandler` y
      `GetTenantScheduleHandler`.
    - Su operación produce la clase implementada `GetCourseScheduleDto`.

88. **`ScheduleAssembler`** — implementación que carga en paralelo las listas de clases periódicas
    y únicas y las proyecta a DTOs de salida con AutoMapper.

    > **Figura: Diagrama de herencia para `ScheduleAssembler`**
    - Implementa la interfaz implementada `IScheduleAssembler`.

    > **Figura: Diagrama de colaboración para `ScheduleAssembler`**
    - Recibe por inyección la interfaz externa `IMapper`.
    - Produce la clase implementada `GetCourseScheduleDto`.

89. **`WeekResolver`** — clase estática que calcula la semana actual del tenant a partir de la zona
    horaria IANA y el instante UTC, y resuelve el lunes de la semana apuntada por un índice de
    desplazamiento.

    > **Figura: Diagrama de herencia para `WeekResolver`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `WeekResolver`**
    - La consume el controlador implementado `ScheduledClassController` antes de despachar las
      consultas de horario.
    - Lee la zona horaria con los tipos externos `TimeZoneInfo` y `DateOnly` de la biblioteca
      base de .NET.

### Seguridad

90. **`AuthClaims`** — clase estática con los nombres de los ocho claims del token: los seis base
    más `IndexCoreServicesPyramid` y `SubscriptionExpiresAt`.

    > **Figura: Diagrama de herencia para `AuthClaims`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `AuthClaims`**
    - La consume la clase implementada `ClaimContext` para nombrar cada claim que lee del token.
    - La consume el módulo implementado `JwtAuthenticationModule` para configurar qué claim aporta
      el nombre de usuario y cuál el rol.
    - La consume el módulo implementado `ClaimsLogScopeModule` para añadir los claims al ámbito
      de log.

91. **`UserRoles`** — clase estática con los nombres de los cuatro roles del sistema y dos
    constantes de combinación para uso en `[Authorize(Roles = ...)]`.

    > **Figura: Diagrama de herencia para `UserRoles`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `UserRoles`**
    - La consumen los controladores implementados y el servicio gRPC implementado
      `ClassExistenceGrpcService` en sus atributos de autorización por rol.

92. **`RequiresServiceTierAttribute`** — filtro de autorización que impide el acceso a la acción
    si el nivel de suscripción del tenant (leído del token) es inferior al mínimo requerido.

    > **Figura: Diagrama de herencia para `RequiresServiceTierAttribute`**
    - Hereda de la clase externa `System.Attribute`.
    - Implementa la interfaz externa `IAuthorizationFilter` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `RequiresServiceTierAttribute`**
    - Recibe por inyección la interfaz implementada `IClaimContext` a través del contenedor de
      servicios del contexto de la petición.
    - Captura la clase implementada `MissingClaimException` para tratar claims ausentes como nivel
      cero en lugar de lanzar excepción.

### Validadores

93. **Familia de validadores (`CreateCourseDtoValidator`, `UpdateCourseDtoValidator`, `CreateClassGroupDtoValidator`, `UpdateClassGroupDtoValidator`, `TransferClassDtoValidator`, `CreateScheduledClassDtoValidator`, `UpdateScheduledClassDtoValidator`, `CourseScheduleParametersDtoValidator`, `WeekPointerDtoValidator`, `CreateUniqueClassDtoValidator`, `UpdateUniqueClassDtoValidator`, `ClassTeacherDtoValidator`)** — doce validadores FluentValidation, uno por cada DTO de entrada. Todos comparten la misma relación estructural.

    > **Figura: Diagrama de herencia para la familia de validadores**
    - Cada uno extiende la clase externa `AbstractValidator<T>` del paquete externo FluentValidation,
      parametrizada con su DTO correspondiente.

    > **Figura: Diagrama de colaboración para la familia de validadores**
    - Los descubre y registra automáticamente el módulo implementado `ValidationModule` mediante
      `AddValidatorsFromAssemblyContaining<Program>()`.
    - Los resuelve y ejecuta la clase implementada `FluentValidationActionFilter` antes de cada
      acción de controlador.

94. **`TeacherListRuleExtensions`** — clase estática de extensión que define reglas FluentValidation
    reutilizables para validar listas de `ClassTeacherDto`: unicidad de identificadores y
    restricción de cardinalidad máxima.

    > **Figura: Diagrama de herencia para `TeacherListRuleExtensions`**
    - Clase estática **raíz** (clase de extensión).

    > **Figura: Diagrama de colaboración para `TeacherListRuleExtensions`**
    - La consumen los validadores implementados `CreateScheduledClassDtoValidator`,
      `UpdateScheduledClassDtoValidator`, `CreateUniqueClassDtoValidator` y
      `UpdateUniqueClassDtoValidator` para aplicar las reglas de lista de profesores sin
      duplicación de lógica.

### Trabajos en segundo plano

95. **`OutboxPublisher`** — trabajo `BackgroundService` que en cada iteración arrienda un lote de
    eventos pendientes del outbox, los publica en paralelo hacia RabbitMQ y actualiza su estado en
    la base de datos.

    > **Figura: Diagrama de herencia para `OutboxPublisher`**
    - Hereda de la clase externa `BackgroundService` (de ASP.NET Core).

    > **Figura: Diagrama de colaboración para `OutboxPublisher`**
    - Recibe por inyección la interfaz externa `IServiceProvider` (para crear un ámbito y resolver
      `IOutboxEventDao` en cada iteración) y la interfaz implementada `IEventPublisher`.
    - Consume la entidad implementada `OutboxEvent`.
    - Emite eventos de diagnóstico a través de la clase implementada `LogEvents`.

96. **`OutboxJanitor`** — trabajo `BackgroundService` que periódicamente elimina los eventos del
    outbox que fueron publicados hace más de siete días.

    > **Figura: Diagrama de herencia para `OutboxJanitor`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `OutboxJanitor`**
    - Recibe por inyección la interfaz externa `IServiceProvider` (para crear un ámbito y resolver
      `IOutboxEventDao` en cada barrido).
    - Emite eventos de diagnóstico a través de la clase implementada `LogEvents`.

### Composición de módulos

97. **`IServiceModule`** — contrato de un módulo que **registra servicios** durante el arranque.

    > **Figura: Diagrama de herencia para `IServiceModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IServiceModule`**
    - Su operación de registro recibe los tipos externos `IServiceCollection` e `IConfiguration`.
    - La implementan todos los módulos de registro del servicio.

98. **`IAppModule`** — contrato de un módulo que **configura la canalización** de la aplicación.

    > **Figura: Diagrama de herencia para `IAppModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IAppModule`**
    - Su operación de configuración recibe el tipo externo `WebApplication`.
    - La implementan los módulos que intervienen en la fase de canalización.

99. **`ModuleHost`** — anfitrión estático que descubre los módulos por reflexión y los ejecuta
     ordenados por su propiedad de orden.

     > **Figura: Diagrama de herencia para `ModuleHost`**
     - Clase estática **raíz**.

     > **Figura: Diagrama de colaboración para `ModuleHost`**
     - Descubre y orquesta las interfaces implementadas `IServiceModule` e `IAppModule`.
     - Recibe los tipos externos `WebApplicationBuilder` (fase de registro) y `WebApplication`
       (fase de configuración).

100. **Familia de módulos concretos (`SecretsValidationModule`, `ForwardedHeadersModule`, `HttpContextModule`, `RequestCorrelationModule`, `JwtAuthenticationModule`, `AuthorizationModule`, `ClaimsLogScopeModule`, `PersistenceModule`, `OptionsModule`, `OpenGenericHandlersModule`, `AutoRegisteredServicesModule`, `AutoMapperModule`, `ValidationModule`, `GrpcServerModule`, `OutboxProducerModule`, `MvcModule`, `ProblemDetailsModule`, `HealthCheckModule`, `DatabaseSeeder`)** — diecinueve módulos concretos que implementan `IServiceModule`, `IAppModule` o ambos. Todos comparten la misma relación de herencia con los contratos.

     > **Figura: Diagrama de herencia para la familia de módulos**
     - Cada módulo implementa la interfaz implementada `IServiceModule`, la interfaz implementada
       `IAppModule` o ambas, según si participa en la fase de registro, la de configuración o ambas.

     > **Figura: Diagrama de colaboración para la familia de módulos**
     - Los descubre y ejecuta la clase implementada `ModuleHost`.
     - `GrpcServerModule` registra y mapea el servicio gRPC implementado
       `ClassExistenceGrpcService`.
     - `OpenGenericHandlersModule` registra el tipo abierto genérico `ClassCreationCoordinator<>`.
     - `AutoRegisteredServicesModule` registra por exploración de ensamblados los Daos, los
       manejadores, los constructores y el `ClaimContext` contra sus interfaces.
     - `OutboxProducerModule` registra el trabajo `OutboxPublisher`, el trabajo `OutboxJanitor` y
       el publicador `RabbitMqEventPublisher`.
     - `AutoMapperModule` registra los cinco perfiles implementados de AutoMapper.
     - `DatabaseSeeder` ejecuta la siembra coordinada por la clase implementada `DBInjector`
       cuando la variable de entorno `SEED_DB=true`.

---

## Comandos de demostración

```bash
# Tipos implementados en CourseManagement (lo que Doxygen diagrama)
find apps/CourseManagement/Backend -name "*.cs" -not -path "*/obj/*" | sort

# Relaciones de herencia por categoría
grep -rn "class .*:\|interface " apps/CourseManagement/Backend --include=*.cs | grep -v "/obj/"

# Manejadores de mediador (comandos y consultas)
grep -rn "ICommandHandler\|IQueryHandler" apps/CourseManagement/Backend --include=*.cs | grep -v "/obj/" | grep "implements\|:"

# Contratos gRPC heredados
grep -rn "GrpcService\|Base$" apps/CourseManagement/Backend/Grpc --include=*.cs

# Generar los grafos de jerarquía, herencia y colaboración del servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
#   salida: extra/graphics/out/doxygen/html/
```
