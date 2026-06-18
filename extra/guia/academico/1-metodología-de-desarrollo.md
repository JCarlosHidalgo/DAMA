# 3.1 Metodología de desarrollo

> **Estado:** ✅ Redactado. Texto académico listo para el capítulo, con su análisis de Ruta
> Crítica (CPM) y los comandos que demuestran el proceso sobre el repositorio.
>
> **Diagramas de esta sección:**
> - Red CPM (Actividad-en-Nodo) con ruta crítica: [`1-metodología-de-desarrollo-cpm-ruta-critica.drawio`](1-metodología-de-desarrollo-cpm-ruta-critica.drawio) (abrir en [app.diagrams.net](https://app.diagrams.net) o la extensión draw.io de VS Code).
> - Fases del WBS (vista isométrica): [`1-metodología-de-desarrollo-fases-wbs.md`](1-metodología-de-desarrollo-fases-wbs.md) (FossFlow).

---

## Introducción

Esta sección describe cómo se condujo y planificó el desarrollo de DAMA. Primero expone el enfoque metodológico adoptado —iterativo e incremental, por ramas de característica integradas en olas verificables— y luego lo complementa con la planificación temporal mediante el Método de la Ruta Crítica (CPM) sobre una red Actividad-en-Nodo: la Estructura de Desglose del Trabajo (WBS), el análisis de tiempos (forward/backward pass, holguras), las dos rutas críticas paralelas, los hitos y la nivelación del recurso escaso. Cierra con comandos de solo lectura que permiten verificar el proceso sobre el historial del repositorio y una conclusión de la sección.

## 3.1.1 Enfoque metodológico adoptado

El desarrollo de DAMA siguió un enfoque **iterativo e incremental**, organizado en **ramas de
característica** (*feature branches*) que se integraban a la rama principal (`main`) al completar
cada incremento. Cada iteración entregó funcionalidad verificable y desplegable, y el producto
creció por capas sucesivas: primero el núcleo de la plataforma (autenticación, multitenancy),
luego los dominios de negocio (cursos, asistencia, pagos), y finalmente las olas de
endurecimiento (calidad estructural, seguridad OWASP y despliegue).

Este enfoque se evidencia en el historial del repositorio: el trabajo se entrega en olas
identificables por sus *merges* —por ejemplo `feature/class-groups`,
`feature/reports-charts-student-admin`, `feature/backend-structural-uniformity`,
`feature/health-readiness-and-outbox-arch-tests` y las tres olas
`feature/owasp-access-control`, `feature/owasp-resource-limits` y
`feature/owasp-auth-crypto-logging`— cada una con su incremento probado antes de integrarse.

La metodología responde al **qué** y al **cómo** de cada iteración. Para responder al **cuándo**
—el orden de las actividades, sus dependencias y qué tan ajustado está el calendario— se
complementa con una técnica de planificación de proyectos: el **Método de la Ruta Crítica
(CPM)**, que se desarrolla a continuación. Ambas son complementarias: la metodología organiza el
trabajo dentro de cada iteración; el CPM ordena y prioriza el conjunto de actividades del
proyecto.

## 3.1.2 Planificación con el Método de la Ruta Crítica (CPM)

Siguiendo el marco de gestión de proyectos de **Lockyer y Gordon** (*Project Management and
Project Network Techniques*), se modela el desarrollo de DAMA como una **red de actividades**
bajo el sistema **Actividad-en-Nodo (AoN)**, estándar predominante en la industria del software
por su capacidad de gestionar dependencias múltiples. La unidad de duración es la **semana**.

### a) Estructura de Desglose del Trabajo (WBS)

El producto se descompone jerárquicamente en paquetes de trabajo manejables, cada uno con una
responsabilidad clara (*Task Owner*). Las actividades se agrupan en las tres fases clásicas
—Concepción, Desarrollo y Realización— más una fase de Despliegue:

```
DAMA — Plataforma SaaS para academias de baile
├── 1. Concepción
│   ├── A. Estudio de viabilidad y definición de alcance        [Líder de proyecto]
│   └── B. Especificación de requisitos (SRS) y elección de stack[Líder + Arquitecto]
├── 2. Desarrollo (diseño y especificación)
│   ├── C. Diseño de arquitectura y contratos gRPC              [Arquitecto]
│   └── D. Diseño de datos y modelo de dominio                  [Arquitecto + DBA]
├── 3. Realización (codificación y pruebas)
│   ├── E. Backend Auth (JWT/RSA, multitenancy, suscripciones)  [Dev backend senior]
│   ├── F. Backend CourseManagement (clases y grupos, gRPC srv) [Dev backend senior]
│   ├── G. Backend Attendance (cliente gRPC, asistencia)        [Dev backend senior]
│   ├── H. Backend Payment (4 ledgers, Todotix, gRPC cliente)   [Dev backend senior]
│   ├── I. Backend Credentials (claims)                         [Dev backend]
│   ├── J. Mensajería Outbox + RabbitMQ (transversal)           [Dev backend senior]
│   ├── K. Frontend Angular 21 (SPA, roles, dashboards)         [Dev frontend]
│   └── L. Infraestructura (Docker, gateway nginx, TLS, seeding)[DevOps]
└── 4. Endurecimiento y despliegue
    ├── M. Pruebas, uniformidad estructural y health checks     [QA + equipo]
    ├── N. Endurecimiento OWASP (olas 1–3), ISO y documentación [Dev backend senior + QA]
    └── O. Despliegue en producción (Dokploy, Cloudflare)       [DevOps]
```

### b) Análisis de tiempo (Forward Pass / Backward Pass)

Para cada actividad se calculan el inicio y fin más tempranos (**EST/EFT**, *forward pass*), el
inicio y fin más tardíos (**LST/LFT**, *backward pass*) y la **Holgura Total** (*Total Float* =
LST − EST). Las dependencias son de tipo **Fin a Inicio** (*Finish-to-Start*). El proyecto inicia
en la semana 0.

| Act | Actividad | Pred. | Dur. | EST | EFT | LST | LFT | Holgura | Crítica |
|-----|-----------|-------|:----:|:---:|:---:|:---:|:---:|:-------:|:-------:|
| A | Viabilidad y alcance | — | 2 | 0 | 2 | 0 | 2 | **0** | ✔ |
| B | Requisitos (SRS) + stack | A | 3 | 2 | 5 | 2 | 5 | **0** | ✔ |
| C | Arquitectura + contratos gRPC | B | 3 | 5 | 8 | 5 | 8 | **0** | ✔ |
| D | Diseño de datos y dominio | B | 2 | 5 | 7 | 6 | 8 | 1 | |
| E | Backend Auth | C, D | 4 | 8 | 12 | 8 | 12 | **0** | ✔ |
| F | Backend CourseManagement | E | 3 | 12 | 15 | 12 | 15 | **0** | ✔ |
| G | Backend Attendance | F, J | 3 | 15 | 18 | 15 | 18 | **0** | ✔ |
| H | Backend Payment | E, J | 4 | 12 | 16 | 14 | 18 | 2 | |
| I | Backend Credentials | E | 1 | 12 | 13 | 17 | 18 | 5 | |
| J | Mensajería Outbox + RabbitMQ | C | 2 | 8 | 10 | 12 | 14 | 4 | |
| K | Frontend Angular 21 | E | 6 | 12 | 18 | 12 | 18 | **0** | ✔ |
| L | Infraestructura | C | 3 | 8 | 11 | 15 | 18 | 7 | |
| M | Pruebas + uniformidad + health | F, G, H, I, K, L | 3 | 18 | 21 | 18 | 21 | **0** | ✔ |
| N | OWASP + ISO + documentación | M | 3 | 21 | 24 | 21 | 24 | **0** | ✔ |
| O | Despliegue en producción | N, L | 2 | 24 | 26 | 24 | 26 | **0** | ✔ |

**Duración total del proyecto (TPT):** **26 semanas**.

### c) Ruta crítica

Las actividades con **holgura total = 0** forman la ruta crítica. En DAMA existen **dos cadenas
críticas paralelas** que convergen en las pruebas (M), porque la pista del frontend (K, 6
semanas) tiene exactamente la misma duración que la cadena de backend
CourseManagement → Attendance (F + G = 3 + 3 = 6 semanas):

- **Ruta crítica 1 (backend):** A → B → C → E → F → G → M → N → O
- **Ruta crítica 2 (frontend):** A → B → C → E → K → M → N → O

Ambas suman **26 semanas**. Cualquier retraso en una actividad crítica retrasa, semana a semana,
la fecha de entrega del proyecto. La existencia de dos rutas críticas implica que el frontend y la
integración CourseManagement↔Attendance deben vigilarse con la **misma prioridad**.

### d) Hitos (Milestones)

Se definen hitos con aprobación, especialmente los que requieren visto bueno antes de pasar de
fase:

| Hito | Momento | Condición de aprobación |
|------|---------|--------------------------|
| **H1 — Alcance y requisitos aprobados** | Fin de B (sem. 5) | Aprobación ejecutiva antes de iniciar Desarrollo. |
| **H2 — Línea base de arquitectura** | Fin de C (sem. 8) | Arquitectura y contratos gRPC congelados. |
| **H3 — Producto funcional integrado** | Fin de M (sem. 21) | Todos los dominios integrados y probados. |
| **H4 — Listo para producción** | Fin de N (sem. 24) | Endurecimiento OWASP/ISO completado. |
| **H5 — Desplegado en producción** | Fin de O (sem. 26) | Operativo en Dokploy tras Cloudflare. |

### e) Restricción de recursos y solapamiento

Los **desarrolladores backend senior son un recurso limitado** (recomendación de Lockyer & Gordon
sobre agregación de recursos). Las actividades E, F, G y H compiten por ese recurso. Como **H
(Payment) tiene 2 semanas de holgura**, puede desplazarse dentro de esa holgura para nivelar la
carga (*resource aggregation/leveling*) **sin extender la duración total**, evitando sobrecargar
al equipo senior durante las semanas 12–15.

Respecto a las dependencias, aunque el análisis modela la actividad de pruebas (M) como Fin a
Inicio, en la práctica el **testeo unitario se solapó con el desarrollo** de cada servicio
(*lag-start* negativo): las suites NUnit y del frontend crecieron en paralelo al código, no
después. El modelo conservador (FS) se mantiene para el cálculo de la ruta crítica.

### f) Efecto de retrasar una actividad no crítica

Si una actividad con **holgura positiva** se retrasa **dentro** de su holgura, la fecha de entrega
no cambia. Si se retrasa **más allá** de su holgura, deja de ser no crítica y empuja el TPT.
Ejemplos en DAMA:

- **D (Diseño de datos), holgura 1:** un retraso de 1 semana es absorbible; un retraso de 2
  semanas atrasa el inicio de E (Auth) y, por estar E en la ruta crítica, retrasa **todo el
  proyecto** una semana.
- **L (Infraestructura), holgura 7:** la más holgada; puede retrasarse hasta 7 semanas sin afectar
  la entrega, lo que la hace candidata ideal para ceder su recurso (DevOps) cuando otra actividad
  lo necesite.
- **H (Payment), holgura 2:** absorbe pequeños deslices; pasados las 2 semanas se convierte en una
  segunda fuente de retraso del proyecto.

---

## 3.1.3 Comandos de demostración

Los siguientes comandos (solo lectura) permiten al lector **verificar sobre el repositorio** el
proceso iterativo-incremental descrito. Se ejecutan desde la raíz del repositorio.

**Visualizar la entrega por olas (grafo de ramas e integraciones):**

```bash
git log --oneline --graph --all
```

**Listar las olas de integración (merges) en orden cronológico:**

```bash
git log --merges --reverse --pretty=format:'%ad | %s' --date=short
```

**Reconstruir la línea de tiempo del proyecto (insumo para estimar duraciones reales):**

```bash
git log --reverse --pretty=format:'%ad | %h | %s' --date=short
```

**Confirmar el rango temporal y el volumen de trabajo:**

```bash
git log --pretty=format:'%ad' --date=short | sort | sed -n '1p;$p'   # primera y última fecha
git rev-list --count HEAD                                            # total de commits
```

**Evidenciar una ola concreta (ejemplo: endurecimiento de seguridad OWASP):**

```bash
git log --oneline --grep='OWASP'
```

> Nota de honestidad metodológica: el historial consolidado de `git` documenta de forma datable
> las olas de **Realización y Endurecimiento** (actividades J–O). El desarrollo inicial por
> servicio (actividades E–I) se consolidó en el *commit* inicial del monorepo; sus duraciones en
> la tabla CPM son **estimaciones realistas** reconstruidas a partir de la estructura del
> repositorio, no cifras optimistas (recomendación de Lockyer & Gordon).

## 3.1.4 Conclusión de la sección

El proyecto DAMA se desarrolló de forma iterativa-incremental y se planificó con CPM sobre una red
AoN de 15 actividades. La duración total estimada es de 26 semanas, con **dos rutas críticas
paralelas** (backend de cursos/asistencia y frontend) que convergen en la fase de pruebas. La
identificación de holguras (D=1, H=2, J=4, I=5, L=7) permitió nivelar el uso del recurso escaso
—los desarrolladores backend senior— sin comprometer la fecha de entrega.
