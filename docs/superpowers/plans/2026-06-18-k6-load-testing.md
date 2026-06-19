# k6 Load Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a self-contained k6 load-testing suite under `infrastructure/environments-test/k6-testing/` that drives the running dev backends directly (bypassing the gateway's rate-limiter) and serves a self-contained static HTML report per service via its own Apache on `:8004`.

**Architecture:** Four one-shot `grafana/k6` containers (auth, course-management, attendance, payment) join the dev stack network `dama_network`, authenticate against `AuthService` with seeded users, exercise one representative authenticated GET flow per service, and export k6's native self-contained web-dashboard HTML + a `summary.json` to per-service named volumes. An `httpd:2.4-alpine` container serves those volumes behind a styled index. Mirrors the existing `compose.test.yaml` test-runner pattern.

**Tech Stack:** Grafana k6 (official image, native web dashboard via `K6_WEB_DASHBOARD_EXPORT`), Docker Compose, Apache httpd, plain JavaScript k6 scripts.

---

## Project conventions that constrain this plan

- **Claude does NOT run `docker compose up/down/build/restart`** — the user owns the stack lifecycle. Every step that starts/stops containers is marked **[USER RUNS]**; the implementer authors the files and asks the user to run the verification.
- **Commits:** per project policy there are **no intermediate commits**. A single commit happens **only at the very end**, after the user confirms the whole plan completed successfully (final task). Author all files first; commit last.
- **No comments in C#** — not relevant here (no C# touched). JS/YAML may carry brief clarifying comments where they earn their place.
- **`.env.k6` is gitignored** (carries the dev admin password); only `.env.k6.example` is committed.

## File structure

```
infrastructure/
├── compose.dev.yaml                          # MODIFY: add `networks: default: name: dama_network`
└── environments-test/k6-testing/             # CREATE (all new)
    ├── compose.yaml                          # 4 k6 services + apache
    ├── .env.k6                               # real dev creds (GITIGNORED)
    ├── .env.k6.example                       # committed placeholder template
    ├── scripts/
    │   ├── lib/
    │   │   ├── config.js                     # env reading: URLs, users, vus/duration, thresholds, scenario
    │   │   └── auth.js                        # login() + authHeaders()
    │   ├── auth.js
    │   ├── course-management.js
    │   ├── attendance.js
    │   └── payment.js
    ├── report/
    │   └── index.html                        # styled landing linking the 4 reports
    └── README.md
.gitignore                                    # MODIFY: add the .env.k6 path
```

## Verified facts (do not re-derive)

- Login: `POST /api/auth/login`, `[AllowAnonymous]`, body `{ "username", "password" }` (binding is case-insensitive). Response `TokenResponseDto` → JSON `{ "accessToken", "refreshToken" }` (ASP.NET default camelCase).
- Refresh: `POST /api/auth/refresh`, `[AllowAnonymous]`, body `{ "refreshToken" }`.
- Tenants list: `GET /api/auth/tenants`, `[Authorize(Roles=Admin)]`, no params.
- Courses list: `GET /api/course-management/course`, `ClientOrStudent`, no params.
- Teacher courses: `GET /api/course-management/course/teacher/me`, `Teacher`, no params.
- Student own scheduled attendance: `GET /api/attendance/attendance/scheduled/me`, `Student`, no params.
- Remaining attendance (self): `GET /api/attendance/remain/me`, `[Authorize]`, no params.
- Debt templates: `GET /api/payment/debt-template`, `ClientOrStudent`, no params.
- Payment summary: `GET /api/payment/summary`, `Client`, no params.
- Seeded users (`infrastructure/seeding/auth/Users.csv`): `Client Example` (Client), `Teacher Example` (Teacher), `Student Example` (Student), all password `Admin123`; `Juan Carlos Hidalgo Sosa Admin` (Admin) password `5o*6gne@V4&2Rq`.
- Backends listen on container port **80** internally; container names are `AuthService`, `CourseManagementService`, `AttendanceService`, `PaymentService` (from `*_HOST_NAME` defaults).
- Existing report-server ports in use: `:8002` (test runner), `:8003` (docs). `:8004` is free.

---

## Task 1: Rename the dev network to `dama_network`

**Files:**
- Modify: `infrastructure/compose.dev.yaml` (append a top-level `networks:` block)

- [ ] **Step 1: Add the network block**

Append at the very end of `infrastructure/compose.dev.yaml`, after the existing `volumes:` block (which currently ends with `  dama-tls:`). This renames the implicit `default` network so every service (all already on `default`) lands on a literally-named `dama_network`, no per-service edits needed:

```yaml

networks:
  default:
    name: dama_network
```

- [ ] **Step 2: Validate compose syntax** **[USER RUNS]**

Ask the user to run:
```bash
docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.dev.yaml config >/dev/null && echo OK
```
Expected: `OK` (no YAML/schema error). The `config` subcommand only parses; it starts nothing.

- [ ] **Step 3: (Deferred) network materializes on next stack recreation**

No action now. Note for the integration task: the renamed network only exists after a `compose-down` + `compose-up` cycle of the dev stack (named `dama_network` in `docker network ls`).

---

## Task 2: Environment files + gitignore

**Files:**
- Create: `infrastructure/environments-test/k6-testing/.env.k6.example`
- Create: `infrastructure/environments-test/k6-testing/.env.k6`
- Modify: `.gitignore`

- [ ] **Step 1: Create the committed template `.env.k6.example`**

```ini
# k6 load-testing configuration (template). Copy to .env.k6 and fill the secret values.
# k6 targets the backend containers DIRECTLY on the dev network `dama_network`,
# bypassing the api-gateway rate-limiter so the numbers reflect backend capacity.

# Direct backend targets (container name : internal port 80)
AUTH_BASE_URL=http://AuthService
COURSE_MANAGEMENT_BASE_URL=http://CourseManagementService
ATTENDANCE_BASE_URL=http://AttendanceService
PAYMENT_BASE_URL=http://PaymentService

# Seeded dev users (login is anonymous against AuthService)
LOAD_CLIENT_USERNAME=Client Example
LOAD_TEACHER_USERNAME=Teacher Example
LOAD_STUDENT_USERNAME=Student Example
LOAD_USER_PASSWORD=Admin123
ADMIN_USERNAME=Juan Carlos Hidalgo Sosa Admin
ADMIN_PASSWORD=__FILL_ME__

# Load profile (LOAD_ prefix avoids clashing with k6 native K6_* options)
LOAD_VUS=10
LOAD_DURATION=30s
LOAD_P95_MS=500
```

- [ ] **Step 2: Create the real (gitignored) `.env.k6`**

Identical to the template but with the real admin password:

```ini
AUTH_BASE_URL=http://AuthService
COURSE_MANAGEMENT_BASE_URL=http://CourseManagementService
ATTENDANCE_BASE_URL=http://AttendanceService
PAYMENT_BASE_URL=http://PaymentService

LOAD_CLIENT_USERNAME=Client Example
LOAD_TEACHER_USERNAME=Teacher Example
LOAD_STUDENT_USERNAME=Student Example
LOAD_USER_PASSWORD=Admin123
ADMIN_USERNAME=Juan Carlos Hidalgo Sosa Admin
ADMIN_PASSWORD=5o*6gne@V4&2Rq

LOAD_VUS=10
LOAD_DURATION=30s
LOAD_P95_MS=500
```

- [ ] **Step 3: Gitignore the real env file**

Add this line to `.gitignore`, right after the existing `infrastructure/.env.prod` line (keep the env entries grouped):

```
infrastructure/environments-test/k6-testing/.env.k6
```

- [ ] **Step 4: Verify the ignore rule**

Run:
```bash
git -C /home/juan/projects/DAMA check-ignore infrastructure/environments-test/k6-testing/.env.k6
```
Expected output: the path is echoed back (meaning it IS ignored). Also confirm the template is NOT ignored:
```bash
git -C /home/juan/projects/DAMA check-ignore infrastructure/environments-test/k6-testing/.env.k6.example || echo "NOT IGNORED (correct)"
```
Expected: `NOT IGNORED (correct)`.

---

## Task 3: Shared k6 library (`config.js`, `auth.js`)

**Files:**
- Create: `infrastructure/environments-test/k6-testing/scripts/lib/config.js`
- Create: `infrastructure/environments-test/k6-testing/scripts/lib/auth.js`

- [ ] **Step 1: Create `scripts/lib/config.js`**

```javascript
const num = (value, fallback) => parseInt(value || String(fallback), 10);

export const config = {
  authBaseUrl: __ENV.AUTH_BASE_URL || 'http://AuthService',
  courseManagementBaseUrl: __ENV.COURSE_MANAGEMENT_BASE_URL || 'http://CourseManagementService',
  attendanceBaseUrl: __ENV.ATTENDANCE_BASE_URL || 'http://AttendanceService',
  paymentBaseUrl: __ENV.PAYMENT_BASE_URL || 'http://PaymentService',
  vus: num(__ENV.LOAD_VUS, 10),
  duration: __ENV.LOAD_DURATION || '30s',
  p95Ms: num(__ENV.LOAD_P95_MS, 500),
};

export const users = {
  client: {
    username: __ENV.LOAD_CLIENT_USERNAME || 'Client Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  teacher: {
    username: __ENV.LOAD_TEACHER_USERNAME || 'Teacher Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  student: {
    username: __ENV.LOAD_STUDENT_USERNAME || 'Student Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  admin: {
    username: __ENV.ADMIN_USERNAME || 'Juan Carlos Hidalgo Sosa Admin',
    password: __ENV.ADMIN_PASSWORD || '',
  },
};

export function thresholds() {
  return {
    http_req_failed: ['rate<0.01'],
    http_req_duration: [`p(95)<${config.p95Ms}`],
  };
}

export function constantVusScenario() {
  return {
    load: {
      executor: 'constant-vus',
      vus: config.vus,
      duration: config.duration,
    },
  };
}
```

- [ ] **Step 2: Create `scripts/lib/auth.js`**

```javascript
import http from 'k6/http';
import { check } from 'k6';
import { config } from './config.js';

export function login(user) {
  const res = http.post(
    `${config.authBaseUrl}/api/auth/login`,
    JSON.stringify({ username: user.username, password: user.password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'login' } },
  );
  check(res, { 'login 200': (r) => r.status === 200 });

  let body = null;
  try {
    body = res.json();
  } catch (error) {
    body = null;
  }

  return {
    accessToken: body && (body.accessToken || body.AccessToken),
    refreshToken: body && (body.refreshToken || body.RefreshToken),
    response: res,
  };
}

export function authHeaders(accessToken) {
  return { headers: { Authorization: `Bearer ${accessToken}` } };
}
```

- [ ] **Step 3: Lint-check the JS parses (no k6 needed)** **[USER RUNS if node available]**

Optional sanity check (these are ES modules; `node --check` validates syntax):
```bash
node --check infrastructure/environments-test/k6-testing/scripts/lib/config.js
node --check infrastructure/environments-test/k6-testing/scripts/lib/auth.js
```
Expected: no output (exit 0). If `node` is unavailable, skip — k6 will validate at run time in Task 8.

---

## Task 4: Per-service k6 scripts

**Files:**
- Create: `infrastructure/environments-test/k6-testing/scripts/auth.js`
- Create: `infrastructure/environments-test/k6-testing/scripts/course-management.js`
- Create: `infrastructure/environments-test/k6-testing/scripts/attendance.js`
- Create: `infrastructure/environments-test/k6-testing/scripts/payment.js`

- [ ] **Step 1: Create `scripts/auth.js`**

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export default function () {
  const session = login(users.admin);
  if (!session.accessToken) {
    sleep(1);
    return;
  }

  const refreshRes = http.post(
    `${config.authBaseUrl}/api/auth/refresh`,
    JSON.stringify({ refreshToken: session.refreshToken }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'refresh' } },
  );
  check(refreshRes, { 'refresh 200': (r) => r.status === 200 });

  const tenantsRes = http.get(
    `${config.authBaseUrl}/api/auth/tenants`,
    { ...authHeaders(session.accessToken), tags: { name: 'tenants' } },
  );
  check(tenantsRes, { 'tenants 200': (r) => r.status === 200 });

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/auth/summary.json': JSON.stringify(data, null, 2) };
}
```

- [ ] **Step 2: Create `scripts/course-management.js`**

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export default function () {
  const client = login(users.client);
  if (client.accessToken) {
    const coursesRes = http.get(
      `${config.courseManagementBaseUrl}/api/course-management/course`,
      { ...authHeaders(client.accessToken), tags: { name: 'list-courses' } },
    );
    check(coursesRes, { 'courses 200': (r) => r.status === 200 });
  }

  const teacher = login(users.teacher);
  if (teacher.accessToken) {
    const teacherCoursesRes = http.get(
      `${config.courseManagementBaseUrl}/api/course-management/course/teacher/me`,
      { ...authHeaders(teacher.accessToken), tags: { name: 'teacher-courses' } },
    );
    check(teacherCoursesRes, { 'teacher courses 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/course-management/summary.json': JSON.stringify(data, null, 2) };
}
```

- [ ] **Step 3: Create `scripts/attendance.js`**

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export default function () {
  const student = login(users.student);
  if (student.accessToken) {
    const headers = authHeaders(student.accessToken);

    const scheduledRes = http.get(
      `${config.attendanceBaseUrl}/api/attendance/attendance/scheduled/me`,
      { ...headers, tags: { name: 'scheduled-attendance-me' } },
    );
    check(scheduledRes, { 'scheduled me 200': (r) => r.status === 200 });

    const remainRes = http.get(
      `${config.attendanceBaseUrl}/api/attendance/remain/me`,
      { ...headers, tags: { name: 'remain-me' } },
    );
    check(remainRes, { 'remain me 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/attendance/summary.json': JSON.stringify(data, null, 2) };
}
```

- [ ] **Step 4: Create `scripts/payment.js`**

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export default function () {
  const client = login(users.client);
  if (client.accessToken) {
    const headers = authHeaders(client.accessToken);

    const templatesRes = http.get(
      `${config.paymentBaseUrl}/api/payment/debt-template`,
      { ...headers, tags: { name: 'debt-templates' } },
    );
    check(templatesRes, { 'debt templates 200': (r) => r.status === 200 });

    const summaryRes = http.get(
      `${config.paymentBaseUrl}/api/payment/summary`,
      { ...headers, tags: { name: 'payment-summary' } },
    );
    check(summaryRes, { 'summary 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/payment/summary.json': JSON.stringify(data, null, 2) };
}
```

- [ ] **Step 5: Syntax-check all four** **[USER RUNS if node available]**

```bash
for f in auth course-management attendance payment; do \
  node --check infrastructure/environments-test/k6-testing/scripts/$f.js; done
```
Expected: no output (exit 0). Skip if `node` unavailable.

---

## Task 5: Report landing page

**Files:**
- Create: `infrastructure/environments-test/k6-testing/report/index.html`

- [ ] **Step 1: Create `report/index.html`**

Self-contained styled index mirroring the test-runner landing (`environments-test/root/index.html`), with one card per service linking to its exported dashboard at `/<svc>/`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>DAMA Load-Test Reports</title>
    <style>
        :root {
            --primary-color: #2c5282;
            --accent-color: #4299e1;
            --text-color: #1a202c;
            --muted-color: #4a5568;
            --background-color: #f7fafc;
            --card-background: #ffffff;
            --border-color: #e2e8f0;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
            background-color: var(--background-color);
            color: var(--text-color);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
        }
        header {
            background-color: var(--primary-color);
            color: white;
            padding: 2.5rem 1.5rem;
            text-align: center;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }
        header h1 { margin: 0 0 0.5rem; font-size: 2rem; font-weight: 600; letter-spacing: -0.02em; }
        header p { margin: 0; opacity: 0.85; font-size: 1rem; }
        main { flex: 1; max-width: 960px; width: 100%; margin: 2rem auto; padding: 0 1.5rem; }
        .services { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1rem; }
        .service-card {
            display: block;
            background-color: var(--card-background);
            border: 1px solid var(--border-color);
            border-left: 4px solid var(--accent-color);
            border-radius: 6px;
            padding: 1.25rem 1.5rem;
            text-decoration: none;
            color: inherit;
            transition: transform 0.15s ease, box-shadow 0.15s ease, border-left-color 0.15s ease;
        }
        .service-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
            border-left-color: var(--primary-color);
        }
        .service-card h2 { margin: 0 0 0.35rem; font-size: 1.15rem; color: var(--primary-color); font-weight: 600; }
        .service-card p { margin: 0; font-size: 0.9rem; color: var(--muted-color); line-height: 1.5; }
        footer { text-align: center; padding: 1.5rem; color: var(--muted-color); font-size: 0.85rem; }
    </style>
</head>
<body>
    <header>
        <h1>DAMA Load-Test Reports</h1>
        <p>k6 web-dashboard results per backend service</p>
    </header>
    <main>
        <div class="services">
            <a class="service-card" href="auth/">
                <h2>Auth</h2>
                <p>Login throughput, token refresh and tenant listing under load.</p>
            </a>
            <a class="service-card" href="course-management/">
                <h2>Course Management</h2>
                <p>Course listing for client and teacher roles under load.</p>
            </a>
            <a class="service-card" href="attendance/">
                <h2>Attendance</h2>
                <p>Student scheduled-attendance and remaining-classes reads under load.</p>
            </a>
            <a class="service-card" href="payment/">
                <h2>Payment</h2>
                <p>Debt-template and summary reads for the client role under load.</p>
            </a>
        </div>
    </main>
    <footer>Generated by Grafana k6 — direct-to-backend capacity run.</footer>
</body>
</html>
```

---

## Task 6: Compose file

**Files:**
- Create: `infrastructure/environments-test/k6-testing/compose.yaml`

- [ ] **Step 1: Create `compose.yaml`**

Four one-shot k6 services + Apache. k6 services run as `root` (to write the named volumes), join the external `dama_network`, load `.env.k6` into the container, and wrap k6 in `sh -c "… || true"` so a threshold breach still exits 0 (Apache gates on `service_completed_successfully`). Each exports the native self-contained dashboard HTML to `/reports/<svc>/index.html`.

```yaml
services:

  auth-load:
    container_name: AuthLoad
    image: grafana/k6
    user: root
    networks: [dama_network]
    env_file: [.env.k6]
    environment:
      K6_WEB_DASHBOARD: "true"
      K6_WEB_DASHBOARD_EXPORT: /reports/auth/index.html
    volumes:
      - ./scripts:/scripts:ro
      - auth-load-results:/reports/auth
    entrypoint: ["sh", "-c", "k6 run /scripts/auth.js || true"]

  course-management-load:
    container_name: CourseManagementLoad
    image: grafana/k6
    user: root
    networks: [dama_network]
    env_file: [.env.k6]
    environment:
      K6_WEB_DASHBOARD: "true"
      K6_WEB_DASHBOARD_EXPORT: /reports/course-management/index.html
    volumes:
      - ./scripts:/scripts:ro
      - course-management-load-results:/reports/course-management
    entrypoint: ["sh", "-c", "k6 run /scripts/course-management.js || true"]

  attendance-load:
    container_name: AttendanceLoad
    image: grafana/k6
    user: root
    networks: [dama_network]
    env_file: [.env.k6]
    environment:
      K6_WEB_DASHBOARD: "true"
      K6_WEB_DASHBOARD_EXPORT: /reports/attendance/index.html
    volumes:
      - ./scripts:/scripts:ro
      - attendance-load-results:/reports/attendance
    entrypoint: ["sh", "-c", "k6 run /scripts/attendance.js || true"]

  payment-load:
    container_name: PaymentLoad
    image: grafana/k6
    user: root
    networks: [dama_network]
    env_file: [.env.k6]
    environment:
      K6_WEB_DASHBOARD: "true"
      K6_WEB_DASHBOARD_EXPORT: /reports/payment/index.html
    volumes:
      - ./scripts:/scripts:ro
      - payment-load-results:/reports/payment
    entrypoint: ["sh", "-c", "k6 run /scripts/payment.js || true"]

  apache:
    container_name: LoadApache
    image: httpd:2.4-alpine
    ports:
      - "8004:80"
    volumes:
      - ./report/index.html:/usr/local/apache2/htdocs/index.html:ro
      - auth-load-results:/usr/local/apache2/htdocs/auth/:ro
      - course-management-load-results:/usr/local/apache2/htdocs/course-management/:ro
      - attendance-load-results:/usr/local/apache2/htdocs/attendance/:ro
      - payment-load-results:/usr/local/apache2/htdocs/payment/:ro
    depends_on:
      auth-load:
        condition: service_completed_successfully
      course-management-load:
        condition: service_completed_successfully
      attendance-load:
        condition: service_completed_successfully
      payment-load:
        condition: service_completed_successfully

networks:
  dama_network:
    name: dama_network
    external: true

volumes:
  auth-load-results:
  course-management-load-results:
  attendance-load-results:
  payment-load-results:
```

- [ ] **Step 2: Validate compose syntax** **[USER RUNS]**

The `external: true` network must already exist for `config` to fully resolve at `up` time, but `config` itself only parses YAML:
```bash
docker compose --env-file infrastructure/environments-test/k6-testing/.env.k6 \
  -f infrastructure/environments-test/k6-testing/compose.yaml config >/dev/null && echo OK
```
Expected: `OK`.

---

## Task 7: README

**Files:**
- Create: `infrastructure/environments-test/k6-testing/README.md`

- [ ] **Step 1: Create `README.md`**

````markdown
# k6 Load Testing

Self-contained load-testing suite for the DAMA backends. Four one-shot `grafana/k6`
containers drive the **running dev backends directly** (bypassing the api-gateway, whose
per-IP rate-limiter — `login 5r/m`, `api 30r/s` — would otherwise cap a single-IP load
generator), then export a self-contained HTML dashboard per service, served by Apache.

This suite is **separate** from the unit-test runner (`infrastructure/compose.test.yaml`,
`:8002`) and the docs viewer (`:8003`). It lives on its own at `:8004`.

## Prerequisites

The dev stack must be **up** and reachable on the `dama_network` Docker network:

```bash
./infrastructure/compose-up.sh up --build
docker network ls | grep dama_network   # confirm the network exists
```

`dama_network` is created by `compose.dev.yaml` (top-level `networks: default: name: dama_network`).
If it is missing, recreate the dev stack (`./infrastructure/compose-down.sh` then up again).

## Configure

Copy the template and set the admin password:

```bash
cp infrastructure/environments-test/k6-testing/.env.k6.example \
   infrastructure/environments-test/k6-testing/.env.k6
# edit .env.k6 → set ADMIN_PASSWORD
```

`.env.k6` is gitignored. Tunable knobs: `LOAD_VUS`, `LOAD_DURATION`, `LOAD_P95_MS`, and the
per-service `*_BASE_URL` (point them at the gateway instead to measure the protected edge).

## Run

```bash
docker compose \
  --env-file infrastructure/environments-test/k6-testing/.env.k6 \
  -f infrastructure/environments-test/k6-testing/compose.yaml up
```

The four k6 containers run to completion, then Apache starts. Open:

- http://localhost:8004/ — index
- http://localhost:8004/auth/ , /course-management/ , /attendance/ , /payment/ — per-service dashboards

Each volume also holds a machine-readable `summary.json` next to the HTML.

## Observe resource usage

While the run is in flight, in another terminal:

```bash
docker stats AuthService CourseManagementService AttendanceService PaymentService
```

## Tear down

```bash
docker compose -f infrastructure/environments-test/k6-testing/compose.yaml down -v
```
````

---

## Task 8: Integration run + verification **[USER RUNS]**

No files. This task confirms the suite works end-to-end. Claude does not run these; ask the user to run them and report output.

- [ ] **Step 1: Ensure the dev stack is up with `dama_network`**

```bash
./infrastructure/compose-up.sh up --build
docker network ls | grep dama_network
```
Expected: a `dama_network` row. If absent, the user must `compose-down` then `compose-up` so the renamed network materializes (Task 1, Step 3).

- [ ] **Step 2: Run the load suite**

```bash
docker compose --env-file infrastructure/environments-test/k6-testing/.env.k6 \
  -f infrastructure/environments-test/k6-testing/compose.yaml up
```
Expected: each `*Load` container logs a k6 run summary (http_reqs, http_req_duration, checks) and exits; then `LoadApache` starts and stays up.

- [ ] **Step 3: Verify the reports are served**

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8004/
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8004/auth/index.html
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8004/payment/index.html
```
Expected: `200` for each. Open http://localhost:8004/ in a browser and confirm the four cards link to populated k6 dashboards (charts render, metrics present).

- [ ] **Step 4: Sanity-check authentication actually worked**

In each per-service dashboard (or the container logs), confirm the `checks` pass rate for the `login 200` check is high and the authenticated GET checks (`tenants 200`, `courses 200`, etc.) are mostly `200`. A wall of `401` means the seeded credentials or `ADMIN_PASSWORD` need correcting in `.env.k6`. (Thresholds on `http_req_duration` may legitimately fail because login does password hashing — that does not block report generation.)

- [ ] **Step 5: Tear down the suite**

```bash
docker compose -f infrastructure/environments-test/k6-testing/compose.yaml down -v
```

---

## Task 9: Infra CLAUDE.md note (optional, recommended)

**Files:**
- Modify: `infrastructure/CLAUDE.md` (add a short "Containerized load-test runner" section)

> Note: the five repo `CLAUDE.md` files are gitignored — edit on disk, never commit.

- [ ] **Step 1: Append a section after "Containerized docs viewer"**

```markdown
## Containerized load-test runner

```bash
# Dev stack must be up first (provides the `dama_network` network):
./infrastructure/compose-up.sh up --build
docker compose --env-file infrastructure/environments-test/k6-testing/.env.k6 \
  -f infrastructure/environments-test/k6-testing/compose.yaml up
# Then browse http://localhost:8004/ (index) or /auth/ /course-management/ /attendance/ /payment/.
```

Four one-shot `grafana/k6` containers drive the backends **directly** on `dama_network`
(bypassing the gateway rate-limiter), exporting k6's native self-contained web-dashboard
HTML per service plus a `summary.json`, served by Apache on `:8004`. Credentials/profile in
the gitignored `infrastructure/environments-test/k6-testing/.env.k6` (template committed).
Separate from the unit-test runner (`:8002`) and docs viewer (`:8003`). Details + scenarios
in `infrastructure/environments-test/k6-testing/README.md`. Note: `compose.dev.yaml` now
names the default network `dama_network` so this suite can join it as `external`.
```

---

## Task 10: 5.1 documentation update (GATED — confirm with user first)

**Files:**
- Modify: `extra/guia/academico/5.1-plan-de-pruebas.md`

> **Do not start this task without explicit user confirmation** (spec §8). The user must
> approve editing the academic guide before any edit is made.

- [ ] **Step 1: Confirm scope with the user**

Ask: "¿Actualizo `5.1-plan-de-pruebas.md` para reflejar que k6 ya cubre las pruebas de carga (§3.5.1.1 alcance, §3.5.1.2 tabla de niveles, §3.5.1.5 brecha #1, §3.5.1.6 comandos)?" Proceed only on explicit yes.

- [ ] **Step 2: Apply the agreed edits**

On confirmation, edit the four sub-sections per spec §8: move load testing out of "fuera de alcance" (or qualify it), add the k6 row to the §3.5.1.2 levels table, convert §3.5.1.5 gap #1 to "cubierta por k6 (ver `infrastructure/environments-test/k6-testing/`)", and add the demo command to §3.5.1.6. Mirror the document's existing tone and table formatting. (Exact prose is drafted at edit time against the then-current file.)

---

## Task 11: Final commit (only after user confirms the whole plan succeeded)

> Per project policy: a single commit at the very end, no intermediate commits, English message, no footer/trailer, no push.

- [ ] **Step 1: Stage the new/modified tracked files**

`.env.k6` is gitignored and must NOT be staged; the template and everything else are tracked.

```bash
git -C /home/juan/projects/DAMA add \
  infrastructure/compose.dev.yaml \
  infrastructure/environments-test/k6-testing/compose.yaml \
  infrastructure/environments-test/k6-testing/.env.k6.example \
  infrastructure/environments-test/k6-testing/scripts \
  infrastructure/environments-test/k6-testing/report \
  infrastructure/environments-test/k6-testing/README.md \
  .gitignore
```

- [ ] **Step 2: Confirm `.env.k6` is not staged**

```bash
git -C /home/juan/projects/DAMA status --porcelain | grep -E "\.env\.k6($|[^.])" && echo "LEAK — unstage it" || echo "clean"
```
Expected: `clean`.

- [ ] **Step 3: Commit**

```bash
git -C /home/juan/projects/DAMA commit -m "test: add k6 load-testing suite under environments-test/k6-testing"
```

---

## Self-review notes (author's checklist — already applied)

- **Spec coverage:** location/isolation (Tasks 2–7), `dama_network` change §4.1 (Task 1), direct-to-backend targeting (Task 2 env + Task 4 scripts), authenticated per-service scenarios (Tasks 3–4), native dashboard + own Apache `:8004` (Tasks 5–6), gitignored `.env.k6` + committed template (Task 2), Credentials excluded (no script), 5.1 docs gated (Task 10), acceptance criteria covered by Task 8. ✓
- **Placeholders:** none — every file has full content; `ADMIN_PASSWORD=__FILL_ME__` is intentional only in the committed template, real value present in the gitignored `.env.k6`. ✓
- **Type/name consistency:** `config`, `users`, `thresholds()`, `constantVusScenario()`, `login()`, `authHeaders()` defined in Task 3 and used identically in Task 4; volume names, `K6_WEB_DASHBOARD_EXPORT` paths, and Apache mount paths all agree (`auth`, `course-management`, `attendance`, `payment`). ✓
- **Lifecycle/commit policy:** all `docker compose up/down` steps marked **[USER RUNS]**; single final commit in Task 11. ✓
