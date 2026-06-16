# API9 · Improper Inventory Management (API Security Top 10 2023)

> **Estado:** 🟢 — Existe un inventario versionado de todos los endpoints (colecciones Bruno por servicio), un **único punto de entrada** (el api-gateway nginx; los backends no publican puerto al host), sondas de salud (`/health`, `/health/ready`) y entornos dev/prod separados con su propia config. Es un control de **proceso/gobernanza**: depende de mantener el inventario sincronizado con el código.

## Qué exige OWASP

La mala gestión de inventario ocurre cuando hay APIs o versiones "fantasma" desplegadas sin documentar (entornos viejos, *debug*, hosts olvidados, versiones deprecadas) que amplían la superficie de ataque sin vigilancia. OWASP pide un inventario actualizado de todos los hosts, entornos, versiones y endpoints, y limitar el acceso a entornos no productivos.

## Cómo lo cumple DAMA

### Inventario de endpoints versionado (Bruno)

Cada servicio tiene su colección de peticiones bajo `api-endpoints/collections/<Servicio>/`, versionada en el repo. Bruno es la única herramienta para ejercer las APIs (Postman quedó descartado). Servicios con colección:

`api-endpoints/collections/` contiene `Auth/`, `Attendance/`, `CourseManagement/`, `Credentials/`, `Payment/` (y `Wallbit/`).

Ejemplo de entradas en `api-endpoints/collections/Auth/` (verificado): `Public- Login Client.yml`, `Public- Refresh Token.yml`, `Admin- Create Tenant.yml`, `Client- Register Student.yml`, etc. — cada endpoint público y autenticado tiene su petición documentada, así que el inventario de la superficie HTTP vive junto al código.

### Un único punto de entrada: el api-gateway

Todo el tráfico externo entra por el gateway nginx en `/api/<svc>/*`; los cinco backends **no publican puerto al host** — sólo son alcanzables por nombre de contenedor dentro de la red de compose. Esto elimina hosts "sueltos" expuestos sin pasar por el gateway.

En dev, sólo `frontend` y `api` (gateway) publican `ports:`; los backends usan `expose:` (puerto de red interna) sin mapeo al host (`infrastructure/compose.prod.yaml:99` y siguientes muestran `expose: - "80"` para cada `*-service`, sin `ports:`).

`infrastructure/environments/api-gateway/nginx.conf:46`

```nginx
upstream auth            { server ${AUTH_HOST_NAME}; }
upstream course-management { server ${COURSE_MANAGEMENT_HOST_NAME}; }
upstream attendance      { server ${ATTENDANCE_HOST_NAME}; }
upstream credentials     { server ${CREDENTIALS_HOST_NAME}; }
upstream payment         { server ${PAYMENT_HOST_NAME}; }
```

Las rutas son *lowercase kebab-case* con barra final (`/api/auth/`, `/api/course-management/`, …): añadir un backend exige declarar `upstream` + `location`, de modo que cada superficie expuesta es explícita en un único archivo.

Servicios auxiliares **sólo tras el gateway**: RabbitMQ management (`/api/rabbitmq/`, `infrastructure/environments/api-gateway/nginx.conf:216`) y DbGate (admin de esquema, prod-only, `/api/db-gate/`, `:193`). Ninguno publica puerto propio: RabbitMQ no expone host ports en ningún compose (sólo alcanzable en red interna o vía gateway), y DbGate no lleva `expose`/`ports` (nginx lo alcanza como `DbGate:3000` en la red `dokploy-network`, `infrastructure/compose.prod.yaml:272`).

### Sondas de salud para descubrir el estado real del despliegue

Cada backend mapea `/health` (liveness, sin dependencias) y `/health/ready` (readiness profunda, una comprobación por dependencia externa).

`apps/Auth/Backend/Modules/HealthCheckModule.cs:28`

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
})
.AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
    ResponseWriter = ReadinessResponseWriter.WriteAsync
})
.AllowAnonymous();
```

`/health/ready` nombra exactamente qué dependencia está caída (Database, RabbitMq, gRPC peer), lo que da inventario operativo del despliegue. **Credentials** es la excepción deliberada: al no tener dependencias externas mapea **sólo** `/health` (`apps/Credentials/Backend/Modules/HealthCheckModule.cs:15`), sin `/health/ready`.

### Entornos dev/prod separados y rastreables

Dos artefactos de compose distintos con su propia config por entorno: `infrastructure/compose.dev.yaml` (stack dev completo, incluye broker; puertos host *offset +100*) y `infrastructure/compose.prod.yaml` (Dokploy: sólo *app services*, `expose` no `ports`, sin MySQL gestionado en compose). La config va 100% por env var desde `.env.dev` / `.env.prod` (ambos *gitignored*; el único template committeado es `infrastructure/.env.example`, el inventario completo de variables). En prod el gateway publica sólo en loopback (`ports: - "127.0.0.1:8000:80"`, `infrastructure/compose.prod.yaml:55`), detrás del Cloudflare Tunnel — no hay puerto público directo.

## Flujo de los componentes

```
inventario de la API
   │
   ▼  api-endpoints/collections/<Servicio>/*.yml   (Bruno, versionado)
   │     · una petición por endpoint público/autenticado
   │
   ▼  ingress único: api-gateway nginx  /api/<svc>/*
   │     · upstreams declarados explícitamente (un archivo)
   │     · backends sin host ports (expose, no ports)
   │     · RabbitMQ y DbGate sólo tras el gateway
   │
   ▼  sondas: /health (liveness) + /health/ready (readiness por dependencia)
   │     · Credentials: sólo /health (sin deps externas)
   │
   ▼  entornos separados: compose.dev.yaml vs compose.prod.yaml · .env.dev vs .env.prod
```

En el diagrama FossFLOW `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API9 · Improper Inventory Management**, que agrupa los nodos **Bruno api-endpoints**, **Gateway unico ingress** y **Health endpoints**.

## Verificación

- Inventario de endpoints: `ls api-endpoints/collections/` lista los servicios; cada `<Servicio>/*.yml` es un endpoint documentado.
- Ingress único: `grep -n "ports:\|expose:" infrastructure/compose.prod.yaml` — sólo `frontend` y `api` (gateway) deben llevar `ports:`; los cinco `*-service` sólo `expose:`.
- Rutas declaradas: `grep -n "location /api/" infrastructure/environments/api-gateway/nginx.conf` enumera la superficie expuesta.
- Sondas: `curl http://localhost:8100/api/auth/../health` (vía red interna) o ver `apps/*/Backend/Modules/HealthCheckModule.cs`.
- Entornos: `infrastructure/compose.dev.yaml` y `infrastructure/compose.prod.yaml` deben divergir en `ports`/`expose` y servicios.

## Notas / brechas conocidas

- **Control de proceso.** El inventario Bruno se mantiene a mano: un endpoint nuevo sin su `.yml` quedaría fuera del inventario. La convención (sincronizar controlador + gateway + frontend + Bruno al añadir rutas) mitiga esto pero no lo automatiza.
- No hay versionado de API por path (`/v1/`, `/v2/`): la API es de versión única, lo que reduce el riesgo de versiones deprecadas conviviendo, pero significa que un cambio de contrato es *breaking* sin ruta de convivencia.
- DbGate y la UI de RabbitMQ son superficies de administración; aunque sólo accesibles tras el gateway, conviene que su autenticación (login DbGate, credenciales RabbitMQ) se trate como secreto de prod y no se reutilice desde dev.
- Las colecciones Bruno usan `.yml` (no `.bru`); cualquier auditoría que busque `*.bru` daría falso negativo.
