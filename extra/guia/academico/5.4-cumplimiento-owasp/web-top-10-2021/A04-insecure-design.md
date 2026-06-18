# A04:2021 · Insecure Design (Web Application Top 10 2021)

> **Estado:** 🟢 — Cubierto por diseño: patrones transaccionales (Outbox/Inbox idempotente), aislamiento multitenant en la propia consulta SQL, bloqueo de cuenta, tiers de suscripción y *architecture tests* que fijan los invariantes en tiempo de compilación.

## Introducción

Esta ficha documenta cómo DAMA aborda el **diseño inseguro** (A04), que no es un defecto puntual sino la ausencia de patrones seguros, modelado de amenazas y controles que hagan imposibles ciertas clases de fallo por construcción. El documento reúne la evidencia técnica de los patrones de diseño presentes en la plataforma: el flujo transaccional Outbox/Inbox con idempotencia, el aislamiento multitenant anclado en la propia consulta SQL, el bloqueo de cuenta atómico en la base de datos, el rate-limiting centralizado en el gateway, los tiers de suscripción como atributo reutilizable y los architecture tests que fijan los invariantes en tiempo de compilación. Cierra con el flujo de componentes, los comandos de verificación y las brechas conocidas (entre ellas la ausencia de un threat model formal versionado).

## Qué exige OWASP

A04 no es un bug concreto sino la ausencia de **diseño seguro**: amenazas modeladas, patrones seguros reutilizables, límites de confianza claros y controles que hagan imposibles ciertas clases de fallo por construcción (no solo por implementación correcta). OWASP pide *secure design patterns*, *threat modeling* y *reference architectures* en lugar de parches puntuales.

## Cómo lo cumple DAMA

### 1. Outbox + Inbox transaccional con idempotencia (no publicar directo al broker)

DAMA nunca publica a RabbitMQ desde un controlador/servicio. En la misma transacción que muta el agregado se inserta una fila en `outbox_events`; un `BackgroundService` la entrega después. Del lado consumidor, la idempotencia se ancla en `processed_events`: el handler marca el `EventId` *antes* del efecto, y un duplicado (MySQL `1062`) significa "ya procesado". El pipeline es *at-least-once*, por lo que el ledger es obligatorio.

El handler de `payment.captured` envuelve su efecto en `IdempotentTransaction.RunAsync`, al que pasa la unidad de trabajo, el DAO de eventos procesados, el `EventId` y el resultado `AlreadyProcessed`; dentro del *callback* incrementa las clases restantes del estudiante y registra el crédito en el ledger de trazabilidad, devolviendo `RemainCredited` solo la primera vez (`apps/Attendance/Backend/Services/Concrete/Events/PaymentCapturedHandler.cs:38`).

`IdempotentTransaction.RunAsync` (paquete `DAMA.Software.MySqlOutbox`) encapsula `BeginAsync → TryMarkProcessed → branch → Commit`: si la fila ya existía, retorna `AlreadyProcessed` sin re-aplicar el efecto. El diseño elimina por construcción los créditos duplicados ante redelivery. (Patrón #7 de `apps/CLAUDE.md`.)

### 2. Multitenancy: el filtro por tenant vive en la consulta, no en el código de aplicación

Los datos de un tenant no pueden filtrarse a otro porque el `tenantId` es parámetro obligatorio del *stored procedure* y forma parte del `WHERE` con sufijo `*ForTenant`:

Por ejemplo, la lectura de un usuario invoca el procedimiento almacenado `GetUserByIdForTenant` enlazando tanto `@userId` como `@tenantId` como parámetros (`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:124`), y el procedimiento filtra con `WHERE td.TenantId = tenantId`, calificando la columna con el alias de tabla (`infrastructure/environments/auth/init.sql:133`).

El `tenantId` se deriva del claim verificado (`IClaimContext`), nunca de un parámetro libre de la petición — el aislamiento es estructural, no opcional. (Patrón #8 de `apps/CLAUDE.md`.)

### 3. Bloqueo de cuenta como decisión de diseño (en la BD, atómica)

El conteo de intentos fallidos y el bloqueo se modelan como un *stored procedure* (`RegisterFailedLoginAttempt`) con umbral y ventana fijos en código, no como lógica dispersa por el servicio:

El umbral y la ventana son constantes del DAO —máximo 5 intentos fallidos y bloqueo de 900 segundos— (`apps/Auth/Backend/DB/Daos/Concrete/Single/Users/UserDao.cs:19`).

El estado `LockedUntil` se lee en el login (`ReadUserWithTenantByUserNameAsync`, línea 229). El conteo atómico en el SP evita condiciones de carrera entre intentos concurrentes.

### 4. Rate-limiting como control de diseño centralizado (en el gateway, no por servicio)

El *throttling* / anti-fuerza-bruta es una decisión arquitectónica: vive en nginx (`infrastructure/environments/api-gateway/nginx.conf`), no replicado en cada backend. Los servicios no llevan `RateLimiter` — un solo punto de entrada (el gateway) aplica el límite, lo que evita inconsistencias entre servicios. (Ver A04 en el diagrama y notas de infraestructura.)

### 5. Tiers de suscripción: autorización por nivel modelada como atributo reutilizable

El acceso a flujos de pago QR exige un nivel mínimo en la "pirámide de servicios", verificado contra el claim y su expiración por un atributo de autorización reutilizable:

El atributo resuelve el nivel efectivo leyendo el claim `IndexCoreServicesPyramid` solo si la suscripción no ha expirado, y devuelve `0` tanto si ya expiró como si el claim falta (`apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:36`).

Una suscripción expirada degrada el nivel efectivo a `0` (fail-safe). El control se aplica declarativamente con `[RequiresServiceTier(3)]` sobre cada acción, no con `if` dispersos.

### 6. Architecture tests que fijan los invariantes de diseño

El invariante "nunca publicar a RabbitMQ fuera de Messaging/Workers/Modules" se hace cumplir en el *build* con NetArchTest, no solo por convención:

La regla declara que ningún tipo fuera de los namespaces `Backend.Messaging`, `Backend.Workers`, `Backend.Modules` y `Backend.ExternalCheck` puede depender de `RabbitMQ.Client` (`apps/Attendance/Test/Architecture/OutboxPublishingRulesTests.cs:15`).

En Auth/CourseManagement/Payment el mismo archivo añade una **segunda** regla que confina la abstracción del publicador a esos namespaces. Añadir un `publish` desde un controlador/servicio rompe la compilación de la suite — el diseño se vuelve no-eludible.

## Flujo de los componentes

Rectángulo **A04 Insecure Design** del diagrama FossFlow `extra/graphics/diagrams/owasp-web-top-10.json` (nodos `Outbox + Inbox`, `Idempotencia processed_events`, `Multitenancy por tenant`, `Account Lockout`):

```
Mutación de agregado (transacción)
   │  INSERT outbox_events  ── mismo commit ──▶ estado consistente o nada
   ▼
[Outbox + Inbox]  OutboxPublisher (BackgroundService) ─▶ dama.events (RabbitMQ)
   │
   ▼  Consumer (otro servicio)
[Idempotencia processed_events]  TryMarkProcessed → 1062 = ya procesado → no re-aplica
   │
   ▼
[Multitenancy por tenant]  SP *ForTenant: WHERE td.TenantId = tenantId
   │
[Account Lockout]  RegisterFailedLoginAttempt (umbral 5 / 900s) → LockedUntil
```

1. La escritura del agregado y la fila **Outbox** comparten una transacción: o se publican juntos o nada.
2. El publicador entrega a `dama.events`; el consumidor aplica el efecto bajo **idempotencia processed_events** (re-entrega = no-op).
3. Toda lectura/escritura respeta el **aislamiento multitenant** en el SP, y el login está protegido por **Account Lockout** atómico.

## Verificación

- Architecture tests (fijan el invariante de publicación):
  ```bash
  cd apps/Attendance/Test && dotnet test --filter "FullyQualifiedName~OutboxPublishingRulesTests"
  ```
- Confirmar que ningún controlador/servicio referencia el cliente de RabbitMQ:
  ```bash
  grep -rln "RabbitMQ.Client" apps/*/Backend/Controllers apps/*/Backend/Services
  ```
- Confirmar idempotencia en los handlers de eventos:
  ```bash
  grep -rln "IdempotentTransaction.RunAsync" apps/*/Backend
  ```

## Notas y brechas conocidas

- **Estado 🟢 (diseño/proceso):** estos controles no requirieron código nuevo en las olas de endurecimiento — son patrones presentes desde el diseño de cada servicio; aquí se documentan y se respaldan con tests.
- El pipeline de eventos es **at-least-once, no exactly-once**: la corrección depende de que todo handler nuevo use `IdempotentTransaction`/`processed_events`. Saltarse el ledger porque "el handler parece idempotente" reintroduce el riesgo.
- El rate-limiting centralizado en nginx implica que un despliegue que exponga un backend **sin** pasar por el gateway perdería ese control; el diseño asume el gateway como único ingreso (ver `infrastructure/CLAUDE.md`).
- No existe un documento formal de *threat model* versionado; el modelado de amenazas está implícito en los patrones y estos `.md` de OWASP. Formalizarlo es trabajo pendiente.
