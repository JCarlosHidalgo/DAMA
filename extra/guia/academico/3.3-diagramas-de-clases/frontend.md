# 3.3.3.N Diagramado del frontend (Angular 21 / Compodoc)

> Contraparte de los archivos de backend, adaptada al frontend. Sigue el mismo enfoque a)/b) de la
> [plantilla](_plantilla.md), pero la herramienta es **Compodoc** (no Doxygen), porque es el
> generador de documentación del stack Angular. El índice `N` se asigna al ensamblar el capítulo
> (después de los cinco servicios de backend).
>
> **Decisiones de adaptación (acordadas):**
> - **Fuente:** Compodoc — grafo de dependencias, grafo por componente (entradas/salidas, *providers*
>   y componentes hijos de la plantilla) y la ficha por clase.
> - **Granularidad (parte b):** los **bloques lógicos** (servicios, guardias, interceptor, *pipes*,
>   directivas, estrategias y capa de lógica) se documentan **clase por clase**; los **58 componentes**
>   se agrupan y diagraman **por *feature*** (página), no uno por uno.
> - **Jerarquía gráfica (parte a):** se usa el **grafo de dependencias de Compodoc** tal cual.
> - **Herencia/colaboración:** «realización» = la interfaz/tipo de función que el bloque implementa
>   (`CanActivateFn`, `HttpInterceptorFn`, `PipeTransform`, `TokenStorage`, …); «colaboración» = las
>   dependencias **inyectadas**, los **componentes hijos** de la plantilla y el uso de su **archivo de
>   capa de lógica**, según el grafo de componente de Compodoc.
>
> **Generar las figuras:** desde `apps/Frontend`,
> `npx @compodoc/compodoc -p ../../infrastructure/docs/compodoc/tsconfig.doc.json -d compodoc-out`
> (produce el grafo de dependencias, los grafos por componente y las fichas por clase).

---

## a) Jerarquía gráfica (grafo de dependencias de Compodoc)

El frontend es una **SPA Angular 21 totalmente *standalone*** (sin `NgModule`): la organización no se
expresa con módulos de Angular, sino con la **estructura de carpetas** que Compodoc refleja en su
**grafo de dependencias**. Tres áreas de primer nivel ordenan el código:

- `core/` — la infraestructura de aplicación: clientes de la API (`core/api/`), autenticación y sesión
  (`core/auth/`), servicios transversales (`core/services/`), estrategia de precarga de rutas
  (`core/router/`) y estrategias de dominio (`core/strategies/`).
- `shared/` — piezas reutilizables: componentes (`shared/components/`), *pipes* (`shared/pipes/`) y
  directivas (`shared/directives/`).
- `pages/` — las pantallas por **feature/rol**: `login/` y `dashboard/` ramificado en `admin/`,
  `client/`, `student/` y `teacher/`.

Sobre cada componente de página se aplica el **patrón de capa de lógica**: el componente queda
«humilde» (orquesta vista y eventos) y delega la lógica pura en un archivo hermano `*.logic.ts` de
**funciones puras** (sin estado ni inyección), lo que sostiene la testabilidad (los `*.logic.ts` se
prueban al 100 %).

Títulos de figura (del grafo de dependencias de Compodoc) y la función de cada grupo:

> **Figura: Grafo de dependencias global del frontend (Compodoc) — áreas `core`, `shared` y `pages`**

Muestra la dependencia general: las páginas dependen de `shared` y de `core`; `core` no depende de
`pages`. Es la vista de más alto nivel de la arquitectura del cliente.

> **Figura: Grafo de dependencias del área `core` (clientes de API, autenticación, servicios, estrategias)**

Función: concentra todo lo que no es pantalla —comunicación con el *gateway*, manejo de sesión y
servicios transversales—, de modo que las páginas no hablen directamente con la red ni con el
almacenamiento.

> **Figura: Grafo de dependencias del área `shared` (componentes, *pipes*, directivas)**

Función: provee los bloques de interfaz reutilizables (tabla adaptable, selección de grupo, gráficos,
*skeletons*, *pipes* de formato) que las páginas componen.

> **Figura: Grafo de componentes de la *feature* `login`**

Función: la pantalla de acceso; punto de entrada no autenticado.

> **Figura: Grafo de componentes de la *feature* `dashboard/admin` (academias, planes de suscripción, analítica)**

Función: las pantallas del operador global (Admin) para gestionar academias, planes y ver la
analítica de ingresos.

> **Figura: Grafo de componentes de la *feature* `dashboard/client` (usuarios, cursos, grupos, horario, plantillas de deuda, configuración, recarga, suscripción, resumen)**

Función: las pantallas del gestor de academia (Client), el conjunto más amplio de la aplicación.

> **Figura: Grafo de componentes de la *feature* `dashboard/student` (horario, marcar asistencia, historial, estado de deuda, pago de clases, resumen)**

Función: las pantallas del estudiante, incluida la de marcado de asistencia por código QR.

> **Figura: Grafo de componentes de la *feature* `dashboard/teacher` (horario y toma de asistencia)**

Función: las pantallas del profesor para ver su horario y registrar asistencia.

---

## b) Diagramas de herencia/realización y colaboración

Una entrada por **bloque lógico** implementado. Las clases/interfaces/funciones **externas** (Angular,
Angular Material, RxJS, etc.) se **referencian** desde las viñetas, sin entrada propia. Los **58
componentes** no se enumeran uno por uno: se documentan por *feature* mediante el grafo de componente
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
   - No recibe dependencias inyectadas (lógica de decodificación autónoma); lo consume `AuthService`.

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
    - Inyecta el servicio implementado `AuthService` (lee el rol) y el servicio externo `Router`.

12. **`subscriptionGuard`** — bloquea el acceso cuando la suscripción de la academia no está vigente.

    > **Figura: Diagrama de herencia/realización para `subscriptionGuard`**
    - Realiza el tipo de función externo `CanActivateFn`.

    > **Figura: Diagrama de colaboración para `subscriptionGuard`**
    - Inyecta el servicio implementado `AuthService` (estado de la suscripción) y el servicio externo
      `Router`.

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
    - Inyecta el servicio externo `MatDialog` (Angular Material).

16. **`HttpErrorMapper`** — traduce los errores HTTP a mensajes de usuario coherentes.

    > **Figura: Diagrama de colaboración para `HttpErrorMapper`**
    - No recibe dependencias inyectadas; lo consume `NotificationService` y se apoya en la capa de
      lógica `http-error-mapper.logic.ts` (entrada 30).

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

### Estrategias de dominio (`core/strategies/`)

20. **`ClassKindStrategy`** — contrato que unifica el tratamiento de los dos tipos de clase
    (recurrente y puntual).

    > **Figura: Diagrama de herencia para `ClassKindStrategy`**
    - Es una interfaz **raíz**; la realizan `ScheduledClassStrategy` y `UniqueClassStrategy`.

    > **Figura: Diagrama de colaboración para `ClassKindStrategy`**
    - Sus operaciones usan los tipos implementados `ClassFormPayload` y `AttendanceTarget` (modelos del
      mismo archivo de estrategia).

21. **`ScheduledClassStrategy`** — estrategia para las clases recurrentes.

    > **Figura: Diagrama de herencia para `ScheduledClassStrategy`**
    - Realiza la interfaz implementada `ClassKindStrategy`.

    > **Figura: Diagrama de colaboración para `ScheduledClassStrategy`**
    - Inyecta los servicios implementados `CourseApi`, `AttendanceApi` y `AttendanceRealtimeService`.

22. **`UniqueClassStrategy`** — estrategia para las clases puntuales.

    > **Figura: Diagrama de herencia para `UniqueClassStrategy`**
    - Realiza la interfaz implementada `ClassKindStrategy`.

    > **Figura: Diagrama de colaboración para `UniqueClassStrategy`**
    - Inyecta los servicios implementados `CourseApi`, `AttendanceApi` y `AttendanceRealtimeService`.

23. **`ClassKindStrategies`** — registro que selecciona la estrategia según el tipo de clase.

    > **Figura: Diagrama de colaboración para `ClassKindStrategies`**
    - Inyecta las clases implementadas `ScheduledClassStrategy` y `UniqueClassStrategy` y entrega la
      adecuada a quien la solicita.

### Pipes (`shared/pipes/`)

24. **`MoneyPipe`** — formatea importes monetarios para la vista.

    > **Figura: Diagrama de herencia para `MoneyPipe`**
    - Realiza la interfaz externa `PipeTransform` (de Angular).

    > **Figura: Diagrama de colaboración para `MoneyPipe`**
    - No recibe dependencias inyectadas (transformación pura).

25. **`TenantDatePipe`** — formatea fechas en la zona horaria de la academia.

    > **Figura: Diagrama de herencia para `TenantDatePipe`**
    - Realiza la interfaz externa `PipeTransform`.

    > **Figura: Diagrama de colaboración para `TenantDatePipe`**
    - Inyecta el servicio implementado `AuthService`, del que obtiene la zona horaria de la academia.

### Directivas (`shared/`)

26. **`NoPasswordManager`** — directiva que desalienta la intervención de gestores de contraseñas en
    campos sensibles.

    > **Figura: Diagrama de herencia para `NoPasswordManager`**
    - Es una directiva de atributo (decorada con `@Directive`); no realiza interfaces de ciclo de vida.

    > **Figura: Diagrama de colaboración para `NoPasswordManager`**
    - No recibe dependencias inyectadas; actúa sobre el elemento anfitrión al que se aplica.

27. **`TableCellDirective`** (`shared/components/responsive-table/`, selector `[appTableCell]`) —
    marca las celdas para que la tabla adaptable las apile en pantallas pequeñas.

    > **Figura: Diagrama de colaboración para `TableCellDirective`**
    - Coopera con el componente de tabla adaptable de su mismo archivo, que la consume para el
      comportamiento responsive.

### Capa de lógica (`*.logic.ts`) — funciones puras

La capa de lógica son **22** módulos de **funciones puras** (sin estado, sin inyección, sin herencia):
no tienen diagrama de herencia ni de colaboración por dependencias; Compodoc las documenta en su
sección de **funciones**. Su «colaboración» es uniforme: **cada archivo es consumido por su componente
hermano** y opera sobre los **modelos** (`*.model.ts`) del dominio. Se enumeran agrupadas por *feature*;
para cada una se cita su figura de funciones de Compodoc.

> **Figura: Funciones de la capa de lógica (Compodoc) — núcleo y compartidos**
- `http-error-mapper.logic.ts` (mapeo de errores HTTP a mensajes), consumido por `HttpErrorMapper`.
- `shared/components/charts/chart-options.logic.ts` y `chart-tokens.logic.ts` (opciones y paleta de
  gráficos), consumidos por los componentes de gráficos.
- `shared/components/group-select/group-select.logic.ts` (claves de consulta y resolución de grupos),
  consumido por el componente de selección de grupo.

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Admin**
- `analytics.logic.ts` (transformación de series para gráficos de analítica), `subscription-plans.logic.ts`
  (etiquetas y carga útil de planes), `tenants.logic.ts` (resolución de alta/edición de academias).

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Client**
- `configuration.logic.ts`, `courses.logic.ts`, `debt-templates.logic.ts`, `recharge.logic.ts`,
  `schedule.logic.ts`, `subscription.logic.ts` (validaciones, mensajes de confirmación y resolución de
  formularios de cada pantalla del Client).

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Student**
- `attendance-history.logic.ts`, `debt-status.logic.ts`, `mark-attendance.logic.ts`,
  `pay-classes.logic.ts`, `schedule.logic.ts`, `summary.logic.ts`,
  `schedule/confirm-attendance-dialog.logic.ts` (lectura del código QR, clasificación de errores,
  series de resumen y validaciones del Student).

> **Figura: Funciones de la capa de lógica (Compodoc) — *feature* Teacher**
- `teacher/schedule/schedule.logic.ts` y `teacher/schedule/attendance-qr-dialog.logic.ts` (subtítulos,
  generación del código QR y altas de asistencia del Teacher).

### Componentes por *feature*

Los **58 componentes** se documentan **por *feature***, no uno por uno. Para cada *feature*, el **grafo
de componente de Compodoc** muestra, por componente: sus **entradas/salidas**, sus **dependencias
inyectadas** (servicios y estrategias de las entradas 1–23) y sus **componentes hijos** de la plantilla
(de `shared/components/`). La regla de colaboración es uniforme: **el componente inyecta los servicios
de `core/` y delega la lógica pura en su archivo `*.logic.ts` hermano**.

> **Figura: Grafo de componentes — *feature* `login`**
> **Figura: Grafo de componentes — *feature* `dashboard/admin`**
> **Figura: Grafo de componentes — *feature* `dashboard/client`**
> **Figura: Grafo de componentes — *feature* `dashboard/student`**
> **Figura: Grafo de componentes — *feature* `dashboard/teacher`**

Para cada *feature*, la función es la descrita en la parte a); el grafo por componente detalla las
asociaciones concretas (qué cliente de API y qué estrategia inyecta cada pantalla, y qué componentes
compartidos compone).

---

## Comandos de demostración

```bash
# Bloques lógicos del frontend (lo que Compodoc documenta clase por clase)
ls apps/Frontend/src/app/core/api apps/Frontend/src/app/core/auth apps/Frontend/src/app/core/services
find apps/Frontend/src/app -name '*.logic.ts' | sort     # capa de lógica (funciones puras)
grep -rlE "@Pipe|@Directive" apps/Frontend/src/app --include=*.ts | grep -v spec

# Generar el grafo de dependencias, los grafos por componente y las fichas por clase
cd apps/Frontend && npx @compodoc/compodoc -p ../../infrastructure/docs/compodoc/tsconfig.doc.json -d compodoc-out
#   abrir compodoc-out/index.html
```
