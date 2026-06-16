# A08:2021 · Fallos de Integridad de Software y Datos (Software and Data Integrity Failures)

> **Estado:** ✅ — Consumo de eventos idempotente (`processed_events`), deserialización segura con `System.Text.Json` (sin `BinaryFormatter`), y validación de la cadena de confianza TLS interna vía CA en el trust store de cada cliente gRPC.

## Qué exige OWASP
Garantizar la integridad de datos y de la cadena de suministro: deserializar solo con serializadores seguros (nunca formatos que permitan ejecución de código), confiar únicamente en fuentes verificadas (firmas/certificados), y proteger las tuberías de datos contra duplicados, replays o mutaciones no autorizadas.

## Cómo lo cumple DAMA

### Idempotencia del consumo de eventos (`processed_events`)
La mensajería entre servicios es **at-least-once**: un reenvío tras un fallo puede re-disparar un evento. La integridad de datos se protege con la tabla `processed_events`, donde el `EventId` se inserta dentro de la misma transacción que el efecto de negocio, **antes** de aplicarlo. Una violación de clave única (MySQL 1062) significa "ya procesado" → no se reaplica.

`apps/Attendance/Backend/DB/Daos/Concrete/Single/Events/ProcessedEventDao.cs:21-34`:

```csharp
const string sql = "INSERT INTO processed_events (EventId, ProcessedAt) " +
                   "VALUES (@id, NOW(6));";
...
try
{
    await command.ExecuteNonQueryAsync();
    return true;
}
catch (MySqlException duplicateException) when (duplicateException.Number == 1062)
{
    return false;
}
```

La tabla tiene el `EventId` como clave primaria (`infrastructure/environments/attendance/init.sql:151-152`):

```sql
CREATE TABLE IF NOT EXISTS processed_events (
    EventId     CHAR(36)    NOT NULL PRIMARY KEY,
```

Cada handler envuelve "marcar procesado + aplicar efecto + commit" con `IdempotentTransaction.RunAsync` (del paquete `DAMA.Software.MySqlOutbox`), que comete por cuenta del llamante. `apps/Attendance/Backend/Services/Concrete/Events/PaymentCapturedHandler.cs:38-43`:

```csharp
return await IdempotentTransaction.RunAsync<PaymentCapturedOutcome>(
    _unitOfWork,
    _processedEventDao,
    paymentCapturedEvent.EventId,
    new PaymentCapturedOutcome.AlreadyProcessed(),
    async scope =>
    {
```

**Invariante de integridad**: el `Id` de la fila outbox del productor y el `EventId` del payload son el mismo `Guid`, así que el `EventId` es a la vez PK del outbox y clave de idempotencia del consumidor. Patrón documentado en `apps/CLAUDE.md` #7.

### Deserialización segura: `System.Text.Json`, nunca `BinaryFormatter`
Los payloads de evento se deserializan con `System.Text.Json` sobre el cuerpo del mensaje; un payload malformado se registra y se ACK-dropa (no se reencola en bucle), y un evento nulo o veneno se descarta igualmente.

`apps/Attendance/Backend/Workers/Infrastructure/RabbitMqMessageDispatcher.cs:79` y `:81-97`:

```csharp
deserializedEvent = JsonSerializer.Deserialize<TEvent>(deliveryEventArgs.Body.Span, SerializerOptions);
...
catch (Exception deserializationException)
{
    LogEvents.BadPayloadDropped(...);
    await channel.BasicAckAsync(deliveryEventArgs.DeliveryTag, multiple: false);
    return;
}

if (deserializedEvent is null || (isPoisonMessage is not null && isPoisonMessage(deserializedEvent)))
{
    LogEvents.InvalidEventDropped(...);
    await channel.BasicAckAsync(deliveryEventArgs.DeliveryTag, multiple: false);
    return;
}
```

No hay `BinaryFormatter`, `SoapFormatter`, `NetDataContractSerializer` ni `TypeNameHandling.All` en el código: los eventos cruzan fronteras como JSON con forma fija (POCOs `Events/*Event.cs`), deliberadamente desacoplados de los tipos internos del productor — no se deserializan tipos arbitrarios desde la red.

### Validación de la cadena de confianza TLS interna
El gRPC interno se valida contra una CA propia. Cada cliente (Attendance, Payment) instala `ca.crt` en el trust store del SO al arrancar, de modo que la librería gRPC valida el certificado del servidor por nombre sin callbacks que se salten la verificación.

`infrastructure/environments/attendance/entrypoint.sh` (idéntico en `payment/entrypoint.sh`):

```sh
if [ -f /etc/dama/tls/ca.crt ]; then
    cp /etc/dama/tls/ca.crt /usr/local/share/ca-certificates/dama-ca.crt
    update-ca-certificates >/dev/null 2>&1 || true
fi
```

La CA y los certs de servidor (SAN = hostname del contenedor) los emite `infrastructure/environments/tls-init/bootstrap-tls.sh` en el volumen `dama-tls`. Así, un servidor gRPC no confiable no supera la validación de la cadena: la integridad del canal entre servicios está anclada en la CA, no en confianza ciega.

## Flujo de los componentes

```
Productor (Auth/CourseManagement/Payment)
  └─ inserta fila en outbox_events  (Id == EventId)  ── misma tx que muta el agregado
       │  OutboxPublisher ──► RabbitMQ (dama.events)
       ▼
Consumidor (Attendance)
  RabbitMqMessageDispatcher
   ├─ JsonSerializer.Deserialize<TEvent>  (System.Text.Json)
   │    └─ payload corrupto / nulo / veneno ──► ACK + drop (sin bucle)
   ▼
  Handler ─ IdempotentTransaction.RunAsync(EventId)
   ├─ INSERT processed_events(EventId)
   │     └─ 1062 (duplicado) ──► AlreadyProcessed, NO reaplica el efecto ──► ACK
   └─ primera vez ──► aplica efecto + commit ──► ACK
                       (fallo/excepción ──► NACK requeue)

Canal síncrono gRPC interno
  cliente con ca.crt en trust store ──► valida cert de servidor por SAN ──► canal confiable
```

Diagrama FossFLOW: rectángulo **"A08 · Software & Data Integrity"** en `extra/graphics/diagrams/owasp-web-top-10.json`, nodos `processed_events idempotente`, `TLS CA trust store` y `System.Text.Json`.

## Verificación
- Idempotencia: `cd apps/Attendance/Test && dotnet test --filter "Handler"` — re-disparar el mismo `EventId` no duplica el efecto (segunda pasada → `AlreadyProcessed`).
- `grep -rn "BinaryFormatter\|SoapFormatter\|TypeNameHandling" apps/` → sin coincidencias.
- TLS: con `ca.crt` instalada, los clientes validan el SAN del servidor; sin la CA, el handshake gRPC falla (no hay bypass de validación).
- Veneno: un mensaje con cuerpo no-JSON se registra (`BadPayloadDropped`) y se ACK-dropa sin reencolar.

## Notas / brechas conocidas
- La idempotencia depende de que el productor mantenga `outbox row Id == payload EventId`; un productor nuevo que rompa esa invariante debilitaría la protección (regla recordada en `apps/CLAUDE.md` #7).
- Las imágenes de contenedor y los paquetes NuGet no se verifican aquí con firma/SBOM; la cadena de suministro de dependencias se cubre en A06 (componentes) más que en este ítem.
- `processed_events` es la barrera de idempotencia; el `payment_credit_ledger` que escribe `PaymentCapturedHandler` es un registro de auditoría adicional, no el mecanismo de idempotencia.
