# 3.6 Despliegue (CI/CD, nube)

> **Estado:** ✅ Redactado. Estrategia y proceso de despliegue de DAMA.
> **Guía:** [4-estructura-marco-practico.md](../4-estructura-marco-practico.md).
> **Fuente operativa:** runbook completo en `infrastructure/environments.md`.
> **Topología (diseño):** la vista de despliegue —nodos y red— es la sección
> [3.7-diagrama-de-despliegue.md](3.7-diagrama-de-despliegue.md) (diagrama FossFlow); aquí se
> documenta el **proceso**, no se redibuja la topología.
>
> **Nota de honestidad:** la parte de **entrega/despliegue (CD)** está **implementada y operativa**
> (Dokploy + Cloudflare). La **integración continua automatizada (CI)** **no está implementada**: hoy
> las barreras de calidad se ejecutan manualmente antes de integrar. La sección 3.6.7 documenta ese
> estado real y propone **Jenkins** como mejora, marcada como **no implementada** (no se presenta como
> si corriera).

---

## 3.6.1 Estrategia general

DAMA se despliega como un conjunto de **contenedores Docker**, uno por pieza (los cinco backends, la
SPA, el *api-gateway*, el broker y los servicios auxiliares). La estrategia descansa en una decisión
de diseño: **el artefacto de cada servicio es el mismo entre entornos**; solo cambia la configuración
inyectada por **variables de entorno** (RNF-022). Así, pasar de desarrollo a producción no recompila
lógica —solo cambia la topología de red y el origen de los datos— y el despliegue es reproducible.

La topología está descrita de forma **versionada** en los archivos de composición
(`infrastructure/compose.dev.yaml`, `compose.prod.yaml`, `compose.test.yaml`, `compose.docs.yaml`):
la infraestructura es código, no configuración manual de servidores.

## 3.6.2 Las dos formas: desarrollo y producción

DAMA corre en dos configuraciones, ambas detrás de **Cloudflare**:

| Aspecto | Desarrollo (`compose.dev.yaml`) | Producción (`compose.prod.yaml`) |
|---------|--------------------------------|----------------------------------|
| **Orquestador** | Docker Compose local (envoltorios `compose-up.sh`/`compose-down.sh`). | **Dokploy** sobre la red externa `dokploy-network`. |
| **Bases de datos** | Contenedores `mysql:9` incluidos en el stack. | **Bases gestionadas por Dokploy** (no hay `mysql:9` en el compose). |
| **Exposición de puertos** | `ports` publicados en el host (gateway 8100, frontend 8101). | `expose` (sin publicar puertos); solo el gateway recibe tráfico externo. |
| **URL pública** | `dev.dama-software.org` / `api-dev.dama-software.org` (subdominio de un solo nivel por la cobertura SSL universal de Cloudflare). | `dama-software.org` / `api.dama-software.org`. |
| **Semillas** | `SEED_DB=true` con fixtures CSV (Auth/CourseManagement/Payment). | **Sin semillas** (no hay `SEED_DB` ni montajes CSV). |
| **TLS inter-servicio** | Automático (`tls-init`). | Automático (`tls-init`, idéntico). |

Ambos entornos comparten el mismo `nginx.conf` del gateway y el mismo mecanismo de TLS interno; la
diferencia es de **topología y datos**, no de imágenes.

## 3.6.3 Despliegue continuo en Dokploy (estado real, operativo)

La entrega a producción la orquesta **Dokploy**, que **construye las imágenes desde el checkout del
repositorio de GitHub** (contexto de build `${CONTEXT}`) y levanta el stack de `compose.prod.yaml`. El
proceso es el runbook de `infrastructure/environments.md`, resumido en estos pasos:

| # | Paso (runbook `environments.md`) | Qué se hace |
|---|----------------------------------|-------------|
| 0 | Prerrequisitos | Host Dokploy con la red externa `dokploy-network`; DNS apuntado; repo clonado por Dokploy. |
| 1 | Provisionar las **cuatro** bases gestionadas | MySQL 9 (igual que dev), una por backend con estado; Credentials no tiene base. |
| 2 | Inicializar cada esquema **una vez** (`init.sql`) | **No hay migraciones**; se aplica el esquema + procedimientos almacenados a mano (las bases gestionadas no auto-ejecutan scripts de init). |
| 3 | Generar **secretos frescos** de producción | Par RSA del JWT, secretos de callback y gRPC de suscripción, credenciales de RabbitMQ/DbGate, clave Todotix. Nunca se reutilizan los de dev. |
| 4 | TLS inter-servicio (**automático**) | El servicio `tls-init` genera la CA + certificados en el volumen `dama-tls`; sin paso manual. |
| 5 | Cargar el entorno en Dokploy | Variables a partir de `infrastructure/.env.example` (nombres de contenedor reales, cadenas de conexión, URLs públicas, secretos). |
| 6 | Desplegar el stack | El orden de arranque lo maneja `depends_on`: RabbitMQ sano → backends → gateway → frontend. |
| 7 | Mapear dominios y configurar **DbGate** | `dama-software.org`→frontend, `api.dama-software.org`→gateway; DbGate va **bajo el gateway** en `/api/db-gate/`. |
| 8 | Habilitar **respaldos S3** | En cada base gestionada, `mysqldump → S3` nativo de Dokploy (cron + retención). |

> **Esto es despliegue continuo (CD), no integración continua (CI):** Dokploy **construye y despliega**
> a partir del repositorio, pero **no ejecuta las pruebas ni las barreras de calidad** antes de
> hacerlo. Esa verificación es hoy manual (ver 3.6.7).

## 3.6.4 TLS: borde público y canal interno

DAMA tiene dos planos de TLS, ambos **sin gestión manual de certificados**:

- **Borde público — Cloudflare Tunnels.** El TLS de cara al usuario lo provee Cloudflare de forma
  automática; **no se usa certbot/Let's Encrypt** en producción. La API pública es
  `api.dama-software.org` (un solo nivel de subdominio, por la cobertura del certificado universal de
  Cloudflare).
- **Canal interno — `tls-init` automático.** El gRPC entre servicios va cifrado de extremo a extremo.
  El servicio de un solo uso `tls-init` genera, en **cada** `up` (dev y prod), una CA interna y un
  certificado por servidor gRPC (CourseManagement y Auth) en el volumen `dama-tls`; los clientes
  (Attendance y Payment) instalan la CA en su almacén de confianza al arrancar. **No hay `COPY` de
  certificados en los Dockerfiles** ni paso manual: un clon limpio compila sin `infrastructure/tls/`
  presente. La generación es idempotente y el volumen persiste, así que los certificados son estables
  entre despliegues (para rotarlos, se borra el volumen y se redespliega).

## 3.6.5 Datos: bases gestionadas, esquema sin migraciones, respaldos y cambios

- **Bases gestionadas (Dokploy).** Cuatro MySQL 9 gestionadas, una por backend con estado. Que sean
  *gestionadas* es lo que habilita los respaldos nativos.
- **Esquema sin migraciones.** El esquema y los procedimientos almacenados se aplican **una vez** desde
  `infrastructure/environments/<svc>/init.sql`. No existe herramienta de migraciones: los cambios de
  esquema posteriores (ALTER TABLE, índices, claves foráneas) se hacen con **DbGate**.
- **Respaldos — recuperabilidad.** Cada base gestionada ejecuta `mysqldump → S3` por cron con
  retención. Esta es la historia de respaldo de **producción** (la recuperabilidad evaluada en
  5.3 §5); el entorno dev no contempla respaldos por diseño (datos efímeros).
- **DbGate tras el gateway.** La administración de esquema usa **DbGate** (web, GPL-3.0), añadido como
  servicio en `compose.prod.yaml` y expuesto **a través del gateway** en `/api/db-gate/` (no como
  dominio aparte), con login propio y conexiones persistidas en el volumen `dbgate-data`.

## 3.6.6 Secretos y arranque con fallo rápido

Los secretos (`JWT_*`, `PAYMENT_CALLBACK_SECRET`, `SUBSCRIPTION_GRPC_SECRET`, `TODOTIX_APPKEY`,
credenciales de RabbitMQ/DbGate) viven solo en `.env.*` (gitignored; el único archivo versionado es
`.env.example`, el inventario). El módulo `SecretsValidationModule` (orden `-100`) de cada backend
**valida los secretos antes que nada en el arranque** y mata el contenedor con un mensaje preciso si
alguno falta o está malformado, en lugar de fallar con un error 500 en tiempo de petición. El playbook
de rotación está en `infrastructure/SECRETS.md`.

## 3.6.7 Integración continua: estado real y propuesta Jenkins

### Estado real (no implementado como automatización)

Hoy **no existe un servidor de integración continua**: no hay `.github/workflows/`, ni GitLab CI, ni
Jenkins en el repositorio. Las **barreras de calidad** documentadas en 3.5 —`dotnet test` de las
cuatro suites (incluidos los NetArchTest de la Bandeja de Salida), `bun run test:ci` y la barrera de
cobertura crítica del frontend, `dotnet format --verify-no-changes` y los *gates* de complejidad de
SonarAnalyzer— se ejecutan **manualmente** por el desarrollador antes de integrar a `main`. Dokploy
construye y despliega después del merge, **sin** correr esas barreras. Es la misma clase de **brecha
honesta** que se declara en 5.3: se reporta el estado real, no se asume una CI inexistente.

### Propuesta: Jenkins como capa de CI (no implementada)

> ⚠️ **Propuesta, no implementada.** Lo siguiente describe cómo se *cerraría* el "CI" del título de la
> sección; **no** corresponde a infraestructura existente en el repositorio.

Jenkins cubriría exactamente ese hueco, insertándose **antes** del merge y del despliegue, sin
solaparse con Dokploy:

```
push / PR a GitHub ──► Jenkins (CI: build + barreras) ──► merge a main ──► Dokploy (CD: build + deploy)
```

Un `Jenkinsfile` declarativo en la raíz reutilizaría, etapa por etapa, las barreras que **ya existen**
en el repo (sin inventar herramientas nuevas), aprovechando el monorepo para paralelizar:

| Etapa propuesta | Comando reutilizado del repo | Barrera que impone |
|-----------------|------------------------------|--------------------|
| Estilo (por backend, en paralelo) | `dotnet format <Backend>.csproj --verify-no-changes` | Convenciones .NET uniformes. |
| Build + complejidad | `dotnet build -c Release` | *Gates* SonarAnalyzer (S3776 ≤ 10, S1541 ≤ 20). |
| Pruebas backend | `dotnet test -c Release -s ./.runsettings` | 598 pruebas + NetArchTest de la Bandeja de Salida; publica cobertura. |
| Pruebas frontend | `bun run test:ci` y `bun run test:coverage:gate` | 653 pruebas + 100 % de cobertura en rutas críticas. |
| Build de imágenes (opcional) | `docker compose -f infrastructure/compose.prod.yaml build` | Verifica que las imágenes de producción compilan. |

El agente correría sobre la imagen `mcr.microsoft.com/dotnet/sdk:9.0` (más Bun), **el mismo entorno**
que ya usan los Dockerfiles, evitando deriva entre lo que se prueba y lo que se despliega. El disparo
sería por *webhook* de GitHub (pipeline multibranch: un job por rama/PR), y con **protección de rama**
en `main` que exija el check verde de Jenkins antes de permitir el merge. Así, los **criterios de
aceptación** de 3.5.1.4 (todas las pruebas pasan, cobertura crítica cumplida, sin regresiones de
formato, NetArchTest verde), que hoy se verifican a mano, pasarían a ser una **puerta automática
obligatoria** previa al CD de Dokploy.

## 3.6.8 Verificación post-despliegue

El runbook cierra con comprobaciones (sección *Verification* de `environments.md`):

- **TLS automático:** `tls-init` termina con éxito y puebla `dama-tls`; los cuatro servicios gRPC
  arrancan solo después.
- **Frontend y API accesibles:** `dama-software.org` carga; `api.dama-software.org` enruta
  `/api/<servicio>/*` a cada backend (`infrastructure/verify-gateway-routes.sh`).
- **Backends vivos:** cada uno responde `GET /health` (sonda de vivacidad superficial).
- **gRPC con TLS:** ambas aristas (Attendance→CourseManagement, Payment→Auth) resuelven sin error de
  certificado.
- **DbGate y respaldos:** el panel responde en `/api/db-gate/`; el primer `mysqldump` aterriza en S3.

## 3.6.9 Comandos de demostración

```bash
# Topología de producción: servicios, expose (no ports) y red externa
grep -nE "container_name:|expose:|dokploy-network|external:" infrastructure/compose.prod.yaml

# DbGate tras el gateway, TLS automático y red Dokploy
grep -n "dbgate\|tls-init\|dokploy-network" infrastructure/compose.prod.yaml

# El runbook completo de producción
sed -n '22,176p' infrastructure/environments.md

# Estado real de CI: no hay pipeline versionado (la propuesta Jenkins NO está implementada)
ls .github/workflows 2>/dev/null || echo "sin CI versionado"
ls Jenkinsfile .gitlab-ci.yml 2>/dev/null || echo "sin Jenkins/GitLab CI"

# Verificación de rutas del gateway tras desplegar
cat infrastructure/verify-gateway-routes.sh
```

## 3.6.10 Conclusión de la sección

El despliegue de DAMA es **infraestructura como código** sobre contenedores: el mismo artefacto por
servicio, configurado por variables de entorno (RNF-022), desplegado en **Dokploy** con bases
gestionadas y respaldos S3, tras **Cloudflare** para el TLS público, con TLS inter-servicio
**automático** y administración de esquema por **DbGate**. La parte de **entrega (CD)** está operativa;
la **integración continua (CI)** **no está automatizada** —las barreras de calidad se corren a mano— y
se propone **Jenkins** para cerrarla, reutilizando las mismas barreras de 3.5 como puerta obligatoria
previa al despliegue. La propuesta se declara explícitamente como **no implementada**, en coherencia
con el rigor de reportar el estado real.
