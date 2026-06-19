# Pruebas de carga con k6 — Diseño

> **Estado:** Aprobado en brainstorming (2026-06-18). Pendiente: plan de implementación.
> **Objetivo:** cerrar la "brecha honesta" de pruebas de carga declarada en
> `extra/guia/academico/5.1-plan-de-pruebas.md` §3.5.1.5, produciendo p95 / capacidad /
> uso de recursos reales de los backends bajo carga, con un reporte estático servible.

## 1. Problema y contexto

`5.1-plan-de-pruebas.md` deja **fuera de alcance por diseño** las pruebas de carga
(§3.5.1.1) y las declara como brecha honesta (§3.5.1.5): no existe una corrida
instrumentada (JMeter/k6) contra el stack levantado, por lo que **no hay p95, capacidad
de usuarios concurrentes ni uso de CPU/memoria medidos**. Este diseño introduce esa
corrida con **Grafana k6**, autocontenida en infraestructura y con reporte estático,
espejando el patrón ya existente del *containerized test runner*
(`infrastructure/compose.test.yaml` → Apache en `:8002`).

### Restricción decisiva: rate-limiting en el gateway

El api-gateway (nginx) aplica throttling **por IP de origen**
(`environments/api-gateway/nginx.conf`):

- `/api/auth/login` → `rate=5r/m`, `burst=2`.
- toda `/api/*` → `zone=api rate=30r/s`, `burst=60`.
- `/api/payment/...callback` → `rate=60r/m`.

k6 desde un contenedor es **una sola IP**, por lo que una prueba de carga *a través del
gateway* mediría el limitador (avalancha de 429), no la capacidad de los servicios. El
rate-limiting es deliberado (vive en nginx, no por servicio). **Decisión:** k6 apunta
**directo a los contenedores backend** por nombre en la red interna, evitando el gateway,
para medir capacidad real. La URL es configurable, de modo que apuntar al gateway (medir
el borde protegido) sigue siendo posible cambiando `*_BASE_URL` en `.env.k6`.

## 2. Decisiones tomadas

| Tema | Decisión |
|---|---|
| Ubicación | `infrastructure/environments-test/k6-testing/`, autocontenida, con `compose.yaml` y `.env.k6` propios, separada del flujo de unit testing. |
| Conexión al stack | k6 se une a la red **`dama_network`** del dev como `external`. No levanta el stack; lo requiere arriba. Requiere un cambio en `compose.dev.yaml` (ver §4.1). |
| Objetivo de medición | Capacidad de backends: `BASE_URL` directo a contenedores backend por nombre (evita el gateway). Parametrizable. |
| Escenarios | Recorridos **autenticados por servicio** (login → flujo representativo), con usuarios sembrados. |
| Reportes | **Web dashboard nativo de k6** (HTML autocontenido, `K6_WEB_DASHBOARD_EXPORT`) + `summary.json`, servidos por un **Apache propio** en `:8004`. |
| Servicios cubiertos | Auth, CourseManagement, Attendance, Payment. **Credentials excluido** (coherente con testing/cobertura). |

## 3. Estructura de archivos

```
infrastructure/environments-test/k6-testing/
├── compose.yaml            # 4 contenedores k6 (one-shot) + 1 Apache
├── .env.k6                 # variables propias (GITIGNORED — lleva credenciales dev)
├── .env.k6.example         # plantilla commiteable (placeholders)
├── scripts/
│   ├── lib/
│   │   ├── auth.js         # helper login(baseUrl, username, password) → JWT
│   │   └── config.js       # lee BASE_URLs, VUs, duración y umbrales desde __ENV
│   ├── auth.js
│   ├── course-management.js
│   ├── attendance.js
│   └── payment.js
├── report/
│   └── index.html          # landing estilada (reusa el patrón de environments-test/root/index.html)
└── README.md
```

## 4. Arquitectura del compose

Espeja `compose.test.yaml`: contenedores **one-shot** que escriben su reporte a un
volumen nombrado, y un Apache que los sirve estáticos tras
`service_completed_successfully`.

- **Imagen k6:** oficial `grafana/k6` (entrypoint `k6`; scripts montados read-only en
  `/scripts`; corre como usuario no-root → el directorio de reportes debe ser escribible).
  Sin Dockerfile propio.
- **Imagen Apache:** `httpd:2.4-alpine`.
- **Red:** bloque `networks:` con `dama_network` declarada `external: true`. Los 4
  servicios k6 se unen a esa red para resolver los contenedores backend por nombre
  (ver §4.1 para el cambio que crea esa red en el dev).
- **Volúmenes:** uno por servicio (`auth-load-results`, `course-management-load-results`,
  `attendance-load-results`, `payment-load-results`), montados en cada k6 en `/reports/<svc>/`
  y en el Apache en `/usr/local/apache2/htdocs/<svc>/` (read-only). El `report/index.html`
  se monta como índice raíz.
- **Puerto host:** Apache en `8004:80` (8002 = test runner, 8003 = docs, 8004 libre).
- **depends_on:** Apache espera `service_completed_successfully` de los 4 k6.

Cada servicio k6 (ejemplo Auth):

```yaml
  auth-load:
    image: grafana/k6
    user: root                         # grafana/k6 corre no-root; root para escribir el volumen
    networks: [dama_network]
    env_file: [.env.k6]                # inyecta URLs + credenciales al contenedor
    environment:
      K6_WEB_DASHBOARD: "true"
      K6_WEB_DASHBOARD_EXPORT: /reports/auth/index.html
    volumes:
      - ./scripts:/scripts:ro
      - auth-load-results:/reports/auth
    entrypoint: ["sh", "-c", "k6 run /scripts/auth.js || true"]  # || true: un fallo de umbral no debe romper el depends_on del Apache
```

### 4.1 Cambio en `compose.dev.yaml`: red `dama_network`

Hoy `compose.dev.yaml` no declara `networks:`; todos los servicios usan la red `default`
implícita (que Docker nombra `<proyecto>_default`, dependiente del directorio de invocación
→ inestable). Para que k6 pueda referenciarla como `external` con un nombre literal estable,
se **renombra la red default** del dev a `dama_network` con un bloque de tres líneas, sin
tocar la definición de cada servicio (todos ya usan `default`):

```yaml
networks:
  default:
    name: dama_network
```

- Es el cambio de **menor huella**: no añade `networks: [...]` a los ~14 servicios.
- El nombre `dama_network` queda literal (sin prefijo de proyecto), por lo que el compose de
  k6 lo referencia de forma determinista.
- No afecta a `compose.prod.yaml` (usa `dokploy-network` external, sin relación) ni a
  `compose.test.yaml` / `compose.docs.yaml` (flujos one-shot que no requieren esa red).
- Tras aplicarlo, una recreación del stack dev (`compose-down` + `compose-up`) materializa la
  red con el nuevo nombre; el TLS y la resolución por `container_name` siguen igual.

## 5. Escenarios

Helper compartido `lib/auth.js` expone `login(user)` que hace `POST {authBaseUrl}/api/auth/login`
(anónimo; las rutas del backend conservan el prefijo `api/auth` aun sin el gateway) y devuelve el
JWT para `Authorization: Bearer`. `lib/config.js` centraliza la lectura de `__ENV` (URLs, VUs,
duración, umbrales) con defaults conservadores.

**Patrón login-una-vez + reúso de token.** Cada script autentica **una sola vez** en `setup()`
(función de k6 que corre antes del test), devuelve el/los token(s) por rol, y el bucle de
iteraciones (`default`) **solo reutiliza** esos tokens para ejercitar las lecturas. Así el coste de
autenticación (hashing de contraseña, CPU-bound) queda **fuera de la ventana medida** y las métricas
de p95/throughput reflejan la capacidad de **lectura** de cada backend, no el login. La capacidad de
login se mediría, si se desea, en un escenario dedicado aparte.

| Script | `setup()` (login una vez) | Bucle (reúso de token) | Rol(es) |
|---|---|---|---|
| `auth.js` | admin | `GET /api/auth/tenants` | admin |
| `course-management.js` | client + teacher | `GET /course` · `GET /course/teacher/me` | Client / Teacher |
| `attendance.js` | student | `GET /attendance/scheduled/me` · `GET /remain/me` | Student |
| `payment.js` | client | `GET /payment/debt-template` · `GET /payment/summary` | Client |

Cada script define `options.scenarios` y `options.thresholds`:

- `http_req_failed: ['rate<0.01']`
- `http_req_duration: ['p(95)<500']` (umbral inicial, ajustable por `__ENV`)

VUs y duración por defecto: **10 VUs / 30s** (parametrizables vía `LOAD_VUS` / `LOAD_DURATION`;
prefijo `LOAD_` y no `K6_` para no chocar con la config nativa de k6).

Cada script incluye `handleSummary(data)` que escribe `summary.json` al volumen
(`/reports/<svc>/summary.json`) además del `stdout` por consola; el HTML autocontenido lo
produce `K6_WEB_DASHBOARD_EXPORT` de forma independiente.

> **Nota sobre rutas exactas:** los endpoints concretos (listar cursos, registrar
> asistencia, consultar deudas) se confirmarán contra los controladores y las colecciones
> Bruno durante la implementación, no se inventan aquí.

## 6. Variables de entorno (`.env.k6` / `.env.k6.example`)

> La red `dama_network` la referencia el `compose.yaml` de k6 directamente como `external`
> (no necesita variable; ver §4.1).

```ini
# Targets directos a los contenedores backend (evitan el gateway/rate-limiter)
AUTH_BASE_URL=http://AuthService
COURSE_MANAGEMENT_BASE_URL=http://CourseManagementService
ATTENDANCE_BASE_URL=http://AttendanceService
PAYMENT_BASE_URL=http://PaymentService

# Credenciales sembradas (DEV)
LOAD_CLIENT_USERNAME=Client Example
LOAD_TEACHER_USERNAME=Teacher Example
LOAD_STUDENT_USERNAME=Student Example
LOAD_USER_PASSWORD=Admin123
ADMIN_USERNAME=Juan Carlos Hidalgo Sosa Admin
ADMIN_PASSWORD=5o*6gne@V4&2Rq

# Carga (prefijo LOAD_ para no chocar con la config nativa K6_* de k6)
LOAD_VUS=10
LOAD_DURATION=30s
LOAD_P95_MS=500
```

En `.env.k6.example` los valores sensibles van como placeholders. `.env.k6` se añade a
`.gitignore` (igual que `.env.dev`).

> **Usuario admin:** confirmado en `infrastructure/seeding/auth/Users.csv` →
> `Juan Carlos Hidalgo Sosa Admin` (rol `Admin`). Existe una segunda fila con rol `Admin`
> (username `w7rudO521J2adG`); se usa la nombrada por defecto.

## 7. Operación

```bash
# 1) Stack dev arriba
./infrastructure/compose-up.sh up --build

# 2) Corrida de carga
docker compose \
  --env-file infrastructure/environments-test/k6-testing/.env.k6 \
  -f infrastructure/environments-test/k6-testing/compose.yaml up

# 3) Reportes
#    http://localhost:8004/  (índice + auth/ course-management/ attendance/ payment/)
```

El README documenta el prerrequisito (stack dev arriba, ya con la red `dama_network`) y los
mandos de afinado de carga.

## 8. Documentación a actualizar (con confirmación previa)

Como parte del plan de implementación se propondrá actualizar
`extra/guia/academico/5.1-plan-de-pruebas.md`:

- §3.5.1.1: quitar las pruebas de carga de "fuera de alcance" (o matizar).
- §3.5.1.2: añadir la fila de k6 en la tabla de niveles/tipos.
- §3.5.1.5: convertir la brecha #1 en "cubierta por k6 (ver infraestructura)".
- §3.5.1.6: añadir el comando de demostración.

Estas ediciones de documentación se confirmarán con el usuario antes de aplicarse.

## 9. Fuera de alcance (YAGNI)

- No se levanta un stack propio para k6 (se reusa el dev).
- No se usa `benc-uk/k6-reporter` (descarga un bundle remoto; el dashboard nativo basta y
  es autocontenido).
- No se integra k6 al índice del test runner existente (`:8002`); vive en su propio Apache.
- No se cubren métricas de CPU/memoria vía cAdvisor/Prometheus en esta iteración; el uso
  de recursos se observa con `docker stats` durante la corrida (documentado en el README).
- Credentials queda excluido.

## 10. Criterios de aceptación

1. `docker compose --env-file .../.env.k6 -f .../k6-testing/compose.yaml up` corre los 4 k6
   contra el stack dev y termina sin errores de orquestación.
2. Cada servicio produce un HTML autocontenido servido en `http://localhost:8004/<svc>/`.
3. El índice raíz en `:8004` enlaza a los 4 reportes.
4. Los escenarios autentican correctamente con los usuarios sembrados y ejercen al menos un
   endpoint autenticado representativo por servicio.
5. `.env.k6` está gitignored; `.env.k6.example` commiteado.
6. El flujo de unit testing existente (`compose.test.yaml`, `:8002`) queda intacto.
7. `compose.dev.yaml` declara la red `dama_network` (§4.1) y el stack dev arranca sin
   regresiones (TLS, resolución por `container_name`, seeding).
