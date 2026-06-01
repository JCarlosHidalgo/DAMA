# Environments

DAMA runs in two shapes. **Dev** is the full local stack (`compose.dev.yaml`, MySQL containers
included) — see `infrastructure/CLAUDE.md` for the wrappers — and is also published behind Cloudflare
at `https://dev.dama-software.org` (frontend) / `https://api.dev.dama-software.org` (gateway).
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
- Payment callback secret → `PAYMENT_CALLBACK_SECRET`:
  ```bash
  openssl rand -base64 64 | tr -d '\n=' | tr '+/' '-_'
  ```
- RabbitMQ and DbGate credentials → strong random strings.
- `TODOTIX_APPKEY` → from the Todotix prod merchant panel.

Each backend's `SecretsValidationModule` (Order = -100) fails fast at boot if any secret is missing
or malformed — a wrong key kills the container with a precise message, not a runtime 500.

### 4. Inter-service TLS (automatic — no manual step)

TLS is bootstrapped **inside the compose stack** by the one-shot **`tls-init`** service
(`infrastructure/environments/tls-init/Dockerfile`), which runs `bootstrap-tls.sh` into the named
volume **`dama-tls`** on every `docker compose up`. It generates the internal CA + the CourseManagement
server cert (SAN = container name). CourseManagement mounts the volume read-only and Kestrel serves
`course-management.crt/.key`; Attendance mounts it and its `entrypoint.sh` installs `ca.crt` into the OS
trust store at container start. Both backends gate on `depends_on: tls-init`
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
- The four `*_DB_CONNECTION_STRING` → point `server=` at each managed DB's internal host, fill the
  prod credentials (no `*_DB_PASSWORD`/`*_DB_SCHEMA` here — those only feed the dev mysql containers).
- `FRONTEND_API_BASE_URL` / `GATEWAY_FRONTEND_ORIGIN` → the prod published URLs
  (`https://api.dama-software.org` / `https://dama-software.org`).
- `JWT_*`, `PAYMENT_CALLBACK_SECRET`, `TODOTIX_APPKEY`, `TODOTIX_CALLBACK_URL`, `RABBITMQ_*`.
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

- **TLS auto-bootstrap:** `tls-init` exits successfully and populates `dama-tls`; CourseManagement and
  Attendance start only after it completes. Images build with no `infrastructure/tls/` on the host.
- **Frontend & API reachable:** `https://dama-software.org` loads; `https://api.dama-software.org`
  routes `/api/<service>/*` to each backend (`infrastructure/verify-gateway-routes.sh` from the host
  checks each upstream resolves through the gateway).
- **Backends live:** each of the five answers `GET /health` (anonymous, shallow — liveness only).
- **gRPC mTLS:** Attendance calls CourseManagement/Payment without a cert error (CA is in the
  caller's trust store; cert SAN = container name, which is identical in prod).
- **DbGate:** `https://api.dama-software.org/api/db-gate/` shows the login form; after authenticating,
  the four databases connect and show the schema created in step 2.
- **Backups:** the first scheduled `mysqldump` lands in the S3 bucket with the configured retention.
