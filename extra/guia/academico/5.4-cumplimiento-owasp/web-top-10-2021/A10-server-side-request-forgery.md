# A10:2021 · Falsificación de Solicitudes del Lado del Servidor (Server-Side Request Forgery)

> **Estado:** 🟢 — Acotado por diseño: la **única** salida HTTP externa es Todotix, con `BaseUrl` fija por configuración, y el tráfico gRPC interno usa **service discovery fijo** por nombre de contenedor desde variables de entorno. Ningún endpoint acepta una URL del usuario para hacer fetch.

## Introducción

Esta ficha documenta cómo DAMA acota la **falsificación de solicitudes del lado del servidor** (A10), el riesgo de que un atacante induzca al servidor a hacer peticiones a destinos que controla o a recursos internos. El documento expone la evidencia técnica de una superficie saliente acotada por diseño: la única salida HTTP externa es Todotix con `BaseUrl` fija por configuración, las rutas son literales y los datos del usuario viajan en el body (nunca en la URL), el gRPC interno usa service discovery fijo por nombre de contenedor desde variables de entorno, y ningún endpoint acepta una URL del usuario para hacer fetch. Concluye con el flujo de componentes, los comandos de verificación y las brechas conocidas (la garantía depende de no introducir a futuro un endpoint que tome URLs del usuario).

## Qué exige OWASP
Prevenir que un atacante induzca al servidor a hacer peticiones a destinos que él controla o a recursos internos. La defensa central es no construir el destino de una petición saliente a partir de entrada del usuario: usar destinos fijos/allow-list por configuración, nunca una URL que llegue en el body o el query string.

## Cómo lo cumple DAMA

### La única salida HTTP externa: Todotix con `BaseUrl` fija por configuración
Payment es el único servicio que llama a una API externa. El `HttpClient` se registra con su `BaseAddress` tomada **de configuración** (`Todotix:BaseUrl`, inyectada por la variable de entorno `Todotix__BaseUrl`), no de ninguna entrada de la petición: el módulo lee `Todotix:BaseUrl`, lanza una excepción si falta y fija esa URL como `BaseAddress` del `HttpClient` con un tiempo de espera de 30 segundos (`apps/Payment/Backend/Modules/TodotixHttpClientModule.cs:14`).

Si la configuración no trae la URL, el servicio falla al arrancar (fail-fast) — nunca cae a un destino por defecto inseguro.

### Rutas fijas; el body lleva datos del usuario, **nunca** la URL
El cliente sólo invoca rutas literales relativas a esa `BaseAddress`. Los datos del usuario (identificador de deuda, clave de la academia) viajan en el **body** del POST, no en la URL: el cliente publica con `PostAsJsonAsync` sobre las rutas literales `/rest/deuda/registrar` y `/rest/deuda/consultar_deudas/por_identificador`, relativas a la `BaseAddress` fija (`apps/Payment/Backend/Services/Concrete/Todotix/TodotixClient.cs:14` y `:28`).

El destino efectivo es siempre `Todotix:BaseUrl` + una ruta constante. No hay concatenación de `Uri` con strings provenientes del request, ni un endpoint que reciba una URL para que el servidor la consulte.

### gRPC interno: service discovery fijo por nombre de contenedor
El tráfico síncrono entre servicios (Attendance→CourseManagement, Payment→Auth) apunta a direcciones fijas leídas de variables de entorno (nombres de contenedor en la red Docker/Dokploy), nunca de entrada del usuario.

En la arista Attendance → CourseManagement, la dirección del cliente gRPC se toma de `Services:CourseManagementUrl` de la configuración, con fallo al arrancar si falta (`apps/Attendance/Backend/Modules/GrpcClientsModule.cs:21`). En Payment → Auth, la dirección se toma de las opciones `SubscriptionGrpcOptions.AuthUrl`, también de configuración (`apps/Payment/Backend/Modules/GrpcClientsModule.cs:23`).

Ambos resuelven su `Uri` de configuración (`Services__CourseManagementUrl`, `Subscription__AuthUrl`), no del cuerpo ni del query string de ningún request.

### Ningún endpoint acepta una URL del usuario para hacer fetch
No existe en el código ningún controlador que tome una URL como parámetro y la consulte server-side (tipo "webhook tester", "fetch image from URL", "import from link"). El único endpoint que recibe datos externos sin autenticar es el callback de Todotix, que recibe **parámetros escalares** por query string (`transaction_id`, `error`, `cancel_order`, `sig`) — no una URL — y verifica la firma HMAC antes de procesar (ver A08 / API10). `apps/Payment/Backend/Controllers/QrPaymentController.cs:121-127`.

## Flujo de los componentes

```
Salida HTTP externa (única)
  Servicio/handler Payment ──► TodotixClient
     destino = Todotix:BaseUrl (config / env)  +  ruta literal ("/rest/deuda/...")
     datos del usuario ──► en el BODY del POST, nunca en la URL
        └─► API Todotix

Salida gRPC interna
  Attendance ──► Services:CourseManagementUrl (config)  ──► CourseManagement
  Payment    ──► Subscription:AuthUrl       (config)  ──► Auth
     destino = nombre de contenedor por env var, no entrada de usuario

Entrada no autenticada (callback) ──► sólo escalares (transaction_id, error, sig)
     NO contiene URL ──► verificación de firma HMAC antes de procesar
```

Diagrama FossFlow: rectángulo **"A10 · Server-Side Request Forgery"** en `extra/graphics/diagrams/owasp-web-top-10.json`, nodos `Todotix BaseUrl por config`, `gRPC service discovery fijo` y `Sin URL desde el usuario`.

## Verificación
- `grep -rn "new Uri(" apps/*/Backend --include=*.cs` → todos los `Uri` se construyen desde `configuration[...]` / `Options`, ninguno desde un DTO o query string.
- `grep -rn "PostAsJsonAsync\|GetAsync\|SendAsync" apps/Payment/Backend --include=*.cs` → las únicas llamadas HTTP usan rutas literales sobre la `BaseAddress` fija.
- Revisar `QrPaymentController.Callback` (`:121-136`): los parámetros del callback son escalares; no hay parámetro URL.
- Confirmar que `Todotix:BaseUrl`, `Services:CourseManagementUrl` y `Subscription:AuthUrl` provienen de `.env.*` (env vars), no de tablas o requests.

## Notas y brechas conocidas
- El riesgo de SSRF es **estructuralmente bajo** porque la superficie saliente es un único destino fijo más dos peers gRPC fijos; no se está mitigando un patrón peligroso existente, sino que el patrón nunca se introdujo. Marcado 🟢 (acotado por diseño), no ✅, porque la garantía depende de no introducir en el futuro un endpoint que tome URLs del usuario.
- No hay un proxy de egreso ni allow-list de red a nivel de infraestructura que bloquee destinos arbitrarios si algún día se añadiera código que los construya; la defensa hoy es de código (destinos por configuración). Si se agrega una segunda integración, debe seguir el mismo patrón `BaseUrl` por config.
