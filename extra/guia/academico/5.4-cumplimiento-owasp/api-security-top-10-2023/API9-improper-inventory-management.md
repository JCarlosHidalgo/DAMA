# API9 · Improper Inventory Management (API Security Top 10 2023)

> **Estado:** 🟢 — Existe un inventario versionado de todos los puntos de acceso (colecciones Bruno por servicio), un **único punto de entrada** (el api-gateway nginx; los backends no publican puerto al host), sondas de disponibilidad (`/health`, `/health/ready`) y entornos dev/prod separados con su propia config. Es un control de **proceso/gobernanza**: depende de mantener el inventario sincronizado con el código.

## Qué exige OWASP

La mala gestión de inventario de APIs ocurre cuando hay APIs o versiones "fantasma" desplegadas sin documentar (entornos viejos, *debug*, hosts olvidados, versiones deprecadas) que amplían la superficie de ataque sin vigilancia. OWASP pide un inventario actualizado de todos los hosts, entornos, versiones y puntos de acceso, y limitar el acceso a entornos no productivos.

## Cómo lo cumple DAMA

### Inventario de puntos de acceso versionado (Bruno)

Cada servicio tiene su colección de peticiones bajo `api-endpoints/collections/<Servicio>/`, versionada en el repo. Bruno es la única herramienta para ejercer las APIs (Postman quedó descartado). El directorio `api-endpoints/collections/` contiene `Auth/`, `Attendance/`, `CourseManagement/`, `Credentials/`, `Payment/` (y `Wallbit/`).

Ejemplo de entradas en `api-endpoints/collections/Auth/` (verificado): `Public- Login Client.yml`, `Public- Refresh Token.yml`, `Admin- Create Tenant.yml`, `Client- Register Student.yml`, etcétera — cada punto de acceso público y autenticado tiene su petición documentada, así que el inventario de la superficie HTTP vive junto al código.

### Un único punto de entrada: la puerta de enlace

Todo el tráfico externo entra por la puerta de enlace nginx en `/api/<svc>/*`; los cinco backends **no publican puerto al host** — sólo son alcanzables por nombre de contenedor dentro de la red de compose. Esto elimina hosts "sueltos" expuestos sin pasar por la puerta de enlace.

En dev, sólo `frontend` y `api` (puerta de enlace) publican `ports:`; los backends usan `expose:` (puerto de red interna) sin mapeo al host (`infrastructure/compose.prod.yaml:99` y siguientes muestran `expose: - "80"` para cada `*-service`, sin `ports:`).

La configuración de nginx declara un `upstream` por cada backend (`auth`, `course-management`, `attendance`, `credentials`, `payment`), cada uno apuntando al nombre de host del contenedor inyectado por variable de entorno (`${AUTH_HOST_NAME}`, `${COURSE_MANAGEMENT_HOST_NAME}`, etc.) (`infrastructure/environments/api-gateway/nginx.conf:46`).

Las rutas son *kebab-case* en minúsculas con barra final (`/api/auth/`, `/api/course-management/`, …): añadir un backend exige declarar su `upstream` y su `location`, de modo que cada superficie expuesta es explícita en un único archivo.

Servicios auxiliares **sólo tras la puerta de enlace**: la consola de gestión de RabbitMQ (`/api/rabbitmq/`, `infrastructure/environments/api-gateway/nginx.conf:216`) y DbGate (administración de esquema, sólo en prod, `/api/db-gate/`, `:193`). Ninguno publica puerto propio: RabbitMQ no expone puertos al host en ningún compose (sólo alcanzable en red interna o vía puerta de enlace), y DbGate no lleva `expose`/`ports` (nginx lo alcanza como `DbGate:3000` en la red `dokploy-network`, `infrastructure/compose.prod.yaml:272`).

### Sondas de disponibilidad para descubrir el estado real del despliegue

Cada backend mapea `/health` (*liveness*, sin dependencias) y `/health/ready` (*readiness* profunda, una comprobación por dependencia externa).

El módulo registra `/health` con un predicado que no incluye ninguna comprobación (responde sólo si el proceso vive) y `/health/ready` con un predicado que selecciona las comprobaciones etiquetadas como `ready` y un escritor de respuesta dedicado; ambos puntos se exponen de forma anónima (`apps/Auth/Backend/Modules/HealthCheckModule.cs:28`).

`/health/ready` nombra exactamente qué dependencia está caída (Database, RabbitMq, par gRPC), lo que da inventario operativo del despliegue. **Credentials** es la excepción deliberada: al no tener dependencias externas mapea **sólo** `/health` (`apps/Credentials/Backend/Modules/HealthCheckModule.cs:15`), sin `/health/ready`.

### Entornos dev/prod separados y rastreables

Dos artefactos de compose distintos con su propia config por entorno: `infrastructure/compose.dev.yaml` (stack dev completo, incluye el broker; puertos host con desplazamiento +100) y `infrastructure/compose.prod.yaml` (Dokploy: sólo *app services*, `expose` en vez de `ports`, sin MySQL gestionado en compose). La config va 100% por variable de entorno desde `.env.dev` / `.env.prod` (ambos *gitignored*; la única plantilla committeada es `infrastructure/.env.example`, el inventario completo de variables). En prod la puerta de enlace publica sólo en loopback (`ports: - "127.0.0.1:8000:80"`, `infrastructure/compose.prod.yaml:55`), detrás del Cloudflare Tunnel — no hay puerto público directo.

## Flujo de los componentes

```
inventario de la API
   │
   ▼  api-endpoints/collections/<Servicio>/*.yml   (Bruno, versionado)
   │     · una petición por punto de acceso público/autenticado
   │
   ▼  punto de entrada único: api-gateway nginx  /api/<svc>/*
   │     · upstreams declarados explícitamente (un archivo)
   │     · backends sin puertos al host (expose, no ports)
   │     · RabbitMQ y DbGate sólo tras la puerta de enlace
   │
   ▼  sondas: /health (liveness) + /health/ready (readiness por dependencia)
   │     · Credentials: sólo /health (sin dependencias externas)
   │
   ▼  entornos separados: compose.dev.yaml vs compose.prod.yaml · .env.dev vs .env.prod
```

En el diagrama FossFlow `extra/graphics/diagrams/owasp-api-top-10.json`, este ítem es el rectángulo **API9 · Improper Inventory Management**, que agrupa los nodos **Bruno api-endpoints**, **Gateway unico ingress** y **Health endpoints**.

## Verificación

- Inventario de puntos de acceso: `ls api-endpoints/collections/` lista los servicios; cada `<Servicio>/*.yml` es un punto de acceso documentado.

```bash
ls api-endpoints/collections/
grep -n "ports:\|expose:" infrastructure/compose.prod.yaml
grep -n "location /api/" infrastructure/environments/api-gateway/nginx.conf
curl http://localhost:8100/api/auth/../health
```

- Punto de entrada único: en `infrastructure/compose.prod.yaml` sólo `frontend` y `api` (puerta de enlace) deben llevar `ports:`; los cinco `*-service` sólo `expose:`.
- Rutas declaradas: el listado de `location /api/` en `infrastructure/environments/api-gateway/nginx.conf` enumera la superficie expuesta.
- Sondas: consultar `/health` por la red interna o ver `apps/*/Backend/Modules/HealthCheckModule.cs`.
- Entornos: `infrastructure/compose.dev.yaml` y `infrastructure/compose.prod.yaml` deben divergir en `ports`/`expose` y servicios.

## Notas y brechas conocidas

- **Control de proceso.** El inventario Bruno se mantiene a mano: un punto de acceso nuevo sin su `.yml` quedaría fuera del inventario. La convención (sincronizar controlador + puerta de enlace + frontend + Bruno al añadir rutas) mitiga esto pero no lo automatiza.
- No hay versionado de API por ruta (`/v1/`, `/v2/`): la API es de versión única, lo que reduce el riesgo de versiones deprecadas conviviendo, pero significa que un cambio de contrato rompe la compatibilidad sin ruta de convivencia.
- DbGate y la consola de RabbitMQ son superficies de administración; aunque sólo accesibles tras la puerta de enlace, conviene que su autenticación (login de DbGate, credenciales de RabbitMQ) se trate como secreto de prod y no se reutilice desde dev.
- Las colecciones Bruno usan `.yml` (no `.bru`); cualquier auditoría que busque `*.bru` daría un falso negativo.
