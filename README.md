# DAMA

Monorepo for the **DAMA** platform: five .NET 9 microservices, an Angular 21 single-page app, shared gRPC contracts, two internal NuGet libraries, and the Docker Compose infrastructure that wires it all together.

## Architecture

```
Browser
  └─▶ Frontend (Angular 21, served by Apache httpd)
  └─▶ api-gateway (nginx)  ──▶  Auth            ─┐
                            ──▶  CourseManagement │  each backend owns its
                            ──▶  Attendance  │  own MySQL database
                            ──▶  Payment          │  (Credentials has none)
                            ──▶  Credentials     ─┘

Async events:  Auth · CourseManagement · Payment  ──(outbox)──▶ RabbitMQ ──▶ Attendance (+ Payment self-consume)
Sync gRPC (TLS): Attendance ──▶ CourseManagement (class/course existence)
External:        Payment ──▶ Todotix (QR payments, HTTPS)
```

- **Backends** (`apps/<Service>/Backend/`) — ASP.NET Core 9. Auth, Attendance, CourseManagement, Payment own a MySQL database each; Credentials is a stateless claims-reflection service.
- **Frontend** (`apps/Frontend/`) — Angular 21 SPA, package manager **Bun**.
- **Messaging** — transactional **outbox** → RabbitMQ, with idempotent consumers (`processed_events`). Producers never publish to the broker directly.
- **Inter-service gRPC** is TLS-terminated end-to-end (Attendance → CourseManagement).

## Repository layout

| Path | What it is |
|------|------------|
| `apps/<Service>/Backend/` | ASP.NET Core 9 service (Auth, Attendance, CourseManagement, Payment, Credentials) |
| `apps/<Service>/Test/` | NUnit test project (real suites for Auth/Attendance/CourseManagement/Payment) |
| `apps/Frontend/` | Angular 21 SPA (Bun) |
| `packages/outbox`, `packages/unit-of-work` | Internal NuGet libraries (`DAMA.Software.MySqlOutbox`, `DAMA.Software.MySqlUnitOfWork`) |
| `packages/grpc-contracts/` | Shared `.proto` package (`DAMA.Software.ValidateCourse`) |
| `infrastructure/` | Compose files, per-service Dockerfiles, DB init/seed, gateway, TLS bootstrap, `.env.*` (all container-runtime config) |
| `api-endpoints/` | Bruno API request collections (per service) |
| `pocs/`, `extra/` | Throwaway proofs of concept / auxiliary tooling (not part of the stack) |

## Prerequisites

- Docker Engine + Docker Compose v2
- (optional, for running a service outside Docker) .NET SDK 9
- (optional, for the frontend dev server) Bun 1.3+

## Quick start (development)

The dev stack runs via `compose.dev.yaml`. Use the wrapper, which bootstraps `infrastructure/.env.dev` from `.env.example` and resolves the build context on first run:

```bash
./infrastructure/compose-up.sh --bootstrap   # first time on a fresh clone
./infrastructure/compose-up.sh up --build     # start everything
./infrastructure/compose-down.sh down -v      # tear down + drop volumes
```

Service URLs once up (dev ports are offset **+100** from prod so dev can run alongside prod on one host):

| Service | URL |
|---------|-----|
| Frontend | http://localhost:8101 |
| API gateway | http://localhost:8100 |
| RabbitMQ management UI | http://localhost:15772 |

Fill secrets in `infrastructure/.env.dev` (template: `infrastructure/.env.example`). See [`infrastructure/SECRETS.md`](infrastructure/SECRETS.md) for the secret inventory and rotation playbook.

## Tests

Containerized runner (four backend suites + frontend, with HTML coverage on `:8002`):

```bash
docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.test.yaml up --build
```

On the host:

```bash
cd apps/Auth/Test && dotnet test     # any backend test project
cd apps/Frontend  && bun run test    # frontend (Vitest)
```

## Production

Production runs on **Dokploy**: app services deploy from `infrastructure/compose.prod.yaml`, and the four MySQL are Dokploy-managed databases (with native `mysqldump`→S3 backups). Full step-by-step in [`environments.md`](environments.md).

## Documentation

- [`environments.md`](environments.md) — dev and prod (Dokploy) environment setup and runbook
- [`infrastructure/SECRETS.md`](infrastructure/SECRETS.md) — secrets inventory and rotation
- Package READMEs under `packages/` describe the shared libraries and gRPC contracts
