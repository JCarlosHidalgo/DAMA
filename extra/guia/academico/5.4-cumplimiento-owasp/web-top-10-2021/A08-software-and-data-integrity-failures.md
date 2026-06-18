# A08:2021 · Fallos de Integridad de Software y Datos (Software and Data Integrity Failures)

> **Estado:** ✅ — Consumo de eventos idempotente (`processed_events`), deserialización segura con `System.Text.Json` (sin `BinaryFormatter`), y validación de la cadena de confianza TLS interna vía CA en el trust store de cada cliente gRPC.

## Introducción

Esta ficha documenta cómo DAMA atiende los **fallos de integridad de software y datos** (A08), el riesgo de no proteger la integridad de los datos y de la cadena de suministro frente a duplicados, replays, mutaciones o deserialización insegura. El documento expone la evidencia técnica: la idempotencia del consumo de eventos anclada en la tabla `processed_events`, la deserialización segura con `System.Text.Json` (sin `BinaryFormatter` ni manejo de tipos arbitrarios) y la validación de la cadena de confianza TLS interna mediante una CA propia en el trust store de cada cliente gRPC. Incluye el flujo de componentes productor-consumidor, los comandos de verificación y las brechas conocidas (la cadena de suministro de dependencias se cubre en A06).

## Qué exige OWASP
Garantizar la integridad de datos y de la cadena de suministro: deserializar solo con serializadores seguros (nunca formatos que permitan ejecución de código), confiar únicamente en fuentes verificadas (firmas/certificados), y proteger las tuberías de datos contra duplicados, replays o mutaciones no autorizadas.

## Cómo lo cumple DAMA

### Idempotencia del consumo de eventos (`processed_events`)
La mensajería entre servicios es **at-least-once**: un reenvío tras un fallo puede re-disparar un evento. La integridad de datos se protege con la tabla `processed_events`, donde el `EventId` se inserta dentro de la misma transacción que el efecto de negocio, **antes** de aplicarlo. Una violación de clave única (MySQL 1062) significa "ya procesado" → no se reaplica.

El DAO de eventos procesados ejecuta un `INSERT` del `EventId` con su marca temporal: si la inserción tiene éxito devuelve verdadero (primera vez), y si la base de datos lanza la excepción de duplicado con número 1062 la captura y devuelve falso, señalando que el evento ya estaba registrado (`apps/Attendance/Backend/DB/Daos/Concrete/Single/Events/ProcessedEventDao.cs:21-34`).

La tabla declara el `EventId` como clave primaria, lo que garantiza la unicidad que sostiene la captura del error 1062 (`infrastructure/environments/attendance/init.sql:151-152`).

Cada handler envuelve "marcar procesado + aplicar efecto + commit" con `IdempotentTransaction.RunAsync` (del paquete `DAMA.Software.MySqlOutbox`), que comete por cuenta del llamante: recibe la unidad de trabajo, el DAO de eventos procesados, el `EventId` del evento y el resultado `AlreadyProcessed` a devolver si ya estaba registrado, y sólo ejecuta el efecto de negocio dentro del ámbito transaccional cuando es la primera vez (`apps/Attendance/Backend/Services/Concrete/Events/PaymentCapturedHandler.cs:38-43`).

**Invariante de integridad**: el `Id` de la fila de la Bandeja de Salida del productor y el `EventId` del payload son el mismo `Guid`, así que el `EventId` es a la vez clave primaria de la Bandeja de Salida y clave de idempotencia del consumidor. Patrón documentado en `apps/CLAUDE.md` #7.

### Deserialización segura: `System.Text.Json`, nunca `BinaryFormatter`
Los payloads de evento se deserializan con `System.Text.Json` sobre el cuerpo del mensaje; un payload malformado se registra y se confirma (ACK) descartándolo en lugar de reencolarlo en bucle, y un evento nulo o veneno se descarta igualmente.

El despachador deserializa el cuerpo del mensaje hacia el tipo de evento esperado; si la deserialización lanza una excepción, registra el evento `BadPayloadDropped`, confirma el mensaje (ACK) y retorna sin reprocesar. Si el evento resultante es nulo o coincide con la condición de mensaje veneno, registra `InvalidEventDropped`, confirma y retorna igualmente (`apps/Attendance/Backend/Workers/Infrastructure/RabbitMqMessageDispatcher.cs:79` y `:81-97`).

No hay `BinaryFormatter`, `SoapFormatter`, `NetDataContractSerializer` ni `TypeNameHandling.All` en el código: los eventos cruzan fronteras como JSON con forma fija (POCOs `Events/*Event.cs`), deliberadamente desacoplados de los tipos internos del productor — no se deserializan tipos arbitrarios desde la red.

### Validación de la cadena de confianza TLS interna
El gRPC interno se valida contra una CA propia. Cada cliente (Attendance, Payment) instala `ca.crt` en el almacén de confianza del SO al arrancar, de modo que la librería gRPC valida el certificado del servidor por nombre sin callbacks que se salten la verificación.

El script de arranque comprueba si existe `ca.crt` en la ruta de certificados, la copia al directorio de CAs locales del contenedor y ejecuta `update-ca-certificates` para registrarla en el almacén de confianza del SO (`infrastructure/environments/attendance/entrypoint.sh`, idéntico en `payment/entrypoint.sh`).

La CA y los certificados de servidor (SAN = hostname del contenedor) los emite `infrastructure/environments/tls-init/bootstrap-tls.sh` en el volumen `dama-tls`. Así, un servidor gRPC no confiable no supera la validación de la cadena: la integridad del canal entre servicios está anclada en la CA, no en confianza ciega.

## Flujo de los componentes

```
Productor (Auth/CourseManagement/Payment)
  └─ inserta fila en Bandeja de Salida  (Id == EventId)  ── misma tx que muta el agregado
       │  Publicador de la Bandeja de Salida ──► RabbitMQ (dama.events)
       ▼
Consumidor (Attendance)
  RabbitMqMessageDispatcher
   ├─ Deserialización con System.Text.Json
   │    └─ payload corrupto / nulo / veneno ──► ACK + descarte (sin bucle)
   ▼
  Handler ─ IdempotentTransaction.RunAsync(EventId)
   ├─ INSERT processed_events(EventId)
   │     └─ 1062 (duplicado) ──► AlreadyProcessed, NO reaplica el efecto ──► ACK
   └─ primera vez ──► aplica efecto + commit ──► ACK
                       (fallo/excepción ──► NACK con reencolado)

Canal síncrono gRPC interno
  cliente con ca.crt en almacén de confianza ──► valida cert de servidor por SAN ──► canal confiable
```

Diagrama FossFlow: rectángulo **"A08 · Software & Data Integrity"** en `extra/graphics/diagrams/owasp-web-top-10.json`, nodos `processed_events idempotente`, `TLS CA trust store` y `System.Text.Json`.

## Verificación
- Idempotencia: re-disparar el mismo `EventId` no duplica el efecto (segunda pasada → `AlreadyProcessed`).

```bash
cd apps/Attendance/Test && dotnet test --filter "Handler"
```

- Ausencia de serializadores inseguros:

```bash
grep -rn "BinaryFormatter\|SoapFormatter\|TypeNameHandling" apps/
```

- TLS: con `ca.crt` instalada, los clientes validan el SAN del servidor; sin la CA, el handshake gRPC falla (no hay forma de saltarse la validación).
- Veneno: un mensaje con cuerpo no-JSON se registra (`BadPayloadDropped`) y se confirma (ACK) descartándolo sin reencolar.

## Notas y brechas conocidas
- La idempotencia depende de que el productor mantenga la igualdad entre el `Id` de la fila de la Bandeja de Salida y el `EventId` del payload; un productor nuevo que rompa esa invariante debilitaría la protección (regla recordada en `apps/CLAUDE.md` #7).
- Las imágenes de contenedor y los paquetes NuGet no se verifican aquí con firma/SBOM; la cadena de suministro de dependencias se cubre en A06 (componentes) más que en este ítem.
- `processed_events` es la barrera de idempotencia; el `payment_credit_ledger` que escribe `PaymentCapturedHandler` es un registro de auditoría adicional, no el mecanismo de idempotencia.
