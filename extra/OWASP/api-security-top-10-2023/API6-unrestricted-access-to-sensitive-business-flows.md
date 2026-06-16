# API6:2023 · Unrestricted Access to Sensitive Business Flows (API Security Top 10 2023)

> **Estado:** ✅ — El flujo de pago QR está cerrado con controles activos en capas: rol + tier de suscripción, firma HMAC del callback externo, re-consulta a Todotix antes de transicionar, e inbox idempotente que neutraliza reintentos/replays.

## Qué exige OWASP

API6 cubre flujos de negocio sensibles (compra, pago, registro masivo, reservas) que un atacante puede **automatizar o abusar** aunque cada petición individual sea "válida": sin límites, sin verificación de procedencia y sin idempotencia, el flujo se puede disparar repetidamente, falsificar o reproducir. OWASP pide identificar esos flujos y protegerlos con controles específicos (no solo autenticación genérica): *device/human detection*, límites, validación de origen y prevención de replays.

## Cómo lo cumple DAMA

El flujo sensible es el **pago QR de clases** en Payment: crear deuda, recibir el callback de la pasarela externa Todotix, y acreditar clases al estudiante. Está protegido en cuatro capas.

### 1. Rol + tier de suscripción (autorización de función + de nivel)

Cada acción del flujo QR exige ser `Student` **y** tener nivel de suscripción ≥ 3 vigente. Dos atributos en cascada:

`apps/Payment/Backend/Controllers/QrPaymentController.cs:43`
```csharp
[Authorize(Roles = UserRoles.Student)]
[RequiresServiceTier(3)]
[HttpPost("{templateId:guid}")]
public async Task<ActionResult> CreateDebt(Guid templateId, CreateQrDebtDto dto)
```

`RequiresServiceTierAttribute` resuelve el nivel efectivo contra el claim y su expiración (suscripción vencida ⇒ nivel `0` ⇒ 403), en `apps/Payment/Backend/Security/RequiresServiceTierAttribute.cs:40`. Sin tier suficiente, el flujo entero (crear, consultar estado, listar) queda cerrado.

### 2. Firma HMAC del callback de Todotix (validación de origen)

El endpoint de callback es `[AllowAnonymous]` (lo llama la pasarela, no un usuario), así que su autenticidad se valida con **HMAC-SHA256** sobre el `transaction_id`. Una firma inválida se descarta silenciosamente (responde `200` para no filtrar información, pero **no encola nada**):

`apps/Payment/Backend/Controllers/QrPaymentController.cs:128`
```csharp
if (!_callbackSignature.Verify(transactionId.ToString("D"), signature ?? string.Empty))
{
    LogEvents.TodotixCallbackInvalidSignature(_logger, transactionId);
    return Ok();
}

await _callbackInbox.TryEnqueueAsync(transactionId, error, cancelOrder);
```

La verificación usa comparación en tiempo constante para evitar *timing attacks*:

`apps/Payment/Backend/Services/Concrete/CallbackSignature.cs:38`
```csharp
return CryptographicOperations.FixedTimeEquals(expected, provided);
```

El secreto (`PAYMENT_CALLBACK_SECRET`) se inyecta solo por entorno; sin él la firma no se puede forjar. Esto impide que un atacante dispare el flujo de "pago confirmado" sin haber pagado.

### 3. Inbox idempotente (anti-replay del callback)

El callback no muta la deuda directamente: se **encola** en `payment_callback_inbox`. El encolado es idempotente — la PK es el `transaction_id`, y un duplicado (MySQL `1062`) se descarta:

`apps/Payment/Backend/DB/Daos/Concrete/Single/QrPayments/PaymentCallbackInboxDao.cs:40`
```csharp
try
{
    await insertCommand.ExecuteNonQueryAsync();
    return true;
}
catch (MySqlException duplicateKeyException) when (duplicateKeyException.Number == 1062)
{
    return false;
}
```

Reproducir el mismo callback (incluso con firma válida) no vuelve a transicionar la deuda: ya está en el inbox. Esto cierra el abuso por *replay* del flujo sensible.

### 4. Re-consulta a Todotix antes de transicionar

El inbox lo drena un `BackgroundService` que, en vez de confiar en los parámetros del callback, re-consulta a Todotix y solo entonces transiciona. El worker resuelve el handler de comando por *scope* fresco y marca procesado tras el efecto:

`apps/Payment/Backend/Workers/QrPayments/PaymentCallbackWorker.cs:83`
```csharp
await callbackHandler.Handle(
    new ProcessQrCallbackCommand(callback.Id, callback.Error, callback.CancelOrder));
await callbackInboxDao.MarkProcessedAsync(callback.Id);
```

El `ProcessQrCallbackCommand` re-verifica la deuda contra la fuente de verdad externa (Todotix) antes de marcarla pagada — un callback "exitoso" forjado no basta para acreditar clases. Reintentos limitados (`MaxAttempts = 3`, línea 16) con backoff evitan loops infinitos sobre callbacks venenosos.

### Cierre del ciclo: el crédito al estudiante también es idempotente

Cuando la deuda se captura, Payment emite `payment.captured` por outbox/RabbitMQ y Attendance acredita las clases bajo `processed_events` (ver A04 §1 y `PaymentCapturedHandler.cs:38`), de modo que ni el extremo de pago ni el de acreditación pueden duplicar el efecto.

## Flujo de los componentes

Rectángulo **API6** del diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json` (nodos `Tier gating (suscripcion)` → `Callback HMAC signature` → `Idempotent inbox`):

```
Student (rol + tier ≥ 3)
   │  [Tier gating]  [Authorize(Student)] + [RequiresServiceTier(3)]
   ▼
POST /api/payment/qr/{templateId}  → crea deuda (Todotix outbox)
   ⋮  el estudiante paga vía pasarela Todotix
   ▼
GET /api/payment/qr/callback  [AllowAnonymous]
   │  [Callback HMAC signature]  Verify(transaction_id, sig)  ── inválida ──▶ 200, descarta
   │ válida
   ▼
[Idempotent inbox]  payment_callback_inbox  ── 1062 ──▶ replay descartado
   │
   ▼  PaymentCallbackWorker (BackgroundService)
re-consulta Todotix → ProcessQrCallbackCommand → transiciona deuda
   │
   ▼  payment.captured (outbox → RabbitMQ)
Attendance: acredita clases bajo processed_events (idempotente)
```

1. **Tier gating** filtra: solo un `Student` con suscripción nivel ≥ 3 vigente inicia o consulta el flujo.
2. El **callback HMAC** valida que el aviso de pago viene realmente de Todotix; firmas inválidas se descartan sin efecto.
3. El **inbox idempotente** absorbe el callback y neutraliza replays; el worker re-consulta a Todotix antes de transicionar, y la acreditación final vuelve a ser idempotente en Attendance.

## Verificación

- Confirmar rol + tier en todas las acciones del controlador QR:
  ```bash
  grep -n "Authorize(Roles\|RequiresServiceTier" apps/Payment/Backend/Controllers/QrPaymentController.cs
  ```
- Confirmar verificación HMAC en tiempo constante:
  ```bash
  grep -n "FixedTimeEquals\|HMACSHA256" apps/Payment/Backend/Services/Concrete/CallbackSignature.cs
  ```
- Confirmar el encolado idempotente del inbox:
  ```bash
  grep -n "1062\|payment_callback_inbox" apps/Payment/Backend/DB/Daos/Concrete/Single/QrPayments/PaymentCallbackInboxDao.cs
  ```
- Tests: `apps/Payment/Test/Security/RequiresServiceTierAttributeTests.cs` cubre el *gating* de nivel; la suite de Payment ejercita firma y procesamiento de callback.

## Notas / brechas conocidas

- El callback responde `200` ante firma inválida **a propósito** (no filtra si el `transaction_id` existe); el rechazo queda en logs vía `LogEvents.TodotixCallbackInvalidSignature`. Es la respuesta correcta para no dar oráculo al atacante, no una falta de control.
- No hay CAPTCHA / *human detection* en la creación de deuda: el control de abuso descansa en rol + tier + rate-limit del gateway (A04/API4), no en detección de bots. Si el flujo se abriera a un tier más bajo, convendría reforzar con un límite por usuario específico del flujo.
- El crédito final atraviesa RabbitMQ (*at-least-once*); su no-duplicación depende de `processed_events` en Attendance — mismo invariante que A04. Saltarse ese ledger reabriría el abuso aguas abajo.
