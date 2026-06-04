# ISO Conformance Assessment

This document records how the DAMA platform aligns with the ISO/IEC standards relevant to a
multi-service web application, and — for the highest-impact gaps — the concrete, file-level work
required to reach full conformance.

- **Assessed:** 2026-06-03
- **Method:** full-repository scan (security/data, quality/process, and concrete technical standards),
  cross-checked against the actual source rather than documentation.
- **Audience:** maintainers deciding where to invest to raise the conformance level.

## Scope note — what "conformance" means here

Two different kinds of ISO standard appear below, and they cannot be judged the same way:

- **Concrete technical standards** (ISO 8601, ISO 4217, ISO 3166, ISO 639, ISO/IEC 40500/WCAG,
  ISO/IEC 25012) describe things code can literally implement. These get a binary verdict:
  **Full / Partial / Absent**.
- **Management-system standards** (ISO/IEC 27001, ISO/IEC 27002, ISO 9001, ISO/IEC 12207) are
  *organizational certifications*. They require policies, a risk register, audits, and traceability
  evidence that live outside a codebase. Code can only demonstrate **technical alignment** with their
  controls — never "100% compliance." These are marked **Aligned\*** and can never reach a green
  "Full" from code alone.

## Conformance matrix

| Standard | Scope | Verdict | Basis |
|---|---|---|---|
| **ISO/IEC 25012** | Data quality | ✅ Full | FK/UK/PK/NOT NULL across every `init.sql`; FluentValidation global filter in all 4 real backends; Angular validators with a 100% coverage gate; soft-deletes + outbox/processed-event ledgers. |
| **ISO/IEC 25010** | Product quality model | 🟡 Partial | Strong on Security, Maintainability, Reliability, Portability, Compatibility; weak on Usability (accessibility); Performance efficiency unmeasured. |
| **ISO/IEC 5055** | Source-code quality measures | 🟡 Partial | SonarLint + analyzers (5 backends), EditorConfig, ESLint + Prettier + TS strict — but no automated measurement against the four 5055 categories. |
| **ISO/IEC 27001 / 27002** | Information security (ISMS) | 🟡 Aligned\* | Technical controls strong (JWT RS256, AES-256-GCM at rest, inter-service gRPC TLS, fail-fast secrets, gateway rate-limiting, tenant-scoped queries). No ISMS, risk register, or audits. |
| **ISO/IEC 27017 / 27018 / 27701** | Cloud security / privacy | 🟡 Aligned\* | Encryption + secret hygiene present; no DPA, no right-to-be-forgotten, no formal privacy program. |
| **ISO/IEC 12207** | Software lifecycle | 🟡 Aligned\* | ~600+ backend tests + frontend specs, coverage tooling, thorough docs. **No CI/CD**, Credentials untested, no requirements↔test traceability. |
| **ISO 9001** | Quality management | 🟡 Aligned\* | Documented conventions, config management, named-test traceability. No formal QMS, audits, or change-control gate. |
| **ISO 8601** | Date / time format | 🟡 Partial | Storage strong (`DATETIME(6)` + IANA tenant timezone via `TimeZoneInfo`); transmission relies on .NET defaults with no explicit JSON config; Todotix boundary uses a non-ISO format. |
| **ISO/IEC 40500 (WCAG 2.0)** | Web accessibility | 🟡 Partial | ARIA labels + semantic HTML on key screens; no a11y lint, no contrast audit, icons lack accessible names, no CDK a11y utilities. |
| **ISO 4217** | Currency codes | ❌ Absent | Monetary amounts are bare `int`; no currency field anywhere. |
| **ISO 3166** | Country codes | ❌ Absent | Not modeled. |
| **ISO 639 / i18n** | Language codes / localization | ❌ Absent | Single-language Spanish, hardcoded; no i18n library or locale registration. |

\* **Aligned** = technical controls are in place, but the standard certifies an organizational
management system that cannot be satisfied by code. See the closing section.

---

## High-impact remediation

Ordered by impact-to-effort. Each section states the current state with evidence, the gap, the
ordered steps citing real files, and a definition of done. No code is changed by this document.

### 1. ISO 4217 — Currency codes (❌ → ✅) — cheapest full close

**Current state.** Money is stored as a unitless integer with no currency attached:
- `apps/Payment/Backend/Entities/DebtTemplates/DebtTemplate.cs:21` → `public int Cost { get; set; }`
- `apps/Payment/Backend/Entities/QrPayments/PendingQrPayment.cs:24` → `public int Cost { get; set; }`
- `infrastructure/environments/payment/init.sql` stores `Cost INT NOT NULL`.
- The Todotix integration sends a `Decimal? MontoTotal` with no currency code.

The currency is implicitly the tenant's local currency, never recorded.

**Gap to 100%.** Every monetary amount must carry an explicit ISO 4217 alphabetic code (e.g.
`PEN`, `BOB`, `USD`), validated on input.

**Actionable steps.**
1. Add a `Currency` field (3-char ISO 4217 code) to the money-bearing entities — `DebtTemplate`,
   `PendingQrPayment` — and to the request/response DTOs under `apps/Payment/Backend/Dtos/`.
2. Add the `Currency` column to `infrastructure/environments/payment/init.sql` (and the prod
   init runbook); a schema change in dev requires `down -v` + re-init.
3. Validate the code against an allowed set in the relevant FluentValidation validators under
   `apps/Payment/Backend/Validators/QrPayments/` (reject unknown codes with a 400).
4. Decide and document the minor-unit convention for `Cost` (smallest unit vs. major unit) so the
   integer amount is unambiguous alongside the currency.
5. Map `Currency` through the Todotix builders (`apps/Payment/Backend/Builders/QrPaymentCreationBuilder.cs`)
   and surface it in API responses.

**Definition of done.** No monetary value exists in the schema, DTOs, or API without an associated,
validated ISO 4217 code; a payment in a disallowed currency is rejected at the boundary.

### 2. CI/CD automation (ISO/IEC 12207 / ISO 9001 — 🟡 → materially stronger)

**Current state.** There is **no CI/CD pipeline** — no `.github/`, `.gitlab-ci.yml`, or `Jenkinsfile`.
The test infrastructure exists but runs only on demand: `infrastructure/compose.test.yaml` runs the
four backend suites + the frontend, and four `apps/*/Test/.runsettings` plus the frontend gate
`apps/Frontend/scripts/check-critical-coverage.mjs` (npm script `test:coverage:gate`) measure coverage.
Nothing enforces them before a merge to `main`.

**Gap to 100%.** Quality gates must run automatically and block integration on failure — the
verification step that lifts 12207 (QA process) and 9001 (change control) from "documented" to
"enforced."

**Actionable steps.**
1. Add a pipeline (GitHub Actions assumed) triggered on PRs to `main`, mirroring what
   `infrastructure/compose.test.yaml` already does.
2. Stages: `dotnet format --verify-no-changes` per backend; the four backend test suites with their
   `.runsettings` coverage; the frontend `bun run test:coverage:gate`.
3. Make the pipeline a required status check on `main` so no merge lands red.
4. Once Credentials has a suite (section 5), add it to the matrix.

**Definition of done.** A PR cannot merge to `main` without green formatting, all backend + frontend
tests, and the coverage gate.

### 3. ISO/IEC 40500 / WCAG 2.0 — Accessibility (🟡 → ✅)

**Current state.** Interactive screens carry ARIA and semantic HTML — `aria-label`/`aria-pressed` in
`apps/Frontend/src/app/pages/login/login.html` and `.../dashboard/dashboard.html`, `<mat-label>`
associations, and the documented `autoFocus="dialog"` sidenav fix. But there is **no a11y lint** in
`apps/Frontend/eslint.config.js`, the icon component `apps/Frontend/src/app/shared/components/icon/icon.ts`
renders `<fa-icon>` with no accessible name, there is no contrast audit of the OKLCH tokens in
`apps/Frontend/src/styles.css`, and no `@angular/cdk/a11y` focus utilities are used.

**Gap to 100%.** Accessibility must be enforced (lint) and verified (contrast), not just present ad hoc.

**Actionable steps.**
1. Enable `angular-eslint` template-accessibility rules in `apps/Frontend/eslint.config.js` and fix
   the violations they surface.
2. Give `<fa-icon>` an accessible-name strategy in `shared/components/icon/icon.ts` (decorative icons
   hidden from the a11y tree; meaningful icons labeled).
3. Run and document a WCAG AA contrast audit against the `--dama-*` tokens in `styles.css`.
4. Adopt `@angular/cdk/a11y` (focus trap / focus monitor) where dialogs and overlay menus need it.

**Definition of done.** The a11y lint rules pass repo-wide and a documented contrast audit shows AA
compliance for text tokens.

### 4. ISO 8601 — Date / time on the wire (🟡 → ✅)

**Current state.** Storage and timezone math are solid (`DATETIME(6)`, IANA `Tenant.Timezone` via
`TimeZoneInfo`). But entities use `DateTime` (not `DateTimeOffset`), and **no explicit HTTP JSON
serialization is configured** — `JsonSerializerOptions` is set only inside the RabbitMQ dispatchers
(`apps/*/Backend/Workers/Infrastructure/RabbitMqMessageDispatcher.cs`), so API responses depend on
the .NET 9 `System.Text.Json` defaults, which only guarantee ISO 8601 + `Z` when `DateTime.Kind` is
`Utc`. The Todotix boundary uses `"yyyy-MM-dd HH:mm"`, which is not ISO 8601.

**Gap to 100%.** API timestamps must be ISO 8601 UTC by guarantee, not by default behavior.

**Actionable steps.**
1. Standardize money/time-bearing entities on `DateTimeOffset` (or enforce `DateTimeKind.Utc`) so
   serialization is unambiguous.
2. Add explicit `System.Text.Json` options for HTTP in each `apps/*/Backend/Modules/MvcModule.cs`
   (or a shared module) to guarantee round-trip ISO 8601 output.
3. Document the Todotix `"yyyy-MM-dd HH:mm"` format as a deliberate external-contract exception, not
   a deviation, in the Payment integration notes.

**Definition of done.** Every timestamp in an API response serializes as ISO 8601 UTC by explicit
configuration; the only non-ISO format is the documented Todotix boundary.

### 5. Credentials test coverage (ISO/IEC 12207 testing / ISO/IEC 25010 — ❌ → ✅ for the service)

**Current state.** `apps/Credentials/` contains only `Backend` — there is **no `Test/` project**, so
the service has zero automated coverage. It is a stateless claims-reflection adapter, but interface
changes go uncaught.

**Gap to 100%.** The service needs a real suite, included in the test runner and CI.

**Actionable steps.**
1. Create `apps/Credentials/Test/` mirroring a sibling test project (e.g. `apps/Auth/Test/`), including
   a `.runsettings` for coverage.
2. Cover the `Infrastructure/Claims/` helpers and the claim-reflection endpoint.
3. Add the Credentials runner to `infrastructure/compose.test.yaml` and to the CI matrix (section 2).

**Definition of done.** Credentials has a real NUnit suite producing a coverage report, run in CI.

---

## Out of reach by code alone

The **Aligned\*** standards in the matrix cannot reach a green "Full" by editing this repository —
they certify an organization, not a codebase. Closing them is organizational follow-up work:

- **ISO/IEC 27001 / 27002** — establish an ISMS: security policy, risk register and treatment plan,
  access-review cadence, incident-response runbook, and recurring internal audits. Per-account lockout
  and a formal key-rotation schedule would also harden the already-strong technical controls.
- **ISO 9001** — a documented quality management system with management review, corrective-action
  tracking, and a change-control board (the CI gate in section 2 is the technical half of this).
- **ISO/IEC 12207** — a requirements↔design↔test **traceability matrix** linking user stories to the
  tests that verify them, plus formally documented lifecycle process definitions.

These are listed for completeness; none is a code change, and all are out of scope for the high-impact
remediation above.
