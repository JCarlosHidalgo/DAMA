# A05 · Security Misconfiguration (Web Top 10 2021)

> **Estado:** ✅ — Cabeceras de seguridad endurecidas en el gateway y en el frontend, secretos sólo por entorno con validación *fail-fast*, errores estructurados sin *stack trace* y sin superficie de documentación (Swagger) expuesta en runtime.

## Introducción

Esta ficha documenta cómo DAMA atiende la **mala configuración de seguridad** (A05), que abarca valores por defecto inseguros, superficies innecesarias, cabeceras HTTP ausentes, errores verbosos y secretos en configuración versionada. El documento recorre la evidencia técnica del endurecimiento: las cabeceras de seguridad emitidas en el gateway nginx, la CSP completa y las cabeceras de marco servidas por el httpd del frontend, el CORS con lista de permitidos en lugar de comodín, los secretos inyectados solo por entorno con validación *fail-fast*, los errores estructurados sin volcado de pila y la ausencia de Swagger expuesto en runtime. Concluye con el flujo de componentes, los comandos de verificación y las brechas conocidas.

## Qué exige OWASP

La mala configuración de seguridad cubre valores por defecto inseguros, superficies innecesarias (consolas, documentación de API, cuentas por defecto), cabeceras HTTP de seguridad ausentes, mensajes de error verbosos que filtran detalles internos y secretos embebidos en configuración versionada. OWASP recomienda un proceso de endurecimiento repetible, mínima superficie expuesta y separación estricta de configuración por entorno.

## Cómo lo cumple DAMA

### Cabeceras de seguridad en la puerta de enlace (nginx)

La puerta de enlace de API emite cabeceras de transporte y tipo en **todas** las respuestas `/api/*`, heredadas por cada `location` porque ninguno declara su propio `add_header`. La configuración fija `Strict-Transport-Security` con `max-age` de un año e `includeSubDomains`, `X-Content-Type-Options: nosniff` y `Referrer-Policy: strict-origin-when-cross-origin`, todas con la marca `always` para que se emitan incluso en respuestas de error (`infrastructure/environments/api-gateway/nginx.conf:127`).

El CSP y `frame-ancestors` no viven aquí (la puerta de enlace sólo emite JSON); residen en el documento HTML servido por el httpd del frontend (ver abajo), tal como anota el comentario en `infrastructure/environments/api-gateway/nginx.conf:122`.

### CSP completa y cabeceras de marco en el frontend (Apache httpd)

El documento de la SPA y sus recursos estáticos llevan una *Content-Security-Policy* estricta más `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy`, `Permissions-Policy` (que deshabilita geolocalización, micrófono y cámara) y HSTS. La directiva CSP restringe `default-src` a `'self'`, admite scripts sólo desde el propio origen más un hash inline concreto, limita `img-src` al origen propio, `data:` y `https://api.todotix.com`, acota `connect-src` al origen de la API y su variante de WebSocket, y fija `frame-ancestors 'none'`, `base-uri 'self'`, `object-src 'none'` y `form-action 'self'` (`infrastructure/environments/frontend/httpd.conf:67`).

El origen de la API (`__API_ORIGIN__`/`__API_WS_ORIGIN__`) y el hash del script inline se sustituyen en tiempo de *build* desde build-args; no hay valores por entorno escritos a mano. Además `ServerTokens Prod` y `ServerSignature Off` (`infrastructure/environments/frontend/httpd.conf:4`) ocultan la versión del servidor.

### CORS con lista de permitidos, nunca comodín

La puerta de enlace resuelve `Access-Control-Allow-Origin` desde un `map` que sólo refleja el origen del frontend del entorno o `localhost`; cualquier otro origen recibe cadena vacía (sin cabecera CORS permisiva). El bloque `map $http_origin $cors_allow_origin` deja `default ""`, refleja el origen sólo cuando coincide con el patrón de `localhost`/`127.0.0.1` o con el valor de `${GATEWAY_FRONTEND_ORIGIN}` (`infrastructure/environments/api-gateway/nginx.conf:86`).

Con `Access-Control-Allow-Credentials: true` (`:133`) un comodín sería inválido por especificación; la lista de permitidos es la única opción correcta.

### Secretos sólo por entorno, con validación de arranque inmediata

No hay secretos en `appsettings.json` — el único JSON versionado lleva sólo `Logging` y `AllowedHosts` (`apps/Auth/Backend/appsettings.json:1`). Las claves se inyectan por variable de entorno desde `.env.dev`/`.env.prod` (ambos *gitignored*; el único template es `infrastructure/.env.example`).

`SecretsValidationModule` corre con `Order => -100` (antes de que nada se cablee) y decodifica/valida cada secreto, fallando el arranque del host con un mensaje preciso si falta o es inválido — nunca en tiempo de petición. Exige una clave RSA privada (`JWT_PRIVATE_KEY_B64`) y una pública (`JWT_PUBLIC_KEY_B64`) válidas antes de continuar (`apps/Auth/Backend/Modules/SecretsValidationModule.cs:8`).

### Errores estructurados sin volcado de pila

Cada backend registra `AddProblemDetails()` y `UseExceptionHandler()`, devolviendo `application/problem+json` sin volcado de excepción al cliente (`apps/Auth/Backend/Modules/ProblemDetailsModule.cs:10`).

### Sin Swagger expuesto en runtime

Swashbuckle figura como `PackageReference` (`apps/Auth/Backend/Backend.csproj:20`), pero **no se mapea**: no hay ninguna llamada a `AddSwaggerGen`/`UseSwagger`/`MapOpenApi` en ningún backend (`grep -rn "AddSwaggerGen\|UseSwagger" apps/*/Backend/` no arroja resultados). La superficie de documentación de API no está accesible en producción.

## Flujo de los componentes

```
respuesta del backend (JSON)
   │
   ▼  puerta de enlace nginx
   │     · cabeceras HSTS / nosniff / Referrer-Policy     (siempre)
   │     · lista de permitidos CORS vía map $cors_allow_origin (sin comodín)
   │
documento SPA (Apache httpd)
   │     · CSP completa, X-Frame-Options DENY, Permissions-Policy, HSTS
   │     · ServerTokens Prod / ServerSignature Off
   │
arranque del backend
         · SecretsValidationModule (Order -100) → fallo inmediato si falta secreto
         · ProblemDetails + UseExceptionHandler → error sin volcado de pila
         · sin AddSwaggerGen/UseSwagger → cero superficie de documentación en runtime
```

En el diagrama FossFlow `extra/graphics/diagrams/owasp-web-top-10.json`, este ítem es el rectángulo **A05 · Security Misconfiguration**, que agrupa los nodos **nginx security headers**, **SecretsValidationModule**, **ProblemDetails (sin volcado de pila)** y **Secretos sólo por entorno**.

## Verificación

Cabeceras de la puerta de enlace: deben mostrarse las tres cabeceras de seguridad.

```bash
curl -sI http://localhost:8100/api/auth/ | grep -i "strict-transport\|x-content-type\|referrer-policy"
```

CSP del frontend: debe aparecer la cabecera `Content-Security-Policy`.

```bash
curl -sI http://localhost:8101/ | grep -i content-security-policy
```

Sin Swagger: el resultado debe estar vacío.

```bash
grep -rn "AddSwaggerGen\|UseSwagger\|MapOpenApi" apps/*/Backend/
```

- Lista de permitidos CORS: una petición con `Origin` ajeno no debe recibir `Access-Control-Allow-Origin` reflejado; sólo el origen del frontend o `localhost`.
- Fallo inmediato de secretos: arrancar un backend sin `JWT_PUBLIC_KEY_B64` debe abortar con el mensaje de `SecretsValidationModule`, no servir tráfico.

## Notas y brechas conocidas

- `set_real_ip_from 0.0.0.0/0` con `real_ip_header CF-Connecting-IP` confía en la cabecera de Cloudflare desde cualquier origen (`infrastructure/environments/api-gateway/nginx.conf:103`). Es seguro **sólo** porque en producción el único ingreso es el Cloudflare Tunnel (la puerta de enlace no publica puerto público); si la puerta de enlace quedara expuesta directamente, esa cabecera sería falsificable. Documentado en el propio comentario del archivo.
- El hash inline del CSP (`__CSP_INLINE_SCRIPT_HASH__`) debe regenerarse cada vez que cambia el `<script>` no-FOUC de `src/index.html`, o el tema temprano será bloqueado por CSP.
- `AllowedHosts: "*"` en `appsettings.json` es aceptable porque el único ingress real es el gateway/Cloudflare; el filtrado de host efectivo ocurre antes del backend.
