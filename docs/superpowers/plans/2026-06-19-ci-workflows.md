# CI Workflows (per-area build + test) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add GitHub Actions CI that, on every PR to `main`, builds and tests only the `apps/` area that changed (a backend or the frontend), plus CodeQL security scanning.

**Architecture:** A single `.github/workflows/ci.yml` whose first job (`changes`) uses `dorny/paths-filter` to compute which areas changed and emits JSON matrices. Backend build/test jobs fan out over those matrices; the frontend has its own guarded jobs. Test jobs declare `needs:` on their build job so test only runs after build passes. A separate `.github/workflows/codeql.yml` runs CodeQL for C# and JS/TS.

**Tech Stack:** GitHub Actions, `actions/setup-dotnet` (.NET 9), `oven-sh/setup-bun` (Bun 1.3.10), `dorny/paths-filter@v3`, `github/codeql-action@v3`.

## Global Constraints

- Runner: `ubuntu-latest`, native toolchains (no reuse of `environments-test/` Dockerfiles).
- Trigger: `pull_request` to `main`, types `[opened, synchronize, reopened]`.
- Backends: `Auth`, `Attendance`, `CourseManagement`, `Payment` build **and** test; `Credentials` builds **only** (no test project).
- Backend build is strict: `-p:TreatWarningsAsErrors=true` so `SonarAnalyzer.CSharp` warnings fail the PR. Backend test jobs do **not** set that flag.
- `.editorconfig` is enforced via `dotnet format <csproj> --verify-no-changes`.
- Frontend uses Bun (`bun.lock`, packageManager `bun@1.3.10`); commands: `bun run format:check`, `bun run lint`, `bun run build`, `bun run test:coverage`.
- Backend tests use the per-service `apps/<Svc>/Test/.runsettings` (business-logic-only coverage).
- Concurrency: `group: ci-${{ github.ref }}`, `cancel-in-progress: true`.
- No source comments in any committed file other than YAML `name:`/keys (these are workflow files, not repo source — YAML has no `//`/`///` rules; keep them clean and self-documenting anyway).
- Commit policy: **do not commit per task**. A single commit is made only in the final task, in English, no footer/trailer, no push.
- Paths under `packages/`, `grpc-contracts/`, `infrastructure/`, or repo root do **not** trigger backend/frontend jobs (libs consumed as published NuGet).

---

### Task 1: Scaffold `ci.yml` with triggers, concurrency, and the `changes` job

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Produces (job outputs consumed by later tasks):
  - `needs.changes.outputs.backends` — JSON array of changed backend names, e.g. `["Auth","Payment"]` or `[]`.
  - `needs.changes.outputs.backends_testable` — same, excluding `Credentials`.
  - `needs.changes.outputs.frontend` — string `'true'` / `'false'`.

- [ ] **Step 1: Verify there is no existing `.github/`**

Run: `ls -la /home/juan/projects/DAMA/.github 2>/dev/null || echo "absent"`
Expected: `absent`

- [ ] **Step 2: Create `.github/workflows/ci.yml` with the header + `changes` job**

```yaml
name: CI

on:
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]

permissions:
  contents: read

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  changes:
    runs-on: ubuntu-latest
    outputs:
      backends: ${{ steps.set.outputs.backends }}
      backends_testable: ${{ steps.set.outputs.backends_testable }}
      frontend: ${{ steps.filter.outputs.frontend }}
    steps:
      - uses: actions/checkout@v4

      - uses: dorny/paths-filter@v3
        id: filter
        with:
          filters: |
            auth: 'apps/Auth/**'
            attendance: 'apps/Attendance/**'
            coursemanagement: 'apps/CourseManagement/**'
            payment: 'apps/Payment/**'
            credentials: 'apps/Credentials/**'
            frontend: 'apps/Frontend/**'

      - id: set
        run: |
          set -euo pipefail
          declare -A flags=(
            [Auth]='${{ steps.filter.outputs.auth }}'
            [Attendance]='${{ steps.filter.outputs.attendance }}'
            [CourseManagement]='${{ steps.filter.outputs.coursemanagement }}'
            [Payment]='${{ steps.filter.outputs.payment }}'
            [Credentials]='${{ steps.filter.outputs.credentials }}'
          )
          backends=()
          testable=()
          for svc in Auth Attendance CourseManagement Payment Credentials; do
            if [ "${flags[$svc]}" = "true" ]; then
              backends+=("$svc")
              if [ "$svc" != "Credentials" ]; then
                testable+=("$svc")
              fi
            fi
          done
          emit() {
            local name="$1"; shift
            if [ "$#" -eq 0 ]; then
              echo "$name=[]" >> "$GITHUB_OUTPUT"
            else
              echo "$name=$(printf '%s\n' "$@" | jq -R . | jq -cs .)" >> "$GITHUB_OUTPUT"
            fi
          }
          emit backends "${backends[@]+"${backends[@]}"}"
          emit backends_testable "${testable[@]+"${testable[@]}"}"
```

- [ ] **Step 3: Validate YAML parses**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 4: Lint the workflow with actionlint (Docker)**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml`
Expected: no output, exit code 0. (If Docker is unavailable in this environment, skip and rely on Step 3 + the live PR verification in Task 7; note the skip.)

---

### Task 2: Add the backend build job (matrix over changed backends)

**Files:**
- Modify: `.github/workflows/ci.yml` (append `build-backend` job under `jobs:`)

**Interfaces:**
- Consumes: `needs.changes.outputs.backends` (JSON array).
- Produces: job `build-backend` (its success gates `test-backend` in Task 4).

- [ ] **Step 1: Append the `build-backend` job**

```yaml
  build-backend:
    needs: changes
    if: ${{ needs.changes.outputs.backends != '[]' }}
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        service: ${{ fromJSON(needs.changes.outputs.backends) }}
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ matrix.service }}-${{ hashFiles(format('apps/{0}/Backend/Backend.csproj', matrix.service)) }}
          restore-keys: |
            nuget-${{ runner.os }}-${{ matrix.service }}-
            nuget-${{ runner.os }}-

      - name: Restore
        run: dotnet restore apps/${{ matrix.service }}/Backend/Backend.csproj

      - name: Verify formatting (.editorconfig + analyzers)
        run: dotnet format apps/${{ matrix.service }}/Backend/Backend.csproj --verify-no-changes --no-restore

      - name: Build (warnings as errors → SonarAnalyzer strict)
        run: dotnet build apps/${{ matrix.service }}/Backend/Backend.csproj -c Release --no-restore -p:TreatWarningsAsErrors=true
```

- [ ] **Step 2: Validate YAML parses**

Run: `python3 -c "import yaml; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 3: Confirm `fromJSON` matrix wiring is well-formed**

Run: `python3 -c "import yaml; d=yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); j=d['jobs']['build-backend']; assert j['needs']=='changes'; assert 'fromJSON(needs.changes.outputs.backends)' in j['strategy']['matrix']['service']; print('ok')"`
Expected: `ok`

- [ ] **Step 4: Lint with actionlint (same command as Task 1 Step 4)**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml`
Expected: exit 0 (or skip if Docker unavailable, noting it).

---

### Task 3: Add the frontend build job

**Files:**
- Modify: `.github/workflows/ci.yml` (append `build-frontend` job)

**Interfaces:**
- Consumes: `needs.changes.outputs.frontend` (`'true'`/`'false'`).
- Produces: job `build-frontend` (gates `test-frontend` in Task 5).

- [ ] **Step 1: Append the `build-frontend` job**

```yaml
  build-frontend:
    needs: changes
    if: ${{ needs.changes.outputs.frontend == 'true' }}
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: apps/Frontend
    steps:
      - uses: actions/checkout@v4

      - uses: oven-sh/setup-bun@v2
        with:
          bun-version: '1.3.10'

      - uses: actions/cache@v4
        with:
          path: ~/.bun/install/cache
          key: bun-${{ runner.os }}-${{ hashFiles('apps/Frontend/bun.lock') }}
          restore-keys: |
            bun-${{ runner.os }}-

      - name: Install
        run: bun install --frozen-lockfile

      - name: Prettier check
        run: bun run format:check

      - name: ESLint
        run: bun run lint

      - name: Build
        run: bun run build
```

- [ ] **Step 2: Validate YAML parses**

Run: `python3 -c "import yaml; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 3: Lint with actionlint**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml`
Expected: exit 0 (or skip if Docker unavailable, noting it).

---

### Task 4: Add the backend test job (gated on `build-backend`)

**Files:**
- Modify: `.github/workflows/ci.yml` (append `test-backend` job)

**Interfaces:**
- Consumes: `needs.changes.outputs.backends_testable` (JSON array, excludes Credentials) and the `build-backend` job (gate).
- Produces: job `test-backend`.

**Note on gate granularity:** `needs: build-backend` is job-level. If two backends changed and one's build fails, the whole `build-backend` job fails and `test-backend` is skipped for both. This is the accepted, simpler "test depends on build" behavior; per-service isolation is out of scope.

- [ ] **Step 1: Append the `test-backend` job**

```yaml
  test-backend:
    needs: [changes, build-backend]
    if: ${{ needs.changes.outputs.backends_testable != '[]' }}
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        service: ${{ fromJSON(needs.changes.outputs.backends_testable) }}
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ matrix.service }}-${{ hashFiles(format('apps/{0}/Backend/Backend.csproj', matrix.service)) }}
          restore-keys: |
            nuget-${{ runner.os }}-${{ matrix.service }}-
            nuget-${{ runner.os }}-

      - name: Restore test project
        run: dotnet restore apps/${{ matrix.service }}/Test/Test.csproj

      - name: Test
        run: >
          dotnet test apps/${{ matrix.service }}/Test/Test.csproj
          --no-restore
          --settings apps/${{ matrix.service }}/Test/.runsettings
          --logger "trx;LogFileName=test-results.trx"
          --results-directory apps/${{ matrix.service }}/Test/TestResults

      - name: Upload test results
        if: ${{ always() }}
        uses: actions/upload-artifact@v4
        with:
          name: test-results-${{ matrix.service }}
          path: apps/${{ matrix.service }}/Test/TestResults/**/*.trx
          if-no-files-found: warn
```

- [ ] **Step 2: Validate YAML parses**

Run: `python3 -c "import yaml; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 3: Confirm the build→test gate is wired**

Run: `python3 -c "import yaml; d=yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); n=d['jobs']['test-backend']['needs']; assert 'build-backend' in n and 'changes' in n, n; print('ok')"`
Expected: `ok`

- [ ] **Step 4: Lint with actionlint**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml`
Expected: exit 0 (or skip if Docker unavailable, noting it).

---

### Task 5: Add the frontend test job (gated on `build-frontend`)

**Files:**
- Modify: `.github/workflows/ci.yml` (append `test-frontend` job)

**Interfaces:**
- Consumes: `needs.changes.outputs.frontend` and the `build-frontend` job (gate).
- Produces: job `test-frontend`.

- [ ] **Step 1: Append the `test-frontend` job**

```yaml
  test-frontend:
    needs: [changes, build-frontend]
    if: ${{ needs.changes.outputs.frontend == 'true' }}
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: apps/Frontend
    steps:
      - uses: actions/checkout@v4

      - uses: oven-sh/setup-bun@v2
        with:
          bun-version: '1.3.10'

      - uses: actions/cache@v4
        with:
          path: ~/.bun/install/cache
          key: bun-${{ runner.os }}-${{ hashFiles('apps/Frontend/bun.lock') }}
          restore-keys: |
            bun-${{ runner.os }}-

      - name: Install
        run: bun install --frozen-lockfile

      - name: Test (with coverage)
        run: bun run test:coverage

      - name: Upload coverage
        if: ${{ always() }}
        uses: actions/upload-artifact@v4
        with:
          name: frontend-coverage
          path: apps/Frontend/coverage/**
          if-no-files-found: warn
```

- [ ] **Step 2: Validate YAML parses**

Run: `python3 -c "import yaml; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/ci.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 3: Lint with actionlint**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml`
Expected: exit 0 (or skip if Docker unavailable, noting it).

---

### Task 6: Create the CodeQL workflow

**Files:**
- Create: `.github/workflows/codeql.yml`

**Interfaces:**
- Independent of `ci.yml`. Uses `build-mode: none` for both languages to avoid compiling the no-`.sln`, NuGet-fed backends.

- [ ] **Step 1: Create `.github/workflows/codeql.yml`**

```yaml
name: CodeQL

on:
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 3 * * 1'

permissions:
  contents: read
  security-events: write
  actions: read

concurrency:
  group: codeql-${{ github.ref }}
  cancel-in-progress: true

jobs:
  analyze:
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        include:
          - language: csharp
            build-mode: none
          - language: javascript-typescript
            build-mode: none
    steps:
      - uses: actions/checkout@v4

      - uses: github/codeql-action/init@v3
        with:
          languages: ${{ matrix.language }}
          build-mode: ${{ matrix.build-mode }}

      - uses: github/codeql-action/analyze@v3
        with:
          category: "/language:${{ matrix.language }}"
```

- [ ] **Step 2: Validate YAML parses**

Run: `python3 -c "import yaml; yaml.safe_load(open('/home/juan/projects/DAMA/.github/workflows/codeql.yml')); print('ok')"`
Expected: `ok`

- [ ] **Step 3: Lint with actionlint**

Run: `docker run --rm -v /home/juan/projects/DAMA:/repo -w /repo rhysd/actionlint:latest -color .github/workflows/codeql.yml`
Expected: exit 0 (or skip if Docker unavailable, noting it).

---

### Task 7: Document the manual GitHub setup, single commit, and live PR verification

**Files:**
- Create: `.github/workflows/README.md`

**Interfaces:** none (docs + verification only).

- [ ] **Step 1: Create `.github/workflows/README.md`**

```markdown
# CI workflows

- `ci.yml` — on every PR to `main`, detects which `apps/` area changed and runs
  build then test for it. Backends: Auth, Attendance, CourseManagement, Payment
  build + test; Credentials builds only. Frontend: build (prettier + eslint +
  ng build) then test (ng test --coverage). Backend builds run with
  `-p:TreatWarningsAsErrors=true` so SonarAnalyzer warnings fail the PR.
- `codeql.yml` — CodeQL security scan for C# and JS/TS, on PR to `main` and
  weekly.

## Manual one-time setup (GitHub UI — not versionable)

In **Settings → Branches → Branch protection rule** for `main`, mark the
relevant checks as **required**. Because jobs are conditional on the changed
area, do **not** require a specific per-area job globally; require `changes`
(always runs) and add the area jobs you care about as non-blocking, or use a
ruleset that tolerates skipped checks.
```

- [ ] **Step 2: Confirm the full file tree**

Run: `find /home/juan/projects/DAMA/.github -type f | sort`
Expected:
```
/home/juan/projects/DAMA/.github/workflows/README.md
/home/juan/projects/DAMA/.github/workflows/ci.yml
/home/juan/projects/DAMA/.github/workflows/codeql.yml
```

- [ ] **Step 3: Single commit (English, no footer, no push)**

```bash
cd /home/juan/projects/DAMA
git checkout -b ci/github-actions-workflows
git add .github/workflows/ci.yml .github/workflows/codeql.yml .github/workflows/README.md docs/superpowers/specs/2026-06-19-ci-workflows-design.md docs/superpowers/plans/2026-06-19-ci-workflows.md
git commit -m "ci: add per-area build/test workflows and CodeQL"
```
Expected: one commit on branch `ci/github-actions-workflows`. (Per repo policy: no push — the user pushes/opens the PR.)

- [ ] **Step 4: Live verification (manual, by the user)**

Push the branch and open a PR to `main`. Then verify:
- A PR touching only `apps/Auth/**` runs `build-backend (Auth)` then `test-backend (Auth)`, and no other area's jobs.
- A PR touching only `apps/Credentials/**` runs `build-backend (Credentials)` and **no** test job.
- A PR touching only `apps/Frontend/**` runs `build-frontend` then `test-frontend`.
- Introduce a deliberate Sonar/style warning in a touched backend → the build job fails.
- Push a second commit to the open PR → the previous run is cancelled.
- `CodeQL` runs for both `csharp` and `javascript-typescript`.

---

## Self-Review

**Spec coverage:**
- Per-PR, per-area build + test → Tasks 1–5. ✓
- Backend build includes editorconfig + Sonar → Task 2 (`dotnet format --verify-no-changes` + `-p:TreatWarningsAsErrors=true`). ✓
- Frontend analogues → Task 3 (`format:check` + `lint` + `build`). ✓
- Test = suite of the affected service → Tasks 4–5. ✓
- Credentials build-only → `backends_testable` excludes it (Task 1) and Task 4 matrix uses it. ✓
- Test depends on build → `needs:` in Tasks 4–5. ✓
- Unified `ci.yml` → Tasks 1–5. ✓
- Sonar strict (`-warnaserror`) → Task 2. ✓
- Concurrency cancel → Task 1 header. ✓
- CodeQL (C# + JS/TS) → Task 6. ✓
- packages/grpc/infra excluded from triggers → only `apps/<area>/**` filters in Task 1. ✓

**Placeholder scan:** none — every step has concrete commands/YAML.

**Type consistency:** output names `backends` / `backends_testable` / `frontend` defined in Task 1 are used verbatim in Tasks 2–5. `build-backend` (Task 2) is the `needs` target in Task 4; `build-frontend` (Task 3) in Task 5. Cache keys use `format('apps/{0}/Backend/Backend.csproj', matrix.service)` consistently in Tasks 2 and 4.
