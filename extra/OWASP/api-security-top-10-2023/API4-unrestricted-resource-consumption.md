# API4 · Unrestricted Resource Consumption (API Security Top 10 2023)

> **Estado:** ✅ — El gateway impone *rate-limiting* por IP y tope de tamaño de cuerpo; los endpoints paginados acotan el índice de página; las llamadas gRPC/HTTP salientes llevan *timeout* + *circuit-breaker*; y los consumidores RabbitMQ limitan el *prefetch*.

## Qué exige OWASP

Una API sin límites permite que peticiones masivas, cuerpos enormes, paginación sin tope o llamadas a terceros sin *timeout* agoten CPU, memoria, conexiones o presupuesto. OWASP pide límites explícitos de tasa, tamaño, paginación y de los recursos que la API consume aguas abajo (DB, colas, APIs externas).

## Cómo lo cumple DAMA

### *Rate-limiting* por IP real en el gateway

Tres zonas `limit_req_zone` por `$binary_remote_addr`: `login` (5r/m), `callback` (60r/m) y una zona general `api` (30r/s) aplicada a todo `/api/*`.

`infrastructure/environments/api-gateway/nginx.conf:73`

```nginx
limit_req_zone $binary_remote_addr zone=login:10m    rate=5r/m;
limit_req_zone $binary_remote_addr zone=callback:10m rate=60r/m;
limit_req_zone $binary_remote_addr zone=api:10m      rate=30r/s;
```

La zona general se aplica a nivel de `server` con ráfaga 60 (`infrastructure/environments/api-gateway/nginx.conf:109`):

```nginx
limit_req        zone=api burst=60 nodelay;
limit_req_status 429;
```

La clave es la **IP real del cliente**: `real_ip_header CF-Connecting-IP` (`:103`) evita que todo el tráfico colapse sobre la IP del túnel de Cloudflare, de modo que el límite por IP cuenta a clientes reales.

### Tope de tamaño de cuerpo

Todo `/api/*` lleva cuerpos JSON pequeños; el gateway rechaza con `413` cualquier cuerpo > 1m antes de llegar al backend.

`infrastructure/environments/api-gateway/nginx.conf:44`

```nginx
client_max_body_size 1m;
```

### Tope de paginación en los validadores

Los endpoints paginados validan que el índice de página no exceda `MaxPageIndex = 10000`, evitando barridos profundos que fuercen escaneos costosos.

Payment (`apps/Payment/Backend/Validators/PaginationParamsDtoValidator.cs:13`) y Attendance (`apps/Attendance/Backend/Validators/PaginationParamsDtoValidator.cs:13`):

```csharp
RuleFor(x => x.Index)
    .GreaterThanOrEqualTo(0).WithMessage("El índice de página no puede ser negativo.")
    .LessThanOrEqualTo(MaxPageIndex).WithMessage("El índice de página excede el máximo permitido.");
```

Auth usa su propio DTO de paginación con el mismo tope (`apps/Auth/Backend/Validators/PaginationQueryDtoValidator.cs:14`):

```csharp
RuleFor(x => x.PageIndex)
    .GreaterThanOrEqualTo(0).WithMessage(InvalidPageIndexMessage)
    .LessThanOrEqualTo(MaxPageIndex).WithMessage(InvalidPageIndexMessage);
```

La validación corre automáticamente vía el filtro global de FluentValidation (patrón #4), así que un índice fuera de rango se rechaza con 400 antes de tocar la DB.

### *Timeout* + *circuit-breaker* en gRPC saliente (Payment → Auth)

La llamada síncrona de suscripción a Auth lleva el *resilience handler* estándar: *timeout* por intento, *timeout* total, reintentos acotados y *circuit-breaker*.

`apps/Payment/Backend/Modules/GrpcClientsModule.cs:31`

```csharp
resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
resilienceOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
resilienceOptions.Retry.MaxRetryAttempts = 2;
resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
resilienceOptions.CircuitBreaker.FailureRatio = 0.5;
resilienceOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
```

### *Timeout* + *circuit-breaker* en HTTP saliente (Payment → Todotix)

El cliente HTTP a Todotix acota `Timeout = 30s` y añade *resilience handler* con *circuit-breaker* (sin reintentos, deliberadamente, para no duplicar efectos de pago).

`apps/Payment/Backend/Modules/TodotixHttpClientModule.cs:19`

```csharp
client.Timeout = TimeSpan.FromSeconds(30);
...
options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
options.Retry.ShouldHandle = static _ => PredicateResult.False();
options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
```

### *Prefetch* acotado en consumidores RabbitMQ

Cada consumidor de Attendance limita el *prefetch* a 10 mensajes en vuelo (`autoAck: false`), de modo que un pico en la cola no carga toda la cola en memoria del proceso.

`apps/Attendance/Backend/Options/RabbitMqOptions.cs:37`

```csharp
public ushort PrefetchCount { get; set; } = 10;
```

Aplicado por cada consumidor (`apps/Attendance/Backend/Workers/Events/StudentRegisteredConsumer.cs:46`) y materializado vía `BasicQosAsync` en `apps/Attendance/Backend/Workers/Infrastructure/RabbitMqTopologyDeclarer.cs:34`.

## Flujo de los componentes

```
cliente
   │
   ▼  api-gateway nginx
   │     · limit_req zone=api 30r/s burst=60  → 429    (clave: IP real CF-Connecting-IP)
   │     · login 5r/m · callback 60r/m
   │     · client_max_body_size 1m            → 413 si excede
   │
   ▼  backend
   │     · PaginationValidator (MaxPageIndex 10000) → 400 si índice excesivo
   │
   ├─▶ gRPC Payment→Auth    : AttemptTimeout 5s + CircuitBreaker
   ├─▶ HTTP Payment→Todotix : Timeout 30s + CircuitBreaker
   └─▶ RabbitMQ consumers    : prefetchCount 10 (autoAck false)
```

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API4 · Unrestricted Resource Consumption**, que agrupa los nodos **Rate-limit nginx**, **Tope de paginacion**, **client_max_body_size** y **gRPC timeout + breaker**.

## Verificación

- *Rate-limit*: ráfaga > 30r/s sostenida a `/api/auth/` debe empezar a recibir `429`; > 5/min a `/api/auth/login` igual.
- Tope de cuerpo: `curl` con un cuerpo > 1MB debe recibir `413`.
- Paginación: `index=10001` (o `pageIndex=10001`) debe recibir `400`. Verificar el tope: `grep -rn "MaxPageIndex" apps/*/Backend/Validators/`.
- Resiliencia: revisar `apps/Payment/Backend/Modules/GrpcClientsModule.cs` y `TodotixHttpClientModule.cs` por `AddStandardResilienceHandler`.
- *Prefetch*: `grep -rn "PrefetchCount\|BasicQos" apps/Attendance/Backend/`.

## Notas / brechas conocidas

- El *rate-limiting* vive **sólo** en el gateway (decisión de arquitectura): no hay `RateLimiter` por servicio. Esto es correcto mientras el gateway sea el único ingress; un backend alcanzable directamente no tendría tope de tasa propio.
- No hay tope explícito de tamaño de página (`pageSize`) en todos los endpoints; el control acota el **índice**, no necesariamente el número de filas por página — depende del *stored procedure*. Considerar un `MaxPageSize` si algún SP acepta tamaño de página variable.
- El cliente Todotix no reintenta a propósito (`ShouldHandle = false`) para no arriesgar pagos duplicados; la resiliencia ahí es *timeout* + *breaker*, no reintento.
