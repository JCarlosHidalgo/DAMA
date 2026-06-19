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
