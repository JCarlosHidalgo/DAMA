# OWASP CI/CD Security Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remediate all OWASP Top 10 CI/CD Security Risk findings across four waves: NuGet lockfiles (Wave 0), workflow hardening (Wave 1), Dockerfile hardening (Wave 2), GitHub config (Wave 3).

**Architecture:** Wave 0 generates `packages.lock.json` for all 5 backends — a hard prerequisite for Wave 1's `--locked-mode`. Wave 1 hardens `.github/workflows/`. Wave 2 hardens all Dockerfiles and adds Hadolint to CI. Wave 3 adds CODEOWNERS, Dependabot, a branch protection runbook, and documents the NuGet prefix reservation. Waves 2 and 3 are independent of Wave 1 and each other.

**Tech Stack:** GitHub Actions, .NET 9 / NuGet, Docker (Debian aspnet:9.0 runtime), gosu, Hadolint v2.12.0, Microsoft.Sbom.DotNet (dotnet global tool), Bun, nginx.

## Global Constraints

- No `Directory.Build.props` — add `RestorePackagesWithLockFile` to each individual `Backend.csproj`.
- No comments in C# code. Dockerfile pragma comments (`# hadolint ignore=`) are allowed.
- Commit messages in English, no footer/trailer.
- Never run `docker compose up/down/build/restart` — user manages stack lifecycle.
- Never run `git push`.
- Never run `dotnet pack/push` — user publishes packages.
- Spec: `docs/superpowers/specs/2026-06-23-owasp-cicd-hardening-design.md`.

---

## File Structure

### Wave 0
| Action | Path |
|---|---|
| Modify | `apps/Auth/Backend/Backend.csproj` |
| Modify | `apps/Attendance/Backend/Backend.csproj` |
| Modify | `apps/CourseManagement/Backend/Backend.csproj` |
| Modify | `apps/Payment/Backend/Backend.csproj` |
| Modify | `apps/Credentials/Backend/Backend.csproj` |
| Generate + commit | `apps/Auth/Backend/packages.lock.json` |
| Generate + commit | `apps/Attendance/Backend/packages.lock.json` |
| Generate + commit | `apps/CourseManagement/Backend/packages.lock.json` |
| Generate + commit | `apps/Payment/Backend/packages.lock.json` |
| Generate + commit | `apps/Credentials/Backend/packages.lock.json` |

### Wave 1
| Action | Path |
|---|---|
| Modify | `.github/workflows/ci.yml` |
| Modify | `.github/workflows/pages.yml` |

### Wave 2
| Action | Path |
|---|---|
| Modify | `infrastructure/environments/auth/Dockerfile` |
| Modify | `infrastructure/environments/attendance/Dockerfile` |
| Modify | `infrastructure/environments/attendance/entrypoint.sh` |
| Modify | `infrastructure/environments/course-management/Dockerfile` |
| Modify | `infrastructure/environments/payment/Dockerfile` |
| Modify | `infrastructure/environments/payment/entrypoint.sh` |
| Modify | `infrastructure/environments/credentials/Dockerfile` |
| Modify | `infrastructure/environments/rabbitmq/Dockerfile` |
| Modify | `.github/workflows/ci.yml` |

### Wave 3
| Action | Path |
|---|---|
| Create | `.github/CODEOWNERS` |
| Create | `.github/dependabot.yml` |
| Create | `.github/branch-protection-runbook.md` |
| Modify | `infrastructure/SECRETS.md` |

---

## Wave 0 — NuGet Lockfiles (prerequisite for Wave 1)

### Task 0: Generate and commit packages.lock.json for all 5 backends

**Files:**
- Modify: `apps/Auth/Backend/Backend.csproj`
- Modify: `apps/Attendance/Backend/Backend.csproj`
- Modify: `apps/CourseManagement/Backend/Backend.csproj`
- Modify: `apps/Payment/Backend/Backend.csproj`
- Modify: `apps/Credentials/Backend/Backend.csproj`
- Generate: `apps/*/Backend/packages.lock.json` (5 files)

- [ ] **Step 1: Verify lockfiles are absent**

```bash
find apps -name "packages.lock.json"
```
Expected: no output.

- [ ] **Step 2: Add `RestorePackagesWithLockFile` to Auth**

In `apps/Auth/Backend/Backend.csproj`, change the `<PropertyGroup>` to:
```xml
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
```

- [ ] **Step 3: Add `RestorePackagesWithLockFile` to Attendance**

In `apps/Attendance/Backend/Backend.csproj`, same change as Step 2.

- [ ] **Step 4: Add `RestorePackagesWithLockFile` to CourseManagement**

In `apps/CourseManagement/Backend/Backend.csproj`, same change as Step 2.

- [ ] **Step 5: Add `RestorePackagesWithLockFile` to Payment**

In `apps/Payment/Backend/Backend.csproj`, same change as Step 2.

- [ ] **Step 6: Add `RestorePackagesWithLockFile` to Credentials**

In `apps/Credentials/Backend/Backend.csproj`, same change as Step 2.

- [ ] **Step 7: Generate lockfiles for all 5 backends**

```bash
dotnet restore apps/Auth/Backend/Backend.csproj
dotnet restore apps/Attendance/Backend/Backend.csproj
dotnet restore apps/CourseManagement/Backend/Backend.csproj
dotnet restore apps/Payment/Backend/Backend.csproj
dotnet restore apps/Credentials/Backend/Backend.csproj
```
Expected: each exits 0 and creates `packages.lock.json` alongside the `.csproj`.

- [ ] **Step 8: Verify all 5 lockfiles exist**

```bash
find apps -name "packages.lock.json" | sort
```
Expected:
```
apps/Attendance/Backend/packages.lock.json
apps/Auth/Backend/packages.lock.json
apps/CourseManagement/Backend/packages.lock.json
apps/Credentials/Backend/packages.lock.json
apps/Payment/Backend/packages.lock.json
```

- [ ] **Step 9: Verify `--locked-mode` passes for each backend**

```bash
dotnet restore apps/Auth/Backend/Backend.csproj --locked-mode
dotnet restore apps/Attendance/Backend/Backend.csproj --locked-mode
dotnet restore apps/CourseManagement/Backend/Backend.csproj --locked-mode
dotnet restore apps/Payment/Backend/Backend.csproj --locked-mode
dotnet restore apps/Credentials/Backend/Backend.csproj --locked-mode
```
Expected: all exit 0.

- [ ] **Step 10: Commit**

```bash
git add \
  apps/Auth/Backend/Backend.csproj \
  apps/Auth/Backend/packages.lock.json \
  apps/Attendance/Backend/Backend.csproj \
  apps/Attendance/Backend/packages.lock.json \
  apps/CourseManagement/Backend/Backend.csproj \
  apps/CourseManagement/Backend/packages.lock.json \
  apps/Payment/Backend/Backend.csproj \
  apps/Payment/Backend/packages.lock.json \
  apps/Credentials/Backend/Backend.csproj \
  apps/Credentials/Backend/packages.lock.json
git commit -m "build: add NuGet lockfiles for all 5 backends"
```

---

## Wave 1 — Workflows

### Task 1: Pin third-party Action SHAs in `ci.yml`

**Files:**
- Modify: `.github/workflows/ci.yml` (lines 25, 109)

- [ ] **Step 1: Resolve the commit SHA for `dorny/paths-filter@v4`**

```bash
gh api repos/dorny/paths-filter/git/refs/tags/v4 --jq '.object.sha'
```
If the output is a 40-char hex string starting with a commit hash (type `commit`), that is the SHA.
If the type is `tag` (annotated), dereference it:
```bash
gh api repos/dorny/paths-filter/git/tags/<SHA_FROM_ABOVE> --jq '.object.sha'
```
Save this value as `DORNY_SHA`.

- [ ] **Step 2: Resolve the commit SHA for `oven-sh/setup-bun@v2`**

```bash
gh api repos/oven-sh/setup-bun/git/refs/tags/v2 --jq '.object.sha'
```
Same dereference logic if annotated. Save as `BUN_SHA`.

- [ ] **Step 3: Update `dorny/paths-filter` reference in `ci.yml`**

In `.github/workflows/ci.yml` line 25, replace:
```yaml
      - uses: dorny/paths-filter@v4
```
with (substitute `DORNY_SHA`):
```yaml
      - uses: dorny/paths-filter@<DORNY_SHA>  # v4
```

- [ ] **Step 4: Update `oven-sh/setup-bun` references in `ci.yml`**

Replace both occurrences (lines ~109 and ~234):
```yaml
      - uses: oven-sh/setup-bun@v2
```
with (substitute `BUN_SHA`):
```yaml
      - uses: oven-sh/setup-bun@<BUN_SHA>  # v2
```

- [ ] **Step 5: Verify the file has no remaining unpinned third-party references**

```bash
grep "uses:" .github/workflows/ci.yml | grep -v "actions/" | grep -v "github/codeql" | grep -v "#"
```
Expected: no output (all third-party actions now have SHA + comment).

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: pin third-party action SHAs in ci.yml"
```

---

### Task 2: Add `--locked-mode` to `dotnet restore` in CI

**Files:**
- Modify: `.github/workflows/ci.yml` (restore steps in `build-backend` and `test-backend`)

**Prerequisite:** Task 0 (Wave 0) must be merged before this task is pushed to a PR targeting `main`, otherwise CI will fail the locked restore.

- [ ] **Step 1: Add `--locked-mode` to the `build-backend` restore**

In `.github/workflows/ci.yml`, find the Restore step in `build-backend` (currently line 91):
```yaml
      - name: Restore
        run: dotnet restore apps/${{ matrix.service }}/Backend/Backend.csproj
```
Change to:
```yaml
      - name: Restore
        run: dotnet restore apps/${{ matrix.service }}/Backend/Backend.csproj --locked-mode
```

- [ ] **Step 2: Verify the change is correct**

```bash
grep "dotnet restore" .github/workflows/ci.yml
```
Expected (2 lines total):
```
        run: dotnet restore apps/${{ matrix.service }}/Backend/Backend.csproj --locked-mode
        run: dotnet restore apps/${{ matrix.service }}/Test/Test.csproj
```
Note: the Test project restore does NOT get `--locked-mode` — Test.csproj has no lockfile.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: enforce NuGet lockfile via --locked-mode in backend restore"
```

---

### Task 3: Add SBOM generation to `build-backend`

**Files:**
- Modify: `.github/workflows/ci.yml` (add 2 steps after the Build step in `build-backend`)

- [ ] **Step 1: Add SBOM generation step after the Build step**

In `.github/workflows/ci.yml`, after the `Build (warnings as errors)` step inside `build-backend`, add:

```yaml
      - name: Generate SBOM
        run: |
          dotnet tool install --global Microsoft.Sbom.DotNet
          export PATH="$HOME/.dotnet/tools:$PATH"
          sbom-tool generate \
            -b apps/${{ matrix.service }}/Backend \
            -bc apps/${{ matrix.service }}/Backend \
            -pn DAMA.${{ matrix.service }} \
            -pv ${{ github.sha }} \
            -ps DAMA-Software \
            -nsb https://github.com/JCarlosHidalgo/DAMA
        continue-on-error: true

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: sbom-${{ matrix.service }}
          path: apps/${{ matrix.service }}/Backend/_manifest/spdx_2.2/
```

- [ ] **Step 2: Verify the yaml is well-formed**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))" && echo "OK"
```
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: generate SPDX 2.2 SBOM per backend service after build"
```

---

### Task 4: Add artifact checksums to `pages.yml`

**Files:**
- Modify: `.github/workflows/pages.yml` (add step after `Extract test reports from volumes`)

- [ ] **Step 1: Add the checksums step in `build-tests`**

In `.github/workflows/pages.yml`, after the `Extract test reports from volumes` step, add:

```yaml
      - name: Generate checksums
        run: |
          for svc in auth payment course-management attendance frontend; do
            find "site/tests/$svc" -type f | sort | \
              xargs sha256sum > "site/tests/$svc/checksums.sha256"
          done
```

- [ ] **Step 2: Verify yaml is well-formed**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/pages.yml'))" && echo "OK"
```
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/pages.yml
git commit -m "ci: generate sha256 checksums for test report artifacts"
```

---

## Wave 2 — Dockerfiles + Infrastructure

### Task 5: Update backend Dockerfiles (sdk → aspnet + non-root user)

**Context:**
- `auth`, `credentials`, `course-management`: no `entrypoint.sh` → add `USER app` directly.
- `attendance`, `payment`: have `entrypoint.sh` that calls `update-ca-certificates` (requires root). Strategy: install `gosu`, drop privileges inside `entrypoint.sh` with `exec gosu app dotnet Backend.dll`, and do NOT add `USER app` in the Dockerfile (the container starts as root, the entrypoint drops to `app` after cert installation).

**Files:**
- Modify: `infrastructure/environments/auth/Dockerfile`
- Modify: `infrastructure/environments/credentials/Dockerfile`
- Modify: `infrastructure/environments/course-management/Dockerfile`
- Modify: `infrastructure/environments/attendance/Dockerfile`
- Modify: `infrastructure/environments/attendance/entrypoint.sh`
- Modify: `infrastructure/environments/payment/Dockerfile`
- Modify: `infrastructure/environments/payment/entrypoint.sh`

- [ ] **Step 1: Replace `auth/Dockerfile` final stage**

Replace the entire `infrastructure/environments/auth/Dockerfile` with:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Auth/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Auth/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

USER app
ENTRYPOINT ["dotnet","Backend.dll"]
```

- [ ] **Step 2: Replace `credentials/Dockerfile` final stage**

Replace the entire `infrastructure/environments/credentials/Dockerfile` with:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Credentials/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Credentials/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

USER app
ENTRYPOINT ["dotnet","Backend.dll"]
```

- [ ] **Step 3: Replace `course-management/Dockerfile` final stage**

Replace the entire `infrastructure/environments/course-management/Dockerfile` with:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/CourseManagement/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/CourseManagement/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. CourseManagement is the gRPC server; its cert + key
# arrive at runtime via the dama-tls volume (mounted read-only at /etc/dama/tls),
# where Kestrel reads course-management.crt/.key. No build-time COPY — the
# tls-init compose service generates the bundle into that volume on every deploy.

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

USER app
ENTRYPOINT ["dotnet","Backend.dll"]
```

- [ ] **Step 4: Replace `attendance/Dockerfile` final stage**

`attendance` uses `entrypoint.sh` which calls `update-ca-certificates` (needs root). `gosu` handles the privilege drop after cert installation. No `USER app` directive — the entrypoint drops to `app` at runtime.

Replace the entire `infrastructure/environments/attendance/Dockerfile` with:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Attendance/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Attendance/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. Attendance is a gRPC client only: it presents no
# server cert and trusts the internal CA at runtime. The CA arrives via the
# dama-tls volume (mounted at /etc/dama/tls); entrypoint.sh installs it into the
# OS trust store before launch, so HttpClient validates CourseManagement
# without a custom callback.
COPY ./infrastructure/environments/attendance/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
```

- [ ] **Step 5: Update `attendance/entrypoint.sh` to drop privileges**

Replace `infrastructure/environments/attendance/entrypoint.sh` with:

```sh
#!/bin/sh
set -e

if [ -f /etc/dama/tls/ca.crt ]; then
    cp /etc/dama/tls/ca.crt /usr/local/share/ca-certificates/dama-ca.crt
    update-ca-certificates >/dev/null 2>&1 || true
fi

exec gosu app dotnet Backend.dll
```

- [ ] **Step 6: Replace `payment/Dockerfile` final stage**

Same approach as `attendance` (has `entrypoint.sh`, needs gosu, no `USER app`).

Replace the entire `infrastructure/environments/payment/Dockerfile` with:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Payment/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Payment/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. Payment is a gRPC client of Auth (synchronous
# tenant-subscription update): it presents no server cert and trusts the
# internal CA at runtime. The CA arrives via the dama-tls volume (mounted at
# /etc/dama/tls); entrypoint.sh installs it into the OS trust store before
# launch, so the gRPC client validates Auth without a custom callback.
COPY ./infrastructure/environments/payment/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
```

- [ ] **Step 7: Update `payment/entrypoint.sh` to drop privileges**

Replace `infrastructure/environments/payment/entrypoint.sh` with:

```sh
#!/bin/sh
set -e

if [ -f /etc/dama/tls/ca.crt ]; then
    cp /etc/dama/tls/ca.crt /usr/local/share/ca-certificates/dama-ca.crt
    update-ca-certificates >/dev/null 2>&1 || true
fi

exec gosu app dotnet Backend.dll
```

- [ ] **Step 8: Build and verify auth image runs as non-root**

```bash
docker build -f infrastructure/environments/auth/Dockerfile . -t dama-auth-test --no-cache
docker run --rm --entrypoint id dama-auth-test
```
Expected: `uid=1654(app) gid=1654(app) groups=1654(app)`

- [ ] **Step 9: Build and verify attendance image has gosu and app user**

```bash
docker build -f infrastructure/environments/attendance/Dockerfile . -t dama-attendance-test --no-cache
docker run --rm dama-attendance-test gosu app id
```
Expected: `uid=1654(app) gid=1654(app) groups=1654(app)`

- [ ] **Step 10: Commit**

```bash
git add \
  infrastructure/environments/auth/Dockerfile \
  infrastructure/environments/credentials/Dockerfile \
  infrastructure/environments/course-management/Dockerfile \
  infrastructure/environments/attendance/Dockerfile \
  infrastructure/environments/attendance/entrypoint.sh \
  infrastructure/environments/payment/Dockerfile \
  infrastructure/environments/payment/entrypoint.sh
git commit -m "build: switch backend runtime images to aspnet and run as non-root user"
```

---

### Task 6: RabbitMQ plugin SHA256 verification

**Files:**
- Modify: `infrastructure/environments/rabbitmq/Dockerfile`

- [ ] **Step 1: Fetch the SHA256 of the plugin for version 4.1.0**

```bash
curl -fsSL https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v4.1.0/rabbitmq_delayed_message_exchange-4.1.0.ez | sha256sum
```
Save the 64-char hex output as `PLUGIN_SHA256` (the trailing `  -` is not part of the hash).

- [ ] **Step 2: Replace the RabbitMQ `RUN` block**

In `infrastructure/environments/rabbitmq/Dockerfile`, replace:
```dockerfile
RUN apk add --no-cache curl ca-certificates \
    && curl -fSL -o /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez \
        "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez" \
    && rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
```
with (substitute `PLUGIN_SHA256`):
```dockerfile
RUN apk add --no-cache curl ca-certificates \
    && curl -fSL -o /tmp/plugin.ez \
        "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez" \
    && echo "<PLUGIN_SHA256>  /tmp/plugin.ez" | sha256sum -c - \
    && mv /tmp/plugin.ez \
        /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez \
    && rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
```

- [ ] **Step 3: Verify the build succeeds with the correct hash**

```bash
docker build -f infrastructure/environments/rabbitmq/Dockerfile . -t dama-rabbitmq-test --no-cache
```
Expected: exits 0, `sha256sum -c` line prints `OK`.

- [ ] **Step 4: Verify the build fails with a tampered hash**

Edit the SHA256 in the Dockerfile temporarily, changing one character. Run:
```bash
docker build -f infrastructure/environments/rabbitmq/Dockerfile . -t dama-rabbitmq-fail --no-cache
```
Expected: fails with `sha256sum: WARNING: 1 computed checksum did NOT match`.
Restore the correct hash before the next step.

- [ ] **Step 5: Commit**

```bash
git add infrastructure/environments/rabbitmq/Dockerfile
git commit -m "build: verify SHA256 of RabbitMQ delayed-message-exchange plugin"
```

---

### Task 7: Add Hadolint to CI

**Files:**
- Modify: `.github/workflows/ci.yml` (new job `lint-dockerfiles`, add to `ci-gate`)

- [ ] **Step 1: Get the pinned Docker image digest for Hadolint v2.12.0**

```bash
docker pull hadolint/hadolint:v2.12.0
docker image inspect hadolint/hadolint:v2.12.0 --format '{{index .RepoDigests 0}}'
```
Save the full `hadolint/hadolint@sha256:...` string as `HADOLINT_REF`.

- [ ] **Step 2: Add `lint-dockerfiles` job to `ci.yml`**

In `.github/workflows/ci.yml`, after the `changes` job definition and before `build-backend`, insert the following new job (substitute `HADOLINT_REF`):

```yaml
  lint-dockerfiles:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - name: Lint Dockerfiles
        run: |
          for dockerfile in \
            infrastructure/environments/auth/Dockerfile \
            infrastructure/environments/attendance/Dockerfile \
            infrastructure/environments/course-management/Dockerfile \
            infrastructure/environments/payment/Dockerfile \
            infrastructure/environments/credentials/Dockerfile \
            infrastructure/environments/api-gateway/Dockerfile \
            infrastructure/environments/frontend/Dockerfile \
            infrastructure/environments/rabbitmq/Dockerfile \
            infrastructure/environments/tls-init/Dockerfile; do
            echo "Linting $dockerfile"
            docker run --rm -i <HADOLINT_REF> < "$dockerfile"
          done
```

- [ ] **Step 3: Add `lint-dockerfiles` to `ci-gate`**

In the `ci-gate` job, find the `needs` array:
```yaml
  ci-gate:
    needs: [build-backend, build-frontend, test-backend, test-frontend]
```
Change to:
```yaml
  ci-gate:
    needs: [build-backend, build-frontend, test-backend, test-frontend, lint-dockerfiles]
```

And in the `results` declare block inside `ci-gate`, add:
```yaml
            [lint-dockerfiles]='${{ needs.lint-dockerfiles.result }}'
```

- [ ] **Step 4: Verify the yaml is well-formed**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))" && echo "OK"
```
Expected: `OK`

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add Hadolint Dockerfile linting job to CI pipeline"
```

---

## Wave 3 — GitHub Configuration

### Task 8: Create CODEOWNERS

**Files:**
- Create: `.github/CODEOWNERS`

- [ ] **Step 1: Create `.github/CODEOWNERS`**

```
*                           @JCarlosHidalgo

/apps/Auth/                 @JCarlosHidalgo
/apps/Attendance/           @JCarlosHidalgo
/apps/CourseManagement/     @JCarlosHidalgo
/apps/Payment/              @JCarlosHidalgo
/apps/Credentials/          @JCarlosHidalgo

/apps/Frontend/             @JCarlosHidalgo

/infrastructure/            @JCarlosHidalgo
/.github/                   @JCarlosHidalgo

/grpc-contracts/            @JCarlosHidalgo
/packages/                  @JCarlosHidalgo
```

- [ ] **Step 2: Verify file is parseable by GitHub syntax**

```bash
cat .github/CODEOWNERS
```
Each non-blank, non-comment line must have the form `<pattern>  <@owner>`. Confirm no trailing spaces or missing `@`.

- [ ] **Step 3: Commit**

```bash
git add .github/CODEOWNERS
git commit -m "ci: add CODEOWNERS for all repository areas"
```

---

### Task 9: Create Dependabot configuration

**Files:**
- Create: `.github/dependabot.yml`

- [ ] **Step 1: Create `.github/dependabot.yml`**

```yaml
version: 2
updates:
  - package-ecosystem: github-actions
    directory: /
    schedule:
      interval: weekly
    labels:
      - dependencies
      - ci

  - package-ecosystem: nuget
    directory: /apps/Auth/Backend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - backend

  - package-ecosystem: nuget
    directory: /apps/Attendance/Backend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - backend

  - package-ecosystem: nuget
    directory: /apps/CourseManagement/Backend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - backend

  - package-ecosystem: nuget
    directory: /apps/Payment/Backend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - backend

  - package-ecosystem: nuget
    directory: /apps/Credentials/Backend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - backend

  - package-ecosystem: nuget
    directory: /packages/outbox
    schedule:
      interval: weekly
    labels:
      - dependencies
      - packages

  - package-ecosystem: nuget
    directory: /packages/unit-of-work
    schedule:
      interval: weekly
    labels:
      - dependencies
      - packages

  - package-ecosystem: nuget
    directory: /packages/grpc-contracts
    schedule:
      interval: weekly
    labels:
      - dependencies
      - packages

  # Dependabot uses npm ecosystem to read package.json.
  # After each Dependabot PR, run `bun install` locally to update bun.lock before merging.
  - package-ecosystem: npm
    directory: /apps/Frontend
    schedule:
      interval: weekly
    labels:
      - dependencies
      - frontend
```

- [ ] **Step 2: Verify yaml is well-formed**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/dependabot.yml'))" && echo "OK"
```
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add .github/dependabot.yml
git commit -m "ci: add Dependabot for NuGet, npm, and GitHub Actions"
```

---

### Task 10: Create branch protection runbook

**Files:**
- Create: `.github/branch-protection-runbook.md`

- [ ] **Step 1: Create `.github/branch-protection-runbook.md`**

```markdown
# Branch Protection Runbook — `main`

Branch protection is configured in the GitHub UI (not versionable as a repo file).
This document records the required settings and the equivalent CLI command so the
configuration is reproducible from this file.

## Required settings for `main`

- Require a pull request before merging
  - Required approving reviews: 1
  - Require review from Code Owners (CODEOWNERS file)
- Require status checks to pass before merging
  - Require branches to be up to date before merging
  - Required checks: `changes`, `ci-gate`
- Do not allow bypassing the above settings (enforce for administrators)
- Allow force pushes: disabled
- Allow deletions: disabled

## Apply via GitHub CLI

Run once after merging this file to `main`:

    gh api repos/JCarlosHidalgo/DAMA/branches/main/protection \
      --method PUT \
      --field 'required_status_checks[strict]=true' \
      --field 'required_status_checks[contexts][]=changes' \
      --field 'required_status_checks[contexts][]=ci-gate' \
      --field 'enforce_admins=true' \
      --field 'required_pull_request_reviews[required_approving_review_count]=1' \
      --field 'required_pull_request_reviews[require_code_owner_reviews]=true' \
      --field 'restrictions=null'

## Verify current state

    gh api repos/JCarlosHidalgo/DAMA/branches/main/protection
```

- [ ] **Step 2: Commit**

```bash
git add .github/branch-protection-runbook.md
git commit -m "ci: add branch protection runbook for main"
```

---

### Task 11: Document NuGet prefix reservation in SECRETS.md

**Files:**
- Modify: `infrastructure/SECRETS.md` (append new section)

- [ ] **Step 1: Check whether the prefix is already reserved**

Open `https://www.nuget.org/packages/JuanCarlosHS.SQLDaosPackage` in a browser. Look for the blue verified prefix badge next to the package name. If present, the prefix `JuanCarlosHS.` is already reserved — record "Already reserved" in Step 3. If absent, proceed to Step 2.

- [ ] **Step 2: Open a prefix reservation request (only if not already reserved)**

Go to `https://github.com/NuGet/NuGetGallery/issues` and open a new issue using the "Package ID Prefix Reservation" template. Request the prefix `JuanCarlosHS.` for the account that owns `JuanCarlosHS.SQLDaosPackage`. Save the issue URL.

- [ ] **Step 3: Append Supply Chain section to `infrastructure/SECRETS.md`**

Add the following at the end of `infrastructure/SECRETS.md`:

```markdown
## Supply Chain

### NuGet prefix reservation — `JuanCarlosHS.`

Protects against dependency confusion attacks where a third party registers a
package under the same namespace on nuget.org.

| Item | Value |
|---|---|
| Prefix | `JuanCarlosHS.` |
| Verified on | 2026-06-23 |
| Status | [Reserved / Reservation requested — <issue URL>] |
| How to verify | Visit nuget.org/packages/JuanCarlosHS.SQLDaosPackage and check for the blue prefix badge |
```

Fill in the actual status and issue URL from Step 1/2 before committing.

- [ ] **Step 4: Commit**

```bash
git add infrastructure/SECRETS.md
git commit -m "docs: record NuGet prefix reservation status in SECRETS.md"
```

---

## Self-Review Checklist

After all tasks are committed, verify coverage against the spec:

| Spec requirement | Task |
|---|---|
| `dorny/paths-filter` and `oven-sh/setup-bun` SHA-pinned | Task 1 |
| `--locked-mode` on dotnet restore | Task 2 |
| SBOM generated per backend | Task 3 |
| Artifact checksums on Pages | Task 4 |
| Backend runtime image: `dotnet/aspnet` | Task 5 |
| Non-root user in all backends | Task 5 (USER app or gosu) |
| RabbitMQ plugin SHA256 verified | Task 6 |
| Hadolint in CI | Task 7 |
| CODEOWNERS | Task 8 |
| Dependabot for NuGet/npm/Actions | Task 9 |
| Branch protection runbook | Task 10 |
| SQLDaosPackage prefix reservation | Task 11 |
| Deferred: Docker image signing (Cosign) | — |
