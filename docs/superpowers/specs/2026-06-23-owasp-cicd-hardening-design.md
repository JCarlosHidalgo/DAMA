# OWASP CI/CD Security Hardening — Design Spec

**Date:** 2026-06-23
**Branch:** `docs/design-patterns`
**Scope:** GitHub Actions workflows, Dockerfiles, GitHub repository configuration
**Out of scope:** Docker image signing (Cosign), moving SQLDaosPackage to private feed

---

## Context

An audit of DAMA's CI/CD pipeline against the OWASP Top 10 CI/CD Security Risks
identified the following findings, grouped by type of change:

| Priority | Risk | Finding |
|---|---|---|
| High | CICD-SEC-07 | Backend Dockerfiles use `dotnet/sdk` (not `dotnet/aspnet`) as runtime image + no `USER` directive |
| Medium | CICD-SEC-03 | NuGet restore lacks `--locked-mode`; no `packages.lock.json` committed |
| Medium | CICD-SEC-03 | RabbitMQ plugin downloaded via `curl` without SHA256 verification |
| Medium | CICD-SEC-03 | `JuanCarlosHS.SQLDaosPackage` on public nuget.org without prefix reservation |
| Low | CICD-SEC-08 | `dorny/paths-filter` and `oven-sh/setup-bun` pinned to mutable version tags, not commit SHAs |
| Low | CICD-SEC-02/10 | No `CODEOWNERS`, no Dependabot |
| Low | CICD-SEC-09 | No SBOM generated; no artifact integrity checksums |
| Info | CICD-SEC-01 | Branch protection not versionable |

**Deferred:** Docker image signing with Cosign — tracked as tech debt.

---

## Approach

Remediation organized in four waves **by type of change**, so each wave touches a
single layer of the stack and can be reviewed, merged, and verified independently.

---

## Wave 0 — Preparatory: NuGet Lockfiles

**Addresses:** CICD-SEC-03 (prerequisite for Wave 1)

Wave 1 adds `--locked-mode` to `dotnet restore` in CI. This flag requires a
committed `packages.lock.json` per project. None currently exist.

### Changes

**Each of the 5 `Backend.csproj` files** (`Auth`, `Attendance`, `CourseManagement`,
`Payment`, `Credentials`) gets a new property group:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

After adding the property, run `dotnet restore` locally for each backend to generate
`packages.lock.json`. Commit all five lockfiles alongside their respective `.csproj`.

`Directory.Build.props` is explicitly **not** used — DAMA convention requires
per-backend self-contained config (see CLAUDE.md).

### Acceptance criteria

- `apps/*/Backend/packages.lock.json` exists for all 5 backends.
- `dotnet restore apps/<Svc>/Backend/Backend.csproj --locked-mode` exits 0
  for each service locally before the wave is merged.

---

## Wave 1 — Workflows (`.github/workflows/`)

**Addresses:** CICD-SEC-03 (`--locked-mode`), CICD-SEC-08 (SHA pinning),
CICD-SEC-09 (SBOM, artifact checksums)

### 1.1 Third-party action SHA pinning (`ci.yml`)

`dorny/paths-filter` and `oven-sh/setup-bun` are replaced with their exact commit
SHAs, with the version tag as an inline comment:

```yaml
- uses: dorny/paths-filter@<SHA>  # v4
- uses: oven-sh/setup-bun@<SHA>   # v2
```

SHAs are resolved at implementation time with:
```bash
gh api repos/dorny/paths-filter/git/ref/tags/v4 --jq '.object.sha'
gh api repos/oven-sh/setup-bun/git/ref/tags/v2 --jq '.object.sha'
```

GitHub first-party actions (`actions/*`, `github/codeql-action/*`) remain on
version tags — they are covered by GitHub's own signing and release process.

Dependabot (Wave 3) will open PRs to keep these SHAs current automatically.

### 1.2 `--locked-mode` in `dotnet restore` (`ci.yml`)

Both restore invocations in `build-backend` and `test-backend` add the flag:

```yaml
run: dotnet restore apps/${{ matrix.service }}/Backend/Backend.csproj --locked-mode
```

Fails the build if the dependency graph diverges from the committed lockfile.
Requires Wave 0 to be merged first.

### 1.3 SBOM generation (`ci.yml`)

New step added at the end of the `build-backend` job, after a successful build.
Uses the official `microsoft/sbom-tool` GitHub Action to produce an SPDX 2.2
SBOM per service:

```yaml
- name: Generate SBOM
  uses: microsoft/sbom-tool@<SHA>  # pinned
  with:
    BuildDropPath: apps/${{ matrix.service }}/Backend
    PackageName: DAMA.${{ matrix.service }}
    PackageVersion: ${{ github.sha }}
    PackageSupplier: DAMA-Software
  continue-on-error: true

- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: sbom-${{ matrix.service }}
    path: _manifest/spdx_2.2/
```

`continue-on-error: true` prevents SBOM failures from blocking merges in this
initial wave. Can be made strict in a follow-up once internal packages
(`DAMA.Software.*`) are confirmed to carry SPDX license metadata.

Known failure modes:
- Internal packages without SPDX license in `.nuspec` → component marked
  `NOASSERTION`, non-blocking with `continue-on-error`.
- Network timeout fetching nuget.org metadata → intermittent failure, non-blocking.
- `packages.lock.json` absent → Wave 0 prerequisite; must be merged first.

### 1.4 Artifact checksums (`pages.yml`)

After the `Extract test reports from volumes` step, a new step generates SHA256
checksums before upload:

```yaml
- name: Generate checksums
  run: |
    for svc in auth payment course-management attendance frontend; do
      find "site/tests/$svc" -type f | sort | \
        xargs sha256sum > "site/tests/$svc/checksums.sha256"
    done
```

The checksum files travel with the artifacts as integrity evidence. No change to
the deploy step.

### Acceptance criteria

- CI passes on a PR that touches a backend after Wave 0 is merged.
- `sbom-<Service>` artifacts appear in the GitHub Actions run summary.
- `checksums.sha256` files appear in the Pages artifact.
- A PR with a manually edited `packages.lock.json` fails the restore step.

---

## Wave 2 — Dockerfiles + Infrastructure

**Addresses:** CICD-SEC-07 (SDK image, root user), CICD-SEC-03 (RabbitMQ hash),
CICD-SEC-07 (Hadolint)

### 2.1 Backend runtime image: `dotnet/sdk` → `dotnet/aspnet`

Applies to all 5 backend Dockerfiles:
`auth`, `attendance`, `coursemanagement`, `payment`, `credentials`.

**Final stage before:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /webapp
COPY --from=build /webapp/out .
```

**Final stage after:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
USER app
```

Notes:
- `dotnet/aspnet:9.0` includes the `app` user (UID 1654) by default.
- `curl` is installed explicitly because `dotnet/aspnet` does not ship it and
  the existing `HEALTHCHECK` depends on it.
- `--chown=app:app` on `COPY` avoids a separate `RUN chown` layer.
- `WORKDIR` changes from `/webapp` to `/app` (Microsoft convention for aspnet
  images). No external path references `/webapp`, so this is safe.
- `payment` and `attendance` Dockerfiles also include an `entrypoint.sh` —
  the same runtime image change applies; the script runs as `app`.

### 2.2 RabbitMQ plugin: SHA256 verification

The plugin download is restructured to verify integrity before installation:

```dockerfile
RUN apk add --no-cache curl ca-certificates \
    && curl -fSL -o /tmp/plugin.ez \
        "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez" \
    && echo "<SHA256_FOR_4.1.0>  /tmp/plugin.ez" | sha256sum -c - \
    && mv /tmp/plugin.ez \
        /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez \
    && rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
```

The SHA256 for `4.1.0` is obtained at implementation time:
```bash
curl -fSL https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v4.1.0/rabbitmq_delayed_message_exchange-4.1.0.ez | sha256sum
```

The hash is hardcoded as a string constant. When `DELAYED_PLUGIN_VERSION` is bumped,
the hash line must be updated to match — this is enforced by the verification
failing the build if they diverge.

### 2.3 Hadolint in CI (`ci.yml`)

New job `lint-dockerfiles` added to `ci.yml`, running in parallel with `changes`
(no dependencies). Uses `docker run` directly to avoid adding a third-party Action:

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
          docker run --rm -i hadolint/hadolint:v2.12.0 < "$dockerfile"
        done
```

`hadolint/hadolint:v2.12.0` is a pinned Docker image tag — immutable on Docker Hub
for that digest. The `lint-dockerfiles` job is added to the `ci-gate` required checks.

Known suppression: `DL3008` (apt package version pinning) may be suppressed inline
with `# hadolint ignore=DL3008` on the `apt-get install` line in backend Dockerfiles
if pinning apt package versions is not desired.

### Acceptance criteria

- `docker run --rm <image> dotnet Backend.dll` starts as UID 1654 (not 0).
- Image size for Auth backend reduces from ~830 MB (SDK) to ~220 MB (aspnet).
- RabbitMQ build fails if the plugin `.ez` file is corrupted or the hash line is wrong.
- A PR introducing a Dockerfile with `ADD` instead of `COPY` is rejected by Hadolint.

---

## Wave 3 — GitHub Configuration + SQLDaosPackage

**Addresses:** CICD-SEC-02 (CODEOWNERS), CICD-SEC-10 (Dependabot),
CICD-SEC-01 (branch protection runbook), CICD-SEC-03 (package prefix reservation)

### 3.1 CODEOWNERS (`.github/CODEOWNERS`)

Establishes ownership per area. Currently a single-developer project; structure
is forward-compatible with future collaborators:

```
*                    @JCarlosHidalgo
/apps/Auth/          @JCarlosHidalgo
/apps/Attendance/    @JCarlosHidalgo
/apps/CourseManagement/ @JCarlosHidalgo
/apps/Payment/       @JCarlosHidalgo
/apps/Credentials/   @JCarlosHidalgo
/apps/Frontend/      @JCarlosHidalgo
/infrastructure/     @JCarlosHidalgo
/.github/            @JCarlosHidalgo
/grpc-contracts/     @JCarlosHidalgo
/packages/           @JCarlosHidalgo
```

Activates the "Require review from Code Owners" checkbox in branch protection
(configured in step 3.3).

### 3.2 Dependabot (`.github/dependabot.yml`)

Configured for three ecosystems:

- **`github-actions`** — keeps Action SHAs (pinned in Wave 1) current via weekly PRs.
- **`nuget`** — one entry per backend project and per package in `packages/`
  (8 entries total). Dependabot does not support glob directories for NuGet.
- **`npm`** — frontend `package.json`. Dependabot uses the npm ecosystem even
  though the runtime is Bun. After each Dependabot PR, `bun install` must be
  run locally to update `bun.lock` before merging. This limitation is documented
  in the `dependabot.yml` file with a comment.

All entries use `schedule: interval: weekly` and carry `labels` for triaging
(`dependencies`, `backend`/`frontend`/`ci`/`packages`).

### 3.3 Branch protection runbook (`.github/branch-protection-runbook.md`)

Branch protection is not versionable as a repo file. This runbook documents the
required configuration and the equivalent GitHub CLI command, making setup
reproducible and auditable via git history:

Required settings for `main`:
- Require a pull request before merging (1 approving review)
- Require review from Code Owners
- Require status checks: `changes`, `ci-gate`
- Require branches to be up to date before merging
- Do not allow bypassing the above settings (enforce for admins)

The runbook includes the `gh api` invocation to apply the configuration from the CLI.

### 3.4 SQLDaosPackage prefix reservation (manual, documented in `SECRETS.md`)

nuget.org offers Package ID Prefix Reservation: the verified owner of a package
can reserve a namespace prefix so no other account can publish under it.

Steps (manual, one-time):
1. Confirm `nuget.org/packages/JuanCarlosHS.SQLDaosPackage` shows the correct
   owner account and has the verified prefix badge (blue checkmark).
2. If not verified, open a prefix reservation request at
   `github.com/NuGet/NuGetGallery` using the issue template for
   "Package ID Prefix Reservation", requesting the prefix `JuanCarlosHS.`.
3. Record the outcome (date, ticket URL, status) in `infrastructure/SECRETS.md`
   under a new "Supply Chain" section.

No files are generated in the repo beyond the `SECRETS.md` update.

### Acceptance criteria

- A test PR that touches `/.github/` shows `@JCarlosHidalgo` as required reviewer.
- Dependabot opens its first batch of PRs within one week of the config being merged.
- `infrastructure/SECRETS.md` contains a "Supply Chain" section with the
  NuGet prefix reservation status.
- `.github/branch-protection-runbook.md` exists and the `gh api` command in it
  executes without error.

---

## Tech Debt Deferred

| Item | Reason |
|---|---|
| Docker image signing (Cosign) | Requires key management infrastructure not yet in place |
| SBOM made strict (`continue-on-error: false`) | Pending SPDX metadata on internal `DAMA.Software.*` packages |
| Dependabot auto-merge for patch versions | Out of scope; evaluate after Dependabot is active for one month |

---

## Wave Dependency Graph

```
Wave 0 (lockfiles)
    └── Wave 1 (workflows) — requires Wave 0 merged before --locked-mode activates
Wave 2 (Dockerfiles) — independent of Wave 0/1, can run in parallel
Wave 3 (GitHub config) — independent of all, can run in parallel with Wave 2
```

Wave 0 → Wave 1 is a hard dependency. Waves 2 and 3 are independent of each other
and of Wave 1, and can be worked in any order or in parallel.
