# 3.6 Despliegue (CI/CD, nube)

> **Estado:** ✅ Redactado. Estrategia y proceso de despliegue de DAMA.
> **Guía:** [4-estructura-marco-practico.md](../4-estructura-marco-practico.md).
> **Fuente operativa:** runbook completo en `infrastructure/environments.md`.
> **Topología (diseño):** la vista de despliegue —nodos y red— es la sección
> [3.7-diagrama-de-despliegue.md](3.7-diagrama-de-despliegue.md) (diagrama FossFlow); aquí se
> documenta el **proceso**, no se redibuja la topología.
>
> **Estado de CI/CD:** ambas partes están **implementadas y operativas**. La **entrega/despliegue (CD)**
> corre en **Dokploy + Cloudflare**; la **integración continua automatizada (CI)** corre en **GitHub
> Actions** (`.github/workflows/ci.yml` y `codeql.yml`), ejecutando las barreras de calidad de 3.5 en
> cada *pull request* a `main` antes de integrar. La sección 3.6.7 documenta ese pipeline.

---

## Introducción

Esta sección documenta el proceso de despliegue de DAMA como infraestructura como código sobre
contenedores Docker, partiendo de la estrategia general —el mismo artefacto por servicio,
configurado solo por variables de entorno— y contrastando las dos formas de correr el sistema,
desarrollo y producción. Detalla el despliegue continuo operativo en Dokploy (con sus pasos de
runbook), los dos planos de TLS sin gestión manual de certificados (Cloudflare en el borde y
`tls-init` en el canal interno), la gestión de datos (bases gestionadas, esquema sin migraciones,
respaldos S3 y administración con DbGate) y el arranque con fallo rápido ante secretos ausentes.
Documenta la integración continua (CI) operativa en **GitHub Actions**, que ejecuta las barreras de
calidad de 3.5 en cada *pull request* a `main` —build, formato, pruebas y análisis de seguridad
CodeQL— como puerta previa al CD de Dokploy; cierra con la verificación post-despliegue y los
comandos de demostración.

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

> **Esto es despliegue continuo (CD), distinto de la integración continua (CI):** Dokploy **construye y
> despliega** a partir del repositorio tras el merge a `main`. Las **pruebas y barreras de calidad** las
> ejecuta antes, en el *pull request*, el pipeline de **GitHub Actions** (ver 3.6.7); con protección de
> rama, ese check verde es requisito para integrar y, por tanto, para que Dokploy despliegue.

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

## 3.6.7 Integración continua: GitHub Actions (estado real, operativo)

La integración continua corre en **GitHub Actions**, versionada en `.github/workflows/`: `ci.yml`
(build + barreras de calidad) y `codeql.yml` (análisis de seguridad). Se inserta **antes** del merge y
del despliegue, sin solaparse con Dokploy:

```
PR a GitHub ──► GitHub Actions (CI: build + barreras + CodeQL) ──► merge a main ──► Dokploy (CD: build + deploy)
```

### `ci.yml` — build y pruebas por área cambiada

El pipeline se dispara en cada `pull_request` a `main` (`opened`, `synchronize`, `reopened`), con
`concurrency` que cancela ejecuciones previas de la misma rama. Su primer job, `changes`, usa
`dorny/paths-filter` para detectar **qué áreas de `apps/` cambiaron** y emite la matriz de backends a
construir y probar; así, un PR que solo toca un servicio no recompila ni reprueba el monorepo entero.
Reutiliza, etapa por etapa, las mismas barreras de 3.5:

| Job | Comando del repo | Barrera que impone |
|-----|------------------|--------------------|
| `build-backend` (matriz por servicio cambiado) | `dotnet format <Backend>.csproj --verify-no-changes` + `dotnet build -c Release -p:TreatWarningsAsErrors=true` | Convenciones .NET y *gates* de SonarAnalyzer (los warnings del analizador fallan el PR). |
| `build-frontend` (si cambió `apps/Frontend/`) | `bun run format:check` + `bun run lint` + `bun run build` | Prettier, ESLint y compilación del bundle Angular; el paso de ESLint además impone el techo de **complejidad ciclomática (≤ 20)**, espejo de la regla S1541 del backend. |
| `test-backend` (matriz; excluye Credentials) | `dotnet test --settings .../.runsettings` + `check-coverage.py` | Las suites de Auth/Attendance/CourseManagement/Payment (incluidos los NetArchTest de la Bandeja de Salida) y la **puerta de cobertura crítica** (falla el PR si la cobertura de líneas de lógica de negocio baja del 100 %); cada servicio publica un *job summary* parseado de su `.trx`. |
| `test-frontend` (si cambió `apps/Frontend/`) | `bun run test:coverage:gate` | Pruebas del frontend con cobertura y **puerta de cobertura crítica** (100 % sobre lógica/validadores/*stores*). |
| `ci-gate` | agrega los resultados | Falla si algún job requerido no terminó en `success`/`skipped`; es el check único que resume el pipeline. |

Credentials **se construye pero no se prueba** (es un *dummy* de solo-claims, sin casos de prueba),
en coherencia con el alcance de 3.5. Los jobs cachean paquetes NuGet y el caché de Bun por *hash* del
lockfile, y la matriz corre con `fail-fast: false` para reportar todas las fallas de un PR de una vez.

### `codeql.yml` — análisis de seguridad

El workflow `codeql.yml` ejecuta el escaneo **CodeQL** para `csharp` y `javascript-typescript` (matriz
de lenguajes, `build-mode: none`) en cada `push` y `pull_request` a `main`, más un cron **semanal**
(lunes 06:00 UTC). Publica los hallazgos en *security-events*, cubriendo el análisis estático de
seguridad como capa adicional a las barreras de calidad de `ci.yml`.

### Puerta obligatoria previa al despliegue

El agente de GitHub Actions corre sobre `ubuntu-latest` con .NET 9 (`actions/setup-dotnet`) y Bun
(`oven-sh/setup-bun`), alineado con el entorno de los Dockerfiles. Con **protección de rama** en `main`
(paso manual en la UI de GitHub, no versionable; ver `.github/workflows/README.md`), el check verde es
requisito para integrar. Así, los **criterios de aceptación** de 3.5.1.4 (todas las pruebas pasan, sin
regresiones de formato, NetArchTest verde) son una **puerta automática obligatoria** previa al CD de
Dokploy.

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

# CI versionada en GitHub Actions: pipeline de calidad + escaneo CodeQL
ls .github/workflows
grep -nE "^name:|^on:|dotnet format|TreatWarningsAsErrors|dotnet test|bun run|paths-filter" .github/workflows/ci.yml
grep -nE "^name:|languages:|cron:" .github/workflows/codeql.yml

# Verificación de rutas del gateway tras desplegar
cat infrastructure/verify-gateway-routes.sh
```

## 3.6.10 Conclusión de la sección

El despliegue de DAMA es **infraestructura como código** sobre contenedores: el mismo artefacto por
servicio, configurado por variables de entorno (RNF-022), desplegado en **Dokploy** con bases
gestionadas y respaldos S3, tras **Cloudflare** para el TLS público, con TLS inter-servicio
**automático** y administración de esquema por **DbGate**. La **entrega (CD)** está operativa en
Dokploy, y la **integración continua (CI)** está automatizada en **GitHub Actions**: `ci.yml` ejecuta
las mismas barreras de 3.5 (build, formato, pruebas por área cambiada) y `codeql.yml` añade el escaneo
de seguridad, ambos en cada *pull request* a `main` como puerta obligatoria previa al despliegue. El
ciclo CI→CD queda así cerrado de extremo a extremo y versionado en el repositorio.
