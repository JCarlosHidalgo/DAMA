# 3.3.3.11 Diagramado del frontend (Angular 21 / Compodoc + dependency-cruiser)

> Contraparte de los archivos de backend, adaptada al frontend. Sigue el mismo enfoque a)/b) de la
> [plantilla](_plantilla.md), pero la herramienta principal es **Compodoc** (documentación de clases
> y componentes) complementada con **dependency-cruiser** (cuatro grafos de dependencias integrados
> en Compodoc como documentación adicional). Es la **sección final** del capítulo, después de los
> cinco servicios de backend (3.3.3.6 a 3.3.3.10).
>
> **Decisiones de adaptación (acordadas):**
> - **Fuente de la jerarquía gráfica (parte a):** cuatro grafos generados por **dependency-cruiser**
>   y servidos dentro de Compodoc (sección «Dependency Graphs»): `global.svg`, `core.svg`,
>   `shared.svg` y `pages.svg`. Cada grafo muestra los módulos TypeScript reales (archivos `.ts`)
>   como nodos y sus importaciones como aristas dirigidas.
> - **Granularidad (parte b):** los **bloques lógicos** (servicios, guardias, interceptor, *pipes*,
>   directivas, estrategias y capa de lógica) se documentan **clase por clase**; los componentes se
>   agrupan y documentan **por *feature*** (página) a través del grafo de componente de Compodoc.
> - **Herencia/colaboración:** «realización» = la interfaz/tipo de función que el bloque implementa
>   (`CanActivateFn`, `HttpInterceptorFn`, `PipeTransform`, `TokenStorage`, …); «colaboración» = las
>   dependencias **inyectadas**, los **componentes hijos** de la plantilla y el uso de su **archivo de
>   capa de lógica**, según el grafo de componente de Compodoc.
>
> **Generar las figuras:** reconstruir el stack de documentación con
> `docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.docs.yaml up --build`
> y abrir `http://localhost:8003/frontend/`. Los cuatro grafos de dependencias están en
> «Additional documentation → Dependency Graphs»; los grafos por componente y las fichas por clase,
> en las secciones propias de Compodoc.

---

## a) Jerarquía gráfica (grafos de dependency-cruiser)

El frontend es una **SPA Angular 21 totalmente *standalone*** (sin `NgModule`): la organización no se
expresa con módulos de Angular, sino con la **estructura de carpetas**. Tres áreas de primer nivel
ordenan el código y son la base de los cuatro grafos:

- `core/` — infraestructura de aplicación: clientes de API (`api/`), autenticación y sesión
  (`auth/`), servicios transversales (`services/`), utilidades de dominio (`utils/`), estrategia de
  precarga de rutas (`router/`) y estrategias de dominio (`strategies/`).
- `shared/` — sistema de diseño reutilizable: componentes (`design/components/`), recetas de estilos
  (`design/`), *pipes* (`pipes/`), directivas (`directives/`) y primitivas de formulario (`forms/`).
- `pages/` — pantallas por **feature/rol**: `login/` y `dashboard/` ramificado en `admin/`,
  `client/`, `student/` y `teacher/`; cada página aplica el **patrón de capa de lógica** (`*.logic.ts`
  hermano del componente).

### Grafo global

> **Figura: Grafo de dependencias global del frontend (dependency-cruiser) — áreas `core`, `shared` y `pages`**

Colapsa todo el árbol a tres nodos: `src/app/core`, `src/app/shared` y `src/app/pages`. Las aristas
muestran la única dirección válida de dependencia: las páginas dependen de `core` y de `shared`;
`core` y `shared` no dependen de `pages`. Es la vista de más alto nivel de la arquitectura del
cliente y el contrato estructural que el resto de grafos desglosa.

### Grafo del área `core`

> **Figura: Grafo de dependencias del área `core` (dependency-cruiser) — archivos individuales**

Muestra los ~40 archivos `.ts` de `core/` como nodos individuales con sus importaciones internas.
Los patrones visibles en el grafo:

- **Clúster de autenticación:** `auth-service.ts` es el nodo más conectado del grafo; de él dependen
  directamente `auth-guard.ts`, `role-guard.ts`, `subscription-guard.ts`,
  `subscription-access-guard.ts`, `auth-interceptor.ts` y `role-aware-preload.ts`. El propio
  `auth-service.ts` depende de `token-storage.ts`, `token-decoder.ts` y `jwt.model.ts`.
- **Cadena de servicios:** `notification-service.ts` → `http-error-mapper.ts` →
  `http-error-mapper.logic.ts`. Los archivos de modelos (`models/`) son hojas sin dependencias
  internas.
- **Utilidades de dominio:** `confirmation-dialog.ts` y `attendance-marked-dialog.ts` dependen de
  sus respectivos archivos `.variants.ts`; `qr-debt-polling.ts` y `qr-debt-outcome.ts` dependen de
  los modelos de `models/`.
- **Estrategias:** `class-kind.strategy.ts` depende de `api/` y de `models/`.
- Los archivos de `api/` (`auth.api.ts`, `course.api.ts`, `attendance.api.ts`, `payment.api.ts`,
  `credentials.api.ts`) son nodos hoja: no tienen importaciones internas dentro de `core/`.

### Grafo del área `shared`

> **Figura: Grafo de dependencias del área `shared` (dependency-cruiser) — archivos individuales**

Muestra todos los archivos `.ts` de `shared/` como nodos individuales. La estructura observada:

- **Sistema de diseño (`design/components/`):** cada componente está formado por un par `*.ts` +
  `*.variants.ts` con acoplamiento exclusivamente entre sí. La única dependencia interna entre
  componentes distintos ocurre dentro de `charts/`: `chart-options.logic.ts` →
  `chart-tokens.logic.ts`. El resto de componentes (`calendar`, `camera-scanner`, `course-color-chip`,
  `empty-state`, `error-state`, `group-select`, `icon`, `loading-skeleton`, `page-head`, `paginator`,
  `qr-card`, `responsive-table`, `stat-card`, `tag`, `theme-toggle`) son pares aislados.
- **`design/index.ts` y `design/recipes.ts`** aparecen como nodos individuales sin dependencias
  internas hacia otros componentes del área.
- **`pipes/`, `directives/` y `forms/`** son conjuntos de archivos sin dependencias cruzadas entre
  ellos.

### Grafo del área `pages`

> **Figura: Grafo de dependencias del área `pages` (dependency-cruiser) — archivos individuales**

Muestra todos los archivos `.ts` de `pages/` como nodos individuales. El patrón es uniforme en
todas las *features*: cada pantalla tiene un archivo de componente `*.ts`, un archivo de estilos
`*.variants.ts` y, en la mayoría de los casos, un archivo de capa de lógica `*.logic.ts`; algunas
páginas añaden `*.validators.ts`. Los archivos de enrutamiento y componentes auxiliares
(`dashboard.ts`, `dashboard.routes.ts`, `dashboard/shared/placeholder.ts`,
`dashboard/shared/schedule-router.ts`, `dashboard/shared/summary-router.ts`) aparecen como nodos
individuales conectados a los componentes de página que los referencian. Las dependencias de `pages/`
hacia `core/` y `shared/` no aparecen en este grafo porque el análisis se limita al área `pages/`;
esas aristas se ven en el grafo global.

---

## b) Diagramas de herencia/realización y colaboración

Una entrada por **bloque lógico** implementado. Las clases/interfaces/funciones **externas** (Angular,
Angular Material, RxJS, etc.) se **referencian** desde las viñetas, sin entrada propia. Los
componentes no se enumeran uno por uno: se documentan por *feature* mediante el grafo de componente
de Compodoc (cierre de esta sección).

### Clientes de la API (`core/api/`)

1. **`AuthApi`** — cliente HTTP de los puntos de acceso de autenticación e identidad.

   > **Figura: Diagrama de colaboración para `AuthApi`**
   - Recibe por inyección de dependencias el cliente externo `HttpClient` (de Angular), con el que
     llama al *gateway*.
   - (Sin realización de interfaz: es un servicio inyectable simple.)

2. **`CourseApi`** — cliente HTTP de la oferta académica (cursos, clases, grupos, horarios).

   > **Figura: Diagrama de colaboración para `CourseApi`**
   - Recibe por inyección el cliente externo `HttpClient`.

3. **`AttendanceApi`** — cliente HTTP de la asistencia y las clases restantes.

   > **Figura: Diagrama de colaboración para `AttendanceApi`**
   - Recibe por inyección el cliente externo `HttpClient`.

4. **`PaymentApi`** — cliente HTTP de cobros, plantillas de deuda y pagos QR.

   > **Figura: Diagrama de colaboración para `PaymentApi`**
   - Recibe por inyección el cliente externo `HttpClient`.

5. **`CredentialsApi`** — cliente HTTP del servicio de reflexión de *claims*.

   > **Figura: Diagrama de colaboración para `CredentialsApi`**
   - Recibe por inyección el cliente externo `HttpClient`.

6. **`AttendanceRealtimeService`** — cliente de tiempo real (canal de notificaciones de asistencia).

   > **Figura: Diagrama de colaboración para `AttendanceRealtimeService`**
   - Recibe por inyección el servicio implementado `AuthService`, del que obtiene el token para
     autenticar la conexión en tiempo real.

### Autenticación y sesión (`core/auth/`)

7. **`AuthService`** — fachada de sesión: inicio/cierre de sesión, renovación y estado del usuario.

   > **Figura: Diagrama de colaboración para `AuthService`**
   - Recibe por inyección el cliente externo `HttpClient`, la clase implementada
     `SessionStorageTokenStorage` (almacenamiento del token) y la clase implementada `TokenDecoder`
     (lectura del contenido del token).

8. **`TokenDecoder`** — decodifica el token de sesión para leer sus *claims* en el cliente.

   > **Figura: Diagrama de colaboración para `TokenDecoder`**
   - Depende del modelo implementado `JwtClaims` (definido en `jwt.model.ts`); no recibe dependencias
     inyectadas. Lo consume `AuthService`.

9. **`SessionStorageTokenStorage`** — persistencia del token en el almacenamiento de sesión del
   navegador, de forma segura para renderizado en servidor.

   > **Figura: Diagrama de herencia para `SessionStorageTokenStorage`**
   - Realiza (implementa) la interfaz implementada `TokenStorage`, que abstrae el almacenamiento del
     token.

   > **Figura: Diagrama de colaboración para `SessionStorageTokenStorage`**
   - No recibe dependencias inyectadas; la consume `AuthService` a través de la interfaz `TokenStorage`.

### Guardias de ruta (`core/auth/`) — funciones `CanActivateFn`

10. **`authGuard`** — protege las rutas que exigen sesión iniciada.

    > **Figura: Diagrama de herencia/realización para `authGuard`**
    - Realiza el tipo de función externo `CanActivateFn` (de Angular Router).

    > **Figura: Diagrama de colaboración para `authGuard`**
    - Inyecta el servicio implementado `AuthService` (comprueba la sesión) y el servicio externo
      `Router` (redirige si no hay sesión).

11. **`roleGuard`** — restringe una ruta a determinados roles.

    > **Figura: Diagrama de herencia/realización para `roleGuard`**
    - Realiza el tipo de función externo `CanActivateFn`.

    > **Figura: Diagrama de colaboración para `roleGuard`**
    - Inyecta el servicio implementado `AuthService` (lee el rol y los *claims* del modelo `JwtClaims`)
      y el servicio externo `Router`.

12. **`subscriptionGuard`** — bloquea el acceso cuando la suscripción de la academia no está vigente.

    > **Figura: Diagrama de herencia/realización para `subscriptionGuard`**
    - Realiza el tipo de función externo `CanActivateFn`.

    > **Figura: Diagrama de colaboración para `subscriptionGuard`**
    - Inyecta el servicio implementado `AuthService` (estado de la suscripción, leído del modelo
      `JwtClaims`) y el servicio externo `Router`.

13. **`subscriptionAccessGuard`** — restringe funcionalidades según el nivel de suscripción contratado.

    > **Figura: Diagrama de herencia/realización para `subscriptionAccessGuard`**
    - Realiza el tipo de función externo `CanActivateFn`.

    > **Figura: Diagrama de colaboración para `subscriptionAccessGuard`**
    - Inyecta el servicio implementado `AuthService` (nivel de suscripción) y el servicio externo
      `Router`.

### Interceptor HTTP (`core/auth/`) — función `HttpInterceptorFn`

14. **`authInterceptor`** — añade el token a las peticiones salientes y reacciona a respuestas no
    autorizadas.

    > **Figura: Diagrama de herencia/realización para `authInterceptor`**
    - Realiza el tipo de función externo `HttpInterceptorFn` (de Angular).

    > **Figura: Diagrama de colaboración para `authInterceptor`**
    - Inyecta el servicio implementado `AuthService` (obtiene el token y gestiona la renovación) y el
      servicio externo `Router` (redirige al expirar la sesión).

### Servicios transversales (`core/services/`)

15. **`DialogService`** — abre diálogos modales reutilizables (confirmación, avisos).

    > **Figura: Diagrama de colaboración para `DialogService`**
    - Inyecta el servicio externo `MatDialog` (Angular Material) y usa las utilidades de diálogo de
      `core/utils/`.

16. **`HttpErrorMapper`** — traduce los errores HTTP a mensajes de usuario coherentes.

    > **Figura: Diagrama de colaboración para `HttpErrorMapper`**
    - Delega la lógica de mapeo en la capa de lógica `http-error-mapper.logic.ts` (entrada 30); no
      recibe dependencias inyectadas.

17. **`NotificationService`** — muestra notificaciones (éxito/error) de forma unificada.

    > **Figura: Diagrama de colaboración para `NotificationService`**
    - Inyecta el servicio externo `MatSnackBar` (Angular Material) y el servicio implementado
      `HttpErrorMapper`.

18. **`ThemeService`** — gestiona el tema claro/oscuro de la interfaz.

    > **Figura: Diagrama de colaboración para `ThemeService`**
    - No recibe dependencias inyectadas (opera sobre el documento y el almacenamiento del navegador).

### Precarga de rutas (`core/router/`)

19. **`RoleAwarePreloadStrategy`** — decide qué rutas precargar según el rol del usuario.

    > **Figura: Diagrama de herencia para `RoleAwarePreloadStrategy`**
    - Realiza (implementa) la interfaz externa `PreloadingStrategy` (de Angular Router).

    > **Figura: Diagrama de colaboración para `RoleAwarePreloadStrategy`**
    - Inyecta el servicio implementado `AuthService` (lee el rol para decidir la precarga).

20. **`DefaultRoute`** — determina la ruta de inicio según el rol autenticado.

    > **Figura: Diagrama de colaboración para `DefaultRoute`**
    - Inyecta el servicio implementado `AuthService` para leer el rol del usuario.

### Estrategias de dominio (`core/strategies/`)

21. **`ClassKindStrategy`** — contrato que unifica el tratamiento de los dos tipos de clase
    (recurrente y puntual).

    > **Figura: Diagrama de herencia para `ClassKindStrategy`**
    - Es una interfaz **raíz**; la realizan `ScheduledClassStrategy` y `UniqueClassStrategy`.

    > **Figura: Diagrama de colaboración para `ClassKindStrategy`**
    - Sus operaciones usan los tipos implementados del dominio definidos en `core/models/` y acceden
      a `core/api/`.

22. **`ScheduledClassStrategy`** — estrategia para las clases recurrentes.

    > **Figura: Diagrama de herencia para `ScheduledClassStrategy`**
    - Realiza la interfaz implementada `ClassKindStrategy`.

    > **Figura: Diagrama de colaboración para `ScheduledClassStrategy`**
    - Inyecta los servicios implementados `CourseApi`, `AttendanceApi` y `AttendanceRealtimeService`.

23. **`UniqueClassStrategy`** — estrategia para las clases puntuales.

    > **Figura: Diagrama de herencia para `UniqueClassStrategy`**
    - Realiza la interfaz implementada `ClassKindStrategy`.

    > **Figura: Diagrama de colaboración para `UniqueClassStrategy`**
    - Inyecta los servicios implementados `CourseApi`, `AttendanceApi` y `AttendanceRealtimeService`.

24. **`ClassKindStrategies`** — registro que selecciona la estrategia según el tipo de clase.

    > **Figura: Diagrama de colaboración para `ClassKindStrategies`**
    - Inyecta las clases implementadas `ScheduledClassStrategy` y `UniqueClassStrategy` y entrega la
      adecuada a quien la solicita.

### *Pipes* (`shared/pipes/`)

25. **`MoneyPipe`** — formatea importes monetarios para la vista.

    > **Figura: Diagrama de herencia para `MoneyPipe`**
    - Realiza la interfaz externa `PipeTransform` (de Angular).

    > **Figura: Diagrama de colaboración para `MoneyPipe`**
    - No recibe dependencias inyectadas (transformación pura).

26. **`TenantDatePipe`** — formatea fechas en la zona horaria de la academia.

    > **Figura: Diagrama de herencia para `TenantDatePipe`**
    - Realiza la interfaz externa `PipeTransform`.

    > **Figura: Diagrama de colaboración para `TenantDatePipe`**
    - Inyecta el servicio implementado `AuthService`, del que obtiene la zona horaria de la academia.

### Directivas (`shared/directives/`)

27. **`NoPasswordManagerDirective`** — directiva que desalienta la intervención de gestores de
    contraseñas en campos sensibles.

    > **Figura: Diagrama de herencia para `NoPasswordManagerDirective`**
    - Es una directiva de atributo (decorada con `@Directive`); no realiza interfaces de ciclo de vida.

    > **Figura: Diagrama de colaboración para `NoPasswordManagerDirective`**
    - No recibe dependencias inyectadas; actúa sobre el elemento anfitrión al que se aplica.

### Capa de lógica (`*.logic.ts`) — funciones puras

La capa de lógica son **22** módulos de **funciones puras** (sin estado, sin inyección, sin herencia):
no tienen diagrama de herencia ni de colaboración por dependencias; Compodoc los documenta en su
sección de **funciones**. Su «colaboración» es uniforme: **cada archivo es consumido por su componente
o servicio hermano** y opera sobre los **modelos** (`*.model.ts`) del dominio. Se enumeran agrupadas
por área; para cada una se cita su figura de funciones de Compodoc.

> **Figura: Funciones de la capa de lógica (Compodoc) — `core` y `shared`**
- `core/services/http-error-mapper.logic.ts` — mapeo de errores HTTP a mensajes, consumido por
  `HttpErrorMapper`.
- `shared/design/components/charts/chart-tokens.logic.ts` — paleta de colores de los gráficos,
  consumido por `chart-options.logic.ts`.
- `shared/design/components/charts/chart-options.logic.ts` — construcción de opciones de Chart.js,
  consumido por los componentes de gráficos.
- `shared/design/components/group-select/group-select.logic.ts` — claves de consulta y resolución
  de grupos, consumido por el componente de selección de grupo.

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Admin**
- `analytics.logic.ts` (transformación de series para gráficos de analítica),
  `subscription-plans.logic.ts` (etiquetas y carga útil de planes),
  `tenants.logic.ts` (resolución de alta/edición de academias).

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Client**
- `configuration.logic.ts`, `courses.logic.ts`, `debt-templates.logic.ts`, `recharge.logic.ts`,
  `schedule.logic.ts`, `subscription.logic.ts` — validaciones, mensajes de confirmación y resolución
  de formularios de cada pantalla del Client.

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Student**
- `attendance-history.logic.ts`, `debt-status.logic.ts`, `mark-attendance.logic.ts`,
  `pay-classes.logic.ts`, `schedule.logic.ts`, `summary.logic.ts`,
  `schedule/confirm-attendance-dialog.logic.ts` — lectura del código QR, clasificación de errores,
  series de resumen y validaciones del Student.

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Teacher**
- `teacher/schedule/schedule.logic.ts` y `teacher/schedule/attendance-qr-dialog.logic.ts` —
  subtítulos, generación del código QR y altas de asistencia del Teacher.

### Componentes por *feature*

Los componentes se documentan **por *feature***, no uno por uno. Para cada *feature*, el **grafo de
componente de Compodoc** muestra, por componente: sus **entradas/salidas**, sus **dependencias
inyectadas** (servicios y estrategias de las entradas 1–24) y sus **componentes hijos** de la
plantilla (de `shared/design/components/`). La regla de colaboración es uniforme: **el componente
inyecta los servicios de `core/` y delega la lógica pura en su archivo `*.logic.ts` hermano**.

> **Figura: Grafo de componentes — *feature* `login`**
> **Figura: Grafo de componentes — *feature* `dashboard/admin`**
> **Figura: Grafo de componentes — *feature* `dashboard/client`**
> **Figura: Grafo de componentes — *feature* `dashboard/student`**
> **Figura: Grafo de componentes — *feature* `dashboard/teacher`**

Para cada *feature*, la función es la descrita en la parte a); el grafo por componente de Compodoc
detalla las asociaciones concretas (qué cliente de API y qué estrategia inyecta cada pantalla, y qué
componentes de `shared/design/components/` compone).

---

## Comandos de demostración

```bash
# Bloques lógicos del frontend (lo que Compodoc documenta clase por clase)
ls apps/Frontend/src/app/core/api apps/Frontend/src/app/core/auth apps/Frontend/src/app/core/services
find apps/Frontend/src/app -name '*.logic.ts' | sort     # capa de lógica (funciones puras)
grep -rlE "@Pipe|@Directive" apps/Frontend/src/app --include=*.ts | grep -v spec

# Generar los cuatro grafos de dependencias y la documentación Compodoc completa
docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.docs.yaml up --build
# Abrir http://localhost:8003/frontend/
# Los grafos de dependencias: Additional documentation → Dependency Graphs
```
