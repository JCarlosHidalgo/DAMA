# CI workflows

- `ci.yml` — on every PR to `main`, detects which `apps/` area changed and runs
  build then test for it. Backends: Auth, Attendance, CourseManagement, Payment
  build + test; each tested backend also runs a critical-coverage gate
  (`infrastructure/environments-test/cobertura-backends/check-coverage.py`) that
  fails the PR if business-logic line coverage drops below 100%. Credentials
  builds only. Frontend: build (prettier + eslint + ng build) then test with a
  critical-coverage gate (`test:coverage:gate`, 100% on logic/validators/stores);
  the ESLint step also enforces the cyclomatic-complexity ceiling (≤ 20). Backend
  builds run with `-p:TreatWarningsAsErrors=true` so SonarAnalyzer warnings fail
  the PR.
  Each tested backend writes a per-service job summary (Test Files / Test
  Results) parsed from its `.trx`, mirroring the summary Vitest emits for the
  frontend; these summaries are conditional, appearing only for the backends
  that actually ran.
- `codeql.yml` — CodeQL security scan for C# and JS/TS, on PR to `main` and
  weekly.

## Manual one-time setup (GitHub UI — not versionable)

In **Settings → Branches → Branch protection rule** for `main`, mark the
relevant checks as **required**. Because jobs are conditional on the changed
area, do **not** require a specific per-area job globally; require `changes`
(always runs) and add the area jobs you care about as non-blocking, or use a
ruleset that tolerates skipped checks.
