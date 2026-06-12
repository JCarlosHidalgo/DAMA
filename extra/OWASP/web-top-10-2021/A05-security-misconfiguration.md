# A05 · Security Misconfiguration (Web Top 10 2021)

> **Estado:** ✅ — Cabeceras de seguridad endurecidas en el gateway y en el frontend, secretos sólo por entorno con validación *fail-fast*, errores estructurados sin *stack trace* y sin superficie de documentación (Swagger) expuesta en runtime.

## Qué exige OWASP

La mala configuración de seguridad cubre defaults inseguros, superficies innecesarias (consolas, documentación de API, cuentas por defecto), cabeceras HTTP de seguridad ausentes, mensajes de error verbosos que filtran detalles internos y secretos embebidos en config versionada. OWASP recomienda un proceso de endurecimiento repetible, mínima superficie expuesta y separación estricta de configuración por entorno.

## Cómo lo cumple DAMA

### Cabeceras de seguridad en el gateway (nginx)

El api-gateway emite cabeceras de transporte y tipo en **todas** las respuestas `/api/*`, heredadas por cada `location` porque ninguno declara su propio `add_header`.

`infrastructure/environments/api-gateway/nginx.conf:127`

```nginx
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
add_header X-Content-Type-Options    "nosniff"                            always;
add_header Referrer-Policy           "strict-origin-when-cross-origin"    always;
```

El CSP y `frame-ancestors` no viven aquí (el gateway sólo emite JSON); residen en el documento HTML servido por el httpd del frontend (ver abajo), tal como anota el comentario en `infrastructure/environments/api-gateway/nginx.conf:122`.

### CSP completa y cabeceras de marco en el frontend (Apache httpd)

El documento SPA y sus *assets* llevan una *Content-Security-Policy* estricta más `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy`, `Permissions-Policy` y HSTS.

`infrastructure/environments/frontend/httpd.conf:67`

```apache
Header always set X-Content-Type-Options "nosniff"
Header always set X-Frame-Options "DENY"
Header always set Referrer-Policy "strict-origin-when-cross-origin"
Header always set Permissions-Policy "geolocation=(), microphone=(), camera=()"
Header always set Strict-Transport-Security "max-age=31536000; includeSubDomains"
Header always set Content-Security-Policy "default-src 'self'; script-src 'self' '__CSP_INLINE_SCRIPT_HASH__'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https://api.todotix.com; font-src 'self' data:; connect-src 'self' __API_ORIGIN__ __API_WS_ORIGIN__; frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'"
```

El origen de la API (`__API_ORIGIN__`/`__API_WS_ORIGIN__`) y el hash del script inline se sustituyen en tiempo de *build* desde build-args; no hay valores por entorno *hardcodeados*. Además `ServerTokens Prod` y `ServerSignature Off` (`infrastructure/environments/frontend/httpd.conf:4`) ocultan la versión del servidor.

### CORS con *allowlist*, nunca comodín

El gateway resuelve `Access-Control-Allow-Origin` desde un `map` que sólo refleja el origen del frontend del entorno o `localhost`; cualquier otro origen recibe cadena vacía (sin cabecera CORS permisiva).

`infrastructure/environments/api-gateway/nginx.conf:86`

```nginx
map $http_origin $cors_allow_origin {
    default "";
    "~^https?://(localhost|127\.0\.0\.1)(:\d+)?$" $http_origin;
    "${GATEWAY_FRONTEND_ORIGIN}"                  $http_origin;
}
```

Con `Access-Control-Allow-Credentials: true` (`:133`) un comodín sería inválido por especificación; la *allowlist* es la única opción correcta.

### Secretos sólo por entorno, con validación *fail-fast*

No hay secretos en `appsettings.json` — el único JSON committeado lleva sólo `Logging` y `AllowedHosts` (`apps/Auth/Backend/appsettings.json:1`). Las claves se inyectan por env var desde `.env.dev`/`.env.prod` (ambos *gitignored*; el único template es `infrastructure/.env.example`).

`SecretsValidationModule` corre con `Order => -100` (antes de que nada se cablee) y decodifica/valida cada secreto, fallando el arranque del host con un mensaje preciso si falta o es inválido — nunca en tiempo de petición.

`apps/Auth/Backend/Modules/SecretsValidationModule.cs:8`

```csharp
public int Order => -100;

public void Register(IServiceCollection services, IConfiguration configuration)
{
    SecretsValidation.RequireRsaPrivateKey(
        configuration["AppSettings:PrivateKey"], "JWT_PRIVATE_KEY_B64");
    SecretsValidation.RequireRsaPublicKey(
        configuration["AppSettings:PublicKey"], "JWT_PUBLIC_KEY_B64");
}
```

### Errores estructurados sin *stack trace*

Cada backend registra `AddProblemDetails()` y `UseExceptionHandler()`, devolviendo `application/problem+json` sin volcado de excepción al cliente.

`apps/Auth/Backend/Modules/ProblemDetailsModule.cs:10`

```csharp
public void Register(IServiceCollection services, IConfiguration configuration)
{
    services.AddProblemDetails();
}

public void Configure(WebApplication app)
{
    app.UseExceptionHandler();
}
```

### Sin Swagger expuesto en runtime

Swashbuckle figura como `PackageReference` (`apps/Auth/Backend/Backend.csproj:20`), pero **no se mapea**: no hay ninguna llamada a `AddSwaggerGen`/`UseSwagger`/`MapOpenApi` en ningún backend (`grep -rn "AddSwaggerGen\|UseSwagger" apps/*/Backend/` no arroja resultados). La superficie de documentación de API no está accesible en producción.

## Flujo de los componentes

```
respuesta del backend (JSON)
   │
   ▼  api-gateway nginx
   │     · add_header HSTS / nosniff / Referrer-Policy   (siempre)
   │     · CORS allowlist via map $cors_allow_origin     (sin comodín)
   │
documento SPA (Apache httpd)
   │     · CSP completa, X-Frame-Options DENY, Permissions-Policy, HSTS
   │     · ServerTokens Prod / ServerSignature Off
   │
arranque del backend
         · SecretsValidationModule (Order -100) → fail-fast si falta secreto
         · ProblemDetails + UseExceptionHandler → error sin stack trace
         · sin AddSwaggerGen/UseSwagger → cero superficie de docs en runtime
```

En el diagrama FossFLOW `extra/fossflow/diagrams/owasp-web-top-10.json`, este ítem es el rectángulo **A05 · Security Misconfiguration**, que agrupa los nodos **nginx security headers**, **SecretsValidationModule**, **ProblemDetails (sin stack)** y **Secrets solo por env**.

## Verificación

- Cabeceras del gateway: `curl -sI http://localhost:8100/api/auth/ | grep -i "strict-transport\|x-content-type\|referrer-policy"` debe mostrar las tres.
- CSP del frontend: `curl -sI http://localhost:8101/ | grep -i content-security-policy`.
- CORS *allowlist*: una petición con `Origin` ajeno no debe recibir `Access-Control-Allow-Origin` reflejado; sólo el origen del frontend o `localhost`.
- Sin Swagger: `grep -rn "AddSwaggerGen\|UseSwagger\|MapOpenApi" apps/*/Backend/` debe estar vacío.
- *Fail-fast* de secretos: arrancar un backend sin `JWT_PUBLIC_KEY_B64` debe abortar con el mensaje de `SecretsValidationModule`, no servir tráfico.

## Notas / brechas conocidas

- `set_real_ip_from 0.0.0.0/0` con `real_ip_header CF-Connecting-IP` confía en la cabecera de Cloudflare desde cualquier origen (`infrastructure/environments/api-gateway/nginx.conf:103`). Es seguro **sólo** porque en prod el único ingress es el Cloudflare Tunnel (el gateway no publica puerto público); si el gateway quedara expuesto directamente, esa cabecera sería *spoofable*. Documentado en el propio comentario del archivo.
- El hash inline del CSP (`__CSP_INLINE_SCRIPT_HASH__`) debe regenerarse cada vez que cambia el `<script>` no-FOUC de `src/index.html`, o el tema temprano será bloqueado por CSP.
- `AllowedHosts: "*"` en `appsettings.json` es aceptable porque el único ingress real es el gateway/Cloudflare; el filtrado de host efectivo ocurre antes del backend.
