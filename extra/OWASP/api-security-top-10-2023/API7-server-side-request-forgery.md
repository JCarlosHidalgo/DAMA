# API7:2023 · Falsificación de Solicitudes del Lado del Servidor (Server-Side Request Forgery)

> **Estado:** 🟢 — Acotado por diseño: ninguna API de DAMA acepta una URI/URL del cliente para que el servidor la consulte. La única salida HTTP externa (Todotix) tiene `BaseUrl` fija por configuración y el tráfico gRPC interno usa discovery fijo por nombre de contenedor.

## Qué exige OWASP
SSRF en APIs ocurre cuando un endpoint obtiene un recurso remoto a partir de una URI que el cliente controla, sin validarla — permitiendo alcanzar servicios internos o destinos arbitrarios. La defensa: no aceptar URLs/identificadores de recurso remotos del cliente; cuando haya salida saliente, fijar el destino por allow-list/configuración.

## Cómo lo cumple DAMA

### Ningún endpoint recibe una URL del cliente
Ninguna acción de los controladores toma una URL como entrada para hacer fetch server-side. La única entrada externa no autenticada es el callback de Todotix, que llega con **parámetros escalares** por query string, no con una URL. `apps/Payment/Backend/Controllers/QrPaymentController.cs:121-127`:

```csharp
[AllowAnonymous]
[HttpGet("callback")]
public async Task<ActionResult> Callback([FromQuery(Name = "transaction_id")] Guid transactionId,
                                          [FromQuery] int error,
                                          [FromQuery(Name = "cancel_order")] int cancelOrder,
                                          [FromQuery(Name = "sig")] string? signature)
```

`transactionId` es un `Guid` y los demás son enteros/strings de firma — nada que el servidor use como destino de una petición.

### Salida externa con `BaseUrl` fija por configuración
La salida HTTP a Todotix se construye sobre una `BaseAddress` tomada de configuración, no de entrada del cliente. `apps/Payment/Backend/Modules/TodotixHttpClientModule.cs:16-18`:

```csharp
string baseUrl = configuration["Todotix:BaseUrl"]
                 ?? throw new InvalidOperationException("Todotix:BaseUrl is not configured.");
client.BaseAddress = new Uri(baseUrl);
```

Las rutas que invoca el cliente son literales constantes; los datos del usuario van en el body. `apps/Payment/Backend/Services/Concrete/Todotix/TodotixClient.cs:28`:

```csharp
HttpResponseMessage response = await httpClient.PostAsJsonAsync("/rest/deuda/consultar_deudas/por_identificador", request);
```

### gRPC interno: discovery fijo por nombre de contenedor
Las dos aristas gRPC (Attendance→CourseManagement, Payment→Auth) resuelven su dirección de configuración, no de la petición del cliente. `apps/Payment/Backend/Modules/GrpcClientsModule.cs:23-25`:

```csharp
SubscriptionGrpcOptions options =
    serviceProvider.GetRequiredService<IOptions<SubscriptionGrpcOptions>>().Value;
grpcClientOptions.Address = new Uri(options.AuthUrl);
```

Y en Attendance, `apps/Attendance/Backend/Modules/GrpcClientsModule.cs:21-23`, `Services:CourseManagementUrl` de la misma forma. El destino es siempre un nombre de contenedor inyectado por env var.

## Flujo de los componentes

```
Cliente ──► API DAMA
   ningún parámetro de entrada es una URL de fetch server-side
   (el callback Todotix sólo trae escalares: transaction_id, error, sig)

Salida externa (Payment ──► Todotix)
   destino = Todotix:BaseUrl (config)  +  ruta literal     (no input del cliente)

Salida interna gRPC
   Attendance ──► Services:CourseManagementUrl (config)
   Payment    ──► Subscription:AuthUrl        (config)
```

Diagrama FossFLOW: rectángulo **"API7 · Server-Side Request Forgery"** en `extra/graphics/diagrams/owasp-api-top-10.json`, nodos `Todotix BaseUrl por config` y `gRPC discovery fijo`.

## Verificación
- `grep -rn "new Uri(" apps/*/Backend --include=*.cs` → todos los destinos salen de `configuration[...]` / `Options`, ninguno de un DTO de request.
- Revisar las firmas de acción de los controladores: ningún parámetro `[FromBody]`/`[FromQuery]` es una URL consumida server-side.
- `grep -rn "PostAsJsonAsync\|GetAsync" apps/Payment/Backend --include=*.cs` → rutas literales sobre `BaseAddress` fija.

## Notas / brechas conocidas
- Mismo razonamiento que A10 (web): la superficie es estructuralmente pequeña (un destino externo fijo + dos peers gRPC fijos), por lo que se marca 🟢, no ✅ — la garantía se mantiene mientras no se introduzca un endpoint que acepte URLs del cliente.
- No existe un proxy/allow-list de egreso a nivel de red; la defensa es de código (destinos por configuración). Cualquier integración nueva debe replicar el patrón `BaseUrl` por config.
