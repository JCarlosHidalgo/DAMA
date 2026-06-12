# API10:2023 · Consumo No Seguro de APIs (Unsafe Consumption of APIs)

> **Estado:** ✅ — Al consumir Todotix, DAMA verifica la firma HMAC del callback antes de procesar, valida la respuesta deserializada (null-checks + `Error == 0`) sin confiar ciegamente en campos ambiguos, encola en un inbox idempotente que **re-consulta** a Todotix antes de transicionar, y protege el `HttpClient` con timeouts y circuit breaker.

## Qué exige OWASP
Tratar los datos de una API de terceros con el mismo recelo que los de un usuario: validar y sanear lo recibido, no confiar en redirecciones ni en datos sin verificar, aplicar timeouts y límites a las llamadas salientes, y no propagar a otros componentes una respuesta del tercero sin comprobarla.

## Cómo lo cumple DAMA

### (a) Verificación de firma HMAC del callback antes de procesar
El callback de Todotix llega sin autenticación de usuario; DAMA verifica una firma HMAC-SHA256 sobre el `transaction_id` **antes** de tocar nada, y descarta (registrando) los callbacks con firma inválida. `apps/Payment/Backend/Controllers/QrPaymentController.cs:128-135`:

```csharp
if (!_callbackSignature.Verify(transactionId.ToString("D"), signature ?? string.Empty))
{
    LogEvents.TodotixCallbackInvalidSignature(_logger, transactionId);
    return Ok();
}

await _callbackInbox.TryEnqueueAsync(transactionId, error, cancelOrder);
return Ok();
```

La verificación usa comparación en **tiempo constante** para no filtrar la firma por timing. `apps/Payment/Backend/Services/Concrete/CallbackSignature.cs:27-38`:

```csharp
byte[] expected = HMACSHA256.HashData(LoadSecret(), Encoding.UTF8.GetBytes(payload));
byte[] provided;
try
{
    provided = FromBase64Url(providedSignature);
}
catch (FormatException)
{
    return false;
}

return CryptographicOperations.FixedTimeEquals(expected, provided);
```

### (b) Validación de la respuesta deserializada
La respuesta de Todotix se deserializa con manejo explícito de errores, se rechaza un cuerpo vacío, y los campos se comprueban en vez de asumirse. `apps/Payment/Backend/Services/Concrete/Todotix/TodotixClient.cs:52-66`:

```csharp
ConsultDebtResponse? responseBody;
try
{
    responseBody = JsonSerializer.Deserialize<ConsultDebtResponse>(rawBody);
}
catch (JsonException deserializationException)
{
    LogEvents.TodotixConsultDebtDeserializationFailed(logger, deserializationException, debtIdentifier);
    throw;
}

if (responseBody is null)
{
    throw new InvalidOperationException("Todotix ConsultDebt returned empty body.");
}
```

El estado "pagado" sólo se acepta si `Error == 0` **y** los datos están presentes con `Pagado: true, PagoAnulado: false`. `TodotixClient.cs:83-87`:

```csharp
private static bool IsPaid(ConsultDebtResponse debtStatus)
{
    return debtStatus.Error == 0
        && debtStatus.Datos is { Pagado: true, PagoAnulado: false };
}
```

**No confiar ciegamente en campos ambiguos del tercero.** Existe un detalle sutil de Todotix: el campo `existente=0` puede convivir con `pagado=true`, así que **no se usa `existente` como guardia** de la decisión de pago. La lógica de `IsPaid` deliberadamente ignora `Existente` y se apoya en `Error == 0` + `Datos.Pagado`/`PagoAnulado`. Cuando la deuda no resulta pagada, se registra el detalle completo (incluido `Existente`) para diagnóstico, sin que ese campo gobierne la transición — `TodotixClient.cs:71-78`:

```csharp
LogEvents.TodotixConsultDebtUnpaid(
    logger,
    debtIdentifier,
    responseBody.Error,
    responseBody.Existente,
    responseBody.Datos?.Pagado,
    responseBody.Datos?.PagoAnulado);
```

### (c) Inbox idempotente que re-consulta a Todotix antes de transicionar
El controlador **no** transiciona la deuda con lo que diga el callback; sólo lo encola en `payment_callback_inbox`. Un `BackgroundService`, `PaymentCallbackWorker`, drena el inbox y delega en el handler de comando, que **re-consulta** a Todotix (fuente de verdad) antes de transicionar. `apps/Payment/Backend/Workers/QrPayments/PaymentCallbackWorker.cs:83-85`:

```csharp
await callbackHandler.Handle(
    new ProcessQrCallbackCommand(callback.Id, callback.Error, callback.CancelOrder));
await callbackInboxDao.MarkProcessedAsync(callback.Id);
```

El worker arrastra leases con reintentos acotados (`MaxAttempts = 3`) y trunca el mensaje de error antes de persistirlo, de modo que un callback envenenado no entra en bucle ni infla el almacenamiento. Así, ni la firma válida basta: la decisión final se toma contra una consulta directa a Todotix, no contra los parámetros del callback.

### (d) Timeouts + circuit breaker en el `HttpClient` de Todotix
Las llamadas salientes están acotadas en tiempo y protegidas por un circuit breaker, de modo que una API de terceros lenta o caída no agota recursos ni se propaga como una cascada de fallos. `apps/Payment/Backend/Modules/TodotixHttpClientModule.cs:19` y `:21-30`:

```csharp
client.Timeout = TimeSpan.FromSeconds(30);
...
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
    options.Retry.ShouldHandle = static _ => PredicateResult.False();
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});
```

Los reintentos automáticos se **desactivan** (`Retry.ShouldHandle => false`) porque el reintento confiable lo da el `todotix_outbox` transaccional (ver `apps/CLAUDE.md` #7), evitando duplicar peticiones de pago.

## Flujo del callback (componentes)

```
Todotix ──► GET /api/payment/qr/callback?transaction_id&error&cancel_order&sig
   │
   ▼  (1) Verificar firma HMAC  (CallbackSignature.Verify, tiempo constante)
   │        firma inválida ──► LogEvents.TodotixCallbackInvalidSignature ──► 200 OK, descartar
   ▼  (2) Encolar en payment_callback_inbox  (NO transiciona aún)
   │
   ▼  PaymentCallbackWorker (BackgroundService) drena el inbox (lease, MaxAttempts=3)
   │
   ▼  (3) Handler ProcessQrCallback ──► RE-CONSULTA a Todotix
   │        TodotixClient.ConsultDebtAsync ──► valida respuesta:
   │           deserialización segura + null-check + Error==0 + Pagado/PagoAnulado
   │           (existente NO se usa como guardia)
   │
   ▼  (4) Transición idempotente del estado de la deuda + MarkProcessed
            (timeouts + circuit breaker protegen toda salida a Todotix)
```

Diagrama FossFLOW: rectángulo **"API10 · Unsafe Consumption of APIs"** en `extra/fossflow/diagrams/owasp-api-top-10.json`, nodos `Signature verify`, `Response validation`, `Idempotent inbox` y `Circuit breaker`.

## Verificación
- Firma: `cd apps/Payment/Test && dotnet test --filter "Callback"` — un callback con `sig` inválida no encola ni transiciona (200 OK + log `TodotixCallbackInvalidSignature`).
- Validación de respuesta: revisar `TodotixClient.IsPaid` (`:83-87`) y confirmar que `Existente` no participa en la decisión; sólo `Error == 0` + `Datos.Pagado`/`PagoAnulado`.
- Re-consulta: el controlador sólo encola (`TryEnqueueAsync`); la transición ocurre en el worker tras `ConsultDebtAsync`, no con los parámetros del callback.
- Resiliencia: inspeccionar `TodotixHttpClientModule` — timeout de 30 s + `AddStandardResilienceHandler` con circuit breaker; reintentos HTTP desactivados (el reintento lo da `todotix_outbox`).

## Notas / brechas conocidas
- La firma del callback depende de un secreto compartido (`PaymentCallbackOptions.Secret`, inyectado por env) que debe rotarse con Todotix; está fuera del código de la app.
- La re-consulta hace que la latencia/disponibilidad de Todotix participe en la confirmación de pagos; el inbox + reintentos acotados + circuit breaker amortiguan esto, pero un Todotix caído retrasa la transición (estado pendiente visible al estudiante), no la corrompe.
- El campo `existente` de Todotix es un ejemplo concreto de "no confiar ciegamente en el tercero": su semántica ambigua (`existente=0` con `pagado=true`) está documentada como gotcha del proyecto y excluida de la lógica de decisión a propósito.
