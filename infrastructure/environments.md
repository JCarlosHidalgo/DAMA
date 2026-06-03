# Environments

DAMA runs in two shapes. **Dev** is the full local stack (`compose.dev.yaml`, MySQL containers
included) — see `infrastructure/CLAUDE.md` for the wrappers — and is also published behind Cloudflare
at `https://dev.dama-software.org` (frontend) / `https://api-dev.dama-software.org` (gateway — a
single-level subdomain so Cloudflare's `*.dama-software.org` Universal SSL cert covers it; a
two-level `api.dev.…` host would fail TLS since the wildcard matches only one label).
**Production** runs on **Dokploy** with managed databases and is the subject of this document.

Per-environment public URLs are never hardcoded: each `.env.*` carries `FRONTEND_API_BASE_URL` (baked
into the Angular bundle via the frontend Dockerfile build-arg `API_BASE_URL`) and
`GATEWAY_FRONTEND_ORIGIN` (the CORS-allowed frontend origin substituted into the gateway `nginx.conf`
at container start by the nginx image's envsubst entrypoint).

The build artifacts already exist: `compose.prod.yaml` (app services only, `expose` not `ports`,
external `dokploy-network`), the four `environments/<svc>/init.sql`, the one-shot `tls-init` service,
and the gateway `nginx.conf`; use `infrastructure/.env.example` as the full variable inventory. This
runbook is the order in which to wire them.

---

## Production runbook (Dokploy)

### 0. Prerequisites

- A Dokploy host with the external Docker network **`dokploy-network`** created (Dokploy creates it
  once per project; the compose file references it as `external: true`).
- DNS for **`dama-software.org`** and **`api.dama-software.org`** pointed at the host. TLS at the
  edge is handled by **Cloudflare Tunnels** — do *not* provision certbot/Let's Encrypt for prod.
- The repo cloned by Dokploy from `https://github.com/JCarlosHidalgo/DAMA`. Note the checkout path on
  the host — it is the build context (`CONTEXT`, set in step 5). TLS now bootstraps itself in-stack
  (step 4), so no manual cert step is needed in the checkout.

### 1. Provision the four managed databases (Dokploy UI)

Create four Dokploy **Database** resources, MySQL **9** (matches dev `mysql:9`), one per backend:
`Auth`, `CourseManagement`, `Attendance`, `Payment`. Credentials has no database. Managed
Databases give Dokploy native `mysqldump → S3` backups (step 8). For each, record the internal host
and credentials — they go into the connection strings in step 6.

### 2. Initialize each schema once (`init.sql`)

There are **no migrations**. Apply each service's schema + stored procedures one time. Dokploy
managed databases do **not** auto-run init scripts (that was a `docker-entrypoint-initdb.d` behaviour,
dev-only on the `mysql:9` containers), so inject them by hand. Each file uses heavy `DELIMITER`
blocks for stored procedures, so use the `mysql` client (which processes `DELIMITER`) — not a SQL
editor that splits on `;`.

Easiest path: from each Database's **Terminal** tab in the Dokploy UI you get `mysql` and `curl` (the
`mysql:9` image ships both). The repo is public, so pull the raw file from GitHub and pipe it into
`mysql`. `mysql -u root` connects over the container's local socket — no host/network needed, and
root has the `CREATE DATABASE`/`USE` rights each file requires (it prompts for the root password):

```bash
# In the Auth database's terminal:
curl -fsSL https://raw.githubusercontent.com/JCarlosHidalgo/DAMA/main/infrastructure/environments/auth/init.sql | mysql -u root -p
# CourseManagement:
curl -fsSL https://raw.githubusercontent.com/JCarlosHidalgo/DAMA/main/infrastructure/environments/course-management/init.sql | mysql -u root -p
# Attendance:
curl -fsSL https://raw.githubusercontent.com/JCarlosHidalgo/DAMA/main/infrastructure/environments/attendance/init.sql | mysql -u root -p
# Payment:
curl -fsSL https://raw.githubusercontent.com/JCarlosHidalgo/DAMA/main/infrastructure/environments/payment/init.sql | mysql -u root -p
```

Credentials has no database, so there is no fifth command. Verify, e.g.:
`mysql -u root -p -e "USE Auth; SHOW TABLES; SHOW PROCEDURE STATUS WHERE Db='Auth';"`.

**Name caveat:** each file does `CREATE DATABASE IF NOT EXISTS <Name>` + `USE <Name>` for `Auth`,
`CourseManagement`, `Attendance`, `Payment`. Name each Dokploy Database **exactly** the same so
the `CREATE` is a no-op and the app user (in `*_DB_CONNECTION_STRING`, `database=...`) already has
grants on that schema. If you named it differently, after running the init as root also
`GRANT ALL ON <Name>.* TO '<app_user>'@'%'; FLUSH PRIVILEGES;`.

Alternatively, run the same `.sql` files through DbGate once the stack is up (step 7) — it understands
`DELIMITER`. No seeding in prod — there is no `SEED_DB=true` and no CSV bind mounts (those are dev-only).

This first release ships the subscription tiers (the "core-services pyramid"): Auth's `init.sql` adds
`TenantAllowedServices` and its `CreateTenant` procedure inserts a level-0 row for every new tenant, so
on a clean launch **every tenant starts at level 0** (no paid services) and ascends by paying — there is
nothing to backfill. Payment's `init.sql` adds the subscription ledgers and seeds the three default
`SubscriptionPlan` rows (Admin edits price + duration later from the UI).

### 3. Generate fresh production secrets

Never reuse dev values in production. Full inventory and rotation playbook live in
`infrastructure/SECRETS.md`. For prod you need, generated on the host:

- JWT RSA pair → `JWT_PRIVATE_KEY_B64` / `JWT_PUBLIC_KEY_B64`:
  ```bash
  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out priv.pem
  openssl rsa -in priv.pem -pubout -out pub.pem
  base64 -w0 priv.pem   # JWT_PRIVATE_KEY_B64
  base64 -w0 pub.pem    # JWT_PUBLIC_KEY_B64
  ```
- Payment callback secret → `PAYMENT_CALLBACK_SECRET`, and the Payment→Auth subscription gRPC secret
  → `SUBSCRIPTION_GRPC_SECRET` (same recipe, one value shared by both Auth and Payment):
  ```bash
  openssl rand -base64 64 | tr -d '\n=' | tr '+/' '-_'
  ```
- RabbitMQ and DbGate credentials → strong random strings.
- `TODOTIX_APPKEY` → from the Todotix prod merchant panel (the global fallback **and** the account that
  collects tenant subscription payments).

Each backend's `SecretsValidationModule` (Order = -100) fails fast at boot if any secret is missing
or malformed — a wrong key kills the container with a precise message, not a runtime 500.

### 4. Inter-service TLS (automatic — no manual step)

TLS is bootstrapped **inside the compose stack** by the one-shot **`tls-init`** service
(`infrastructure/environments/tls-init/Dockerfile`), which runs `bootstrap-tls.sh` into the named
volume **`dama-tls`** on every `docker compose up`. It generates the internal CA + one server cert per
gRPC server: `course-management.crt/.key` (SAN = `${COURSE_MANAGEMENT_HOST_NAME}`) for the
Attendance→CourseManagement edge, and `auth.crt/.key` (SAN = `${AUTH_HOST_NAME}`) for the
Payment→Auth edge — Auth serves a gRPC endpoint so Payment can apply a tenant's subscription level the
moment a payment is captured. Both hostnames are passed to the `tls-init` service.

The two gRPC servers (CourseManagement, Auth) mount the volume read-only and their Kestrel serves the
matching `<name>.crt/.key`; the two clients (Attendance, Payment) mount it and their `entrypoint.sh`
installs `ca.crt` into the OS trust store at container start. All four gate on `depends_on: tls-init`
(`service_completed_successfully`), so the bundle always exists before they boot.

Generation is **idempotent** (skips anything already present) and the volume persists across deploys, so
certs are stable. There is **no manual host step and no build-time `COPY`** — a fresh clone builds even
without `infrastructure/tls/` present. To rotate, delete the volume (`docker volume rm <stack>_dama-tls`)
and redeploy. The same script (`infrastructure/environments/tls-init/bootstrap-tls.sh`) can be run on
the host for local `dotnet run`.

### 5. Fill the Dokploy environment

Base it on the variable set in `infrastructure/.env.example` and paste into the Dokploy Compose service's
**Environment** editor (Dokploy writes a `.env` next to the compose; `compose.prod.yaml` reads it via
`${VAR}`). A real `infrastructure/.env.prod` stays gitignored if you ever run the prod compose by
hand. Set:

- `CONTEXT` → the Dokploy checkout path from step 0.
- The five `*_HOST_NAME` (app: `AuthService`, `CourseManagementService`, … defaults) → set each to the
  **real container name Dokploy assigns** (it appends an identifier). They drive `container_name`, the
  gateway upstreams, the two gRPC URLs (Attendance→CourseManagement at `${COURSE_MANAGEMENT_HOST_NAME}`,
  Payment→Auth at `https://${AUTH_HOST_NAME}:81`, derived in compose) and the two gRPC cert SANs, so they
  must match what Docker DNS actually resolves. If you change `COURSE_MANAGEMENT_HOST_NAME` or
  `AUTH_HOST_NAME` after a first deploy, delete the `dama-tls` volume so the affected cert SAN regenerates.
- The four `*_DB_HOST_NAME` → each managed DB's internal host (these interpolate into `server=` of the
  matching `*_DB_CONNECTION_STRING`). Then set the four `*_DB_CONNECTION_STRING` with prod credentials
  (no `*_DB_PASSWORD`/`*_DB_SCHEMA` here — those only feed the dev mysql containers).
- `FRONTEND_API_BASE_URL` / `GATEWAY_FRONTEND_ORIGIN` → the prod published URLs
  (`https://api.dama-software.org` / `https://dama-software.org`).
- `JWT_*`, `PAYMENT_CALLBACK_SECRET`, `TODOTIX_BASE_URL`, `TODOTIX_APPKEY`, `TODOTIX_CALLBACK_URL`, `RABBITMQ_*`.
- `SUBSCRIPTION_GRPC_SECRET` → the shared secret for the Payment→Auth subscription call (compose injects
  the same value into both services; `SUBSCRIPTION_GRPC_AUTH_URL` is derived, no need to set it).
- `DBGATE_LOGIN` / `DBGATE_PASSWORD`.

### 6. Deploy the stack

Point the Dokploy Compose service at `infrastructure/compose.prod.yaml` and deploy. Startup ordering
is handled by `depends_on`: RabbitMQ healthy → backends → api-gateway. `frontend` waits on the
gateway.

### 7. Map domains and configure DbGate

In the Dokploy UI map ingress:

- `dama-software.org` → service `frontend`, container port `80`
- `api.dama-software.org` → service `api-gateway`, container port `80`

DbGate is **not** a separate domain — it rides the gateway at
`https://api.dama-software.org/api/db-gate/` (served under `WEB_ROOT=/api/db-gate`, login from
`DBGATE_LOGIN`/`DBGATE_PASSWORD`). Log in once and add the four managed databases as connections;
they persist in the `dbgate-data` volume. Use DbGate for schema changes (ALTER TABLE, indexes, FKs) —
there is no migration tooling.

### 8. Enable S3 backups

On each of the four managed Databases, enable Dokploy's native `mysqldump → S3` (cron + retention).
This is the production backup story; the schema-evolution story is DbGate (step 7).

---

## Verification

- **TLS auto-bootstrap:** `tls-init` exits successfully and populates `dama-tls` with the CA + the
  `course-management` and `auth` server certs; the four gRPC services (CourseManagement, Auth,
  Attendance, Payment) start only after it completes. Images build with no `infrastructure/tls/` on the host.
- **Frontend & API reachable:** `https://dama-software.org` loads; `https://api.dama-software.org`
  routes `/api/<service>/*` to each backend (`infrastructure/verify-gateway-routes.sh` from the host
  checks each upstream resolves through the gateway).
- **Backends live:** each of the five answers `GET /health` (anonymous, shallow — liveness only).
- **gRPC TLS:** both edges resolve without cert errors — Attendance→CourseManagement and Payment→Auth
  (each client has the CA in its trust store; each cert SAN = the server's container name).
- **DbGate:** `https://api.dama-software.org/api/db-gate/` shows the login form; after authenticating,
  the four databases connect and show the schema created in step 2.
- **Backups:** the first scheduled `mysqldump` lands in the S3 bucket with the configured retention.
- **Subscription tiers (smoke test):** every tenant launches at level 0 (the `index_core_services_pyramid`
  JWT claim) and ascends by paying DAMA via QR. Drive one purchase end-to-end with the Bruno `Payment`
  collection (`api-endpoints/collections/Payment/`): *Admin: List/Update Subscription Plans* (price +
  duration are seeded and editable) → *Client: Create Subscription QR Debt* (`level` 1–3) → *Client: Get
  Subscription QR Debt Status* (poll until `Ready`) → *Public: Todotix Callback* with that
  `transaction_id` and a valid `sig` (HMAC-SHA256 of the id with `PAYMENT_CALLBACK_SECRET`), or pay the
  QR for real. On capture `PaymentCallbackWorker` makes the **synchronous gRPC Payment→Auth** call
  (authenticated by `SUBSCRIPTION_GRPC_SECRET`) and `Auth.TenantAllowedServices` shows the bought level +
  `ExpiresAt`; re-login the Client and the fresh JWT carries that `index_core_services_pyramid` and
  `subscription_expires_at`. Confirm gating: a level-0 Client sees only Resumen + Suscripción (plus the
  timezone part of Configuración), Teacher/Student of a level-0 tenant are blocked at login, and after the
  purchase the new level's tabs unlock — the schedule read-only at level 1, interactable at ≥2 — falling
  back to level 0 in place when `ExpiresAt` passes (Auth's `SubscriptionExpiryJanitor` persists the reset).
