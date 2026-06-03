# Frontend Logic-Layer Migration — Rollout Roadmap

> **For agentic workers:** execute wave-by-wave with superpowers:subagent-driven-development. Each feature follows the repeatable recipe below (TDD, no TestBed for the logic units). The pattern reference is `apps/Frontend/src/app/pages/dashboard/client/courses/` and the design spec is `docs/superpowers/specs/2026-06-03-frontend-logic-layer-pattern-design.md`.

## Context

The logic-layer pattern prototype succeeded on `courses` (pure `new`-able validators/logic + humble component + a critical-coverage gate keyed on the suffixes `*.logic.ts` / `*.validators.ts` / `*-store.ts`, with humble components excluded via `angular.json` `coverageExclude`). This roadmap applies it across the whole frontend as a dedicated program.

Decisions: **wave-based roadmap + repeatable recipe** (each feature analyzed and migrated with TDD at execution time; exact code is not pre-specified), scope = **only components with extractable logic** (display-only components are left "already humble", YAGNI). Goal: bring *logic* coverage to a gated 100% across the ~17 logic-bearing features while keeping components humble and excluded.

The gate infrastructure already supports this with no per-feature changes: the suffix rule auto-gates new logic files at 100%, and `coverageExclude` already drops all `pages/**` components. Migrating a feature = create its logic files and slim its component.

## The repeatable recipe (per feature)

Reference: `apps/Frontend/src/app/pages/dashboard/client/courses/`.

1. **Analyze** the component: find (a) inline/custom `ValidatorFn`s, (b) pure decisions (skip/continue, payload building, state branches), (c) derived values (`computed` with logic), (d) stateful orchestration with dependencies.
2. **Extract with TDD** (red test → impl → green → commit per unit):
   - `<feature>.validators.ts` (+`.spec.ts`) — `ValidatorFn`s, tested with a fake control. No TestBed.
   - `<feature>.logic.ts` (+`.spec.ts`) — pure functions returning **explicit outcome objects** (`{ kind: 'skip' } | { kind: 'create'; … }`). No TestBed.
   - **OPTIONAL** `<feature>-store.ts` (+`.spec.ts`) — class with **constructor-injected** deps, tested with `new Store(...fakes...)` (`vi.fn()` implementing the API/notifier surface). No TestBed. Only when stateful orchestration warrants it.
3. **Slim the component**: delegate to the units; keep only `injectQuery`/`injectMutation`, Material dialogs, template binding. Behavior identical.
4. **Verify**: `bunx vitest run <feature>` (direct filter; note `bun run test:ci -- <filter>` does NOT filter) → units green; `bun run test:ci` → full suite, no regression; `bun run lint` + prettier on touched `.ts`.
5. **Coverage + gate**: `bun run test:coverage` && `node scripts/check-critical-coverage.mjs` → new `.logic`/`.validators`/`-store` at 100%, gate passes.
6. **Commit** (English, no footer): `feat(frontend): extract <feature> validators/logic into pure units` + `refactor(frontend): make <feature> component humble`.

**Per-feature acceptance:** logic files at 100% (gate green), behavior preserved (existing specs pass; for features with no spec today, add specs for the new units), full suite green.

**Escape to Approach B** (documented in CLAUDE.md): for features with complex TanStack orchestration worth covering (notably `client/schedule`), that single feature may use an `inject()`-based store tested via TestBed.

## Prioritized backlog (waves)

Order = (value: low coverage + logic density) balanced with (risk: leave mega-features last). Current coverage and signals (`meth`/`computed`/`query`/`valid`) in parentheses.

**Wave 0 — done:** `client/courses` (reference).

**Wave 1 — Quick wins + unblock reuse:**
- `core/services/http-error-mapper.ts` (70%) — near-pure status mapping; extract the mapping into a testable unit.
- `shared/components/group-select` (26%, meth9/qry5/vald3) — used by the schedule pages; migrating it first unblocks them.
- `client/recharge` (80%, vald5) and `client/configuration` (87%, meth7) — small, already high; quick closers.

**Wave 2 — 0% features (biggest coverage + bug-risk):**
- `admin/tenants` (0%, meth4/vald3), `admin/subscription-plans` (0%, meth5/vald3) — whole Admin role, untested today.
- `client/subscription` (0%, meth9/cmpt2/vald2).
- `student/attendance-history` (0%, meth5).

**Wave 3 — Payment/attendance (mid coverage):**
- `client/debt-templates` (64%), `student/pay-classes` (68%), `student/debt-status` (28%), `student/mark-attendance` (84%), `teacher/schedule` (78%).

**Wave 4 — Dialogs:**
- `teacher/schedule/attendance-qr-dialog` (1%, meth9), `student/schedule/confirm-attendance-dialog` (2%).

**Wave 5 — Mega-components (highest risk, last):**
- `client/schedule` (12%, **meth25**/vald5) — largest; likely `-store` and/or Approach B.
- `student/schedule` (1%, meth12/cmpt2).

**Separate (low priority):** `core/api/attendance-realtime-service` (1%, SignalR) — framework glue; cover only what is extractable or accept low coverage (Approach-B candidate).

## Out of scope (mark "already humble / no extraction")

No logic worth extracting (YAGNI): `client/students`, `client/teachers` (table wrappers, 0 methods), `client/summary`, `student/summary` (display), the presentational `shared/components` (empty-state, error-state, stat-card, tag, page-head, paginator, qr-card, loading-skeleton, icon, course-color-chip, theme-toggle), `calendar`/`camera-scanner` (library wrappers). The remaining API clients (thin HTTP wrappers) and `dashboard.ts` (nav, already 94%) stay optional/low-priority.

## Execution model

- One **wave = one subagent-driven cycle**: a fresh implementer subagent per feature + spec & quality review, commits per feature, on a branch `feat/frontend-logic-layer-rollout-waveN`. Review between features; stop only on BLOCKED.
- After each wave: merge to `main` (fast-forward), confirm gate green and suite green.
- No push (user-managed). Commit messages in English, no footer.

## Verification (per wave and global)

From `apps/Frontend`:
1. `bun run test:ci` — full suite green (grows with each migrated feature).
2. `bun run test:coverage:gate` — gate passes with all new `*.logic.ts`/`*.validators.ts`/`*-store.ts` at 100%; migrated components absent from the report.
3. `bun run lint && bun run format:check` — clean.
4. Progress metric per wave: count of features with a 100% logic layer + delta of global statement coverage (baseline after `courses` is 77.3%).

## Notes / risks

- No per-feature gate config changes needed (suffixes + `coverageExclude` are generic).
- 0% features (Wave 2 + the no-spec ones) gain *logic* coverage; the component stays excluded. For render confidence on those untested components, add an optional Testing Library render-smoke (not gated) — per-feature decision.
- `client/schedule` (25 methods) is the biggest risk: first assess whether to split the component in addition to extracting logic; expected `-store`/Approach-B case.
- Rough sizing: ~17 features. Waves 1–4 are mostly mechanical (one implementer each); Wave 5 is the expensive one.
