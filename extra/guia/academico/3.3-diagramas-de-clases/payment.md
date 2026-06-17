# 3.3.3.10 Diagramado del servicio Payment

> Diagramado detallado de clases del servicio **Payment**, el backend de mayor tamaño de la
> plataforma. Estructura definida en [`_plantilla.md`](_plantilla.md); el modelo de calidad es el
> piloto [`credentials.md`](credentials.md) y el hermano grande
> [`course-management.md`](course-management.md). Todos los grafos citados los genera **Doxygen**
> desde el código (`UML_LOOK`, `GRAPHICAL_HIERARCHY`, `COLLABORATION_GRAPH`); aquí solo se titulan las
> figuras y se explican las **relaciones** que muestran, sin describir métodos.
>
> **Generar las figuras:** `cd extra/graphics && docker compose --profile docs run --rm doxygen`
> (salida en `extra/graphics/out/doxygen/html/`).

---

## a) Jerarquía gráfica

Payment es el servicio de **cobros**: emite deudas (de clases sueltas y de suscripciones de academia),
las cobra a través de la pasarela externa **Todotix** mediante un código QR, concilia el resultado del
cobro y proyecta analíticas e ingresos. Su rasgo distintivo respecto de los demás backends es que
generaliza el patrón transversal de **bandeja de salida transaccional** (patrón #9) a **cuatro
registros contables (*ledgers*)** con **dos modelos de estado**: tres bandejas de **salida**
(*outbox*) —eventos de dominio, vencimientos programados e integración con Todotix— y una bandeja de
**entrada** (*inbox*) para las notificaciones de cobro que llegan de la pasarela. A esto se suma un
almacén de **eventos procesados** que da idempotencia a los consumidores.

El código se organiza por **namespaces**, y cada namespace corresponde a un **rol estructural**. El
servicio reparte además su dominio en cinco subdominios —**QrPayments** (cobros de clase por QR),
**Subscriptions** (suscripciones de academia), **DebtTemplates** (plantillas de deuda),
**PaymentCredentials** (credenciales de la pasarela por academia) y **Todotix** (la integración con la
pasarela)—, y ese reparto se repite en la mayoría de los grupos estructurales (datos, entidades,
acceso a datos, servicios, validadores), lo que produce **familias** de tipos paralelos que este
documento consolida.

A diferencia de Credentials (sin estado), Payment **sí posee** modelo persistente, acceso a datos,
capa de servicio, constructores, mensajería y trabajos en segundo plano. A diferencia de
CourseManagement, **no expone un servidor gRPC**: aquí gRPC se usa solo del lado **cliente**, para
informar a Auth del cambio de nivel de suscripción de una academia. Los grupos estructurales presentes
son los siguientes (un título de figura por grupo y su función):

> **Figura: Jerarquía gráfica de la abstracción de claims (`IClaimContext`, `ClaimContext`, `MissingClaimException`)**

Expone, de forma tipada y con **fallo rápido**, los datos de identidad y de academia del token al resto
del servicio, igual que en los demás backends.

> **Figura: Jerarquía gráfica del mediador de aplicación (`ICommandHandler`, `IQueryHandler`)**

Define un **mediador propio y mínimo**: dos contratos genéricos que parametrizan un comando (o una
consulta) y su resultado, sin bus externo. Los controladores y los trabajadores dependen de la
interfaz parametrizada y no de la implementación concreta del manejador.

> **Figura: Jerarquía gráfica de los comandos y manejadores de aplicación**

Agrupa los **comandos** (registros de intención de escritura) y sus **manejadores**, que orquestan la
creación de deudas y el procesamiento de la conciliación de cobro coordinando acceso a datos,
constructores, unidad de trabajo y los *ledgers*.

> **Figura: Jerarquía gráfica de las estrategias de conciliación de cobro (`IDebtCallbackStrategy` y derivadas)**

Implementa el **patrón estrategia** para conciliar la notificación de cobro según el tipo de deuda
(clase o suscripción): un contrato, una base abstracta con la plantilla común y una estrategia por
subdominio.

> **Figura: Jerarquía gráfica de los resultados discriminados (uniones de resultado por caso de uso)**

Modela el resultado de cada operación de negocio como una **unión discriminada** (un registro
abstracto raíz con casos sellados anidados), de modo que el controlador traduzca cada caso a una
respuesta HTTP sin usar excepciones para el flujo previsible.

> **Figura: Jerarquía gráfica de los cuatro *ledgers* de bandeja de salida y entrada (patrón #9)**

Es el grupo más característico del servicio. Reúne las **entidades de evento** persistidas, los
**objetos de acceso a datos** de cada *ledger*, los **publicadores** hacia el agente de mensajería y
los **trabajadores** que relevan, consumen y limpian cada registro. Materializa la entrega fiable
*al menos una vez* con idempotencia.

> **Figura: Jerarquía gráfica de la idempotencia de cobros (`QrPaymentIdempotency` y su acceso a datos)**

Garantiza que una misma plantilla de deuda no genere cobros duplicados, registrando una clave de
idempotencia por intento.

> **Figura: Jerarquía gráfica del acceso a datos por subdominio (interfaces de segregación y objetos de acceso a datos)**

Aplica la **segregación de interfaces**: cada subdominio define contratos de acceso a datos estrechos
(lectura/escritura separadas donde corresponde) y una implementación concreta apoyada en las clases
base de acceso a datos del paquete externo de acceso a MySQL.

> **Figura: Jerarquía gráfica de las filas de proyección analítica (`record struct` de consultas)**

Tipos de valor de solo lectura que transportan, sin asignación en el montículo, las filas crudas que
las consultas analíticas devuelven antes de proyectarse a datos de salida.

> **Figura: Jerarquía gráfica de los inyectores de semilla y utilidades de base de datos**

Pobla los datos de prueba del entorno de desarrollo y centraliza la apertura de conexiones, igual que
en los demás backends.

> **Figura: Jerarquía gráfica de los constructores (`*Builder`)**

Encapsula el ensamblado de los objetos persistentes y de salida (creación, transición de estado y
vista) detrás de contratos, separando la lógica de armado de la capa de servicio.

> **Figura: Jerarquía gráfica de la capa de servicio por subdominio (contratos e implementaciones)**

Encapsula cada responsabilidad de negocio (plantillas, consultas de cobro, planes y consultas de
suscripción, analíticas de administración, resumen, manejo de vencimientos y firma de notificaciones)
detrás de un contrato, de modo que el controlador dependa del contrato y no de la implementación.

> **Figura: Jerarquía gráfica de la integración con la pasarela Todotix**

Aísla todo el trato con la pasarela externa: el cliente HTTP, el publicador de deudas, el resolutor de
la clave de aplicación por academia, el servicio de credenciales, los actualizadores de la imagen del
QR y los datos de transporte del protocolo de Todotix.

> **Figura: Jerarquía gráfica del cliente gRPC de suscripción**

Contiene el único uso de gRPC del servicio: el actualizador que informa a Auth del nuevo nivel de
suscripción de una academia y el **interceptor de cliente** que adjunta el secreto compartido a cada
llamada.

> **Figura: Jerarquía gráfica de los datos de entrada y salida (objetos de transferencia)**

Define, por subdominio, la forma de los datos que la API recibe y devuelve, con interfaces de
segregación que comparten las proyecciones comunes.

> **Figura: Jerarquía gráfica del modelo persistente (entidades de dominio)**

Las entidades que se guardan en la base de datos, agrupadas por subdominio, más las enumeraciones del
dominio.

> **Figura: Jerarquía gráfica de los eventos de dominio (`PaymentCapturedEvent`, `DebtExpiredEvent`)**

Los mensajes que el servicio publica o consume a través del agente de mensajería para comunicar hechos
de negocio a otros servicios y a sus propios trabajadores.

> **Figura: Jerarquía gráfica del perfil de AutoMapper, la seguridad y los filtros de MVC**

El perfil de proyección entre entidades y datos de salida, las constantes y atributos de autorización,
y los filtros que conectan la validación con la canalización de MVC.

> **Figura: Jerarquía gráfica de los validadores (`AbstractValidator<T>`)**

Las reglas de validación de los datos de entrada, una por cada objeto de entrada, registradas en bloque.

> **Figura: Jerarquía gráfica de las comprobaciones de disponibilidad y la infraestructura de mensajería**

Las sondas de salud de las dependencias externas (base de datos, agente de mensajería y par gRPC) y la
infraestructura compartida de conexión, topología y despacho de RabbitMQ.

> **Figura: Jerarquía gráfica de las opciones tipadas, los tipos comunes y el log estructurado**

Los objetos de configuración enlazados por `IOptions<T>`, los tipos de paginación reutilizables y el
log de alto rendimiento generado en tiempo de compilación.

> **Figura: Jerarquía gráfica de la composición de módulos (`IServiceModule`, `IAppModule`, `ModuleHost` y los módulos `*Module`)**

El **arranque modular**: dos contratos (registro de servicios y configuración de la canalización) que
cada módulo implementa, y un anfitrión que los descubre por reflexión y los ejecuta ordenados. Es el
grupo más numeroso y el que define el orden de arranque del servicio.

---

## b) Diagramas de herencia y colaboración

Una entrada por cada clase/interfaz **implementada** en Payment. Las clases/interfaces externas (del
framework .NET y ASP.NET Core, de paquetes NuGet, de los contratos gRPC generados, o de los paquetes
internos `DAMA.Software.MySqlOutbox` y `DAMA.Software.MySqlUnitOfWork`) se **referencian** desde las
viñetas, sin entrada propia. Dada la escala del servicio, las **familias** de tipos estructuralmente
idénticos que solo difieren por el subdominio que envuelven se consolidan en una sola entrada que
nombra a todos sus miembros.

### Abstracción de claims

1. **`IClaimContext`** — contrato que define la lectura tipada de los claims de identidad y academia
   del usuario autenticado.

   > **Figura: Diagrama de herencia para `IClaimContext`**
   - Es una interfaz **raíz**: no hereda de ninguna otra.

   > **Figura: Diagrama de colaboración para `IClaimContext`**
   - Sus propiedades son de tipos de valor del lenguaje (`Guid`, `string`); es un contrato puro sin
     dependencia con otros tipos implementados.

2. **`ClaimContext`** — implementación que lee cada claim del usuario autenticado, con fallo rápido
   ante ausencia.

   > **Figura: Diagrama de herencia para `ClaimContext`**
   - Implementa la interfaz implementada `IClaimContext`.

   > **Figura: Diagrama de colaboración para `ClaimContext`**
   - Recibe por inyección de dependencias la interfaz externa `IHttpContextAccessor` (de ASP.NET
     Core), desde la cual obtiene los claims.
   - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim que lee.
   - Construye y lanza la clase implementada `MissingClaimException` ante un claim ausente o malformado.

3. **`MissingClaimException`** — excepción específica que señala un claim requerido ausente o
   malformado.

   > **Figura: Diagrama de herencia para `MissingClaimException`**
   - Hereda de la clase externa `System.Exception`.

   > **Figura: Diagrama de colaboración para `MissingClaimException`**
   - La construye y lanza la clase implementada `ClaimContext`.

### Mediador de aplicación

4. **`ICommandHandler<TCommand, TResult>`** — contrato genérico de un manejador que ejecuta un comando
   de escritura y devuelve su resultado discriminado.

   > **Figura: Diagrama de herencia para `ICommandHandler`**
   - Es una interfaz **raíz** genérica, con el parámetro de comando contravariante.

   > **Figura: Diagrama de colaboración para `ICommandHandler`**
   - La implementan los manejadores de comandos del servicio (entradas 7 a 9).
   - La consumen, parametrizada, los controladores implementados `QrPaymentController` y
     `SubscriptionPaymentController`, y el trabajador implementado `PaymentCallbackWorker`.

5. **`IQueryHandler<TQuery, TResult>`** — contrato genérico de un manejador que ejecuta una consulta de
   lectura y devuelve su resultado.

   > **Figura: Diagrama de herencia para `IQueryHandler`**
   - Es una interfaz **raíz** genérica, con el parámetro de consulta contravariante.

   > **Figura: Diagrama de colaboración para `IQueryHandler`**
   - Es el contrato de lectura simétrico a `ICommandHandler`; lo registra el módulo implementado
     `OpenGenericHandlersModule` por su forma genérica abierta.

### Comandos de aplicación

6. **Familia de comandos** — `CreateClassQrDebtCommand`, `CreateSubscriptionQrDebtCommand`,
   `ProcessQrCallbackCommand`. Registros sellados que transportan la intención de una escritura (crear
   una deuda de clase, crear una deuda de suscripción, procesar una notificación de cobro). Se
   consolidan por ser estructuralmente idénticos en su rol.

   > **Figura: Diagrama de herencia para la familia de comandos**
   - Cada uno es un `record` **raíz**: no hereda de otro tipo implementado.

   > **Figura: Diagrama de colaboración para la familia de comandos**
   - Cada comando es el parámetro de tipo del contrato implementado `ICommandHandler` de su manejador
     correspondiente (entradas 7 a 9).
   - `CreateClassQrDebtCommand` transporta el objeto de entrada implementado `CreateQrDebtDto`.

### Manejadores de comandos

7. **`CreateClassQrDebtCommandHandler`** — orquesta la creación de una deuda de clase por QR: valida la
   plantilla, arma el cobro pendiente y encola la publicación hacia Todotix y el vencimiento programado.

   > **Figura: Diagrama de herencia para `CreateClassQrDebtCommandHandler`**
   - Implementa el contrato implementado `ICommandHandler<CreateClassQrDebtCommand, CreateQrDebtOutcome>`.

   > **Figura: Diagrama de colaboración para `CreateClassQrDebtCommandHandler`**
   - Recibe por inyección de dependencias las interfaces implementadas `IDebtTemplateDao`,
     `IPendingQrPaymentDao`, `ITodotixOutboxDao`, `IExpirationOutboxDao`, `IClaimContext`,
     `IQrPaymentCreationBuilder` e `ITodotixAppKeyResolver`.
   - Recibe la interfaz externa `IUnitOfWork` (del paquete `DAMA.Software.MySqlUnitOfWork`) para
     escribir el cobro pendiente y los asientos de *outbox* en una sola transacción.
   - Devuelve el resultado discriminado implementado `CreateQrDebtOutcome`.

8. **`CreateSubscriptionQrDebtCommandHandler`** — orquesta la creación de una deuda de suscripción de
   academia por QR.

   > **Figura: Diagrama de herencia para `CreateSubscriptionQrDebtCommandHandler`**
   - Implementa el contrato implementado
     `ICommandHandler<CreateSubscriptionQrDebtCommand, CreateSubscriptionDebtOutcome>`.

   > **Figura: Diagrama de colaboración para `CreateSubscriptionQrDebtCommandHandler`**
   - Recibe por inyección de dependencias las interfaces implementadas `ISubscriptionPlanDao`,
     `IPendingSubscriptionPaymentDao`, `ITodotixOutboxDao`, `IClaimContext` e
     `ISubscriptionCreationBuilder`.
   - Recibe la interfaz externa `IUnitOfWork` y el tipo externo `IOptions<TodotixOptions>` (enlazado a
     las opciones implementadas `TodotixOptions`).
   - Devuelve el resultado discriminado implementado `CreateSubscriptionDebtOutcome`.

9. **`ProcessQrCallbackCommandHandler`** — procesa la notificación de cobro delegando en la estrategia
   de conciliación adecuada.

   > **Figura: Diagrama de herencia para `ProcessQrCallbackCommandHandler`**
   - Implementa el contrato implementado
     `ICommandHandler<ProcessQrCallbackCommand, ProcessQrCallbackResult>`.

   > **Figura: Diagrama de colaboración para `ProcessQrCallbackCommandHandler`**
   - Recibe por inyección de dependencias la colección de la interfaz implementada
     `IDebtCallbackStrategy` (todas las estrategias registradas) y recorre las estrategias hasta que
     una concilia la notificación.
   - Devuelve el resultado discriminado implementado `ProcessQrCallbackResult`.

### Estrategias de conciliación de cobro

10. **`IDebtCallbackStrategy`** — contrato de una estrategia que intenta conciliar una notificación de
    cobro contra un cobro pendiente.

    > **Figura: Diagrama de herencia para `IDebtCallbackStrategy`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IDebtCallbackStrategy`**
    - La implementa la base abstracta implementada `DebtCallbackStrategyBase<TPending>`.
    - La consume, como colección, el manejador implementado `ProcessQrCallbackCommandHandler`.

11. **`DebtCallbackStrategyBase<TPending>`** — base abstracta que fija la plantilla común de
    conciliación y deja el detalle por subdominio a las derivadas.

    > **Figura: Diagrama de herencia para `DebtCallbackStrategyBase`**
    - Implementa la interfaz implementada `IDebtCallbackStrategy`.
    - La heredan las clases implementadas `ClassDebtCallbackStrategy` y
      `SubscriptionDebtCallbackStrategy`.

    > **Figura: Diagrama de colaboración para `DebtCallbackStrategyBase`**
    - Su parámetro de tipo `TPending` se enlaza a las entidades implementadas `PendingQrPayment` y
      `PendingSubscriptionPayment` en las derivadas.

12. **`ClassDebtCallbackStrategy`** — concilia el cobro de una deuda de **clase**.

    > **Figura: Diagrama de herencia para `ClassDebtCallbackStrategy`**
    - Hereda de la clase implementada `DebtCallbackStrategyBase<PendingQrPayment>`.

    > **Figura: Diagrama de colaboración para `ClassDebtCallbackStrategy`**
    - Recibe por inyección de dependencias las interfaces implementadas `IPendingQrPaymentDao`,
      `ISuccessQrPaymentDao`, `IFailedQrPaymentDao`, `IOutboxEventDao`, `ITodotixClient`,
      `ITodotixAppKeyResolver` e `IQrPaymentTransitionBuilder`.
    - Recibe la interfaz externa `IUnitOfWork` para mover el cobro de pendiente a exitoso o fallido y
      escribir el asiento de *outbox* de evento de dominio en una transacción.

13. **`SubscriptionDebtCallbackStrategy`** — concilia el cobro de una deuda de **suscripción**.

    > **Figura: Diagrama de herencia para `SubscriptionDebtCallbackStrategy`**
    - Hereda de la clase implementada `DebtCallbackStrategyBase<PendingSubscriptionPayment>`.

    > **Figura: Diagrama de colaboración para `SubscriptionDebtCallbackStrategy`**
    - Colabora con el acceso a datos de suscripción (`IPendingSubscriptionPaymentDao`,
      `ISuccessSubscriptionPaymentDao`, `IFailedSubscriptionPaymentDao`), la interfaz externa
      `IUnitOfWork` y, al confirmarse el cobro, el actualizador de suscripción para propagar el nuevo
      nivel a Auth.

14. **`SubscriptionExpiryCalculator`** — auxiliar estático que calcula la nueva fecha de vencimiento de
    una suscripción a partir de su plan.

    > **Figura: Diagrama de herencia para `SubscriptionExpiryCalculator`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `SubscriptionExpiryCalculator`**
    - Opera sobre la entidad implementada `SubscriptionPlan` y su enumeración
      `SubscriptionDurationUnit`.

### Resultados discriminados

15. **`CreateQrDebtOutcome`** — unión de resultado de crear una deuda de clase: `Success` (con el dato
    de salida creado), `TemplateNotFound` y `PaymentNotConfigured`.

    > **Figura: Diagrama de herencia para `CreateQrDebtOutcome`**
    - Es un `record` abstracto **raíz**; sus casos sellados anidados heredan de él.

    > **Figura: Diagrama de colaboración para `CreateQrDebtOutcome`**
    - Lo devuelve el manejador implementado `CreateClassQrDebtCommandHandler`.
    - Su caso `Success` transporta el dato de salida implementado `QrDebtPendingDto`.

16. **`CreateSubscriptionDebtOutcome`** — unión de resultado de crear una deuda de suscripción:
    `Success`, `PlanNotFound` y `PaymentNotConfigured`.

    > **Figura: Diagrama de herencia para `CreateSubscriptionDebtOutcome`**
    - `record` abstracto **raíz** con casos sellados anidados.

    > **Figura: Diagrama de colaboración para `CreateSubscriptionDebtOutcome`**
    - Lo devuelve el manejador implementado `CreateSubscriptionQrDebtCommandHandler`; su caso `Success`
      transporta el dato de salida implementado `QrDebtPendingDto`.

17. **`GetQrDebtStatusOutcome`** — unión de resultado de consultar el estado de una deuda por QR:
    `Found` (con el estado) y `NotFound`.

    > **Figura: Diagrama de herencia para `GetQrDebtStatusOutcome`**
    - `record` abstracto **raíz** con casos sellados anidados.

    > **Figura: Diagrama de colaboración para `GetQrDebtStatusOutcome`**
    - Su caso `Found` transporta el dato de salida implementado `QrDebtStatusDto`.

18. **`HandleDebtExpiredOutcome`** — unión de resultado de manejar el vencimiento de una deuda:
    `Processed`, `AlreadyProcessed`, `PendingMissing` y `Failed` (con motivo).

    > **Figura: Diagrama de herencia para `HandleDebtExpiredOutcome`**
    - `record` abstracto **raíz** con casos sellados anidados.

    > **Figura: Diagrama de colaboración para `HandleDebtExpiredOutcome`**
    - Lo devuelve la implementación implementada `DebtExpiredHandler`; el caso `AlreadyProcessed`
      refleja la idempotencia del consumidor.

19. **`ProcessQrCallbackResult`** — unión de resultado de procesar la notificación de cobro:
    `Processed` y `DebtNotFound`.

    > **Figura: Diagrama de herencia para `ProcessQrCallbackResult`**
    - `record` abstracto **raíz** con casos sellados anidados.

    > **Figura: Diagrama de colaboración para `ProcessQrCallbackResult`**
    - Lo devuelve el manejador implementado `ProcessQrCallbackCommandHandler`.

20. **Familia de resultados de plantillas de deuda** — `CreateDebtTemplateOutcome` (con `Success` y
    `Replayed`), `GetDebtTemplateOutcome` (`Found`/`NotFound`), `UpdateDebtTemplateOutcome`
    (`Updated`/`NotFound`) y `DeleteDebtTemplateOutcome` (`Deleted`/`NotFound`). Se consolidan por
    compartir la misma forma de unión discriminada sobre el subdominio de plantillas.

    > **Figura: Diagrama de herencia para la familia de resultados de plantillas de deuda**
    - Cada uno es un `record` abstracto **raíz** con sus casos sellados anidados.

    > **Figura: Diagrama de colaboración para la familia de resultados de plantillas de deuda**
    - Los devuelve la implementación implementada `DebtTemplateService`.
    - Los casos con dato transportan los datos de salida implementados `DebtTemplateDto`. El caso
      `Replayed` de creación materializa la idempotencia de la creación de plantillas.

21. **Familia de resultados de Todotix** — `PublishOutcome` (`Success`, `TransientFailure`,
    `PermanentFailure`), `TestTodotixCredentialOutcome` (`Works`, `NotConfigured`, `Failed`) y
    `UpdateTodotixAppKeyOutcome` (`Updated`). Se consolidan por compartir la forma de unión
    discriminada sobre la integración con la pasarela.

    > **Figura: Diagrama de herencia para la familia de resultados de Todotix**
    - Cada uno es un `record` abstracto **raíz** con casos sellados anidados.

    > **Figura: Diagrama de colaboración para la familia de resultados de Todotix**
    - `PublishOutcome` lo devuelve el publicador de deudas implementado `TodotixDebtPublisher`, y su
      distinción entre fallo **transitorio** y **permanente** gobierna el reintento del trabajador
      implementado `TodotixOutboxWorker`.
    - `TestTodotixCredentialOutcome` y `UpdateTodotixAppKeyOutcome` los devuelve la implementación
      implementada `TodotixCredentialService`.

### Los cuatro *ledgers* de bandeja de salida y entrada

> Este grupo materializa el **patrón #9** (bandeja de salida transaccional con consumidor idempotente),
> generalizado a **cuatro registros**: tres bandejas de **salida** —eventos de dominio
> (`OutboxEvent`), vencimientos programados (`ExpirationOutboxEvent`) e integración con Todotix
> (`TodotixOutboxEvent`)— y una bandeja de **entrada** —notificaciones de cobro (`PaymentCallback`)—,
> más el almacén de **eventos procesados** que da idempotencia. Las tres bandejas de salida comparten
> un **modelo de estado** de relevo (pendiente → arrendado → publicado); la bandeja de entrada y el
> almacén de procesados usan el **otro modelo de estado**, de consumo (pendiente → arrendado →
> consumido/procesado).

22. **`OutboxEvent`** — asiento de bandeja de salida de un **evento de dominio** a publicar en el
    agente de mensajería.

    > **Figura: Diagrama de herencia para `OutboxEvent`**
    - Implementa las interfaces externas `IOutboxEvent` (del paquete `DAMA.Software.MySqlOutbox`) e
      `IEntity` (del paquete de acceso a datos).

    > **Figura: Diagrama de colaboración para `OutboxEvent`**
    - Lo escribe la estrategia implementada `ClassDebtCallbackStrategy` (vía `IOutboxEventDao`) y lo
      releva el publicador implementado `RabbitMqDomainEventPublisher`.

23. **`ExpirationOutboxEvent`** — asiento de bandeja de salida de un **vencimiento programado** de
    deuda.

    > **Figura: Diagrama de herencia para `ExpirationOutboxEvent`**
    - Implementa las interfaces externas `IOutboxEvent` e `IEntity`.

    > **Figura: Diagrama de colaboración para `ExpirationOutboxEvent`**
    - Lo escribe el manejador implementado `CreateClassQrDebtCommandHandler` (vía `IExpirationOutboxDao`)
      y lo releva el publicador implementado `RabbitMqExpirationPublisher`.

24. **`TodotixOutboxEvent`** — asiento de bandeja de salida de la **integración con Todotix** (la
    petición de registro de deuda en la pasarela).

    > **Figura: Diagrama de herencia para `TodotixOutboxEvent`**
    - Implementa las interfaces externas `IOutboxEvent` e `IEntity`.

    > **Figura: Diagrama de colaboración para `TodotixOutboxEvent`**
    - Lo escriben los manejadores de creación de deuda (vía `ITodotixOutboxDao`) y lo procesa el
      trabajador implementado `TodotixOutboxWorker`, que lo publica con reintento acotado.

25. **`PaymentCallback`** — asiento de bandeja de **entrada**: la notificación de cobro recibida de la
    pasarela, pendiente de procesar de forma idempotente.

    > **Figura: Diagrama de herencia para `PaymentCallback`**
    - Implementa las interfaces externas `IOutboxEvent` e `IEntity` (reutiliza el contrato de asiento,
      pero bajo el modelo de estado de **consumo**).

    > **Figura: Diagrama de colaboración para `PaymentCallback`**
    - Lo encola el controlador implementado `QrPaymentController` (vía `IPaymentCallbackInboxDao`) al
      recibir la notificación, y lo consume el trabajador implementado `PaymentCallbackWorker`.

26. **`IOutboxDao<TOutboxEvent>`** — contrato genérico de acceso a una bandeja de salida (arrendar
    pendientes, marcar publicados, borrar antiguos).

    > **Figura: Diagrama de herencia para `IOutboxDao`**
    - Es una interfaz **raíz** genérica; la especializan los contratos implementados `IOutboxEventDao`
      e `IExpirationOutboxDao`.

    > **Figura: Diagrama de colaboración para `IOutboxDao`**
    - La consumen, parametrizada, los trabajadores genéricos implementados `OutboxRelayWorker<TOutboxEvent>`
      y `OutboxJanitor<TOutboxEvent>`.

27. **`IOutboxEventDao` / `OutboxEventDao`** — contrato e implementación del acceso a la bandeja de
    salida de eventos de dominio.

    > **Figura: Diagrama de herencia para `IOutboxEventDao` / `OutboxEventDao`**
    - `IOutboxEventDao` especializa la interfaz implementada `IOutboxDao<OutboxEvent>`; `OutboxEventDao`
      implementa `IOutboxEventDao`.

    > **Figura: Diagrama de colaboración para `IOutboxEventDao` / `OutboxEventDao`**
    - `OutboxEventDao` opera sobre la entidad implementada `OutboxEvent` apoyándose en la conexión que
      provee la utilidad implementada `DBConnector`.

28. **`IExpirationOutboxDao` / `ExpirationOutboxDao`** — contrato e implementación del acceso a la
    bandeja de salida de vencimientos.

    > **Figura: Diagrama de herencia para `IExpirationOutboxDao` / `ExpirationOutboxDao`**
    - `IExpirationOutboxDao` especializa la interfaz implementada `IOutboxDao<ExpirationOutboxEvent>`;
      `ExpirationOutboxDao` la implementa.

    > **Figura: Diagrama de colaboración para `IExpirationOutboxDao` / `ExpirationOutboxDao`**
    - Opera sobre la entidad implementada `ExpirationOutboxEvent`.

29. **`ITodotixOutboxDao` / `TodotixOutboxDao`** — contrato e implementación del acceso a la bandeja de
    salida de Todotix, con arrendamiento y conteo de intentos.

    > **Figura: Diagrama de herencia para `ITodotixOutboxDao` / `TodotixOutboxDao`**
    - `ITodotixOutboxDao` es una interfaz **raíz** (no extiende `IOutboxDao` porque su ciclo de
      reintento difiere); `TodotixOutboxDao` la implementa.

    > **Figura: Diagrama de colaboración para `ITodotixOutboxDao` / `TodotixOutboxDao`**
    - Opera sobre la entidad implementada `TodotixOutboxEvent`.
    - La consumen los manejadores de creación de deuda (al encolar) y el trabajador implementado
      `TodotixOutboxWorker` (al relevar).

30. **`IPaymentCallbackInboxDao` / `PaymentCallbackInboxDao`** — contrato e implementación del acceso a
    la bandeja de **entrada** de notificaciones de cobro.

    > **Figura: Diagrama de herencia para `IPaymentCallbackInboxDao` / `PaymentCallbackInboxDao`**
    - `IPaymentCallbackInboxDao` es una interfaz **raíz**; `PaymentCallbackInboxDao` la implementa.

    > **Figura: Diagrama de colaboración para `IPaymentCallbackInboxDao` / `PaymentCallbackInboxDao`**
    - Opera sobre la entidad implementada `PaymentCallback`.
    - La consumen el controlador implementado `QrPaymentController` (al encolar) y el trabajador
      implementado `PaymentCallbackWorker` (al consumir).

31. **`IProcessedEventDao` / `ProcessedEventDao`** — contrato e implementación del **almacén de eventos
    procesados** que da idempotencia a los consumidores.

    > **Figura: Diagrama de herencia para `IProcessedEventDao` / `ProcessedEventDao`**
    - `IProcessedEventDao` especializa la interfaz externa `IProcessedEventStore` (del paquete
      `DAMA.Software.MySqlOutbox`); `ProcessedEventDao` implementa `IProcessedEventDao`.

    > **Figura: Diagrama de colaboración para `IProcessedEventDao` / `ProcessedEventDao`**
    - La consume la implementación implementada `DebtExpiredHandler` para descartar eventos ya
      procesados; el trabajador implementado `ProcessedEventsJanitor` purga sus registros antiguos.

32. **`IOutboxPublisher<TOutboxEvent>`** — contrato genérico de un publicador que entrega un asiento de
    bandeja de salida al agente de mensajería.

    > **Figura: Diagrama de herencia para `IOutboxPublisher`**
    - Es una interfaz **raíz** genérica; la implementan los publicadores `RabbitMqDomainEventPublisher`
      y `RabbitMqExpirationPublisher`.

    > **Figura: Diagrama de colaboración para `IOutboxPublisher`**
    - La consume, parametrizada, el trabajador implementado `OutboxRelayWorker<TOutboxEvent>`.

33. **`RabbitMqDomainEventPublisher`** — publica los eventos de dominio en el agente de mensajería.

    > **Figura: Diagrama de herencia para `RabbitMqDomainEventPublisher`**
    - Implementa la interfaz implementada `IOutboxPublisher<OutboxEvent>`.

    > **Figura: Diagrama de colaboración para `RabbitMqDomainEventPublisher`**
    - Colabora con la infraestructura de mensajería implementada `RabbitMqPublisherChannel` para
      publicar; el trabajador implementado `OutboxRelayWorker<OutboxEvent>` lo invoca.

34. **`RabbitMqExpirationPublisher`** — publica los vencimientos programados en el intercambio de
    mensajes retardados.

    > **Figura: Diagrama de herencia para `RabbitMqExpirationPublisher`**
    - Implementa la interfaz implementada `IOutboxPublisher<ExpirationOutboxEvent>`.

    > **Figura: Diagrama de colaboración para `RabbitMqExpirationPublisher`**
    - Colabora con `RabbitMqPublisherChannel`; el trabajador implementado
      `OutboxRelayWorker<ExpirationOutboxEvent>` lo invoca. Publica con el retardo que determina el
      vencimiento, para que el mensaje se entregue al expirar la deuda.

35. **`OutboxRelayWorker<TOutboxEvent>`** — trabajador en segundo plano genérico que releva una bandeja
    de salida: arrienda asientos pendientes y los publica.

    > **Figura: Diagrama de herencia para `OutboxRelayWorker`**
    - Hereda de la clase externa `BackgroundService` (de ASP.NET Core); su parámetro `TOutboxEvent` se
      restringe a la interfaz externa `IOutboxEvent`.

    > **Figura: Diagrama de colaboración para `OutboxRelayWorker`**
    - Recibe por inyección de dependencias la interfaz implementada `IOutboxPublisher<TOutboxEvent>` y
      el tipo externo `IServiceProvider`, del que resuelve por ámbito la interfaz implementada
      `IOutboxDao<TOutboxEvent>`.
    - Se registra dos veces (cerrado sobre `OutboxEvent` y sobre `ExpirationOutboxEvent`) por los
      módulos implementados `DomainEventOutboxModule` y `ExpirationOutboxModule`.

36. **`OutboxJanitor<TOutboxEvent>`** — trabajador en segundo plano genérico que borra periódicamente
    los asientos ya publicados más antiguos que la retención.

    > **Figura: Diagrama de herencia para `OutboxJanitor`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `OutboxJanitor`**
    - Resuelve por ámbito, desde el tipo externo `IServiceProvider`, la interfaz implementada
      `IOutboxDao<TOutboxEvent>` para purgar los asientos publicados.

37. **`TodotixOutboxWorker`** — trabajador en segundo plano que releva la bandeja de salida de Todotix
    y publica cada deuda en la pasarela con reintento acotado.

    > **Figura: Diagrama de herencia para `TodotixOutboxWorker`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `TodotixOutboxWorker`**
    - Resuelve por ámbito las interfaces implementadas `ITodotixOutboxDao` e `IPaymentDebtPublisher`.
    - Decide reintentar o desistir según el resultado discriminado implementado `PublishOutcome`
      (transitorio frente a permanente).

38. **`PaymentCallbackWorker`** — trabajador en segundo plano que consume la bandeja de entrada de
    notificaciones de cobro y delega en el manejador de conciliación.

    > **Figura: Diagrama de herencia para `PaymentCallbackWorker`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `PaymentCallbackWorker`**
    - Resuelve por ámbito la interfaz implementada `IPaymentCallbackInboxDao` y el contrato implementado
      `ICommandHandler<ProcessQrCallbackCommand, ProcessQrCallbackResult>`, al que entrega cada
      notificación arrendada.

39. **`ProcessedEventsJanitor`** — trabajador en segundo plano que purga el almacén de eventos
    procesados.

    > **Figura: Diagrama de herencia para `ProcessedEventsJanitor`**
    - Hereda de la clase externa `BackgroundService`.

    > **Figura: Diagrama de colaboración para `ProcessedEventsJanitor`**
    - Resuelve por ámbito la interfaz implementada `IProcessedEventDao` para borrar los registros
      vencidos.

### Idempotencia de cobros

40. **`QrPaymentIdempotency`** — entidad que registra la clave de idempotencia de un intento de cobro,
    para que una misma plantilla no genere deudas duplicadas.

    > **Figura: Diagrama de herencia para `QrPaymentIdempotency`**
    - Es una clase **raíz** (no implementa `IEntity`; se persiste por su propio objeto de acceso a
      datos de clave única).

    > **Figura: Diagrama de colaboración para `QrPaymentIdempotency`**
    - La gestiona la interfaz implementada `IQrPaymentIdempotencyDao`.

41. **`IQrPaymentIdempotencyDao` / `QrPaymentIdempotencyDao`** — contrato e implementación del registro
    de claves de idempotencia.

    > **Figura: Diagrama de herencia para `IQrPaymentIdempotencyDao` / `QrPaymentIdempotencyDao`**
    - `QrPaymentIdempotencyDao` hereda de la clase base externa `MySQLBaseDao<QrPaymentIdempotency>` (del
      paquete de acceso a datos) e implementa la interfaz implementada `IQrPaymentIdempotencyDao`.

    > **Figura: Diagrama de colaboración para `IQrPaymentIdempotencyDao` / `QrPaymentIdempotencyDao`**
    - La consume la implementación implementada `DebtTemplateService` al crear plantillas de forma
      idempotente.

### Acceso a datos por subdominio

42. **Familia de acceso a datos de cobros por QR** — contratos `IPendingQrPaymentDao`,
    `ISuccessQrPaymentDao`, `IFailedQrPaymentDao` y sus implementaciones `PendingQrPaymentDao`,
    `SuccessQrPaymentDao`, `FailedQrPaymentDao`. Se consolidan por compartir la misma forma sobre las
    tres entidades del ciclo de un cobro por QR (pendiente, exitoso, fallido).

    > **Figura: Diagrama de herencia para la familia de acceso a datos de cobros por QR**
    - Cada contrato especializa la interfaz externa `ISingleDao<T>` (del paquete de acceso a datos)
      cerrada sobre su entidad; cada implementación hereda de la clase base externa `MySQLSingleDao<T>`
      y realiza su contrato.

    > **Figura: Diagrama de colaboración para la familia de acceso a datos de cobros por QR**
    - Operan sobre las entidades implementadas `PendingQrPayment`, `SuccessQrPayment` y
      `FailedQrPayment`.
    - Las consumen los manejadores, las estrategias de conciliación, el servicio de consulta de cobros
      y el de resumen.

43. **`IStudentAnalyticsDao` / `StudentAnalyticsDao`** — contrato e implementación de las consultas
    analíticas del gasto del estudiante.

    > **Figura: Diagrama de herencia para `IStudentAnalyticsDao` / `StudentAnalyticsDao`**
    - `IStudentAnalyticsDao` es una interfaz **raíz** (consultas de proyección, no acceso por
      entidad); `StudentAnalyticsDao` la implementa.

    > **Figura: Diagrama de colaboración para `IStudentAnalyticsDao` / `StudentAnalyticsDao`**
    - Devuelve las filas de proyección implementadas `StudentQrBreakdownRow` y `StudentSpendMonthRow`.
    - La consume el servicio implementado `QrPaymentQueryService`.

44. **Familia de acceso a datos de cobros de suscripción** — contratos `IPendingSubscriptionPaymentDao`,
    `ISuccessSubscriptionPaymentDao`, `IFailedSubscriptionPaymentDao` y sus implementaciones. Réplica
    de la familia de cobros por QR sobre el subdominio de suscripciones.

    > **Figura: Diagrama de herencia para la familia de acceso a datos de cobros de suscripción**
    - Cada contrato es una interfaz **raíz** y cada implementación la realiza apoyándose en el acceso a
      datos del paquete externo.

    > **Figura: Diagrama de colaboración para la familia de acceso a datos de cobros de suscripción**
    - Operan sobre las entidades implementadas `PendingSubscriptionPayment`, `SuccessSubscriptionPayment`
      y `FailedSubscriptionPayment`.
    - Las consumen el manejador de creación de deuda de suscripción, la estrategia de conciliación de
      suscripción y el servicio de consulta de suscripciones.

45. **`ISubscriptionPlanDao` / `SubscriptionPlanDao`** — contrato e implementación del acceso a los
    planes de suscripción (los niveles ofrecidos).

    > **Figura: Diagrama de herencia para `ISubscriptionPlanDao` / `SubscriptionPlanDao`**
    - `ISubscriptionPlanDao` es una interfaz **raíz**; `SubscriptionPlanDao` la implementa.

    > **Figura: Diagrama de colaboración para `ISubscriptionPlanDao` / `SubscriptionPlanDao`**
    - Opera sobre la entidad implementada `SubscriptionPlan`.
    - La consumen el manejador de creación de deuda de suscripción y los servicios de plan y de consulta.

46. **`IAdminSubscriptionAnalyticsDao` / `AdminSubscriptionAnalyticsDao`** — contrato e implementación
    de las consultas analíticas de ingresos por suscripción para administración.

    > **Figura: Diagrama de herencia para `IAdminSubscriptionAnalyticsDao` / `AdminSubscriptionAnalyticsDao`**
    - `IAdminSubscriptionAnalyticsDao` es una interfaz **raíz**; `AdminSubscriptionAnalyticsDao` la
      implementa.

    > **Figura: Diagrama de colaboración para `IAdminSubscriptionAnalyticsDao` / `AdminSubscriptionAnalyticsDao`**
    - Devuelve las filas de proyección implementadas `SubscriptionRevenueTotalRow`,
      `SubscriptionRevenueMonthRow` y `SubscriptionRevenueTierRow`.
    - La consume el servicio implementado `AdminAnalyticsService`.

47. **`IDebtTemplateDao` / `DebtTemplateDao`** — contrato e implementación del acceso a las plantillas
    de deuda.

    > **Figura: Diagrama de herencia para `IDebtTemplateDao` / `DebtTemplateDao`**
    - `IDebtTemplateDao` especializa la interfaz externa `ISingleDao<DebtTemplate>`; `DebtTemplateDao`
      hereda de la clase base externa `MySQLSingleDao<DebtTemplate>` y realiza el contrato.

    > **Figura: Diagrama de colaboración para `IDebtTemplateDao` / `DebtTemplateDao`**
    - Opera sobre la entidad implementada `DebtTemplate`.
    - La consumen el servicio implementado `DebtTemplateService` y el manejador de creación de deuda de
      clase.

48. **Acceso a las credenciales de pago de la academia** — contratos segregados
    `ITenantPaymentCredentialReader` (lectura) e `ITenantPaymentCredentialWriter` (escritura) y la
    implementación única `TenantPaymentCredentialDao`. Ejemplo claro de **segregación de interfaces**:
    una sola clase realiza dos contratos estrechos para que cada consumidor dependa solo de lo que usa.

    > **Figura: Diagrama de herencia para el acceso a las credenciales de pago de la academia**
    - `TenantPaymentCredentialDao` hereda de la clase base externa
      `MySQLSingleDao<TenantPaymentCredential>` e implementa ambas interfaces implementadas de lectura y
      escritura.

    > **Figura: Diagrama de colaboración para el acceso a las credenciales de pago de la academia**
    - Opera sobre la entidad implementada `TenantPaymentCredential`.
    - El lector lo consumen el resolutor implementado `TodotixAppKeyResolver` y el servicio implementado
      `TodotixCredentialService`; el escritor lo consume `TodotixCredentialService`.

### Filas de proyección analítica

49. **Familia de filas de proyección** — `StudentQrBreakdownRow`, `StudentSpendMonthRow`,
    `SubscriptionRevenueTotalRow`, `SubscriptionRevenueMonthRow` y `SubscriptionRevenueTierRow`. Tipos
    de valor de solo lectura (`readonly record struct`) que transportan las filas crudas de las
    consultas analíticas.

    > **Figura: Diagrama de herencia para la familia de filas de proyección**
    - Cada uno es un `readonly record struct` **raíz**: no participa en jerarquía de herencia.

    > **Figura: Diagrama de colaboración para la familia de filas de proyección**
    - Las devuelven los objetos de acceso a datos analíticos implementados `StudentAnalyticsDao` y
      `AdminSubscriptionAnalyticsDao`, y las proyectan a datos de salida los servicios implementados
      `QrPaymentQueryService` y `AdminAnalyticsService`.

### Inyectores de semilla y utilidades de base de datos

50. **Familia de inyectores de semilla** — `DebtTemplateInjector`, `PendingQrPaymentInjector`,
    `SuccessQrPaymentInjector`, `FailedQrPaymentInjector` y `SuccessSubscriptionPaymentInjector`.
    Estructuralmente idénticos: cada uno carga los datos de prueba de una tabla en el entorno de
    desarrollo.

    > **Figura: Diagrama de herencia para la familia de inyectores de semilla**
    - Cada uno hereda de la clase base externa `DataInjector` (del paquete de acceso a datos).

    > **Figura: Diagrama de colaboración para la familia de inyectores de semilla**
    - Los orquesta la utilidad implementada `DBInjector`, que los descubre y ejecuta durante la siembra
      del entorno de desarrollo.

51. **`DBConnector`** — utilidad estática que centraliza la apertura de conexiones a la base de datos.

    > **Figura: Diagrama de herencia para `DBConnector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBConnector`**
    - La usan los objetos de acceso a datos implementados para obtener su conexión.

52. **`DBInjector`** — utilidad estática que descubre y ejecuta los inyectores de semilla.

    > **Figura: Diagrama de herencia para `DBInjector`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `DBInjector`**
    - Orquesta la familia de inyectores implementados que heredan de `DataInjector`; la invoca la clase
      estática implementada `DatabaseSeeder` durante el arranque del entorno de desarrollo.

### Constructores

53. **`IQrPaymentCreationBuilder` / `QrPaymentCreationBuilder`** — contrato e implementación que arma el
    cobro pendiente por QR y la petición de registro de deuda en Todotix.

    > **Figura: Diagrama de herencia para `IQrPaymentCreationBuilder` / `QrPaymentCreationBuilder`**
    - `QrPaymentCreationBuilder` implementa la interfaz implementada `IQrPaymentCreationBuilder`.

    > **Figura: Diagrama de colaboración para `IQrPaymentCreationBuilder` / `QrPaymentCreationBuilder`**
    - Recibe por inyección de dependencias el servicio implementado `ICallbackSignature` y el tipo
      externo `IOptions<TodotixOptions>`.
    - Construye la entidad implementada `PendingQrPayment` y la entidad de *outbox* implementada
      `TodotixOutboxEvent`. Lo consume el manejador implementado `CreateClassQrDebtCommandHandler`.

54. **`IQrPaymentTransitionBuilder` / `QrPaymentTransitionBuilder`** — contrato e implementación que arma
    la transición de un cobro de pendiente a exitoso o fallido.

    > **Figura: Diagrama de herencia para `IQrPaymentTransitionBuilder` / `QrPaymentTransitionBuilder`**
    - `QrPaymentTransitionBuilder` implementa la interfaz implementada `IQrPaymentTransitionBuilder`.

    > **Figura: Diagrama de colaboración para `IQrPaymentTransitionBuilder` / `QrPaymentTransitionBuilder`**
    - Construye las entidades implementadas `SuccessQrPayment` y `FailedQrPayment` a partir de un
      `PendingQrPayment`. Lo consumen la estrategia implementada `ClassDebtCallbackStrategy` y la
      implementación implementada `DebtExpiredHandler`.

55. **`IQrPaymentViewBuilder` / `QrPaymentViewBuilder`** — contrato e implementación que arma los datos
    de salida (vista) de un cobro.

    > **Figura: Diagrama de herencia para `IQrPaymentViewBuilder` / `QrPaymentViewBuilder`**
    - `QrPaymentViewBuilder` implementa la interfaz implementada `IQrPaymentViewBuilder`.

    > **Figura: Diagrama de colaboración para `IQrPaymentViewBuilder` / `QrPaymentViewBuilder`**
    - Construye los datos de salida implementados de cobro (`PendingQrDebtDto`, `QrDebtStatusDto`, …).
      Lo consumen los servicios implementados `QrPaymentQueryService` y `SubscriptionQueryService`.

56. **`ISubscriptionCreationBuilder` / `SubscriptionCreationBuilder`** — contrato e implementación que
    arma el cobro pendiente de suscripción y su petición a Todotix.

    > **Figura: Diagrama de herencia para `ISubscriptionCreationBuilder` / `SubscriptionCreationBuilder`**
    - `SubscriptionCreationBuilder` implementa la interfaz implementada `ISubscriptionCreationBuilder`.

    > **Figura: Diagrama de colaboración para `ISubscriptionCreationBuilder` / `SubscriptionCreationBuilder`**
    - Recibe por inyección de dependencias el servicio implementado `ICallbackSignature` y el tipo
      externo `IOptions<TodotixOptions>`.
    - Construye la entidad implementada `PendingSubscriptionPayment` y el asiento implementado
      `TodotixOutboxEvent`. Lo consume el manejador implementado `CreateSubscriptionQrDebtCommandHandler`.

57. **`ISubscriptionTransitionBuilder` / `SubscriptionTransitionBuilder`** — contrato e implementación
    que arma la transición de estado de un cobro de suscripción.

    > **Figura: Diagrama de herencia para `ISubscriptionTransitionBuilder` / `SubscriptionTransitionBuilder`**
    - `SubscriptionTransitionBuilder` implementa la interfaz implementada `ISubscriptionTransitionBuilder`.

    > **Figura: Diagrama de colaboración para `ISubscriptionTransitionBuilder` / `SubscriptionTransitionBuilder`**
    - Construye las entidades implementadas `SuccessSubscriptionPayment` y `FailedSubscriptionPayment`.
      Lo consume la estrategia implementada `SubscriptionDebtCallbackStrategy`.

58. **`IDebtTemplateBuilder` / `DebtTemplateBuilder`** — contrato e implementación que arma la entidad de
    plantilla de deuda a partir de los datos de entrada.

    > **Figura: Diagrama de herencia para `IDebtTemplateBuilder` / `DebtTemplateBuilder`**
    - `DebtTemplateBuilder` implementa la interfaz implementada `IDebtTemplateBuilder`.

    > **Figura: Diagrama de colaboración para `IDebtTemplateBuilder` / `DebtTemplateBuilder`**
    - Construye la entidad implementada `DebtTemplate`. Lo consume el servicio implementado
      `DebtTemplateService`.

59. **`ITodotixCredentialTestBuilder` / `TodotixCredentialTestBuilder`** — contrato e implementación que
    arma la petición de prueba de una credencial de Todotix.

    > **Figura: Diagrama de herencia para `ITodotixCredentialTestBuilder` / `TodotixCredentialTestBuilder`**
    - `TodotixCredentialTestBuilder` implementa la interfaz implementada `ITodotixCredentialTestBuilder`.

    > **Figura: Diagrama de colaboración para `ITodotixCredentialTestBuilder` / `TodotixCredentialTestBuilder`**
    - Lo consume el servicio implementado `TodotixCredentialService` al probar una credencial.

60. **`ITodotixCredentialViewBuilder` / `TodotixCredentialViewBuilder`** — contrato e implementación que
    arma los datos de salida del estado de la credencial de Todotix.

    > **Figura: Diagrama de herencia para `ITodotixCredentialViewBuilder` / `TodotixCredentialViewBuilder`**
    - `TodotixCredentialViewBuilder` implementa la interfaz implementada `ITodotixCredentialViewBuilder`.

    > **Figura: Diagrama de colaboración para `ITodotixCredentialViewBuilder` / `TodotixCredentialViewBuilder`**
    - Construye los datos de salida implementados `TodotixAppKeyStatusDto` y `PaymentAvailabilityDto`. Lo
      consume el servicio implementado `TodotixCredentialService`.

### Capa de servicio

61. **`IAdminAnalyticsService` / `AdminAnalyticsService`** — contrato e implementación de las analíticas
    de ingresos por suscripción para administración.

    > **Figura: Diagrama de herencia para `IAdminAnalyticsService` / `AdminAnalyticsService`**
    - `AdminAnalyticsService` implementa la interfaz implementada `IAdminAnalyticsService`.

    > **Figura: Diagrama de colaboración para `IAdminAnalyticsService` / `AdminAnalyticsService`**
    - Recibe por inyección de dependencias la interfaz implementada `IAdminSubscriptionAnalyticsDao` y el
      tipo externo `IOptions<CurrencyOptions>`.
    - Devuelve los datos de salida implementados `SubscriptionRevenueTotalDto`,
      `SubscriptionRevenuePointDto` y `SubscriptionRevenueByTierDto`. Lo consume el controlador
      implementado `AdminAnalyticsController`.

62. **`IDebtTemplateService` / `DebtTemplateService`** — contrato e implementación de la gestión de
    plantillas de deuda (crear, consultar, actualizar, borrar) de forma idempotente.

    > **Figura: Diagrama de herencia para `IDebtTemplateService` / `DebtTemplateService`**
    - `DebtTemplateService` implementa la interfaz implementada `IDebtTemplateService`.

    > **Figura: Diagrama de colaboración para `IDebtTemplateService` / `DebtTemplateService`**
    - Recibe por inyección de dependencias las interfaces implementadas `IDebtTemplateDao`,
      `IQrPaymentIdempotencyDao`, `IClaimContext` e `IDebtTemplateBuilder`, el tipo externo
      `IUnitOfWork` y el tipo externo `IMapper` (de AutoMapper).
    - Devuelve la familia de resultados implementada de plantillas (entrada 20). Lo consume el
      controlador implementado `DebtTemplateController`.

63. **`IQrPaymentQueryService` / `QrPaymentQueryService`** — contrato e implementación de las consultas
    de cobros por QR y del desglose de gasto del estudiante.

    > **Figura: Diagrama de herencia para `IQrPaymentQueryService` / `QrPaymentQueryService`**
    - `QrPaymentQueryService` implementa la interfaz implementada `IQrPaymentQueryService`.

    > **Figura: Diagrama de colaboración para `IQrPaymentQueryService` / `QrPaymentQueryService`**
    - Recibe por inyección de dependencias las interfaces implementadas `IPendingQrPaymentDao`,
      `ISuccessQrPaymentDao`, `IFailedQrPaymentDao`, `ITodotixOutboxDao`, `IStudentAnalyticsDao`,
      `IClaimContext` e `IQrPaymentViewBuilder`, más los tipos externos `IMapper` e
      `IOptions<CurrencyOptions>`.
    - Lo consume el controlador implementado `QrPaymentController`.

64. **`ISubscriptionPlanService` / `SubscriptionPlanService`** — contrato e implementación de la consulta
    y actualización de los planes de suscripción.

    > **Figura: Diagrama de herencia para `ISubscriptionPlanService` / `SubscriptionPlanService`**
    - `SubscriptionPlanService` implementa la interfaz implementada `ISubscriptionPlanService`.

    > **Figura: Diagrama de colaboración para `ISubscriptionPlanService` / `SubscriptionPlanService`**
    - Recibe por inyección de dependencias la interfaz implementada `ISubscriptionPlanDao` y el tipo
      externo `IOptions<CurrencyOptions>`. Lo consume el controlador implementado
      `SubscriptionPaymentController`.

65. **`ISubscriptionQueryService` / `SubscriptionQueryService`** — contrato e implementación de las
    consultas de cobros de suscripción.

    > **Figura: Diagrama de herencia para `ISubscriptionQueryService` / `SubscriptionQueryService`**
    - `SubscriptionQueryService` implementa la interfaz implementada `ISubscriptionQueryService`.

    > **Figura: Diagrama de colaboración para `ISubscriptionQueryService` / `SubscriptionQueryService`**
    - Recibe por inyección de dependencias las interfaces implementadas `IPendingSubscriptionPaymentDao`,
      `ISuccessSubscriptionPaymentDao`, `ITodotixOutboxDao`, `ISubscriptionPlanDao`, `IClaimContext` e
      `IQrPaymentViewBuilder`. Lo consume el controlador implementado `SubscriptionPaymentController`.

66. **`ISummaryService` / `SummaryService`** — contrato e implementación del resumen de cobros de la
    academia.

    > **Figura: Diagrama de herencia para `ISummaryService` / `SummaryService`**
    - `SummaryService` implementa la interfaz implementada `ISummaryService`.

    > **Figura: Diagrama de colaboración para `ISummaryService` / `SummaryService`**
    - Recibe por inyección de dependencias la interfaz implementada `ISuccessQrPaymentDao`,
      `IClaimContext` y el tipo externo `IOptions<CurrencyOptions>`.
    - Devuelve el dato de salida implementado `PaymentSummaryDto`. Lo consume el controlador implementado
      `SummaryController`.

67. **`IDebtExpiredHandler` / `DebtExpiredHandler`** — contrato e implementación que maneja, de forma
    idempotente, el vencimiento de una deuda consumido del agente de mensajería.

    > **Figura: Diagrama de herencia para `IDebtExpiredHandler` / `DebtExpiredHandler`**
    - `DebtExpiredHandler` implementa la interfaz implementada `IDebtExpiredHandler`.

    > **Figura: Diagrama de colaboración para `IDebtExpiredHandler` / `DebtExpiredHandler`**
    - Recibe por inyección de dependencias las interfaces implementadas `IProcessedEventDao`,
      `IPendingQrPaymentDao`, `IFailedQrPaymentDao` e `IQrPaymentTransitionBuilder`, y el tipo externo
      `IUnitOfWork`.
    - Devuelve el resultado discriminado implementado `HandleDebtExpiredOutcome`; consulta el almacén de
      eventos procesados para no manejar dos veces el mismo vencimiento. Lo consume el consumidor
      implementado `DebtExpiredConsumer`.

68. **`ICallbackSignature` / `CallbackSignature`** — contrato e implementación que firma y verifica la
    autenticidad de la notificación de cobro.

    > **Figura: Diagrama de herencia para `ICallbackSignature` / `CallbackSignature`**
    - `CallbackSignature` implementa la interfaz implementada `ICallbackSignature`.

    > **Figura: Diagrama de colaboración para `ICallbackSignature` / `CallbackSignature`**
    - Recibe por inyección de dependencias el tipo externo `IOptions<PaymentCallbackOptions>`.
    - La consumen los constructores implementados `QrPaymentCreationBuilder` y
      `SubscriptionCreationBuilder` (al firmar la URL de notificación) y el controlador implementado
      `QrPaymentController` (al verificar la notificación entrante).

### Integración con la pasarela Todotix

69. **`ITodotixClient` / `TodotixClient`** — contrato e implementación del cliente HTTP de la pasarela
    Todotix.

    > **Figura: Diagrama de herencia para `ITodotixClient` / `TodotixClient`**
    - `TodotixClient` implementa la interfaz implementada `ITodotixClient`.

    > **Figura: Diagrama de colaboración para `ITodotixClient` / `TodotixClient`**
    - Recibe por inyección de dependencias el tipo externo `HttpClient` (registrado por el módulo
      implementado `TodotixHttpClientModule`).
    - Intercambia los datos de transporte implementados `RegisterDebtRequest`/`RegisterDebtResponse` y
      `ConsultDebtRequest`/`ConsultDebtResponse`. Lo consumen el publicador implementado
      `TodotixDebtPublisher`, las estrategias de conciliación y el servicio implementado
      `TodotixCredentialService`.

70. **`IPaymentDebtPublisher` / `TodotixDebtPublisher`** — contrato e implementación que publica una
    deuda pendiente en Todotix y actualiza la imagen del QR resultante.

    > **Figura: Diagrama de herencia para `IPaymentDebtPublisher` / `TodotixDebtPublisher`**
    - `TodotixDebtPublisher` implementa la interfaz implementada `IPaymentDebtPublisher`.

    > **Figura: Diagrama de colaboración para `IPaymentDebtPublisher` / `TodotixDebtPublisher`**
    - Recibe por inyección de dependencias la interfaz implementada `ITodotixClient` y la **colección**
      de la interfaz implementada `IQrImageUrlUpdater` (selecciona la adecuada según el tipo de deuda).
    - Devuelve el resultado discriminado implementado `PublishOutcome`. Lo consume el trabajador
      implementado `TodotixOutboxWorker`.

71. **`ITodotixAppKeyResolver` / `TodotixAppKeyResolver`** — contrato e implementación que resuelve y
    descifra la clave de aplicación de Todotix de la academia.

    > **Figura: Diagrama de herencia para `ITodotixAppKeyResolver` / `TodotixAppKeyResolver`**
    - `TodotixAppKeyResolver` implementa la interfaz implementada `ITodotixAppKeyResolver`.

    > **Figura: Diagrama de colaboración para `ITodotixAppKeyResolver` / `TodotixAppKeyResolver`**
    - Recibe por inyección de dependencias las interfaces implementadas `ITenantPaymentCredentialReader`
      e `IAppKeyCipher`. Lo consumen los manejadores de creación de deuda y las estrategias de
      conciliación.

72. **`ITodotixCredentialService` / `TodotixCredentialService`** — contrato e implementación de la
    gestión de la credencial de Todotix de la academia (estado, revelado, actualización y prueba).

    > **Figura: Diagrama de herencia para `ITodotixCredentialService` / `TodotixCredentialService`**
    - `TodotixCredentialService` implementa la interfaz implementada `ITodotixCredentialService`.

    > **Figura: Diagrama de colaboración para `ITodotixCredentialService` / `TodotixCredentialService`**
    - Recibe por inyección de dependencias las interfaces implementadas
      `ITenantPaymentCredentialReader`, `ITenantPaymentCredentialWriter`, `IAppKeyCipher`,
      `IClaimContext`, `ITodotixCredentialViewBuilder`, `ITodotixClient` e
      `ITodotixCredentialTestBuilder`.
    - Devuelve las familias de resultados implementadas `UpdateTodotixAppKeyOutcome` y
      `TestTodotixCredentialOutcome`. Lo consume el controlador implementado `TodotixCredentialController`.

73. **`IQrImageUrlUpdater` y sus implementaciones** — contrato e implementaciones que actualizan la URL
    de la imagen del QR en el cobro pendiente, una por tipo de deuda: `ClassQrImageUrlUpdater` y
    `SubscriptionQrImageUrlUpdater`.

    > **Figura: Diagrama de herencia para `IQrImageUrlUpdater` y sus implementaciones**
    - `ClassQrImageUrlUpdater` y `SubscriptionQrImageUrlUpdater` implementan la misma interfaz
      implementada `IQrImageUrlUpdater`, distinguiéndose por la enumeración implementada `DebtKind` que
      cada una declara.

    > **Figura: Diagrama de colaboración para `IQrImageUrlUpdater` y sus implementaciones**
    - `ClassQrImageUrlUpdater` colabora con `IPendingQrPaymentDao`; `SubscriptionQrImageUrlUpdater`, con
      `IPendingSubscriptionPaymentDao`.
    - El publicador implementado `TodotixDebtPublisher` recibe ambas como colección y selecciona la
      adecuada por su `DebtKind`.

74. **`TodotixDebtState`** — enumeración del estado de una deuda en la pasarela.

    > **Figura: Diagrama de herencia para `TodotixDebtState`**
    - Es una enumeración **raíz**.

    > **Figura: Diagrama de colaboración para `TodotixDebtState`**
    - La usan el cliente implementado `TodotixClient` y las estrategias de conciliación al interpretar
      la respuesta de la pasarela.

75. **Familia de datos de transporte de Todotix** — `RegisterDebtRequest`, `RegisterDebtLine`,
    `RegisterDebtResponse`, `ConsultDebtRequest`, `ConsultDebtData` y `ConsultDebtResponse`. Modelan el
    cuerpo de las peticiones y respuestas del protocolo HTTP de la pasarela.

    > **Figura: Diagrama de herencia para la familia de datos de transporte de Todotix**
    - Cada uno es una clase **raíz** de datos de transporte: no participa en jerarquía de herencia.

    > **Figura: Diagrama de colaboración para la familia de datos de transporte de Todotix**
    - Los serializa e interpreta el cliente implementado `TodotixClient`. `RegisterDebtRequest` agrega
      varias `RegisterDebtLine`.

### Cliente gRPC de suscripción

76. **`IAuthSubscriptionUpdater` / `GrpcAuthSubscriptionUpdater`** — contrato e implementación que informa
    a Auth, por gRPC, del nuevo nivel y vencimiento de suscripción de una academia.

    > **Figura: Diagrama de herencia para `IAuthSubscriptionUpdater` / `GrpcAuthSubscriptionUpdater`**
    - `GrpcAuthSubscriptionUpdater` implementa la interfaz implementada `IAuthSubscriptionUpdater`.

    > **Figura: Diagrama de colaboración para `IAuthSubscriptionUpdater` / `GrpcAuthSubscriptionUpdater`**
    - Recibe por inyección de dependencias el cliente externo generado
      `TenantSubscription.TenantSubscriptionClient` (del paquete `DAMA.Software.GrpcContracts`) y le
      envía el mensaje externo generado `UpdateTenantSubscriptionRequest`.
    - Lo consume la estrategia implementada `SubscriptionDebtCallbackStrategy` al confirmarse el cobro de
      una suscripción.

77. **`SubscriptionSecretClientInterceptor`** — interceptor de cliente gRPC que adjunta el secreto
    compartido a cada llamada al servidor de suscripción de Auth.

    > **Figura: Diagrama de herencia para `SubscriptionSecretClientInterceptor`**
    - Hereda de la clase externa `Interceptor` (de `Grpc.Core.Interceptors`).

    > **Figura: Diagrama de colaboración para `SubscriptionSecretClientInterceptor`**
    - Recibe por inyección de dependencias el tipo externo `IOptions<SubscriptionGrpcOptions>`, de cuyo
      secreto compone la cabecera de metadatos de la llamada.
    - Lo enlaza al canal del cliente generado `TenantSubscription.TenantSubscriptionClient` el módulo
      implementado `GrpcClientsModule`.

### Datos de entrada y salida

78. **Interfaces de segregación de datos** — `IDebtTemplateData`, `IQrDebtState` e `IQrPaymentLine`.
    Contratos que comparten las proyecciones comunes entre datos de entrada y de salida del mismo
    subdominio, para que cada consumidor dependa solo de los campos que usa.

    > **Figura: Diagrama de herencia para las interfaces de segregación de datos**
    - Cada una es una interfaz **raíz**; las realizan los datos de entrada/salida correspondientes
      (entradas 79 a 81).

    > **Figura: Diagrama de colaboración para las interfaces de segregación de datos**
    - `IDebtTemplateData` la comparten `CreateDebtTemplateDto` y `UpdateDebtTemplateDto`; `IQrDebtState`
      la comparten `QrDebtPendingDto` y `QrDebtStatusDto`; `IQrPaymentLine` la comparten
      `PendingQrDebtDto`, `SuccessQrPaymentDto` y `FailedQrPaymentDto`.

79. **Familia de datos de plantillas de deuda** — entrada `CreateDebtTemplateDto`,
    `UpdateDebtTemplateDto` y salida `DebtTemplateDto`.

    > **Figura: Diagrama de herencia para la familia de datos de plantillas de deuda**
    - `CreateDebtTemplateDto` y `UpdateDebtTemplateDto` implementan la interfaz implementada
      `IDebtTemplateData`; `DebtTemplateDto` es una clase **raíz**.

    > **Figura: Diagrama de colaboración para la familia de datos de plantillas de deuda**
    - Los de entrada los validan los validadores implementados de plantilla y los consume el servicio
      implementado `DebtTemplateService`; el de salida lo proyecta el perfil implementado
      `DebtTemplateProfile`.

80. **Familia de datos de cobros por QR** — `CreateQrDebtDto` (entrada), y salidas `PendingQrDebtDto`,
    `QrDebtPendingDto`, `QrDebtStatusDto`, `FailedQrPaymentDto`, `SuccessQrPaymentDto`,
    `StudentQrBreakdownDto` y `StudentSpendPointDto`.

    > **Figura: Diagrama de herencia para la familia de datos de cobros por QR**
    - `QrDebtPendingDto` y `QrDebtStatusDto` implementan la interfaz implementada `IQrDebtState`;
      `PendingQrDebtDto`, `SuccessQrPaymentDto` y `FailedQrPaymentDto` implementan `IQrPaymentLine`; el
      resto son clases **raíz**.

    > **Figura: Diagrama de colaboración para la familia de datos de cobros por QR**
    - Los arma el constructor implementado `QrPaymentViewBuilder` y los devuelven los servicios y
      controladores de cobro. `CreateQrDebtDto` lo valida `CreateQrDebtDtoValidator`.

81. **Familia de datos de suscripción** — entrada `CreateSubscriptionDebtDto`, `UpdateSubscriptionPlanDto`
    y salida `SubscriptionPlanDto`.

    > **Figura: Diagrama de herencia para la familia de datos de suscripción**
    - Cada una es una clase sellada **raíz**.

    > **Figura: Diagrama de colaboración para la familia de datos de suscripción**
    - Los de entrada los validan los validadores implementados de suscripción; el de salida lo devuelve
      el servicio implementado `SubscriptionPlanService`.

82. **Familia de datos analíticos de administración** — `SubscriptionRevenueTotalDto`,
    `SubscriptionRevenuePointDto` y `SubscriptionRevenueByTierDto`.

    > **Figura: Diagrama de herencia para la familia de datos analíticos de administración**
    - Cada una es una clase **raíz**.

    > **Figura: Diagrama de colaboración para la familia de datos analíticos de administración**
    - Los proyecta, desde las filas de proyección implementadas, el servicio implementado
      `AdminAnalyticsService`.

83. **`PaymentSummaryDto`** — dato de salida del resumen de cobros de la academia.

    > **Figura: Diagrama de herencia para `PaymentSummaryDto`**
    - Es una clase **raíz**.

    > **Figura: Diagrama de colaboración para `PaymentSummaryDto`**
    - Lo construye y devuelve el servicio implementado `SummaryService`.

84. **Familia de datos de la credencial de Todotix** — entrada `UpdateTodotixAppKeyDto` y salidas
    `PaymentAvailabilityDto`, `TodotixAppKeyRevealDto` y `TodotixAppKeyStatusDto`.

    > **Figura: Diagrama de herencia para la familia de datos de la credencial de Todotix**
    - Cada una es una clase **raíz**.

    > **Figura: Diagrama de colaboración para la familia de datos de la credencial de Todotix**
    - El de entrada lo valida `UpdateTodotixAppKeyDtoValidator`; los de salida los arma el constructor
      implementado `TodotixCredentialViewBuilder` y los devuelve el servicio implementado
      `TodotixCredentialService`.

### Modelo persistente

85. **Familia de entidades de cobro por QR** — `PendingQrPayment`, `SuccessQrPayment` y
    `FailedQrPayment`: el ciclo de vida de un cobro de clase (pendiente, exitoso, fallido).

    > **Figura: Diagrama de herencia para la familia de entidades de cobro por QR**
    - Cada una implementa la interfaz externa `IEntity` (del paquete de acceso a datos).

    > **Figura: Diagrama de colaboración para la familia de entidades de cobro por QR**
    - Las gestionan los objetos de acceso a datos implementados de la entrada 42 y las construyen los
      constructores implementados `QrPaymentCreationBuilder` y `QrPaymentTransitionBuilder`.

86. **Familia de entidades de suscripción** — `PendingSubscriptionPayment`, `SuccessSubscriptionPayment`
    y `SubscriptionPlan`.

    > **Figura: Diagrama de herencia para la familia de entidades de suscripción**
    - Cada una implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para la familia de entidades de suscripción**
    - `PendingSubscriptionPayment` y `SuccessSubscriptionPayment` las gestionan los objetos de acceso a
      datos de la entrada 44 y las construyen los constructores de suscripción; `SubscriptionPlan` la
      gestiona `SubscriptionPlanDao` y la usa el cálculo implementado `SubscriptionExpiryCalculator`.

87. **`DebtTemplate`** — entidad de una plantilla de deuda reutilizable.

    > **Figura: Diagrama de herencia para `DebtTemplate`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `DebtTemplate`**
    - La gestiona la interfaz implementada `IDebtTemplateDao` y la construye el constructor implementado
      `DebtTemplateBuilder`.

88. **`TenantPaymentCredential`** — entidad de la credencial de pago (clave de aplicación de Todotix)
    de una academia.

    > **Figura: Diagrama de herencia para `TenantPaymentCredential`**
    - Implementa la interfaz externa `IEntity`.

    > **Figura: Diagrama de colaboración para `TenantPaymentCredential`**
    - La gestionan los contratos implementados `ITenantPaymentCredentialReader` e
      `ITenantPaymentCredentialWriter`; su clave la cifra y descifra la implementación implementada
      `AppKeyCipher`.

89. **Enumeraciones del dominio** — `DebtKind` (tipo de deuda: clase o suscripción), `FailureReason`
    (motivo de fallo de un cobro) y `SubscriptionDurationUnit` (unidad de duración de un plan).

    > **Figura: Diagrama de herencia para las enumeraciones del dominio**
    - Cada una es una enumeración **raíz**.

    > **Figura: Diagrama de colaboración para las enumeraciones del dominio**
    - `DebtKind` distingue las implementaciones de `IQrImageUrlUpdater`; `FailureReason` lo fijan los
      constructores de transición; `SubscriptionDurationUnit` lo lee el cálculo implementado
      `SubscriptionExpiryCalculator`.

### Eventos de dominio

90. **`PaymentCapturedEvent`** (con `PaymentCapturedData`) — evento que comunica que un cobro fue
    capturado, para que otros servicios reaccionen (por ejemplo, Attendance).

    > **Figura: Diagrama de herencia para `PaymentCapturedEvent`**
    - Es un `record` **raíz**; agrega el `record` implementado `PaymentCapturedData` como carga.

    > **Figura: Diagrama de colaboración para `PaymentCapturedEvent`**
    - Lo serializa, como cuerpo de un `OutboxEvent`, el publicador implementado
      `RabbitMqDomainEventPublisher` al relevar la bandeja de salida de eventos de dominio.

91. **`DebtExpiredEvent`** (con `DebtExpiredData`) — evento de vencimiento de deuda que el servicio se
    publica a sí mismo a través del intercambio de mensajes retardados.

    > **Figura: Diagrama de herencia para `DebtExpiredEvent`**
    - Es una clase **raíz**; agrega la clase implementada `DebtExpiredData` como carga.

    > **Figura: Diagrama de colaboración para `DebtExpiredEvent`**
    - Lo publica (con retardo) el publicador implementado `RabbitMqExpirationPublisher` y lo consume,
      como parámetro de tipo del despachador implementado `RabbitMqMessageDispatcher<DebtExpiredEvent>`,
      el consumidor implementado `DebtExpiredConsumer`.

### Perfil de AutoMapper

92. **`DebtTemplateProfile`** — perfil de proyección entre la entidad de plantilla y su dato de salida.

    > **Figura: Diagrama de herencia para `DebtTemplateProfile`**
    - Hereda de la clase externa `Profile` (de AutoMapper).

    > **Figura: Diagrama de colaboración para `DebtTemplateProfile`**
    - Define la proyección de la entidad implementada `DebtTemplate` al dato de salida implementado
      `DebtTemplateDto`. Lo registra el módulo implementado `AutoMapperModule`.

### Seguridad

93. **`AuthClaims`** — clase estática con los nombres de los claims del token.

    > **Figura: Diagrama de herencia para `AuthClaims`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `AuthClaims`**
    - La consumen la clase implementada `ClaimContext` y el módulo implementado `JwtAuthenticationModule`.

94. **`UserRoles`** — clase estática con los nombres de los roles del sistema.

    > **Figura: Diagrama de herencia para `UserRoles`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `UserRoles`**
    - La consumen los seis controladores implementados en sus atributos de autorización por rol.

95. **`RequiresServiceTierAttribute`** — atributo de autorización que exige a la academia un nivel de
    suscripción mínimo para acceder a un punto de la API.

    > **Figura: Diagrama de herencia para `RequiresServiceTierAttribute`**
    - Hereda de la clase externa `Attribute` e implementa la interfaz externa `IAuthorizationFilter`
      (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `RequiresServiceTierAttribute`**
    - Lee el claim de nivel de suscripción a través del contexto de autorización de MVC; lo aplican los
      controladores que requieren un nivel mínimo.

96. **`IAppKeyCipher` / `AppKeyCipher`** — contrato e implementación del cifrado simétrico de la clave de
    aplicación de Todotix.

    > **Figura: Diagrama de herencia para `IAppKeyCipher` / `AppKeyCipher`**
    - `AppKeyCipher` implementa la interfaz implementada `IAppKeyCipher`.

    > **Figura: Diagrama de colaboración para `IAppKeyCipher` / `AppKeyCipher`**
    - Recibe la clave de cifrado de 32 bytes en su construcción (provista por el módulo de arranque).
    - La consumen el resolutor implementado `TodotixAppKeyResolver` y el servicio implementado
      `TodotixCredentialService`.

### Filtros de MVC

97. **`FluentValidationActionFilter`** — filtro que ejecuta los validadores de FluentValidation antes de
    la acción del controlador y normaliza los errores.

    > **Figura: Diagrama de herencia para `FluentValidationActionFilter`**
    - Implementa la interfaz externa `IAsyncActionFilter` (de ASP.NET Core MVC).

    > **Figura: Diagrama de colaboración para `FluentValidationActionFilter`**
    - Resuelve los validadores implementados `AbstractValidator<T>` del contenedor y consulta el atributo
      implementado `RuleSetAttribute` para elegir el conjunto de reglas.

98. **`RuleSetAttribute`** — atributo que indica qué conjunto de reglas de validación aplicar a una
    acción.

    > **Figura: Diagrama de herencia para `RuleSetAttribute`**
    - Hereda de la clase externa `Attribute`.

    > **Figura: Diagrama de colaboración para `RuleSetAttribute`**
    - Lo lee el filtro implementado `FluentValidationActionFilter`.

### Validadores

99. **Familia de validadores** — `CreateDebtTemplateDtoValidator`, `UpdateDebtTemplateDtoValidator`,
    `CreateQrDebtDtoValidator`, `CreateSubscriptionDebtDtoValidator`, `UpdateSubscriptionPlanDtoValidator`,
    `UpdateTodotixAppKeyDtoValidator` y `PaginationParamsDtoValidator`. Estructuralmente idénticos: cada
    uno declara las reglas de un objeto de entrada.

    > **Figura: Diagrama de herencia para la familia de validadores**
    - Cada uno hereda de la clase base externa `AbstractValidator<T>` (de FluentValidation) cerrada
      sobre su dato de entrada.

    > **Figura: Diagrama de colaboración para la familia de validadores**
    - Los descubre y registra en bloque el módulo implementado `ValidationModule`, y los ejecuta el
      filtro implementado `FluentValidationActionFilter`.

### Comprobaciones de disponibilidad

100. **Familia de sondas de salud** — `DatabaseHealthCheck`, `RabbitMqHealthCheck` y
     `GrpcPeerHealthCheck`. Cada una comprueba la disponibilidad de una dependencia externa (base de
     datos, agente de mensajería y par gRPC de Auth).

     > **Figura: Diagrama de herencia para la familia de sondas de salud**
     - Cada una implementa la interfaz externa `IHealthCheck` (de ASP.NET Core).

     > **Figura: Diagrama de colaboración para la familia de sondas de salud**
     - Las registra el módulo implementado `HealthCheckModule` y las nombra la clase implementada
       `ExternalCheckNaming`; el resultado lo formatea la clase implementada `ReadinessResponseWriter`.

101. **`ExternalCheckNaming`, `ExternalDependency` y `ReadinessResponseWriter`** — la clase estática de
     nombres de comprobación, la enumeración de dependencias externas y el escritor estático de la
     respuesta de disponibilidad.

     > **Figura: Diagrama de herencia para `ExternalCheckNaming`, `ExternalDependency` y `ReadinessResponseWriter`**
     - `ExternalCheckNaming` y `ReadinessResponseWriter` son clases estáticas **raíz**;
       `ExternalDependency` es una enumeración **raíz**.

     > **Figura: Diagrama de colaboración para `ExternalCheckNaming`, `ExternalDependency` y `ReadinessResponseWriter`**
     - Los consumen las sondas de salud implementadas y el módulo implementado `HealthCheckModule` al
       publicar el punto de acceso de disponibilidad.

### Infraestructura de mensajería RabbitMQ

102. **`RabbitMqConnectionFactory`** — fábrica que crea y reabre la conexión con el agente de mensajería.

     > **Figura: Diagrama de herencia para `RabbitMqConnectionFactory`**
     - Es una clase **raíz**.

     > **Figura: Diagrama de colaboración para `RabbitMqConnectionFactory`**
     - La consumen el consumidor implementado `DebtExpiredConsumer` y el canal implementado
       `RabbitMqPublisherChannel`; se apoya en la biblioteca externa `RabbitMQ.Client`.

103. **`RabbitMqTopologyDeclarer`** — declara la topología (intercambios, colas y enlaces) necesaria en
     el agente de mensajería.

     > **Figura: Diagrama de herencia para `RabbitMqTopologyDeclarer`**
     - Es una clase **raíz**.

     > **Figura: Diagrama de colaboración para `RabbitMqTopologyDeclarer`**
     - Aplica el descriptor implementado `RabbitMqTopologyDescriptor`. Lo consume el consumidor
       implementado `DebtExpiredConsumer`.

104. **`RabbitMqMessageDispatcher<TEvent>`** — despachador genérico que entrega cada mensaje consumido al
     manejador correspondiente dentro de un ámbito de inyección de dependencias.

     > **Figura: Diagrama de herencia para `RabbitMqMessageDispatcher`**
     - Es una clase **raíz** genérica.

     > **Figura: Diagrama de colaboración para `RabbitMqMessageDispatcher`**
     - Recibe por inyección de dependencias el tipo externo `IServiceScopeFactory`, del que abre un
       ámbito para resolver el manejador. Lo consume el consumidor implementado `DebtExpiredConsumer`
       cerrado sobre `DebtExpiredEvent`.

105. **`RabbitMqPublisherChannel`** — canal de publicación reutilizable hacia el agente de mensajería.

     > **Figura: Diagrama de herencia para `RabbitMqPublisherChannel`**
     - Implementa la interfaz externa `IAsyncDisposable` (del lenguaje).

     > **Figura: Diagrama de colaboración para `RabbitMqPublisherChannel`**
     - Se apoya en la fábrica implementada `RabbitMqConnectionFactory`. Lo consumen los publicadores
       implementados `RabbitMqDomainEventPublisher` y `RabbitMqExpirationPublisher`.

106. **`RabbitMqTopologyDescriptor`** — registro que describe la topología (intercambio retardado, cola,
     clave de enrutamiento y prefetch).

     > **Figura: Diagrama de herencia para `RabbitMqTopologyDescriptor`**
     - Es un `record` sellado **raíz**.

     > **Figura: Diagrama de colaboración para `RabbitMqTopologyDescriptor`**
     - Lo arma el consumidor implementado `DebtExpiredConsumer` a partir de las opciones implementadas
       `RabbitMqOptions` y lo aplica el declarador implementado `RabbitMqTopologyDeclarer`.

107. **`DebtExpiredConsumer`** — consumidor en segundo plano que escucha los vencimientos de deuda y los
     entrega al manejador idempotente.

     > **Figura: Diagrama de herencia para `DebtExpiredConsumer`**
     - Hereda de la clase externa `BackgroundService`.

     > **Figura: Diagrama de colaboración para `DebtExpiredConsumer`**
     - Recibe por inyección de dependencias la fábrica implementada `RabbitMqConnectionFactory`, el
       declarador implementado `RabbitMqTopologyDeclarer`, el despachador implementado
       `RabbitMqMessageDispatcher<DebtExpiredEvent>` y el tipo externo `IOptions<RabbitMqOptions>`.
     - El despachador resuelve, por evento, el manejador implementado `IDebtExpiredHandler`.

### Opciones tipadas, tipos comunes y log estructurado

108. **Familia de opciones tipadas** — `CurrencyOptions`, `PaymentCallbackOptions`, `RabbitMqOptions`,
     `SubscriptionGrpcOptions` y `TodotixOptions`. Objetos de configuración enlazados por `IOptions<T>`.

     > **Figura: Diagrama de herencia para la familia de opciones tipadas**
     - Cada una es una clase sellada **raíz**.

     > **Figura: Diagrama de colaboración para la familia de opciones tipadas**
     - Las enlaza a la configuración el módulo implementado `OptionsModule` y las consumen, vía el tipo
       externo `IOptions<T>`, los servicios, constructores, trabajadores e interceptor que las necesitan.

109. **`PageDto<T>`** — dato de salida genérico de una página de resultados.

     > **Figura: Diagrama de herencia para `PageDto`**
     - Es una clase **raíz** genérica.

     > **Figura: Diagrama de colaboración para `PageDto`**
     - La devuelven los servicios de consulta implementados al paginar listados.

110. **`PaginationParamsDto`** — dato de entrada de los parámetros de paginación.

     > **Figura: Diagrama de herencia para `PaginationParamsDto`**
     - Es una clase **raíz**.

     > **Figura: Diagrama de colaboración para `PaginationParamsDto`**
     - Lo valida el validador implementado `PaginationParamsDtoValidator` y lo reciben los puntos de
       acceso de listado de los controladores.

111. **`LogEvents`** — clase estática parcial de log de alto rendimiento generado en tiempo de
     compilación.

     > **Figura: Diagrama de herencia para `LogEvents`**
     - Clase estática parcial **raíz**.

     > **Figura: Diagrama de colaboración para `LogEvents`**
     - La consumen los trabajadores, consumidores y servicios para emitir el log estructurado del
       servicio.

### Composición de módulos

112. **`IServiceModule`** — contrato de un módulo que **registra servicios** durante el arranque.

     > **Figura: Diagrama de herencia para `IServiceModule`**
     - Es una interfaz **raíz**.

     > **Figura: Diagrama de colaboración para `IServiceModule`**
     - Su operación de registro recibe los tipos externos `IServiceCollection` e `IConfiguration`.
     - La implementan los módulos de registro del servicio (entrada 115).

113. **`IAppModule`** — contrato de un módulo que **configura la canalización** de la aplicación.

     > **Figura: Diagrama de herencia para `IAppModule`**
     - Es una interfaz **raíz**.

     > **Figura: Diagrama de colaboración para `IAppModule`**
     - Su operación de configuración recibe el tipo externo `WebApplication`.
     - La implementan los módulos que intervienen en la fase de aplicación (entrada 115).

114. **`ModuleHost`** — anfitrión estático que descubre los módulos por reflexión y los ejecuta ordenados
     por su propiedad de orden.

     > **Figura: Diagrama de herencia para `ModuleHost`**
     - Clase estática **raíz**.

     > **Figura: Diagrama de colaboración para `ModuleHost`**
     - Descubre y orquesta las interfaces implementadas `IServiceModule` e `IAppModule`.
     - Recibe los tipos externos `WebApplicationBuilder` (fase de registro) y `WebApplication` (fase de
       configuración).

115. **Familia de módulos de composición** — los veintiséis módulos `*Module` del servicio, consolidados
     por compartir el mismo rol estructural (cada uno implementa `IServiceModule`, `IAppModule` o ambos
     y configura una porción del arranque). Por su función se agrupan así:
     - **Validación de secretos y cabeceras:** `SecretsValidationModule` (con la clase auxiliar interna
       `SecretsValidation`), `ForwardedHeadersModule`, `RequestCorrelationModule`.
     - **Autenticación y autorización:** `JwtAuthenticationModule`, `AuthorizationModule`,
       `ClaimsLogScopeModule`.
     - **Persistencia y registro automático:** `PersistenceModule`, `AutoRegisteredServicesModule`,
       `OpenGenericHandlersModule`, `AutoMapperModule`, `OptionsModule`, `ValidationModule`.
     - **Mensajería y los cuatro *ledgers*:** `RabbitMqInfrastructureModule`, `DomainEventOutboxModule`,
       `ExpirationOutboxModule`, `ProcessedEventsModule`.
     - **Integración Todotix y gRPC:** `TodotixHttpClientModule`, `TodotixIntegrationWorkersModule`,
       `GrpcClientsModule`.
     - **Canalización de la API y observabilidad:** `MvcModule`, `ProblemDetailsModule`,
       `HttpContextModule`, `HealthCheckModule`.

     > **Figura: Diagrama de herencia para la familia de módulos de composición**
     - Cada módulo implementa la interfaz implementada `IServiceModule`, la interfaz implementada
       `IAppModule`, o ambas, según participe en la fase de registro, en la de configuración o en las
       dos.

     > **Figura: Diagrama de colaboración para la familia de módulos de composición**
     - Los descubre y ejecuta ordenados la clase implementada `ModuleHost`.
     - Entre ellos enlazan las piezas del servicio: `PersistenceModule` registra los objetos de acceso a
       datos; `AutoRegisteredServicesModule` registra servicios y claims por exploración de namespaces;
       `OpenGenericHandlersModule` registra los manejadores genéricos del mediador; los módulos de
       *outbox* registran los trabajadores genéricos implementados `OutboxRelayWorker<T>` y
       `OutboxJanitor<T>`; `GrpcClientsModule` enlaza el cliente generado de suscripción con el
       interceptor implementado `SubscriptionSecretClientInterceptor`; `SecretsValidationModule` valida
       con la clase auxiliar implementada `SecretsValidation` la clave que recibe la implementación
       implementada `AppKeyCipher`.

> **Nota sobre `DatabaseSeeder` y `SecretsValidation`.** `DatabaseSeeder` es una clase estática del
> namespace `Modules` que, en el entorno de desarrollo, dispara la siembra a través de la utilidad
> implementada `DBInjector`; no es un módulo (no implementa los contratos de módulo). `SecretsValidation`
> es la clase auxiliar **interna** y estática que acompaña a `SecretsValidationModule`. Doxygen las
> grafica junto a las piezas que las usan.

---

## Comandos de demostración

```bash
# Tipos implementados en Payment (lo que Doxygen diagrama)
find apps/Payment/Backend -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | sort

# Relaciones de herencia (qué implementa/hereda cada tipo)
grep -rn "class .*:\|interface \|abstract class\|static class\|record " apps/Payment/Backend --include=*.cs | grep -v "/obj/"

# Los cuatro ledgers: entidades de outbox/inbox y sus objetos de acceso a datos
grep -rln "IOutboxEvent\|IOutboxDao\|ProcessedEvent\|PaymentCallback" apps/Payment/Backend --include=*.cs | grep -v "/obj/"

# Generar los grafos de jerarquía, herencia y colaboración del servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
#   salida: extra/graphics/out/doxygen/html/
```
