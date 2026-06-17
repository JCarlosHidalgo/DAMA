# API8 · Security Misconfiguration (API Security Top 10 2023)

> **Estado:** ✅ — Cabeceras de seguridad + HSTS en el gateway, CORS por *allowlist* (sin comodín), SQL siempre parametrizado (parámetros nombrados + *stored procedures*) y secretos validados *fail-fast* sin defaults inseguros.

## Qué exige OWASP

La mala configuración en APIs incluye CORS permisivo, cabeceras de seguridad ausentes, *defaults* inseguros, mensajes de error verbosos, superficies de administración expuestas y rutas de inyección por construcción dinámica de consultas. OWASP pide un endurecimiento sistemático y repetible y minimizar lo que la API expone.

## Cómo lo cumple DAMA

### Cabeceras de seguridad + HSTS en la puerta de enlace

La configuración de nginx añade a toda respuesta de `/api/*`, con la directiva `always`, las cabeceras `Strict-Transport-Security` (HSTS, con `max-age` de un año e `includeSubDomains`), `X-Content-Type-Options: nosniff` y `Referrer-Policy: strict-origin-when-cross-origin`, de modo que cada `location` las hereda (`infrastructure/environments/api-gateway/nginx.conf:127`).

### CORS por lista de permitidos, no comodín

El origen permitido se resuelve mediante un bloque `map` sobre `$http_origin`: por defecto devuelve cadena vacía, y sólo refleja el origen entrante cuando coincide con `localhost`/`127.0.0.1` o con el origen del frontend del entorno (`${GATEWAY_FRONTEND_ORIGIN}`, inyectado desde `.env.*`). Cualquier otro origen recibe cadena vacía (`infrastructure/environments/api-gateway/nginx.conf:86`).

El preflight `OPTIONS` se responde directamente con `204` en nginx (`:140`), sin tocar el backend.

### SQL siempre parametrizado (parámetros nombrados + *stored procedures*)

Ningún DAO concatena entrada de usuario en SQL. Las consultas usan parámetros nombrados con `AddWithValue`, y la mayoría del acceso pasa por *stored procedures* (`CommandType.StoredProcedure`), nunca SQL ad-hoc construido con interpolación.

El `INSERT` de tokens de refresco usa una sentencia con marcadores nombrados (`@Id`, `@UserId`, `@TokenHash`, etc.) cuyos valores se enlazan uno a uno con `AddWithValue` sobre el `MySqlCommand`, sin interpolar nunca la entrada en la cadena SQL (`apps/Auth/Backend/DB/Daos/Concrete/Single/Tokens/RefreshTokenDao.cs:29`).

La revocación se invoca como *stored procedure*: se construye el `MySqlCommand` con `CommandType.StoredProcedure` sobre el nombre `RevokeRefreshToken` y se pasa el identificador como parámetro nombrado (`apps/Auth/Backend/DB/Daos/Concrete/Single/Tokens/RefreshTokenDao.cs:44`).

Este punto es el mismo control que cubre **A03 · Injection** en la lista Web; aquí se reitera porque la inyección es una mala configuración de construcción de consultas a nivel de API.

### Secretos validados *fail-fast*, sin defaults inseguros

Las claves no viven en `appsettings.json` (`apps/Auth/Backend/appsettings.json:1` sólo lleva `Logging`/`AllowedHosts`); se inyectan por variable de entorno y `SecretsValidationModule` (con `Order => -100`) las valida al arrancar, abortando el host con un mensaje preciso si faltan. El módulo decodifica y verifica las claves RSA antes de cablear nada (`apps/Auth/Backend/Modules/SecretsValidationModule.cs:8`).

### Errores sin filtración interna y sin Swagger en *runtime*

La combinación de `AddProblemDetails()` y `UseExceptionHandler()` (`apps/Auth/Backend/Modules/ProblemDetailsModule.cs:10`) devuelve `application/problem+json` sin *stack trace*. Swashbuckle está como paquete pero **no se mapea** (no hay `AddSwaggerGen`/`UseSwagger` en ningún backend), así que la API no expone documentación interactiva en producción.

## Flujo de los componentes

```
petición /api/<svc>/*
   │
   ▼  api-gateway nginx
   │     · HSTS / nosniff / Referrer-Policy   (siempre)
   │     · CORS lista de permitidos (map)     · OPTIONS → 204
   │
   ▼  backend
   │     · DAO: parámetros nombrados + stored procedures  (sin SQL dinámico)
   │     · ProblemDetails → error sin stack trace
   │     · sin Swagger mapeado
   │
arranque: SecretsValidationModule (Order -100) → fail-fast, sin defaults inseguros
```

En el diagrama FossFlow `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API8 · Security Misconfiguration**, que agrupa los nodos **nginx headers + HSTS**, **CORS allowlist**, **SQL parametrizado** y **Secrets fail-fast**.

## Verificación

- Cabeceras: `curl -sI http://localhost:8100/api/auth/ | grep -i strict-transport`.
- CORS: petición con `Origin` ajeno no debe recibir `Access-Control-Allow-Origin`.
- SQL parametrizado: `grep -rn "AddWithValue\|StoredProcedure" apps/*/Backend/DB/Daos/` muestra el patrón; no debe existir interpolación de cadenas de usuario en SQL.
- Sin Swagger: `grep -rn "AddSwaggerGen\|UseSwagger" apps/*/Backend/` vacío.

## Notas y brechas conocidas

- `client_max_body_size 1m` y la limitación de tasa de peticiones también son configuración de la puerta de enlace, pero su objetivo (consumo de recursos) se documenta en `API4-unrestricted-resource-consumption.md`.
- La confianza en `CF-Connecting-IP` desde cualquier origen es segura sólo bajo el supuesto de que el único punto de entrada es el Cloudflare Tunnel (ver A05 y las notas de `nginx.conf:95`).
