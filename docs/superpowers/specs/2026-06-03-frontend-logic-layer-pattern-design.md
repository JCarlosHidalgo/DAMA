# Frontend logic-layer pattern — design

**Date:** 2026-06-03
**Status:** Approved design (pending spec review)
**Scope:** A reusable, canonical pattern for the Angular frontend; applied feature-by-feature, not a big-bang migration.

## Context

The backend reaches ~100% coverage of its logical functions because of its **architecture**, not its tests. A service like `Attendance/Backend/Services/Concrete/Attendance/ScheduledClassService.cs` is a plain class whose dependencies are abstractions (`IDao`, `IClaimContext`, gRPC client, `IMapper`, `IOptions`). Its test (`ScheduledClassServiceTests.cs`) does `new ScheduledClassService(...mocks...)` with Moq `MockBehavior.Strict` — no DI container, no MySQL, no gRPC, no HTTP. The logic lives outside the framework and depends only on ports, so every branch is a cheap unit test.

The frontend does not get this for free. In `apps/Frontend/src/app/pages/dashboard/client/courses/courses.ts` the decision logic is fused into the Angular component: `injectQuery`/`injectMutation`, `FormBuilder`, an inline `hasDot` validator, `MatDialog`, `QueryClient`. Testing `onEdit`'s "skip if name unchanged/empty" rule requires `TestBed` + a fake `HttpClient` + the DOM — i.e. testing *through* the infrastructure. That is why `courses.ts` sits at ~22% line coverage while the pure `core/utils/*` files sit at 100%.

The frontend already proves the approach works: `core/utils/*` (qr-payload, schedule-normalize, tenant-time, course-color, tab-state, qr-debt-polling) are at 100% precisely because they are framework-free. This pattern formalizes and grows that layer.

**Today's baseline:** the only plain logic class is `core/utils/tab-state.ts`; there is no store/facade convention; 4 components use TanStack Query inline.

## Goal and decisions

Design the **canonical pattern** (logic layer + how to test it + how it lands in the coverage gate) to apply feature-by-feature. Locked decisions:

1. **No new dependencies.** Hand-rolled with signals, plain classes, pure functions, and the existing `@tanstack/angular-query-experimental`. No NgRx.
2. **Architecture = Approach A:** logic units are `new`-able via **constructor injection**, tested with `new Unit(fakes)` and zero `TestBed` — the closest mirror of `new Service(mocks)`. This leans on the `Frontend/CLAUDE.md`-sanctioned exception ("`inject()` for DI … Exception: when an external test or factory genuinely needs to pass dependencies positionally").
3. **Gate mirrors the backend:** the logic layer is enforced at 100%; humble component files are excluded from coverage. "100% of the logic, ignore the wiring" — just as the backend does not unit-test controllers.
4. **The store is optional.** Simple CRUD features may need only `validators` + `logic`.
5. **Approach A's boundary is accepted, with an escape hatch to B.** TanStack orchestration stays in the excluded component and is not covered. If a feature's orchestration is genuinely complex and worth covering, that specific feature may use an `inject()`-based store tested via `TestBed` (Approach B). B is the justified exception, not the default.

## Architecture: the three roles

Per feature, logic is split into up to three co-located files, each with one role and one test style.

### 1. `<feature>.validators.ts` — pure `ValidatorFn`s
Named, exported `ValidatorFn`s (e.g. `noDotValidator`, length rules) replacing inline validators in form groups.
- **Backend analog:** `*Validator` (FluentValidation).
- **Test:** call the `ValidatorFn` with a fake `AbstractControl` (`{ value } as AbstractControl`); assert the error object or `null`. No Angular.

### 2. `<feature>.logic.ts` — pure functions
Stateless decisions, payload construction, data normalization, and derived values. Functions return **explicit outcome objects** (discriminated unions) so the component pattern-matches exhaustively.
- **Backend analog:** domain logic + CourseManagement's discriminated-union result `record`s.
- **Example:** `resolveCourseEdit(currentName: string, dialogResult: string | undefined): { kind: 'skip' } | { kind: 'update'; name: string }`.
- **Test:** direct calls with plain inputs. No Angular.

### 3. `<feature>-store.ts` — OPTIONAL `new`-able class
Only when a feature has stateful orchestration or dependency-touching logic worth owning and testing (local selection/filter state, multi-step flows, notifier interactions). A plain `@Injectable` class that **receives its dependencies via constructor parameters**, holding signals and exposing methods.
- **Backend analog:** the `*Service` POCO (dependencies via interfaces).
- **Test:** `const store = new CoursesStore(fakeApi, fakeNotifier)` with hand-rolled fakes / `vi.fn()` implementing the `CourseApi` / `NotificationService` surface (the mirror of `Mock<IDao>(MockBehavior.Strict)`). Assert on signals and method outcomes. **No `TestBed`.**

The store does **not** own `injectQuery`/`injectMutation`: those require an injection context and would break `new`-ability. Reactive data orchestration stays in the component (see below).

## The humble component

The component keeps **only framework-bound concerns** and is excluded from the gate:
- `injectQuery` / `injectMutation` configuration.
- Template binding, Material dialog opening, `mutate()` calls.
- In `queryFn` / `onSuccess` / `onError`, it **delegates** the decision to `logic` / `store` and only wires.

Worked example (`courses.ts` `onEdit`):

```ts
async onEdit(course: Course): Promise<void> {
  const name = await this.openCourseDialog({ mode: 'edit', name: course.name });
  const outcome = resolveCourseEdit(course.name, name); // courses.logic.ts (pure, tested)
  if (outcome.kind === 'skip') {
    return;
  }
  this.updateCourse.mutate({ id: course.id, name: outcome.name });
}
```

The `resolveCourseEdit` rule and the `noDot` validator leave the component for pure files; the component is thin glue. The TanStack query key, invalidation, and success toast remain in the excluded component (accepted boundary; escape to Approach B only when justified).

## Testing approach per layer

- **`*.validators.ts` / `*.logic.ts`:** specs import the function and call it with plain data. Zero `TestBed`, HTTP, or DOM. 100% is trivial to reach.
- **`*-store.ts`:** `new Store(...fakes...)`; fakes are plain objects / `vi.fn()` implementing the API/notification surface. Assert signals and outcomes. No `TestBed`.
- **Component:** its logic is no longer there, so it is not unit-tested. An optional render-smoke with `@testing-library/angular` is allowed but **not gated**.

## Coverage-gate policy

- **Enforcement (100% floor):** extend `apps/Frontend/scripts/check-critical-coverage.mjs` to treat as critical — in addition to the current `CRITICAL_PREFIXES` (`core/auth/`, `core/utils/`, `core/strategies/`, `core/router/`, `shared/pipes/`) — any file matching the **suffixes** `*.logic.ts`, `*.validators.ts`, `*-store.ts` in any path. The new logic layer is born with a 100% floor.
- **Component exclusion:** in `apps/Frontend/vitest.config.ts`, exclude page/dialog component files from coverage while **preserving** the logic suffixes. Mechanism: a picomatch negation glob, e.g. `src/app/pages/**/!(*.logic|*.validators|*.variants|*-store).ts` (exact glob to be validated during implementation). Result: the overall % is no longer dragged by untestable wiring, and the gate measures "logic at 100%".

## Migration rule (incremental)

Not a big-bang. **When a feature is touched** (new work or a bug), extract *its* logic into `logic` / `validators` (+ `store` if warranted), cover it to 100%, and let it enter the gate. Suggested early targets are the lowest-coverage / most-logic pages: `courses`, `users/user-list`, `client/schedule`, `student/schedule`. The pattern is documented in `apps/Frontend/CLAUDE.md` as the canonical shape for new frontend code.

## Out of scope

- Rewriting every page at once.
- Adopting a state-management library.
- Covering component rendering / TanStack orchestration to 100% (explicitly excluded).
- Changing the backend or the existing critical-path tests.

## Success criteria

- The three roles, their naming, test style, and the Approach-A/B boundary are documented in `apps/Frontend/CLAUDE.md`.
- The critical-coverage gate enforces 100% on `*.logic.ts` / `*.validators.ts` / `*-store.ts`, and component files are excluded from coverage.
- A reference feature (e.g. `courses`) demonstrates the pattern end-to-end: pure `logic`/`validators` (+ optional store) at 100%, a humble component, and a measurable jump in that feature's logic coverage.
