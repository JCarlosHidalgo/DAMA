# 3.3.3.9 Diagramado del servicio Attendance

> **Attendance** es el servicio más complejo del conjunto en cuanto a relaciones externas: consume
> cuatro eventos RabbitMQ (de tres productores distintos), expone un canal SignalR de tiempo real,
> y llama gRPC a CourseManagement —con TLS mutua y reenvío de JWT— para validar la existencia de
> clases antes de registrar asistencia. Su código se organiza en más de ciento treinta archivos;
> aquí se describe la estructura de clases que Doxygen grafica, sin reproducir los métodos.
>
> **Generar las figuras:** `cd extra/graphics && docker compose --profile docs run --rm doxygen`
> (salida en `extra/graphics/out/doxygen/html/`).

---

## a) Jerarquía gráfica

El código de Attendance se distribuye por **namespaces** que coinciden exactamente con los
**roles estructurales** del patrón transversal de los backends:

- `Backend.Claims` — **abstracción de claims**: dos contratos (`IClaimContext`, `IHubClaimContext`)
  y sus implementaciones, más `MissingClaimException`. `IHubClaimContext` es exclusivo de
  Attendance porque los manejadores de SignalR no pueden usar `IHttpContextAccessor`.
- `Backend.Security` — **constantes de seguridad** (nombres de claims y roles) más el filtro de
  autorización por nivel de suscripción (`RequiresServiceTierAttribute`).
- `Backend.Options` — **POCOs de configuración** enlazados por `IOptions<T>`: ventana de
  asistencia, límites de saldo, credenciales de RabbitMQ y secreto de callback de pago.
- `Backend.Entities` — **modelo persistente**: tres entidades que representan los registros de
  asistencia y el saldo de clases por estudiante.
- `Backend.Dtos` — **datos de entrada/salida**: DTOs de asistencia y saldo con sus interfaces de
  segregación (`IAttendanceLine`, `IRemainIncrement`).
- `Backend.Common` — **tipos compartidos de paginación**: DTO de parámetros de consulta y objeto
  de respuesta paginada.
- `Backend.Results` — **uniones discriminadas de resultados**: tipos cerrados (`abstract record`
  con variantes `sealed record`) para cada caso de uso de asistencia, saldo y consumo de eventos.
- `Backend.Transporters` — **objetos de transporte internos**: registros que circulan entre
  capas sin cruzar la API (contexto de marcado, resultado de construcción, metadatos de clase,
  descriptor de topología RabbitMQ).
- `Backend.Events` — **POCOs de eventos entrantes**: los cuatro tipos de mensaje que llegan de
  RabbitMQ, uno por productor y tipo de evento.
- `Backend.DB.Daos.Abstract` — **contratos de acceso a datos** por consumidor (ISP): seis
  interfaces, dos por familia (asistencia, eventos, saldo).
- `Backend.DB.Daos.Concrete` — **implementaciones de acceso a datos**: siete clases concretas,
  con `AttendanceDaoBase<T>` como clase abstracta intermedia.
- `Backend.DB.Injectors` y `Backend.DB.Utils` — **siembra de datos** (`DatabaseSeeder`,
  inyectores CSV) y utilidades de conexión; no aportan jerarquía de herencia relevante.
- `Backend.AutoMapperProfiles` — **perfil de proyección** AutoMapper de entidades a respuestas.
- `Backend.Builders` — **constructores de entidades y páginas**: dos contratos y dos
  implementaciones.
- `Backend.Services.Abstract` y `Backend.Services.Concrete` — **capa de servicio/aplicación**:
  nueve contratos y once implementaciones (servicios de asistencia y saldo, manejadores de
  eventos, cliente gRPC, extensiones de claims, y clases auxiliares estáticas).
- `Backend.Controllers` — **controlador de la API REST**: un único controlador para toda la
  superficie de asistencia y saldo.
- `Backend.Hubs` — **concentrador SignalR**: `AttendanceHub`, exclusivo de este servicio.
- `Backend.Filters` — **filtros de acción**: el filtro global de validación FluentValidation y el
  atributo de conjunto de reglas.
- `Backend.Validators` — **validadores FluentValidation**: tres validadores concretos.
- `Backend.Workers` — **consumidores RabbitMQ** y su infraestructura de mensajería: cuatro
  consumidores `BackgroundService` más las tres clases de infraestructura compartida.
- `Backend.Grpc` — **cliente gRPC**: interceptor de reenvío de JWT y utilidad de parseo de
  identificadores.
- `Backend.ExternalCheck` — **sondas de disponibilidad**: tres implementaciones de comprobación
  de salud (base de datos, RabbitMQ, par gRPC), más las constantes de nomenclatura, el escritor
  de respuestas y la enumeración de dependencias externas.
- `Backend.Logging` — **eventos de registro estructurado**: clase estática parcial de mensajes
  de log (sin jerarquía de herencia propia).
- `Backend.Modules` — **composición de módulos**: los diecinueve módulos que arrancan el
  servicio, descubiertos y ejecutados por `ModuleHost`.

**No existen** en Attendance los grupos «operaciones CQRS-lite» (ese patrón es exclusivo de
CourseManagement), «outbox de producción de eventos» (Attendance consume, no produce), ni
«cliente HTTP externo» (el acceso externo sincrónico es únicamente gRPC). La capa de
mensajería es de **solo consumo**: `RabbitMqInfrastructureModule` no registra ningún canal
publicador.

A continuación, un título de figura por grupo estructural:

> **Figura: Jerarquía gráfica de la abstracción de claims (`IClaimContext`, `ClaimContext`, `IHubClaimContext`, `HubClaimContext`, `MissingClaimException`)**

Este grupo proporciona lectura tipada y con fallo rápido de los claims del token JWT. Attendance
amplía el contrato base del patrón transversal con un segundo contrato dedicado a SignalR, porque
el concentrador opera sobre la conexión WebSocket establecida —donde `IHttpContextAccessor` es
poco fiable— y necesita leer el identificador de academia directamente desde `ClaimsPrincipal`.

> **Figura: Jerarquía gráfica de las constantes de seguridad (`AuthClaims`, `UserRoles`, `RequiresServiceTierAttribute`)**

Centraliza, como constantes, los nombres de claims y roles. `RequiresServiceTierAttribute` añade
la verificación de nivel de suscripción (pirámide de servicios principales) mediante un filtro
de autorización de MVC que descarta la petición si el tenant no tiene el nivel mínimo activo.

> **Figura: Jerarquía gráfica de los POCOs de configuración (`AttendanceOptions`, `RabbitMqOptions`, `RemainLimits`, `CallbackOptions`)**

Cuatro clases de configuración ligadas a secciones de entorno por `OptionsModule` mediante
`IOptions<T>`, con validación al arranque. Cubren la ventana horaria de asistencia, los límites
de saldo, la conexión a RabbitMQ y el secreto del callback de pago.

> **Figura: Jerarquía gráfica del modelo persistente (`ScheduledClassAttendance`, `UniqueClassAttendance`, `StudentRemainClasses`)**

Las tres entidades de la base de datos de Attendance: los registros de asistencia para clases
periódicas y para clases únicas (ambos con clave primaria de tres dimensiones: academia, clase y
estudiante, más la fecha para las periódicas), y el saldo de clases pendientes por estudiante.

> **Figura: Jerarquía gráfica de los datos de entrada/salida (`ScheduledAttendanceDto`, `UniqueAttendanceDto`, `ScheduledAttendanceResponse`, `UniqueAttendanceResponse`, `IAttendanceLine`, `IncrementStudentRemainDto`, `IncrementTenantRemainDto`, `IRemainIncrement`, `RemainResponse`)**

Los DTOs de entrada que la API recibe del cliente y los de salida que devuelve. `IAttendanceLine`
es el contrato compartido por las dos respuestas de asistencia; `IRemainIncrement` es el contrato
compartido por los dos DTOs de incremento de saldo, que permiten que sus respectivos validadores
accedan a los campos comunes.

> **Figura: Jerarquía gráfica de los tipos compartidos de paginación (`PaginationParamsDto`, `PageDto<T>`, `Pagination`)**

Los objetos de consulta y respuesta paginada reutilizados por los listados de asistencia del
estudiante autenticado. `Pagination` es una clase estática de cálculo de índices.

> **Figura: Jerarquía gráfica de las uniones discriminadas de resultados (familias `MarkAttendanceOutcome`, `GetScheduledByStudentOutcome`, `GetUniqueByStudentOutcome`, `GetRemainForStudentOutcome`, `IncrementStudentRemainOutcome`, `IncrementTenantRemainOutcome`, `StudentRegisteredOutcome`, `CourseDeletedOutcome`, `ClassDeletedOutcome`, `PaymentCapturedOutcome`)**

Diez familias de resultados discriminados, cada una compuesta por un `abstract record` raíz y
variantes `sealed record`. Cubren los casos de uso de marcado de asistencia, consulta por
estudiante, operaciones de saldo y resultados de los cuatro manejadores de eventos. El
controlador y los consumidores hacen `switch` exhaustivo sobre ellas.

> **Figura: Jerarquía gráfica de los objetos de transporte internos (`AttendanceMarkContext`, `AttendanceBuildResult<TEntity>`, `ClassExistenceMeta`, `RabbitMqTopologyDescriptor`)**

Registros que circulan entre capas internas del servicio sin cruzar la superficie de la API.
`AttendanceMarkContext` agrupa los datos del estudiante que `AttendanceMarker` necesita para
registrar una asistencia. `AttendanceBuildResult<TEntity>` lleva la entidad construida junto
con el límite de aforo. `ClassExistenceMeta` transporta los metadatos que el cliente gRPC
recibe de CourseManagement. `RabbitMqTopologyDescriptor` describe una cola y su topología.

> **Figura: Jerarquía gráfica de los POCOs de eventos entrantes (`StudentRegisteredEvent`, `CourseDeletedEvent`, `ClassDeletedEvent`, `PaymentCapturedEvent`)**

Los cuatro objetos planos que los consumidores deserializan al recibir mensajes de RabbitMQ. Su
forma (JSON sobre el intercambio `dama.events`) está intencionalmente desacoplada de los tipos
internos de sus productores.

> **Figura: Jerarquía gráfica de los contratos de acceso a datos (`IScheduledClassAttendanceDao`, `IUniqueClassAttendanceDao`, `IPaymentCreditLedgerDao`, `IProcessedEventDao`, `IRemainRequestDao`, `IStudentRemainClassesDao`)**

Seis interfaces de acceso a datos, segregadas por consumidor según el principio de segregación de
interfaces. Dos son contratos de asistencia (hereden de `IThreeForeignDao<T>` del paquete
externo), una es el ledger de créditos de pago, dos son contratos idempotentes (extienden
`IProcessedEventStore` del paquete `DAMA.Software.MySqlOutbox`) y una es el contrato del saldo.

> **Figura: Jerarquía gráfica del acceso a datos (`AttendanceDaoBase<T>`, `ScheduledClassAttendanceDao`, `UniqueClassAttendanceDao`, `PaymentCreditLedgerDao`, `ProcessedEventDao`, `RemainRequestDao`, `StudentRemainClassesDao`)**

Siete implementaciones concretas. `AttendanceDaoBase<T>` es una clase abstracta intermedia que
unifica el mapeo de filas y deshabilita las operaciones genéricas del paquete externo que no
aplican a la clave compuesta de asistencia; `ScheduledClassAttendanceDao` y
`UniqueClassAttendanceDao` la especializan. Las cuatro restantes son implementaciones directas de
sus interfaces.

> **Figura: Jerarquía gráfica del perfil AutoMapper (`AttendanceProfile`)**

Registra las tres proyecciones de entidad a respuesta que usa la capa de servicio, heredando de
la clase externa `Profile` de AutoMapper.

> **Figura: Jerarquía gráfica de los constructores (`IAttendanceClassBuilder`, `AttendanceClassBuilder`, `IRemainClassBuilder`, `RemainClassBuilder`)**

Dos pares contrato/implementación que encapsulan la fabricación de entidades de asistencia (con
los metadatos de la clase recibidos de gRPC) y la construcción del objeto de saldo vacío
(cuando un estudiante aún no tiene registro).

> **Figura: Jerarquía gráfica de la capa de servicio y aplicación (`IAttendanceMarker`, `AttendanceMarker`, `IScheduledClassService`, `ScheduledClassService`, `IUniqueClassService`, `UniqueClassService`, `IRemainClassReader`, `RemainClassReader`, `IRemainClassWriter`, `RemainClassWriter`, `ICourseManagementClient`, `CourseManagementClient`, `IClassDeletedHandler`, `ClassDeletedHandler`, `ICourseDeletedHandler`, `CourseDeletedHandler`, `IStudentRegisteredHandler`, `StudentRegisteredHandler`, `IPaymentCapturedHandler`, `PaymentCapturedHandler`, y los auxiliares estáticos `AttendanceTimeWindow`, `AttendancePaging`, `AttendanceRecording`, `ClaimContextExtensions`)**

La capa de negocio completa: dos servicios de asistencia (periódica y única), dos servicios de
saldo (lectura y escritura), un marcador transversal que coordina la ventana horaria y la
transacción, un cliente gRPC que abstrae la llamada a CourseManagement, cuatro manejadores de
eventos idempotentes, y cuatro clases estáticas auxiliares que encapsulan lógica reutilizable
(ventana horaria, paginación, grabación atómica, extensión de claims).

> **Figura: Jerarquía gráfica del controlador de la API (`AttendanceController`)**

El único controlador del servicio; expone los endpoints de asistencia (leer y marcar clases
periódicas y únicas) y de saldo (leer, incrementar por estudiante, incrementar por academia).
Aplica el filtro `[RequiresServiceTier]` a nivel de clase y delega toda la lógica en los cuatro
servicios de la capa de aplicación.

> **Figura: Jerarquía gráfica del concentrador SignalR (`AttendanceHub`)**

El concentrador que gestiona la suscripción de clientes en tiempo real a grupos de clase. Emite
el evento `AttendanceMarked` cuando `AttendanceMarker` registra una asistencia exitosa. Usa
`IHubClaimContext` en lugar de `IClaimContext` para leer el identificador de academia.

> **Figura: Jerarquía gráfica de los filtros de validación (`FluentValidationActionFilter`, `RuleSetAttribute`)**

El filtro global que intercepta cada acción del controlador, resuelve el validador concreto para
cada argumento y rechaza la petición con 400 si falla. `RuleSetAttribute` permite marcar una
acción con el nombre de un subconjunto de reglas.

> **Figura: Jerarquía gráfica de los validadores (`PaginationParamsDtoValidator`, `IncrementStudentRemainDtoValidator`, `IncrementTenantRemainDtoValidator`)**

Los tres validadores FluentValidation del servicio, registrados automáticamente por el módulo de
validación. Los de incremento de saldo leen los límites configurados de `RemainLimits`.

> **Figura: Jerarquía gráfica de los consumidores de eventos (`StudentRegisteredConsumer`, `CourseDeletedConsumer`, `ClassDeletedConsumer`, `PaymentCapturedConsumer`)**

Cuatro `BackgroundService` que cubren el ciclo de vida de conexión, declaración de topología,
deserialización y reenvío al manejador correspondiente. Cada uno declara su propia cola durable
sobre el intercambio `dama.events` con su clave de enrutamiento específica.

> **Figura: Jerarquía gráfica de la infraestructura de mensajería (`RabbitMqConnectionFactory`, `RabbitMqTopologyDeclarer`, `RabbitMqMessageDispatcher<TEvent>`)**

Las tres clases de infraestructura compartida por los cuatro consumidores: fábrica de conexiones,
declarador de topología (intercambio, cola, enlace, calidad de servicio) y despachador genérico
que gestiona deserialización, detección de mensajes envenenados, despacho a manejador y
confirmación/rechazo.

> **Figura: Jerarquía gráfica del cliente gRPC (`JwtForwardClientInterceptor`, `GuidParser`)**

El interceptor que propaga la cabecera `Authorization` de la petición HTTP entrante a la llamada
gRPC saliente (patrón transversal de todos los clientes gRPC del proyecto) y la utilidad de
parseo de identificadores con error tipado de gRPC.

> **Figura: Jerarquía gráfica de las sondas de disponibilidad (`DatabaseHealthCheck`, `RabbitMqHealthCheck`, `GrpcPeerHealthCheck`, `ExternalDependency`, `ExternalCheckNaming`, `ReadinessResponseWriter`)**

Las tres comprobaciones de salud que `HealthCheckModule` registra bajo la etiqueta `ready`
(base de datos MySQL, broker RabbitMQ y par gRPC de CourseManagement). `ExternalDependency` es
la enumeración de las tres dependencias; `ExternalCheckNaming` genera su nombre canónico;
`ReadinessResponseWriter` serializa el informe de salud en JSON para `/health/ready`.

> **Figura: Jerarquía gráfica de la composición de módulos (`IServiceModule`, `IAppModule`, `ModuleHost`, y los dieciséis módulos `*Module`)**

El conjunto más numeroso: dos contratos de arranque, un anfitrión estático por reflexión y los
módulos que orquestan el registro de servicios y la configuración de la canalización, en orden
explícito por su propiedad `Order`.

---

## b) Diagramas de herencia y colaboración

Una entrada por cada clase/interfaz **implementada** en Attendance. Los tipos externos (del
framework .NET, ASP.NET Core, paquetes NuGet y stubs gRPC generados) se referencian desde las
viñetas sin entrada propia.

### Abstracción de claims

1. **`IClaimContext`** — contrato que define la lectura tipada de ocho claims del token JWT:
   identificador y nombre de la academia, zona horaria, identificador y nombre del usuario, rol,
   índice de la pirámide de servicios principales y fecha de expiración de la suscripción.
   Attendance expone dos claims adicionales respecto al contrato mínimo de Credentials.

   > **Figura: Diagrama de herencia para `IClaimContext`**
   - Es una interfaz **raíz**: no hereda de ninguna otra interfaz.

   > **Figura: Diagrama de colaboración para `IClaimContext`**
   - La implementa la clase implementada `ClaimContext`.
   - La consumen `AttendanceMarker`, `ScheduledClassService`, `UniqueClassService`,
     `RemainClassReader`, `RemainClassWriter` y `RequiresServiceTierAttribute`.
   - La extensión estática implementada `ClaimContextExtensions` la recibe como `this`.

2. **`ClaimContext`** — implementación que lee cada claim del usuario autenticado con memorización
   en primera lectura y fallo rápido ante ausencia o formato inválido.

   > **Figura: Diagrama de herencia para `ClaimContext`**
   - Implementa la interfaz implementada `IClaimContext`.

   > **Figura: Diagrama de colaboración para `ClaimContext`**
   - Recibe por inyección de dependencias la interfaz externa `IHttpContextAccessor` (de ASP.NET
     Core) para acceder al principal del usuario autenticado.
   - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim que lee.
   - Construye y lanza la clase implementada `MissingClaimException` cuando un claim falta o no
     puede convertirse al tipo esperado.

3. **`IHubClaimContext`** — contrato exclusivo de Attendance para leer el identificador de
   academia desde un `ClaimsPrincipal` ya disponible, sin pasar por `IHttpContextAccessor`.
   Existe porque los manejadores de un concentrador SignalR operan sobre la conexión WebSocket,
   donde el contexto HTTP de la petición original ya no está disponible.

   > **Figura: Diagrama de herencia para `IHubClaimContext`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IHubClaimContext`**
   - La implementa la clase implementada `HubClaimContext`.
   - La consume la clase implementada `AttendanceHub`.

4. **`HubClaimContext`** — implementación que extrae el identificador de academia del
   `ClaimsPrincipal` recibido como parámetro, lanzando `MissingClaimException` si el claim
   falta o no es un identificador global único válido.

   > **Figura: Diagrama de herencia para `HubClaimContext`**
   - Implementa la interfaz implementada `IHubClaimContext`.

   > **Figura: Diagrama de colaboración para `HubClaimContext`**
   - Usa las constantes de la clase implementada `AuthClaims` para nombrar el claim de academia.
   - Construye y lanza la clase implementada `MissingClaimException` ante claim ausente.
   - La invoca la clase implementada `AttendanceHub`.

5. **`MissingClaimException`** — excepción específica que señala un claim requerido ausente o
   malformado en el token JWT.

   > **Figura: Diagrama de herencia para `MissingClaimException`**
   - Hereda de la clase externa `System.Exception`.

   > **Figura: Diagrama de colaboración para `MissingClaimException`**
   - La construye y lanza la clase implementada `ClaimContext` (para peticiones HTTP).
   - La construye y lanza la clase implementada `HubClaimContext` (para el concentrador SignalR).
   - La captura la clase implementada `RequiresServiceTierAttribute` para devolver 403 en lugar
     de propagar la excepción.
   - La captura la clase implementada `AttendanceHub` para lanzar `HubException`.

### Constantes de seguridad y filtro de suscripción

6. **`AuthClaims`** — clase estática con los nombres de los claims del token.

   > **Figura: Diagrama de herencia para `AuthClaims`**
   - Clase estática **raíz**: no participa en jerarquía de herencia.

   > **Figura: Diagrama de colaboración para `AuthClaims`**
   - La consumen `ClaimContext`, `HubClaimContext`, `ClaimsLogScopeModule` y
     `JwtAuthenticationModule`.

7. **`UserRoles`** — clase estática con los nombres de los roles del sistema y sus combinaciones.

   > **Figura: Diagrama de herencia para `UserRoles`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `UserRoles`**
   - La consume `AttendanceController` en sus atributos de autorización por rol.
   - La consume `AttendanceHub` en su atributo de autorización.
   - La consume la clase implementada `ClaimContextExtensions` para comparar el rol de estudiante.

8. **`RequiresServiceTierAttribute`** — atributo de autorización que verifica, como filtro de
   MVC, que el nivel de suscripción del tenant sea igual o superior al mínimo requerido por el
   endpoint. Activo en todos los endpoints del servicio a nivel de controlador, y también a nivel
   de acción para los incrementos de saldo que requieren el nivel 3.

   > **Figura: Diagrama de herencia para `RequiresServiceTierAttribute`**
   - Hereda de la clase externa `System.Attribute`.
   - Implementa la interfaz externa `IAuthorizationFilter` (de ASP.NET Core MVC).

   > **Figura: Diagrama de colaboración para `RequiresServiceTierAttribute`**
   - Resuelve por inyección desde el contenedor la interfaz implementada `IClaimContext` para
     leer el índice de la pirámide y la fecha de expiración.
   - Captura la clase implementada `MissingClaimException` y devuelve 403 ante claim ausente.

### POCOs de configuración (familia)

*Familia de cuatro tipos estructuralmente paralelos: cada uno es un POCO de opciones enlazado a
una sección de entorno por `OptionsModule` mediante `IOptions<T>` con validación al arranque.*

9–12. **`AttendanceOptions`**, **`RabbitMqOptions`**, **`RemainLimits`**, **`CallbackOptions`** —
   `AttendanceOptions` agrupa la ventana horaria permitida y el tamaño de página.
   `RabbitMqOptions` agrupa la conexión al broker y los nombres de colas y claves de enrutamiento.
   `RemainLimits` establece los límites mínimos y máximos del incremento de saldo y la longitud
   máxima del nombre del estudiante. `CallbackOptions` guarda el secreto del callback de pago.

   > **Figura: Diagrama de herencia para `AttendanceOptions` / `RabbitMqOptions` / `RemainLimits` / `CallbackOptions`**
   - Cada una es una clase **raíz** (no hereda).

   > **Figura: Diagrama de colaboración para `AttendanceOptions` / `RabbitMqOptions` / `RemainLimits` / `CallbackOptions`**
   - Cada una es enlazada y validada por la clase implementada `OptionsModule`.
   - `AttendanceOptions` la consumen `AttendanceMarker`, `ScheduledClassService` y
     `UniqueClassService` (a través de `IOptions<AttendanceOptions>`).
   - `RabbitMqOptions` la consumen los cuatro consumidores de eventos y `RabbitMqConnectionFactory`
     y `RabbitMqHealthCheck`.
   - `RemainLimits` la consumen `IncrementStudentRemainDtoValidator` e
     `IncrementTenantRemainDtoValidator`.
   - `CallbackOptions` la valida `SecretsValidationModule` (longitud mínima del secreto).

### Modelo persistente

13. **`ScheduledClassAttendance`** — entidad que representa un registro de asistencia a una clase
   periódica; su clave primaria natural comprende academia, clase, fecha y estudiante.

   > **Figura: Diagrama de herencia para `ScheduledClassAttendance`**
   - Implementa la interfaz externa `IThreeForeignEntity` (del paquete `JuanCarlosHS.SQLDaosPackage`).

   > **Figura: Diagrama de colaboración para `ScheduledClassAttendance`**
   - La construye la clase implementada `AttendanceClassBuilder`.
   - La persiste y recupera la clase implementada `ScheduledClassAttendanceDao`.
   - La proyecta a `ScheduledAttendanceResponse` el perfil implementado `AttendanceProfile`.
   - La usan `ScheduledClassService` y `AttendanceMarker` como tipo genérico.

14. **`UniqueClassAttendance`** — entidad análoga para clases únicas; su clave primaria natural
   comprende academia, clase y estudiante (la fecha es parte de los datos, no de la clave).

   > **Figura: Diagrama de herencia para `UniqueClassAttendance`**
   - Implementa la interfaz externa `IThreeForeignEntity`.

   > **Figura: Diagrama de colaboración para `UniqueClassAttendance`**
   - La construye la clase implementada `AttendanceClassBuilder`.
   - La persiste y recupera la clase implementada `UniqueClassAttendanceDao`.
   - La proyecta a `UniqueAttendanceResponse` el perfil implementado `AttendanceProfile`.
   - La usan `UniqueClassService` y `AttendanceMarker` como tipo genérico.

15. **`StudentRemainClasses`** — entidad que lleva el saldo de clases disponibles de un estudiante
   dentro de una academia; actúa como registro de crédito para el marcado de asistencia.

   > **Figura: Diagrama de herencia para `StudentRemainClasses`**
   - Implementa la interfaz externa `IEntity` (del paquete `JuanCarlosHS.SQLDaosPackage`).

   > **Figura: Diagrama de colaboración para `StudentRemainClasses`**
   - La construye la clase implementada `RemainClassBuilder`.
   - La lee y modifica la clase implementada `StudentRemainClassesDao`.
   - La proyecta a `RemainResponse` el perfil implementado `AttendanceProfile`.

### Datos de entrada/salida

16. **`IAttendanceLine`** — contrato compartido por las dos respuestas de asistencia, que define
   los campos comunes a una línea de registro de asistencia.

   > **Figura: Diagrama de herencia para `IAttendanceLine`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IAttendanceLine`**
   - La implementan las clases implementadas `ScheduledAttendanceResponse` y
     `UniqueAttendanceResponse`.

17. **`ScheduledAttendanceResponse`** y **`UniqueAttendanceResponse`** — respuestas de la API para
   los registros de asistencia periódicos y únicos respectivamente. Estructuralmente idénticas
   en sus campos de salida, aunque corresponden a tablas distintas.

   *(Familia: misma relación de herencia y colaboración.)*

   > **Figura: Diagrama de herencia para `ScheduledAttendanceResponse` / `UniqueAttendanceResponse`**
   - Cada una implementa la interfaz implementada `IAttendanceLine`.

   > **Figura: Diagrama de colaboración para `ScheduledAttendanceResponse` / `UniqueAttendanceResponse`**
   - Las produce el perfil implementado `AttendanceProfile` (AutoMapper).
   - Las devuelve `AttendanceController` envueltas en el tipo externo `ActionResult`.
   - `ScheduledAttendanceResponse` es el tipo genérico de respuesta en `ScheduledClassService` y
     `AttendanceMarker`; `UniqueAttendanceResponse` en `UniqueClassService` y `AttendanceMarker`.

18. **`IRemainIncrement`** — contrato compartido por los dos DTOs de incremento de saldo, que
   define los campos comunes a una solicitud de incremento (identificador de solicitud y cantidad).

   > **Figura: Diagrama de herencia para `IRemainIncrement`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IRemainIncrement`**
   - La implementan las clases implementadas `IncrementStudentRemainDto` e
     `IncrementTenantRemainDto`.

19. **`IncrementStudentRemainDto`** — DTO de entrada para incrementar el saldo de un estudiante
   específico; incluye opcionalmente el nombre del estudiante para crearlo si aún no existe.

   > **Figura: Diagrama de herencia para `IncrementStudentRemainDto`**
   - Implementa la interfaz implementada `IRemainIncrement`.

   > **Figura: Diagrama de colaboración para `IncrementStudentRemainDto`**
   - La valida la clase implementada `IncrementStudentRemainDtoValidator`.
   - La recibe `AttendanceController` en el endpoint de incremento por estudiante.

20. **`IncrementTenantRemainDto`** — DTO de entrada para incrementar el saldo de todos los
   estudiantes de una academia a la vez; no incluye nombre de estudiante.

   > **Figura: Diagrama de herencia para `IncrementTenantRemainDto`**
   - Implementa la interfaz implementada `IRemainIncrement`.

   > **Figura: Diagrama de colaboración para `IncrementTenantRemainDto`**
   - La valida la clase implementada `IncrementTenantRemainDtoValidator`.
   - La recibe `AttendanceController` en el endpoint de incremento por academia.

21. **`RemainResponse`** — respuesta de la API con el saldo actual de clases de un estudiante.

   > **Figura: Diagrama de herencia para `RemainResponse`**
   - Clase **raíz**: no hereda.

   > **Figura: Diagrama de colaboración para `RemainResponse`**
   - La produce el perfil implementado `AttendanceProfile` desde `StudentRemainClasses`.
   - La devuelve `AttendanceController` y la usan `RemainClassReader` y el tipo de resultado
     implementado `GetRemainForStudentOutcome.Found`.

### Tipos compartidos de paginación

22. **`PaginationParamsDto`** — DTO de entrada para parámetros de paginación; el campo `Index`
   corresponde al parámetro de consulta `index` de la API.

   > **Figura: Diagrama de herencia para `PaginationParamsDto`**
   - Clase **raíz**.

   > **Figura: Diagrama de colaboración para `PaginationParamsDto`**
   - La valida la clase implementada `PaginationParamsDtoValidator`.
   - La recibe `AttendanceController` en los tres endpoints de listado paginado.

23. **`PageDto<T>`** — respuesta genérica de paginación con índice actual, índice máximo y lista
   de ítems.

   > **Figura: Diagrama de herencia para `PageDto<T>`**
   - Clase genérica **raíz**.

   > **Figura: Diagrama de colaboración para `PageDto<T>`**
   - La construye la clase implementada `AttendanceClassBuilder` (mediante `BuildPage`).
   - La devuelven `ScheduledClassService` y `UniqueClassService` en sus listados paginados.

24. **`Pagination`** — clase estática con la lógica de cálculo del índice máximo de páginas.

   > **Figura: Diagrama de herencia para `Pagination`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `Pagination`**
   - La usa la clase estática implementada `AttendancePaging` para calcular el índice máximo.

### Uniones discriminadas de resultados (familias)

*Diez familias de resultados. Todas siguen el mismo patrón: un `abstract record` raíz con
variantes `sealed record` anidadas. Se agrupan por dominio de uso.*

#### Resultados de asistencia

25. **`MarkAttendanceOutcome`** — unión discriminada del resultado del marcado de asistencia, con
   siete variantes: `Marked`, `AlreadyMarked`, `NoRemainingClasses`, `InvalidClass`, `ClassFull`,
   `OutsideAllowedWindow` e `InvalidTenantTimezone`.

   > **Figura: Diagrama de herencia para `MarkAttendanceOutcome`**
   - `abstract record` raíz. Cada variante hereda de él como `sealed record`.

   > **Figura: Diagrama de colaboración para `MarkAttendanceOutcome`**
   - Lo producen `AttendanceMarker` y `AttendanceRecording`.
   - Lo consumen `ScheduledClassService`, `UniqueClassService` y `AttendanceController`
     (exhaustivamente mediante `switch`).

26. **`GetScheduledByStudentOutcome`** — unión discriminada del resultado de la consulta de
   asistencia periódica por estudiante: `Found(List<ScheduledAttendanceResponse>)` y `Forbidden`.

   > **Figura: Diagrama de herencia para `GetScheduledByStudentOutcome`**
   - `abstract record` raíz con dos variantes `sealed record`.

   > **Figura: Diagrama de colaboración para `GetScheduledByStudentOutcome`**
   - Lo produce `ScheduledClassService`.
   - Lo consume `AttendanceController`.

27. **`GetUniqueByStudentOutcome`** — unión análoga para asistencia única por estudiante:
   `Found(List<UniqueAttendanceResponse>)` y `Forbidden`.

   > **Figura: Diagrama de herencia para `GetUniqueByStudentOutcome`**
   - `abstract record` raíz con dos variantes `sealed record`.

   > **Figura: Diagrama de colaboración para `GetUniqueByStudentOutcome`**
   - Lo produce `UniqueClassService`.
   - Lo consume `AttendanceController`.

#### Resultados de saldo

28. **`GetRemainForStudentOutcome`** — unión discriminada del resultado de la consulta de saldo
   por estudiante: `Found(RemainResponse)` y `Forbidden`.

   > **Figura: Diagrama de herencia para `GetRemainForStudentOutcome`**
   - `abstract record` raíz con dos variantes `sealed record`.

   > **Figura: Diagrama de colaboración para `GetRemainForStudentOutcome`**
   - Lo produce `RemainClassReader`.
   - Lo consume `AttendanceController`.

29. **`IncrementStudentRemainOutcome`** — unión discriminada del resultado del incremento de saldo
   de un estudiante específico: `Applied` y `AlreadyApplied`.

   > **Figura: Diagrama de herencia para `IncrementStudentRemainOutcome`**
   - `abstract record` raíz con dos variantes `sealed record`.

   > **Figura: Diagrama de colaboración para `IncrementStudentRemainOutcome`**
   - Lo produce `RemainClassWriter`.
   - Lo consume `AttendanceController`.

30. **`IncrementTenantRemainOutcome`** — unión discriminada del resultado del incremento masivo de
   saldo de toda la academia: `Applied(int Affected)` y `AlreadyApplied`.

   > **Figura: Diagrama de herencia para `IncrementTenantRemainOutcome`**
   - `abstract record` raíz con dos variantes `sealed record`.

   > **Figura: Diagrama de colaboración para `IncrementTenantRemainOutcome`**
   - Lo produce `RemainClassWriter`.
   - Lo consume `AttendanceController`.

#### Resultados de manejadores de eventos (familia)

*Cuatro tipos estructuralmente idénticos: cada uno tiene las variantes `Succeeded`/`RemainCreated`/
`RemainCredited`/`AttendancesDeleted`, `AlreadyProcessed` y `Failed`.*

31–34. **`StudentRegisteredOutcome`** (`RemainCreated | AlreadyProcessed | Failed`),
   **`PaymentCapturedOutcome`** (`RemainCredited | AlreadyProcessed | Failed`),
   **`CourseDeletedOutcome`** (`AttendancesDeleted | AlreadyProcessed | Failed`),
   **`ClassDeletedOutcome`** (`AttendancesDeleted | AlreadyProcessed | Failed`) — el manejador
   de cada evento devuelve uno de estos resultados; el consumidor lo convierte en
   ACK (`true`) o NACK (`false`).

   > **Figura: Diagrama de herencia para cada `*Outcome`**
   - Cada uno es un `abstract record` raíz con tres variantes `sealed record`.

   > **Figura: Diagrama de colaboración para cada `*Outcome`**
   - Lo produce el manejador implementado correspondiente (`StudentRegisteredHandler`,
     `PaymentCapturedHandler`, `CourseDeletedHandler`, `ClassDeletedHandler`).
   - Lo consume el consumidor implementado correspondiente (`StudentRegisteredConsumer`,
     `PaymentCapturedConsumer`, `CourseDeletedConsumer`, `ClassDeletedConsumer`).

### Objetos de transporte internos

35. **`AttendanceMarkContext`** — registro que agrupa los datos de identidad del estudiante que
   `AttendanceMarker` necesita: identificador de academia, identificador del estudiante, nombre
   del estudiante e identificador de zona horaria.

   > **Figura: Diagrama de herencia para `AttendanceMarkContext`**
   - `sealed record` **raíz**.

   > **Figura: Diagrama de colaboración para `AttendanceMarkContext`**
   - Lo construye la clase implementada `AttendanceMarker` a partir de `IClaimContext`.
   - Lo reciben `ScheduledClassService` y `UniqueClassService` en las lambdas de resolución.

36. **`AttendanceBuildResult<TEntity>`** — registro genérico que agrupa la entidad de asistencia
   construida y el límite máximo de aforo de la clase.

   > **Figura: Diagrama de herencia para `AttendanceBuildResult<TEntity>`**
   - `sealed record` genérico **raíz**.

   > **Figura: Diagrama de colaboración para `AttendanceBuildResult<TEntity>`**
   - Lo construyen `ScheduledClassService` y `UniqueClassService` en sus lambdas de resolución.
   - Lo consume `AttendanceMarker` para extraer la entidad y el límite antes de la transacción.

37. **`ClassExistenceMeta`** — registro con los metadatos devueltos por CourseManagement vía gRPC:
   hora de inicio, hora de fin, fecha (solo para clases únicas) y límite de aforo.

   > **Figura: Diagrama de herencia para `ClassExistenceMeta`**
   - `record` **raíz**.

   > **Figura: Diagrama de colaboración para `ClassExistenceMeta`**
   - Lo construye la clase implementada `CourseManagementClient` al recibir la respuesta gRPC.
   - Lo consumen `ScheduledClassService` y `UniqueClassService` para llamar al constructor de
     entidades `AttendanceClassBuilder`.

38. **`RabbitMqTopologyDescriptor`** — registro que describe una cola: nombre del intercambio,
   nombre de la cola, clave de enrutamiento y contador de capturas previas.

   > **Figura: Diagrama de herencia para `RabbitMqTopologyDescriptor`**
   - `sealed record` **raíz**.

   > **Figura: Diagrama de colaboración para `RabbitMqTopologyDescriptor`**
   - Lo construye cada consumidor de eventos al inicio de su ejecución.
   - Lo consume la clase implementada `RabbitMqTopologyDeclarer` para declarar la topología.

### POCOs de eventos entrantes (familia)

*Cuatro tipos structuralmente paralelos: POCOs simples deserializados desde JSON de RabbitMQ.*

39–42. **`StudentRegisteredEvent`**, **`CourseDeletedEvent`**, **`ClassDeletedEvent`**,
   **`PaymentCapturedEvent`** — cada uno contiene los campos `EventId`, `EventType`,
   `OccurredAt`, `AggregateId` y `Data` (un objeto anidado con los datos específicos del evento).
   Su forma JSON está desacoplada de los tipos internos de los productores.

   > **Figura: Diagrama de herencia para cada `*Event`**
   - Clase **raíz**: no hereda ni implementa interfaces.

   > **Figura: Diagrama de colaboración para cada `*Event`**
   - Lo deserializa la clase implementada `RabbitMqMessageDispatcher<TEvent>` desde el cuerpo
     del mensaje RabbitMQ.
   - Lo pasa el consumidor correspondiente al manejador correspondiente.

### Contratos de acceso a datos

43. **`IScheduledClassAttendanceDao`** — contrato de acceso a la tabla de asistencia de clases
   periódicas; segregado por consumidor según el principio de segregación de interfaces.

   > **Figura: Diagrama de herencia para `IScheduledClassAttendanceDao`**
   - Extiende la interfaz externa `IThreeForeignDao<ScheduledClassAttendance>` del paquete
     `JuanCarlosHS.SQLDaosPackage`.

   > **Figura: Diagrama de colaboración para `IScheduledClassAttendanceDao`**
   - La implementa la clase implementada `ScheduledClassAttendanceDao`.
   - La consumen `ScheduledClassService` y `ClassDeletedHandler`.

44. **`IUniqueClassAttendanceDao`** — contrato análogo para la tabla de asistencia de clases
   únicas; misma estructura que `IScheduledClassAttendanceDao`.

   > **Figura: Diagrama de herencia para `IUniqueClassAttendanceDao`**
   - Extiende la interfaz externa `IThreeForeignDao<UniqueClassAttendance>`.

   > **Figura: Diagrama de colaboración para `IUniqueClassAttendanceDao`**
   - La implementa la clase implementada `UniqueClassAttendanceDao`.
   - La consumen `UniqueClassService` y `ClassDeletedHandler`.

45. **`IStudentRemainClassesDao`** — contrato de acceso a la tabla de saldo de clases.

   > **Figura: Diagrama de herencia para `IStudentRemainClassesDao`**
   - Extiende la interfaz externa `ISingleDao<StudentRemainClasses>` del paquete
     `JuanCarlosHS.SQLDaosPackage`.

   > **Figura: Diagrama de colaboración para `IStudentRemainClassesDao`**
   - La implementa la clase implementada `StudentRemainClassesDao`.
   - La consumen `AttendanceMarker`, `RemainClassReader`, `RemainClassWriter`,
     `StudentRegisteredHandler` y `PaymentCapturedHandler`.

46. **`IProcessedEventDao`** — contrato del repositorio de eventos procesados; extiende el contrato
   del paquete para la tabla `processed_events` que garantiza la idempotencia de los cuatro
   consumidores.

   > **Figura: Diagrama de herencia para `IProcessedEventDao`**
   - Extiende la interfaz externa `IProcessedEventStore` del paquete
     `DAMA.Software.MySqlOutbox`.

   > **Figura: Diagrama de colaboración para `IProcessedEventDao`**
   - La implementa la clase implementada `ProcessedEventDao`.
   - La consumen los cuatro manejadores de eventos (`StudentRegisteredHandler`,
     `PaymentCapturedHandler`, `CourseDeletedHandler`, `ClassDeletedHandler`) a través de
     `IdempotentTransaction.RunAsync`.

47. **`IRemainRequestDao`** — contrato del repositorio de solicitudes de saldo ya aplicadas; actúa
   como ledger de idempotencia para los incrementos manuales del operador.

   > **Figura: Diagrama de herencia para `IRemainRequestDao`**
   - Extiende la interfaz externa `IProcessedEventStore` del paquete
     `DAMA.Software.MySqlOutbox`.

   > **Figura: Diagrama de colaboración para `IRemainRequestDao`**
   - La implementa la clase implementada `RemainRequestDao`.
   - La consume `RemainClassWriter` a través de `IdempotentTransaction.RunAsync`.

48. **`IPaymentCreditLedgerDao`** — contrato del ledger de auditoría de créditos por pago
   capturado; escribe un registro de trazabilidad por cada evento `PaymentCaptured` aplicado.

   > **Figura: Diagrama de herencia para `IPaymentCreditLedgerDao`**
   - Es una interfaz **raíz** (no extiende ningún contrato externo).

   > **Figura: Diagrama de colaboración para `IPaymentCreditLedgerDao`**
   - La implementa la clase implementada `PaymentCreditLedgerDao`.
   - La consume `PaymentCapturedHandler` dentro de la misma transacción idempotente.

### Acceso a datos

49. **`AttendanceDaoBase<TAttendance>`** — clase abstracta intermedia que hereda del objeto de
   acceso a datos externo de tres claves foráneas y restringe las operaciones genéricas
   (`Read`, `Delete`, `Create`, `Update` por clave simple) que no aplican al esquema de
   asistencia con clave compuesta.

   > **Figura: Diagrama de herencia para `AttendanceDaoBase<TAttendance>`**
   - Hereda de la clase externa `MySQLThreeForeignDao<TAttendance>` del paquete
     `JuanCarlosHS.SQLDaosPackage`.

   > **Figura: Diagrama de colaboración para `AttendanceDaoBase<TAttendance>`**
   - La especializan las clases implementadas `ScheduledClassAttendanceDao` y
     `UniqueClassAttendanceDao`.

50. **`ScheduledClassAttendanceDao`** — acceso a la tabla de asistencia de clases periódicas;
   implementa las operaciones específicas de conteo con bloqueo, marcado atómico, listado y
   eliminación masiva.

   > **Figura: Diagrama de herencia para `ScheduledClassAttendanceDao`**
   - Hereda de la clase implementada `AttendanceDaoBase<ScheduledClassAttendance>`.
   - Implementa la interfaz implementada `IScheduledClassAttendanceDao`.

   > **Figura: Diagrama de colaboración para `ScheduledClassAttendanceDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.
   - Recibe la interfaz externa `ITransactionContext` en las operaciones que participan en la
     transacción de marcado.

51. **`UniqueClassAttendanceDao`** — acceso a la tabla de asistencia de clases únicas; estructura
   idéntica a `ScheduledClassAttendanceDao` salvo que no incluye la fecha de clase como parte
   de la clave de búsqueda en el conteo de concurrencia.

   > **Figura: Diagrama de herencia para `UniqueClassAttendanceDao`**
   - Hereda de la clase implementada `AttendanceDaoBase<UniqueClassAttendance>`.
   - Implementa la interfaz implementada `IUniqueClassAttendanceDao`.

   > **Figura: Diagrama de colaboración para `UniqueClassAttendanceDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.

52. **`StudentRemainClassesDao`** — acceso a la tabla de saldo de clases; implementa el incremento
   con upsert, el decremento atómico y la lectura por par academia-estudiante.

   > **Figura: Diagrama de herencia para `StudentRemainClassesDao`**
   - Hereda de la clase externa `MySQLSingleDao<StudentRemainClasses>` del paquete
     `JuanCarlosHS.SQLDaosPackage`.
   - Implementa la interfaz implementada `IStudentRemainClassesDao`.

   > **Figura: Diagrama de colaboración para `StudentRemainClassesDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.

53. **`ProcessedEventDao`** — repositorio de la tabla `processed_events`, que el paquete
   `DAMA.Software.MySqlOutbox` usa para garantizar la idempotencia al insertar el identificador
   del evento antes del efecto secundario.

   > **Figura: Diagrama de herencia para `ProcessedEventDao`**
   - Implementa la interfaz implementada `IProcessedEventDao`.

   > **Figura: Diagrama de colaboración para `ProcessedEventDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.
   - Lo usa el tipo externo `IdempotentTransaction` del paquete `DAMA.Software.MySqlOutbox`.

54. **`RemainRequestDao`** — repositorio de la tabla de solicitudes de saldo ya aplicadas; usa el
   mismo mecanismo de idempotencia que `ProcessedEventDao` pero para operaciones manuales del
   operador, no para eventos de mensajería.

   > **Figura: Diagrama de herencia para `RemainRequestDao`**
   - Implementa la interfaz implementada `IRemainRequestDao`.

   > **Figura: Diagrama de colaboración para `RemainRequestDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.
   - Lo usa el tipo externo `IdempotentTransaction`.

55. **`PaymentCreditLedgerDao`** — repositorio del ledger `payment_credit_ledger`; escribe el
   registro de auditoría de crédito dentro de la misma transacción que el incremento de saldo.

   > **Figura: Diagrama de herencia para `PaymentCreditLedgerDao`**
   - Implementa la interfaz implementada `IPaymentCreditLedgerDao`.

   > **Figura: Diagrama de colaboración para `PaymentCreditLedgerDao`**
   - Recibe por inyección la conexión externa `MySqlConnection`.
   - Lo consume `PaymentCapturedHandler`.

### Perfil AutoMapper

56. **`AttendanceProfile`** — declara las tres proyecciones de entidad a respuesta: asistencia
   periódica, asistencia única y saldo (con remapeo del campo `Id` a `StudentId`).

   > **Figura: Diagrama de herencia para `AttendanceProfile`**
   - Hereda de la clase externa `Profile` (de AutoMapper).

   > **Figura: Diagrama de colaboración para `AttendanceProfile`**
   - Proyecta la clase implementada `ScheduledClassAttendance` a `ScheduledAttendanceResponse`.
   - Proyecta la clase implementada `UniqueClassAttendance` a `UniqueAttendanceResponse`.
   - Proyecta la clase implementada `StudentRemainClasses` a `RemainResponse`.
   - Lo registra la clase implementada `AutoMapperModule`.

### Constructores de entidades

57. **`IAttendanceClassBuilder`** — contrato del constructor de entidades de asistencia y de
   respuestas paginadas.

   > **Figura: Diagrama de herencia para `IAttendanceClassBuilder`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IAttendanceClassBuilder`**
   - La implementa la clase implementada `AttendanceClassBuilder`.
   - La consumen `ScheduledClassService`, `UniqueClassService` y la clase estática
     `AttendancePaging`.

58. **`AttendanceClassBuilder`** — implementación que fabrica `ScheduledClassAttendance`,
   `UniqueClassAttendance` y `PageDto<T>` a partir de los datos del contexto de marcado y los
   metadatos gRPC.

   > **Figura: Diagrama de herencia para `AttendanceClassBuilder`**
   - Implementa la interfaz implementada `IAttendanceClassBuilder`.

   > **Figura: Diagrama de colaboración para `AttendanceClassBuilder`**
   - Construye las clases implementadas `ScheduledClassAttendance` y `UniqueClassAttendance`.
   - Construye la clase implementada `PageDto<T>`.
   - Recibe `ClassExistenceMeta` como fuente de metadatos gRPC.

59. **`IRemainClassBuilder`** — contrato del constructor del saldo vacío inicial.

   > **Figura: Diagrama de herencia para `IRemainClassBuilder`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IRemainClassBuilder`**
   - La implementa la clase implementada `RemainClassBuilder`.
   - La consume `RemainClassReader` cuando el estudiante aún no tiene registro de saldo.

60. **`RemainClassBuilder`** — implementación que construye un `StudentRemainClasses` con saldo
   cero para un par academia-estudiante recién registrado.

   > **Figura: Diagrama de herencia para `RemainClassBuilder`**
   - Implementa la interfaz implementada `IRemainClassBuilder`.

   > **Figura: Diagrama de colaboración para `RemainClassBuilder`**
   - Construye la clase implementada `StudentRemainClasses`.

### Capa de servicio y aplicación

61. **`ICourseManagementClient`** — contrato de la fachada gRPC hacia CourseManagement; abstrae
   la verificación de existencia de clases periódicas y únicas.

   > **Figura: Diagrama de herencia para `ICourseManagementClient`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `ICourseManagementClient`**
   - La implementa la clase implementada `CourseManagementClient`.
   - La consumen `ScheduledClassService` y `UniqueClassService`.

62. **`CourseManagementClient`** — implementación que llama al stub gRPC generado
   `ClassExistence.ClassExistenceClient` (del paquete `DAMA.Software.GrpcContracts`), traduce
   la respuesta al registro implementado `ClassExistenceMeta` y convierte `RpcException` en
   `HttpRequestException` para que la capa de servicio no conozca el transporte gRPC.

   > **Figura: Diagrama de herencia para `CourseManagementClient`**
   - Implementa la interfaz implementada `ICourseManagementClient`.

   > **Figura: Diagrama de colaboración para `CourseManagementClient`**
   - Recibe por inyección el stub externo `ClassExistence.ClassExistenceClient` (generado por
     `Grpc.Tools` desde el contrato `DAMA.Software.GrpcContracts`).
   - Construye y devuelve la clase implementada `ClassExistenceMeta`.
   - La registra la clase implementada `GrpcClientsModule` con interceptor y resiliencia.

63. **`IAttendanceMarker`** — contrato del coordinador transversal de marcado de asistencia.

   > **Figura: Diagrama de herencia para `IAttendanceMarker`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IAttendanceMarker`**
   - La implementa la clase implementada `AttendanceMarker`.
   - La consumen `ScheduledClassService` y `UniqueClassService`.

64. **`AttendanceMarker`** — implementación que coordina: verificación de la ventana horaria, llamada
   a la lambda de resolución/construcción de la entidad, transacción de marcado (via
   `IUnitOfWork`) y emisión del evento SignalR a los suscriptores del grupo.

   > **Figura: Diagrama de herencia para `AttendanceMarker`**
   - Implementa la interfaz implementada `IAttendanceMarker`.

   > **Figura: Diagrama de colaboración para `AttendanceMarker`**
   - Recibe por inyección `IClaimContext`, la interfaz externa `IUnitOfWork` (paquete
     `DAMA.Software.MySqlUnitOfWork`), `IStudentRemainClassesDao`, la interfaz externa
     `IHubContext<AttendanceHub>` (de ASP.NET Core SignalR), la interfaz externa `IMapper`
     (de AutoMapper) y `IOptions<AttendanceOptions>`.
   - Usa la clase estática implementada `AttendanceTimeWindow` para validar la ventana horaria.
   - Delega la grabación atómica en la clase estática implementada `AttendanceRecording`.
   - Emite el evento SignalR al grupo calculado por `AttendanceHub.ScheduledGroup` o
     `AttendanceHub.UniqueGroup`.

65. **`IScheduledClassService`** — contrato del servicio de asistencia de clases periódicas.

   > **Figura: Diagrama de herencia para `IScheduledClassService`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IScheduledClassService`**
   - La implementa la clase implementada `ScheduledClassService`.
   - La consume `AttendanceController`.

66. **`ScheduledClassService`** — implementación que cubre la consulta por clase, la consulta por
   estudiante (con guarda de acceso cruzado), el listado paginado del estudiante autenticado y
   el marcado de asistencia (con llamada gRPC previa para validar existencia).

   > **Figura: Diagrama de herencia para `ScheduledClassService`**
   - Implementa la interfaz implementada `IScheduledClassService`.

   > **Figura: Diagrama de colaboración para `ScheduledClassService`**
   - Recibe `IScheduledClassAttendanceDao`, `ICourseManagementClient`, `IAttendanceMarker`,
     `IClaimContext`, `IOptions<AttendanceOptions>`, `IAttendanceClassBuilder` e `IMapper`.
   - Usa la extensión estática `ClaimContextExtensions.IsStudentAccessingOtherStudent`.
   - Usa la clase estática `AttendancePaging` para los listados paginados.
   - Devuelve tipos de resultado implementados de la familia `*Outcome`.

67. **`IUniqueClassService`** — contrato del servicio de asistencia de clases únicas.

   > **Figura: Diagrama de herencia para `IUniqueClassService`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IUniqueClassService`**
   - La implementa la clase implementada `UniqueClassService`.
   - La consume `AttendanceController`.

68. **`UniqueClassService`** — implementación análoga a `ScheduledClassService` para clases únicas;
   la diferencia de coordinación es que la fecha de la clase proviene de los metadatos gRPC
   (no del reloj del tenant) al marcar asistencia única.

   > **Figura: Diagrama de herencia para `UniqueClassService`**
   - Implementa la interfaz implementada `IUniqueClassService`.

   > **Figura: Diagrama de colaboración para `UniqueClassService`**
   - Recibe `IUniqueClassAttendanceDao`, `ICourseManagementClient`, `IAttendanceMarker`,
     `IClaimContext`, `IOptions<AttendanceOptions>`, `IAttendanceClassBuilder` e `IMapper`.

69. **`IRemainClassReader`** — contrato del servicio de lectura del saldo de clases.

   > **Figura: Diagrama de herencia para `IRemainClassReader`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IRemainClassReader`**
   - La implementa la clase implementada `RemainClassReader`.
   - La consume `AttendanceController`.

70. **`RemainClassReader`** — implementación que devuelve el saldo del estudiante autenticado o de
   un estudiante específico (con guarda de acceso cruzado), materializando un saldo vacío si aún
   no existe registro.

   > **Figura: Diagrama de herencia para `RemainClassReader`**
   - Implementa la interfaz implementada `IRemainClassReader`.

   > **Figura: Diagrama de colaboración para `RemainClassReader`**
   - Recibe `IStudentRemainClassesDao`, `IClaimContext`, `IRemainClassBuilder` e `IMapper`.
   - Usa `ClaimContextExtensions.IsStudentAccessingOtherStudent`.
   - Devuelve la clase implementada `RemainResponse` o el resultado implementado
     `GetRemainForStudentOutcome`.

71. **`IRemainClassWriter`** — contrato del servicio de escritura (incremento) del saldo de clases.

   > **Figura: Diagrama de herencia para `IRemainClassWriter`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `IRemainClassWriter`**
   - La implementa la clase implementada `RemainClassWriter`.
   - La consume `AttendanceController`.

72. **`RemainClassWriter`** — implementación que incrementa el saldo de un estudiante específico o
   de todos los de una academia, con idempotencia garantizada por `IRemainRequestDao` a través
   de `IdempotentTransaction.RunAsync` del paquete externo.

   > **Figura: Diagrama de herencia para `RemainClassWriter`**
   - Implementa la interfaz implementada `IRemainClassWriter`.

   > **Figura: Diagrama de colaboración para `RemainClassWriter`**
   - Recibe `IStudentRemainClassesDao`, `IRemainRequestDao`, la interfaz externa `IUnitOfWork` e
     `IClaimContext`.
   - Usa el tipo externo `IdempotentTransaction` del paquete `DAMA.Software.MySqlOutbox`.

#### Manejadores de eventos (familia)

*Cuatro pares contrato/implementación estructuralmente idénticos: el contrato es una interfaz
raíz con una única operación asíncrona que devuelve el resultado discriminado correspondiente; la
implementación usa `IdempotentTransaction.RunAsync` con `IProcessedEventDao`.*

73. **`IStudentRegisteredHandler`** / **`StudentRegisteredHandler`** — maneja el evento de
   registro de estudiante creando el registro de saldo inicial (con delta cero) mediante upsert
   idempotente.

   > **Figura: Diagrama de herencia para `IStudentRegisteredHandler`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de herencia para `StudentRegisteredHandler`**
   - Implementa la interfaz implementada `IStudentRegisteredHandler`.

   > **Figura: Diagrama de colaboración para `StudentRegisteredHandler`**
   - Recibe `IUnitOfWork`, `IProcessedEventDao`, `IStudentRemainClassesDao`.
   - Usa `IdempotentTransaction.RunAsync` (tipo externo del paquete `DAMA.Software.MySqlOutbox`).
   - Devuelve el tipo implementado `StudentRegisteredOutcome`.
   - Lo invoca `StudentRegisteredConsumer`.

74. **`IPaymentCapturedHandler`** / **`PaymentCapturedHandler`** — maneja el evento de pago
   capturado incrementando el saldo del estudiante y registrando la entrada de auditoría en el
   ledger de créditos de pago, todo dentro de la misma transacción idempotente.

   > **Figura: Diagrama de herencia para `IPaymentCapturedHandler`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de herencia para `PaymentCapturedHandler`**
   - Implementa la interfaz implementada `IPaymentCapturedHandler`.

   > **Figura: Diagrama de colaboración para `PaymentCapturedHandler`**
   - Recibe `IUnitOfWork`, `IProcessedEventDao`, `IStudentRemainClassesDao`,
     `IPaymentCreditLedgerDao`.
   - Usa `IdempotentTransaction.RunAsync`.
   - Devuelve `PaymentCapturedOutcome`.
   - Lo invoca `PaymentCapturedConsumer`.

75. **`ICourseDeletedHandler`** / **`CourseDeletedHandler`** — maneja el evento de eliminación de
   curso borrando todos los registros de asistencia del curso en la academia.

   > **Figura: Diagrama de herencia para `ICourseDeletedHandler`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de herencia para `CourseDeletedHandler`**
   - Implementa la interfaz implementada `ICourseDeletedHandler`.

   > **Figura: Diagrama de colaboración para `CourseDeletedHandler`**
   - Recibe `IUnitOfWork`, `IProcessedEventDao`, `IScheduledClassAttendanceDao`,
     `IUniqueClassAttendanceDao`.
   - Devuelve `CourseDeletedOutcome`.
   - Lo invoca `CourseDeletedConsumer`.

76. **`IClassDeletedHandler`** / **`ClassDeletedHandler`** — maneja el evento de eliminación de
   clase borrando los registros de asistencia de esa clase específica.

   > **Figura: Diagrama de herencia para `IClassDeletedHandler`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de herencia para `ClassDeletedHandler`**
   - Implementa la interfaz implementada `IClassDeletedHandler`.

   > **Figura: Diagrama de colaboración para `ClassDeletedHandler`**
   - Recibe `IUnitOfWork`, `IProcessedEventDao`, `IScheduledClassAttendanceDao`,
     `IUniqueClassAttendanceDao`.
   - Devuelve `ClassDeletedOutcome`.
   - Lo invoca `ClassDeletedConsumer`.

#### Auxiliares estáticos de la capa de servicio

77. **`AttendanceTimeWindow`** — clase estática que verifica si la hora local del tenant cae dentro
   de la ventana horaria permitida para registrar asistencia, convirtiendo la zona horaria IANA.

   > **Figura: Diagrama de herencia para `AttendanceTimeWindow`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `AttendanceTimeWindow`**
   - La usa `AttendanceMarker` para decidir si procede con el marcado.

78. **`AttendancePaging`** — clase estática interna que encapsula la lógica de paginación: cuenta
   total, cálculo de índice máximo, obtención de la página y proyección mediante AutoMapper.

   > **Figura: Diagrama de herencia para `AttendancePaging`**
   - Clase estática interna **raíz**.

   > **Figura: Diagrama de colaboración para `AttendancePaging`**
   - La usan `ScheduledClassService` y `UniqueClassService` en sus listados paginados.
   - Usa la clase estática implementada `Pagination` para el cálculo del índice máximo.
   - Usa la interfaz implementada `IAttendanceClassBuilder` para construir el `PageDto<T>`.

79. **`AttendanceRecording`** — clase estática interna que ejecuta la lógica de grabación atómica
   de una asistencia: verifica el aforo, decrementa el saldo y registra la asistencia, devolviendo
   el `MarkAttendanceOutcome` correspondiente y si debe confirmarse la transacción.

   > **Figura: Diagrama de herencia para `AttendanceRecording`**
   - Clase estática interna **raíz**.

   > **Figura: Diagrama de colaboración para `AttendanceRecording`**
   - La usa `AttendanceMarker` dentro del delegado de `IUnitOfWork.RunInTransactionAsync`.
   - Usa `IStudentRemainClassesDao` para el decremento atómico.
   - Devuelve variantes del tipo implementado `MarkAttendanceOutcome`.

80. **`ClaimContextExtensions`** — clase estática con el método de extensión de `IClaimContext`
   que determina si un estudiante intenta acceder a los datos de otro estudiante.

   > **Figura: Diagrama de herencia para `ClaimContextExtensions`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `ClaimContextExtensions`**
   - Extiende la interfaz implementada `IClaimContext`.
   - Usa las constantes de la clase implementada `UserRoles` para comparar el rol.
   - La invocan `ScheduledClassService`, `UniqueClassService` y `RemainClassReader`.

### Controlador de la API

81. **`AttendanceController`** — único controlador del servicio; expone nueve endpoints bajo la
   ruta `api/attendance` para asistencia (periódica y única) y saldo. Aplica
   `[RequiresServiceTier(2)]` a nivel de clase y autorización por rol en cada acción.

   > **Figura: Diagrama de herencia para `AttendanceController`**
   - Hereda de la clase externa `ControllerBase` (de ASP.NET Core MVC).

   > **Figura: Diagrama de colaboración para `AttendanceController`**
   - Recibe por inyección `IScheduledClassService`, `IUniqueClassService`, `IRemainClassReader`,
     `IRemainClassWriter` e `IOptions<AttendanceOptions>`.
   - Devuelve los DTOs implementados de salida envueltos en el tipo externo `ActionResult`.
   - Hace `switch` exhaustivo sobre las uniones discriminadas de resultados implementadas.
   - Referencia las constantes de `UserRoles` en sus atributos de autorización.
   - Aplica el filtro implementado `RequiresServiceTierAttribute`.

### Concentrador SignalR

82. **`AttendanceHub`** — concentrador SignalR que gestiona la suscripción de clientes (operadores
   y profesores) a grupos de clase para recibir notificaciones de asistencia en tiempo real.
   Define los nombres de grupo como métodos estáticos reutilizados por `AttendanceMarker`.

   > **Figura: Diagrama de herencia para `AttendanceHub`**
   - Hereda de la clase externa `Hub` (de ASP.NET Core SignalR).

   > **Figura: Diagrama de colaboración para `AttendanceHub`**
   - Recibe por inyección la interfaz implementada `IHubClaimContext`.
   - Captura la clase implementada `MissingClaimException` y la convierte en `HubException`.
   - Sus métodos estáticos `ScheduledGroup` y `UniqueGroup` los usa `AttendanceMarker` para
     dirigir el evento `AttendanceMarked` al grupo correcto.
   - Referencia las constantes de `UserRoles` en su atributo de autorización.

### Filtros de validación

83. **`FluentValidationActionFilter`** — filtro de acción global que, antes de cada acción del
   controlador, resuelve el validador FluentValidation concreto para cada argumento y rechaza
   la petición con 400 si la validación falla.

   > **Figura: Diagrama de herencia para `FluentValidationActionFilter`**
   - Implementa la interfaz externa `IAsyncActionFilter` (de ASP.NET Core MVC).

   > **Figura: Diagrama de colaboración para `FluentValidationActionFilter`**
   - Lee el atributo implementado `RuleSetAttribute` de la acción en ejecución.
   - Resuelve por reflexión la interfaz externa `IValidator<T>` (FluentValidation) para cada
     argumento.
   - Lo registra la clase implementada `MvcModule`.

84. **`RuleSetAttribute`** — atributo que marca una acción con los nombres de los subconjuntos de
   reglas que debe ejecutar el filtro de validación.

   > **Figura: Diagrama de herencia para `RuleSetAttribute`**
   - Hereda de la clase externa `System.Attribute`.

   > **Figura: Diagrama de colaboración para `RuleSetAttribute`**
   - Lo lee `FluentValidationActionFilter` mediante reflexión sobre el descriptor de la acción.

### Validadores FluentValidation

85. **`PaginationParamsDtoValidator`** — valida que el índice de página sea un entero no negativo.

   > **Figura: Diagrama de herencia para `PaginationParamsDtoValidator`**
   - Hereda de la clase externa `AbstractValidator<PaginationParamsDto>` (FluentValidation).

   > **Figura: Diagrama de colaboración para `PaginationParamsDtoValidator`**
   - Valida la clase implementada `PaginationParamsDto`.

86. **`IncrementStudentRemainDtoValidator`** — valida que la cantidad de incremento de saldo por
   estudiante esté dentro de los límites configurados en `RemainLimits` y que el nombre del
   estudiante no exceda la longitud máxima.

   > **Figura: Diagrama de herencia para `IncrementStudentRemainDtoValidator`**
   - Hereda de la clase externa `AbstractValidator<IncrementStudentRemainDto>`.

   > **Figura: Diagrama de colaboración para `IncrementStudentRemainDtoValidator`**
   - Recibe `IOptions<RemainLimits>` para los límites de validación.
   - Valida la clase implementada `IncrementStudentRemainDto`.

87. **`IncrementTenantRemainDtoValidator`** — valida que la cantidad de incremento masivo esté
   dentro de los límites configurados.

   > **Figura: Diagrama de herencia para `IncrementTenantRemainDtoValidator`**
   - Hereda de la clase externa `AbstractValidator<IncrementTenantRemainDto>`.

   > **Figura: Diagrama de colaboración para `IncrementTenantRemainDtoValidator`**
   - Recibe `IOptions<RemainLimits>`.
   - Valida la clase implementada `IncrementTenantRemainDto`.

### Consumidores de eventos RabbitMQ (familia)

*Cuatro `BackgroundService` estructuralmente idénticos: declaran su topología, se suscriben a
la cola y delegan en el despachador genérico. Se consolidan; las diferencias son los nombres de
cola, clave de enrutamiento, tipo de evento y tipo de manejador.*

88–91. **`StudentRegisteredConsumer`**, **`CourseDeletedConsumer`**, **`ClassDeletedConsumer`**,
   **`PaymentCapturedConsumer`** — cada uno declara la cola durable correspondiente sobre el
   intercambio `dama.events`, establece la calidad de servicio con prefetchCount configurado
   y consume con reconocimiento manual. Delegan la resolución del manejador al
   `RabbitMqMessageDispatcher<TEvent>` genérico.

   > **Figura: Diagrama de herencia para cada `*Consumer`**
   - Hereda de la clase externa `BackgroundService` (de ASP.NET Core).

   > **Figura: Diagrama de colaboración para cada `*Consumer`**
   - Reciben la clase implementada `RabbitMqConnectionFactory`, la clase implementada
     `RabbitMqTopologyDeclarer` y la clase implementada `RabbitMqMessageDispatcher<TEvent>`
     (con el tipo de evento concreto correspondiente).
   - Reciben `IOptions<RabbitMqOptions>` para los parámetros de conexión y topología.
   - Construyen la clase implementada `RabbitMqTopologyDescriptor` con la configuración de la
     cola.
   - Resuelven por `IServiceProvider` el manejador implementado correspondiente
     (`IStudentRegisteredHandler`, `ICourseDeletedHandler`, `IClassDeletedHandler`,
     `IPaymentCapturedHandler`) y hacen `switch` sobre el resultado discriminado.
   - Los registra la clase implementada `EventConsumersModule`.

### Infraestructura de mensajería

92. **`RabbitMqConnectionFactory`** — fábrica singleton que abre conexiones y canales RabbitMQ con
   los parámetros de `RabbitMqOptions`.

   > **Figura: Diagrama de herencia para `RabbitMqConnectionFactory`**
   - Clase **raíz**: no hereda ni implementa interfaces propias.

   > **Figura: Diagrama de colaboración para `RabbitMqConnectionFactory`**
   - Recibe `IOptions<RabbitMqOptions>`.
   - La usan los cuatro consumidores de eventos.

93. **`RabbitMqTopologyDeclarer`** — declara el intercambio, la cola, el enlace y la calidad de
   servicio sobre un canal ya abierto, a partir del registro implementado
   `RabbitMqTopologyDescriptor`.

   > **Figura: Diagrama de herencia para `RabbitMqTopologyDeclarer`**
   - Clase **raíz**.

   > **Figura: Diagrama de colaboración para `RabbitMqTopologyDeclarer`**
   - Recibe la clase implementada `RabbitMqTopologyDescriptor` como parámetro de la operación.
   - La usan los cuatro consumidores.

94. **`RabbitMqMessageDispatcher<TEvent>`** — despachador genérico singleton que gestiona el ciclo
   de vida completo de un mensaje: deserialización JSON, detección de mensajes envenenados,
   creación de un ámbito de inyección de dependencias, invocación del manejador y confirmación
   o rechazo del mensaje.

   > **Figura: Diagrama de herencia para `RabbitMqMessageDispatcher<TEvent>`**
   - Clase genérica **raíz**: no hereda.

   > **Figura: Diagrama de colaboración para `RabbitMqMessageDispatcher<TEvent>`**
   - Recibe la interfaz externa `IServiceScopeFactory` para aislar cada mensaje en un ámbito.
   - Lo usan los cuatro consumidores con su tipo de evento concreto.
   - Usa la clase estática implementada `LogEvents` para registrar errores y descartes.

### Cliente gRPC

95. **`JwtForwardClientInterceptor`** — interceptor de cliente gRPC que extrae el encabezado
   `Authorization` de la petición HTTP en curso y lo adjunta a los metadatos de la llamada
   saliente, de modo que el servidor CourseManagement pueda validar el JWT del usuario.

   > **Figura: Diagrama de herencia para `JwtForwardClientInterceptor`**
   - Hereda de la clase externa `Interceptor` (de `Grpc.Core.Interceptors`).

   > **Figura: Diagrama de colaboración para `JwtForwardClientInterceptor`**
   - Recibe por inyección la interfaz externa `IHttpContextAccessor`.
   - Lo registra la clase implementada `GrpcClientsModule` como transient y lo adjunta al cliente
     gRPC `ClassExistence.ClassExistenceClient`.

96. **`GuidParser`** — utilidad estática que parsea una cadena a `Guid` y lanza `RpcException` con
   código `InvalidArgument` si el formato no es válido; prevé su uso en servicios gRPC expositores
   aunque Attendance solo sea cliente.

   > **Figura: Diagrama de herencia para `GuidParser`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `GuidParser`**
   - No tiene colaboradores en el código de Attendance actual; está disponible para uso futuro
     en servicios gRPC que expongan Attendance.

### Sondas de disponibilidad

97. **`ExternalDependency`** — enumeración con los tres nombres de dependencia externa del servicio:
   `Database`, `RabbitMq` y `CourseManagementGrpc`.

   > **Figura: Diagrama de herencia para `ExternalDependency`**
   - Enumeración **raíz**: no hereda.

   > **Figura: Diagrama de colaboración para `ExternalDependency`**
   - La usa la clase implementada `ExternalCheckNaming` para generar el nombre canónico.
   - La usa `HealthCheckModule` para registrar cada comprobación.

98. **`ExternalCheckNaming`** — clase estática que genera el nombre canónico de cada comprobación
   de salud bajo la convención `"AttendanceService-{Dependency}"`.

   > **Figura: Diagrama de herencia para `ExternalCheckNaming`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `ExternalCheckNaming`**
   - La usa `HealthCheckModule` al registrar las tres comprobaciones.

99. **`DatabaseHealthCheck`** — comprobación de salud de la base de datos: abre una conexión
   MySQL directa (usando `DBConnector`) y ejecuta `SELECT 1`.

   > **Figura: Diagrama de herencia para `DatabaseHealthCheck`**
   - Implementa la interfaz externa `IHealthCheck` (de `Microsoft.Extensions.Diagnostics.HealthChecks`).

   > **Figura: Diagrama de colaboración para `DatabaseHealthCheck`**
   - Usa la clase estática implementada `DBConnector` para obtener la cadena de conexión.
   - La registra `HealthCheckModule` bajo la etiqueta `ready`.

100. **`RabbitMqHealthCheck`** — comprobación de salud del broker RabbitMQ: abre y cierra una
    conexión de prueba con los parámetros de `RabbitMqOptions`.

    > **Figura: Diagrama de herencia para `RabbitMqHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck`.

    > **Figura: Diagrama de colaboración para `RabbitMqHealthCheck`**
    - Recibe `IOptions<RabbitMqOptions>`.
    - La registra `HealthCheckModule`.

101. **`GrpcPeerHealthCheck`** — comprobación de salud del par gRPC CourseManagement: intenta
    conectar al canal gRPC con un tiempo límite de cinco segundos.

    > **Figura: Diagrama de herencia para `GrpcPeerHealthCheck`**
    - Implementa la interfaz externa `IHealthCheck`.

    > **Figura: Diagrama de colaboración para `GrpcPeerHealthCheck`**
    - Recibe la dirección del par (`Services:CourseManagementUrl`) como parámetro de construcción.
    - La registra `HealthCheckModule` pasando la URL de CourseManagement como argumento de
      activación por tipo.

102. **`ReadinessResponseWriter`** — clase estática que serializa el informe de salud completo como
    JSON estructurado para el endpoint `/health/ready`.

    > **Figura: Diagrama de herencia para `ReadinessResponseWriter`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ReadinessResponseWriter`**
    - Lo configura `HealthCheckModule` como escritor de respuesta del endpoint `/health/ready`.

### Composición de módulos

103. **`IServiceModule`** — contrato de un módulo que registra servicios durante el arranque.

    > **Figura: Diagrama de herencia para `IServiceModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IServiceModule`**
    - La implementan los dieciséis módulos de registro del servicio.
    - La descubre y ejecuta la clase estática implementada `ModuleHost`.

104. **`IAppModule`** — contrato de un módulo que configura la canalización de la aplicación.

    > **Figura: Diagrama de herencia para `IAppModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IAppModule`**
    - La implementan los módulos que intervienen en la fase de configuración.
    - La descubre y ejecuta `ModuleHost`.

105. **`ModuleHost`** — anfitrión estático que descubre todos los módulos del ensamblado por
    reflexión y los ejecuta ordenados por su propiedad `Order`.

    > **Figura: Diagrama de herencia para `ModuleHost`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ModuleHost`**
    - Descubre y orquesta las interfaces implementadas `IServiceModule` e `IAppModule`.
    - Recibe los tipos externos `WebApplicationBuilder` y `WebApplication`.

#### Módulos de arranque (familia de dieciséis módulos)

*Los módulos se agrupan por interfaz implementada: los de registro puro solo implementan
`IServiceModule`; los de doble fase implementan ambas.*

106. **`SecretsValidationModule`** — módulo de orden -100 que valida, antes que cualquier otro,
    que la clave pública RSA exista y sea válida, y que el secreto del callback de pago tenga la
    longitud mínima.

    > **Figura: Diagrama de herencia para `SecretsValidationModule`**
    - Implementa la interfaz implementada `IServiceModule`.

    > **Figura: Diagrama de colaboración para `SecretsValidationModule`**
    - Usa la clase auxiliar interna `SecretsValidation` (misma nota que en Credentials: clase
      interna no pública que acompaña al módulo en el mismo archivo).
    - Lee la clave pública y el secreto del tipo externo `IConfiguration`.

107–121. **Módulos restantes** — a continuación se enumeran individualmente por su rol. Los que
    implementan `IServiceModule` e `IAppModule` se indican explícitamente.

107. **`OptionsModule`** — orden 10. Enlaza y valida los cuatro POCOs de opciones.

    > **Figura: Diagrama de herencia para `OptionsModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `OptionsModule`**
    - Enlaza las clases implementadas `AttendanceOptions`, `RabbitMqOptions`, `RemainLimits` y
      `CallbackOptions` con validación al arranque.

108. **`RequestCorrelationModule`** — orden 12. Añade el identificador de correlación a la
    respuesta y al ámbito de registro.

    > **Figura: Diagrama de herencia para `RequestCorrelationModule`**
    - Implementa `IAppModule`.

    > **Figura: Diagrama de colaboración para `RequestCorrelationModule`**
    - Inserta un middleware inline sobre el tipo externo `WebApplication`.

109. **`ForwardedHeadersModule`** — orden 20/20. Hace respetar las cabeceras de reenvío del
    gateway.

    > **Figura: Diagrama de herencia para `ForwardedHeadersModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `ForwardedHeadersModule`**
    - Configura el tipo externo de opciones de cabeceras reenviadas sobre `WebApplication`.

110. **`HttpContextModule`** — orden 30. Registra el acceso al contexto HTTP.

    > **Figura: Diagrama de herencia para `HttpContextModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `HttpContextModule`**
    - Registra la interfaz externa `IHttpContextAccessor`, de la que depende `ClaimContext`.

111. **`ClaimsLogScopeModule`** — orden 35. Añade los claims de academia, usuario y rol al ámbito
    de registro estructurado de cada petición autenticada.

    > **Figura: Diagrama de herencia para `ClaimsLogScopeModule`**
    - Implementa `IAppModule`.

    > **Figura: Diagrama de colaboración para `ClaimsLogScopeModule`**
    - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim.
    - Inserta un middleware inline sobre `WebApplication`.

112. **`PersistenceModule`** — orden 40. Registra `MySqlConnection` como scoped.

    > **Figura: Diagrama de herencia para `PersistenceModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `PersistenceModule`**
    - Registra la conexión externa `MySqlConnection` obteniendo la cadena de la clase estática
      implementada `DBConnector`.
    - Registra la interfaz externa `IUnitOfWork` con su implementación `MySqlUnitOfWork` del
      paquete `DAMA.Software.MySqlUnitOfWork`.

113. **`AuthorizationModule`** — orden 40/40. Define la política de denegar por defecto y activa
    el middleware de autorización.

    > **Figura: Diagrama de herencia para `AuthorizationModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `AuthorizationModule`**
    - Construye la política con el tipo externo `AuthorizationPolicyBuilder`.

114. **`JwtAuthenticationModule`** — orden 50/30. Configura la validación del JWT con clave pública
    RSA y extrae el token de la cadena de consulta para las conexiones SignalR.

    > **Figura: Diagrama de herencia para `JwtAuthenticationModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `JwtAuthenticationModule`**
    - Referencia las constantes de `AuthClaims` para `NameClaimType` y `RoleClaimType`.
    - Extrae el parámetro `access_token` de la cadena de consulta cuando la ruta comienza con
      `/hubs`, habilitando la autenticación de WebSocket para el concentrador SignalR.

115. **`ValidationModule`** — orden 70. Registra todos los validadores FluentValidation del
    ensamblado.

    > **Figura: Diagrama de herencia para `ValidationModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `ValidationModule`**
    - Registra los tres validadores implementados por escaneo de ensamblado.

116. **`AutoMapperModule`** — orden 75. Registra el perfil AutoMapper del servicio.

    > **Figura: Diagrama de herencia para `AutoMapperModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `AutoMapperModule`**
    - Registra la clase implementada `AttendanceProfile`.

117. **`AutoRegisteredServicesModule`** — orden 80. Registra por exploración de namespaces todos
    los servicios, DAOs, claims y constructores del servicio.

    > **Figura: Diagrama de herencia para `AutoRegisteredServicesModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `AutoRegisteredServicesModule`**
    - Escanea `Backend.Services.Concrete`, `Backend.DB.Daos.Concrete`, `Backend.Claims` y
      `Backend.Builders` mediante la biblioteca externa Scrutor.

118. **`RabbitMqInfrastructureModule`** — orden 90. Registra las tres clases de infraestructura
    de mensajería como singletons. A diferencia de los backends productores, no registra ningún
    canal publicador (`RabbitMqPublisherChannel`).

    > **Figura: Diagrama de herencia para `RabbitMqInfrastructureModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `RabbitMqInfrastructureModule`**
    - Registra las clases implementadas `RabbitMqConnectionFactory`, `RabbitMqTopologyDeclarer`
      y `RabbitMqMessageDispatcher<>` (genérico abierto) como singletons.

119. **`GrpcClientsModule`** — orden 91. Registra el cliente gRPC tipado con interceptor de JWT
    y manejador de resiliencia (reintentos, cortacircuito, tiempos límite).

    > **Figura: Diagrama de herencia para `GrpcClientsModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `GrpcClientsModule`**
    - Registra como transient la clase implementada `JwtForwardClientInterceptor`.
    - Registra el cliente externo `ClassExistence.ClassExistenceClient` (stub generado por
      `DAMA.Software.GrpcContracts`) apuntando a `Services:CourseManagementUrl`, con el
      interceptor y la política de resiliencia estándar de ASP.NET Core.

120. **`EventConsumersModule`** — orden 95. Registra los cuatro consumidores de eventos como
    servicios alojados.

    > **Figura: Diagrama de herencia para `EventConsumersModule`**
    - Implementa `IServiceModule`.

    > **Figura: Diagrama de colaboración para `EventConsumersModule`**
    - Registra como `IHostedService` las clases implementadas `StudentRegisteredConsumer`,
      `CourseDeletedConsumer`, `ClassDeletedConsumer` y `PaymentCapturedConsumer`.

121. **`SignalRModule`** — orden 100/110. Registra SignalR y mapea el concentrador de asistencia.

    > **Figura: Diagrama de herencia para `SignalRModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `SignalRModule`**
    - Mapea la clase implementada `AttendanceHub` en la ruta `/hubs/attendance`.

122. **`MvcModule`** — orden 200/200. Registra y mapea los controladores con el filtro de
    validación global.

    > **Figura: Diagrama de herencia para `MvcModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `MvcModule`**
    - Registra la clase implementada `FluentValidationActionFilter` como filtro global.
    - Mapea la clase implementada `AttendanceController`.

123. **`ProblemDetailsModule`** — orden 210/210. Normaliza las respuestas de error y activa el
    manejador de excepciones.

    > **Figura: Diagrama de herencia para `ProblemDetailsModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `ProblemDetailsModule`**
    - Activa el middleware de manejo de excepciones sobre `WebApplication`.

124. **`HealthCheckModule`** — orden 220/5. Registra las tres comprobaciones de salud y mapea
    `/health` (vivacidad) y `/health/ready` (disponibilidad profunda con escritor personalizado).

    > **Figura: Diagrama de herencia para `HealthCheckModule`**
    - Implementa `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `HealthCheckModule`**
    - Registra `DatabaseHealthCheck`, `RabbitMqHealthCheck` y `GrpcPeerHealthCheck` usando los
      nombres de `ExternalCheckNaming`.
    - Configura `ReadinessResponseWriter.WriteAsync` como escritor de respuesta de `/health/ready`.

> **Nota sobre `SecretsValidation`.** Es una clase auxiliar **interna** y estática que acompaña
> a `SecretsValidationModule` (entrada 106) en el mismo archivo; valida la clave pública RSA y la
> longitud mínima de secretos. Doxygen la grafica junto a su módulo en el diagrama de colaboración
> de la entrada 106.

> **Nota sobre los inyectores de siembra y las utilidades de base de datos.** Las clases
> `ScheduledClassAttendanceInjector`, `UniqueClassAttendanceInjector`,
> `StudentRemainClassesInjector`, `DBConnector` y `DBInjector` forman parte del camino de siembra
> de datos de desarrollo; no participan en la canalización de la aplicación y Doxygen las grafica
> con relaciones mínimas. `DBConnector` es estática y la referencian `PersistenceModule` y
> `DatabaseHealthCheck`; los inyectores heredan de la clase externa `DataInjector` del paquete
> `JuanCarlosHS.SQLDaosPackage`.

> **Nota sobre `LogEvents`.** Es una clase estática parcial con mensajes de log compilados en
> tiempo de arranque (`[LoggerMessage]`); no aporta jerarquía de herencia significativa y Doxygen
> la grafica como clase de utilidad referenciada desde varios tipos del servicio.

---

## Comandos de demostración

```bash
# Tipos implementados en Attendance (lo que Doxygen diagrama)
find apps/Attendance/Backend -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | sort

# Relaciones de herencia y declaraciones de tipos
grep -rn "class .*:\|interface \|abstract class\|static class\|record " \
    apps/Attendance/Backend --include=*.cs | grep -v "/obj/" | grep -v "/bin/"

# Cliente gRPC: verificar el contrato consumido
grep -rn "ClassExistence\|GrpcContracts" \
    apps/Attendance/Backend --include=*.cs | grep -v "/obj/"

# Consumidores de eventos: verificar las colas declaradas
grep -rn "QueueName\|RoutingKey\|ExchangeName" \
    apps/Attendance/Backend/Options --include=*.cs

# Generar los grafos de jerarquía, herencia y colaboración del servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
#   salida: extra/graphics/out/doxygen/html/
```
