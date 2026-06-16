# API8 · Security Misconfiguration (API Security Top 10 2023)

> **Estado:** ✅ — Cabeceras de seguridad + HSTS en el gateway, CORS por *allowlist* (sin comodín), SQL siempre parametrizado (parámetros nombrados + *stored procedures*) y secretos validados *fail-fast* sin defaults inseguros.

## Qué exige OWASP

La mala configuración en APIs incluye CORS permisivo, cabeceras de seguridad ausentes, *defaults* inseguros, mensajes de error verbosos, superficies de administración expuestas y rutas de inyección por construcción dinámica de consultas. OWASP pide un endurecimiento sistemático y repetible y minimizar lo que la API expone.

## Cómo lo cumple DAMA

### Cabeceras de seguridad + HSTS en el gateway

Todas las respuestas `/api/*` salen con HSTS, `nosniff` y `Referrer-Policy`, heredadas por cada `location`.

`infrastructure/environments/api-gateway/nginx.conf:127`

```nginx
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
add_header X-Content-Type-Options    "nosniff"                            always;
add_header Referrer-Policy           "strict-origin-when-cross-origin"    always;
```

### CORS por *allowlist*, no comodín

El origen permitido se resuelve por `map`: sólo el origen del frontend del entorno (`${GATEWAY_FRONTEND_ORIGIN}`, inyectado desde `.env.*`) o `localhost`. Cualquier otro origen recibe cadena vacía.

`infrastructure/environments/api-gateway/nginx.conf:86`

```nginx
map $http_origin $cors_allow_origin {
    default "";
    "~^https?://(localhost|127\.0\.0\.1)(:\d+)?$" $http_origin;
    "${GATEWAY_FRONTEND_ORIGIN}"                  $http_origin;
}
```

El preflight `OPTIONS` se responde directamente con `204` en nginx (`:140`), sin tocar el backend.

### SQL siempre parametrizado (parámetros nombrados + *stored procedures*)

Ningún DAO concatena entrada de usuario en SQL. Las consultas usan parámetros nombrados con `AddWithValue`, y la mayoría del acceso pasa por *stored procedures* (`CommandType.StoredProcedure`), nunca SQL ad-hoc construido con interpolación.

`apps/Auth/Backend/DB/Daos/Concrete/Single/Tokens/RefreshTokenDao.cs:29`

```csharp
const string sql = "INSERT INTO RefreshToken (Id, UserId, TokenHash, ExpiresAt, RevokedAt, CreatedAt) " +
                   "VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, NULL, @CreatedAt);";
MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
insertCommand.Parameters.AddWithValue("@Id", refreshToken.Id.ToString());
insertCommand.Parameters.AddWithValue("@UserId", refreshToken.UserId.ToString());
```

Vía *stored procedure* (`apps/Auth/Backend/DB/Daos/Concrete/Single/Tokens/RefreshTokenDao.cs:44`):

```csharp
MySqlCommand command = new MySqlCommand("RevokeRefreshToken", _connection, sqlTransaction)
{
    CommandType = CommandType.StoredProcedure
};
command.Parameters.AddWithValue("@tokenId", id.ToString());
```

Este punto es el mismo control que cubre **A03 · Injection** en la lista Web; aquí se reitera porque la inyección es una mala configuración de construcción de consultas a nivel de API.

### Secretos *fail-fast*, sin defaults inseguros

Las claves no viven en `appsettings.json` (`apps/Auth/Backend/appsettings.json:1` sólo lleva `Logging`/`AllowedHosts`); se inyectan por env var y `SecretsValidationModule` (`Order => -100`) las valida al arrancar, abortando el host con mensaje preciso si faltan.

`apps/Auth/Backend/Modules/SecretsValidationModule.cs:8` (decodifica y verifica las claves RSA antes de cablear nada).

### Errores sin filtración interna y sin Swagger en runtime

`AddProblemDetails()` + `UseExceptionHandler()` (`apps/Auth/Backend/Modules/ProblemDetailsModule.cs:10`) devuelven `application/problem+json` sin *stack trace*. Swashbuckle está como paquete pero **no se mapea** (sin `AddSwaggerGen`/`UseSwagger` en ningún backend), así que la API no expone documentación interactiva en producción.

## Flujo de los componentes

```
petición /api/<svc>/*
   │
   ▼  api-gateway nginx
   │     · HSTS / nosniff / Referrer-Policy   (siempre)
   │     · CORS allowlist (map)               · OPTIONS → 204
   │
   ▼  backend
   │     · DAO: parámetros nombrados + stored procedures  (sin SQL dinámico)
   │     · ProblemDetails → error sin stack trace
   │     · sin Swagger mapeado
   │
arranque: SecretsValidationModule (Order -100) → fail-fast, sin defaults inseguros
```

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API8 · Security Misconfiguration**, que agrupa los nodos **nginx headers + HSTS**, **CORS allowlist**, **SQL parametrizado** y **Secrets fail-fast**.

## Verificación

- Cabeceras: `curl -sI http://localhost:8100/api/auth/ | grep -i strict-transport`.
- CORS: petición con `Origin` ajeno no debe recibir `Access-Control-Allow-Origin`.
- SQL parametrizado: `grep -rn "AddWithValue\|StoredProcedure" apps/*/Backend/DB/Daos/` muestra el patrón; no debe existir interpolación de cadenas de usuario en SQL.
- Sin Swagger: `grep -rn "AddSwaggerGen\|UseSwagger" apps/*/Backend/` vacío.

## Notas / brechas conocidas

- `client_max_body_size 1m` y los límites de tasa también son configuración del gateway, pero su objetivo (consumo de recursos) se documenta en `API4-unrestricted-resource-consumption.md`.
- La confianza en `CF-Connecting-IP` desde cualquier origen es segura sólo bajo el supuesto de que el único ingress es el Cloudflare Tunnel (ver A05 y las notas de `nginx.conf:95`).
