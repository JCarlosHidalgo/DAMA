# Frontend Logic-Layer Pattern Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the canonical, hand-rolled logic-layer pattern on the frontend — pure `validators`/`logic` units (optional `-store`) that are tested without TestBed — demonstrate it on the `courses` feature, and make the coverage gate enforce it.

**Architecture:** Approach A from the design spec (`docs/superpowers/specs/2026-06-03-frontend-logic-layer-pattern-design.md`). Decision logic and validators move out of the Angular component into co-located, framework-free files tested by direct calls. The component stays "humble" (TanStack Query + template binding only) and is excluded from coverage. The critical-coverage gate enforces 100% on the new logic suffixes; components are excluded.

**Tech Stack:** Angular 21 (standalone, signals), Vitest + jsdom, `@vitest/coverage-v8`, Bun (package manager). No new dependencies.

**Conventions for the implementer:** Commit messages in English, no footer/trailer. Never push. No comments in code (repo rule). Same-directory `./` imports are allowed; `../` is forbidden by ESLint. Run commands from `apps/Frontend`.

---

## File Structure

- **Create** `apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.ts` — pure `ValidatorFn`s for the course form (`noDotValidator`).
- **Create** `apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.spec.ts` — direct unit tests, no TestBed.
- **Create** `apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.ts` — pure decision functions returning explicit outcome objects (`resolveCourseCreate`, `resolveCourseEdit`, `coursesSubtitle`).
- **Create** `apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.spec.ts` — direct unit tests, no TestBed.
- **Modify** `apps/Frontend/src/app/pages/dashboard/client/courses/courses.ts` — consume the new units; component becomes humble.
- **Modify** `apps/Frontend/scripts/check-critical-coverage.mjs` — add suffix-based critical matching.
- **Modify** `apps/Frontend/vitest.config.ts` — exclude humble page components from coverage, preserving the logic suffixes.
- **Modify** `apps/Frontend/CLAUDE.md` — document the pattern as canonical.

No `-store.ts` is created: `courses` is simple CRUD and does not warrant a store. The `-store` suffix is still wired into the gate (Task 4) and documented (Task 5) for the first feature that genuinely needs it.

---

## Task 1: Course form validators

**Files:**
- Create: `apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.ts`
- Test: `apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.spec.ts`

- [ ] **Step 1: Write the failing test**

Create `courses.validators.spec.ts`:

```ts
import { AbstractControl } from '@angular/forms';
import { describe, it, expect } from 'vitest';

import { noDotValidator } from './courses.validators';

function control(value: unknown): AbstractControl {
  return { value } as AbstractControl;
}

describe('noDotValidator', () => {
  it('flags a value containing a dot', () => {
    expect(noDotValidator(control('Yoga 2.0'))).toEqual({ hasDot: true });
  });

  it('passes a value without a dot', () => {
    expect(noDotValidator(control('Yoga'))).toBeNull();
  });

  it('passes a nullish value', () => {
    expect(noDotValidator(control(null))).toBeNull();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd apps/Frontend && bun run test:ci -- courses.validators`
Expected: FAIL — cannot resolve `./courses.validators` (module does not exist yet).

- [ ] **Step 3: Write minimal implementation**

Create `courses.validators.ts`:

```ts
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const noDotValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  control.value?.includes('.') ? { hasDot: true } : null;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd apps/Frontend && bun run test:ci -- courses.validators`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.ts apps/Frontend/src/app/pages/dashboard/client/courses/courses.validators.spec.ts
git commit -m "feat(frontend): extract course form validators into a pure unit"
```

---

## Task 2: Course decision logic

**Files:**
- Create: `apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.ts`
- Test: `apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.spec.ts`

- [ ] **Step 1: Write the failing test**

Create `courses.logic.spec.ts`:

```ts
import { describe, it, expect } from 'vitest';

import { coursesSubtitle, resolveCourseCreate, resolveCourseEdit } from './courses.logic';

describe('resolveCourseCreate', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveCourseCreate(undefined)).toEqual({ kind: 'skip' });
  });

  it('creates with the provided name', () => {
    expect(resolveCourseCreate('Yoga')).toEqual({ kind: 'create', name: 'Yoga' });
  });
});

describe('resolveCourseEdit', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveCourseEdit('Yoga', undefined)).toEqual({ kind: 'skip' });
  });

  it('skips when the name is unchanged', () => {
    expect(resolveCourseEdit('Yoga', 'Yoga')).toEqual({ kind: 'skip' });
  });

  it('updates when the name changed', () => {
    expect(resolveCourseEdit('Yoga', 'Pilates')).toEqual({ kind: 'update', name: 'Pilates' });
  });
});

describe('coursesSubtitle', () => {
  it('renders the count label', () => {
    expect(coursesSubtitle(3)).toBe('3 curso(s)');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd apps/Frontend && bun run test:ci -- courses.logic`
Expected: FAIL — cannot resolve `./courses.logic`.

- [ ] **Step 3: Write minimal implementation**

Create `courses.logic.ts`:

```ts
export type CourseCreateOutcome = { kind: 'skip' } | { kind: 'create'; name: string };
export type CourseEditOutcome = { kind: 'skip' } | { kind: 'update'; name: string };

export function resolveCourseCreate(dialogResult: string | undefined): CourseCreateOutcome {
  if (!dialogResult) {
    return { kind: 'skip' };
  }
  return { kind: 'create', name: dialogResult };
}

export function resolveCourseEdit(
  currentName: string,
  dialogResult: string | undefined,
): CourseEditOutcome {
  if (!dialogResult || dialogResult === currentName) {
    return { kind: 'skip' };
  }
  return { kind: 'update', name: dialogResult };
}

export function coursesSubtitle(count: number): string {
  return `${count} curso(s)`;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd apps/Frontend && bun run test:ci -- courses.logic`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.ts apps/Frontend/src/app/pages/dashboard/client/courses/courses.logic.spec.ts
git commit -m "feat(frontend): extract course create/edit decisions into a pure unit"
```

---

## Task 3: Make the courses component humble

**Files:**
- Modify: `apps/Frontend/src/app/pages/dashboard/client/courses/courses.ts`

This task only rewires the component to the units from Tasks 1-2. Behavior is identical, so the existing `courses.spec.ts` (CourseDialog form tests) keeps passing — that is the regression check.

- [ ] **Step 1: Replace the form imports and add the unit imports**

In `courses.ts`, change the `@angular/forms` import (it currently imports `AbstractControl`, no longer needed):

Old:
```ts
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
```
New:
```ts
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
```

Then add these two imports next to the existing `import { courseDialogStyles, coursesStyles } from './courses.variants';` line:
```ts
import { noDotValidator } from './courses.validators';
import { coursesSubtitle, resolveCourseCreate, resolveCourseEdit } from './courses.logic';
```

- [ ] **Step 2: Use `noDotValidator` in the dialog form**

In `CourseDialog`, replace the inline validator block.

Old:
```ts
  readonly form = this.formBuilder.nonNullable.group({
    name: [
      this.data.name,
      [
        Validators.required,
        Validators.maxLength(128),
        (control: AbstractControl) => (control.value?.includes('.') ? { hasDot: true } : null),
      ],
    ],
  });
```
New:
```ts
  readonly form = this.formBuilder.nonNullable.group({
    name: [this.data.name, [Validators.required, Validators.maxLength(128), noDotValidator]],
  });
```

- [ ] **Step 3: Delegate the subtitle and the create/edit decisions**

In `Courses`, replace the subtitle computed.

Old:
```ts
  protected readonly subtitle = computed(() => `${this.courses().length} curso(s)`);
```
New:
```ts
  protected readonly subtitle = computed(() => coursesSubtitle(this.courses().length));
```

Replace `onCreate`.

Old:
```ts
  async onCreate(): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'create', name: '' });
    if (!name) {
      return;
    }
    this.createCourse.mutate({ name });
  }
```
New:
```ts
  async onCreate(): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'create', name: '' });
    const outcome = resolveCourseCreate(name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.createCourse.mutate({ name: outcome.name });
  }
```

Replace `onEdit`.

Old:
```ts
  async onEdit(course: Course): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'edit', name: course.name });
    if (!name || name === course.name) {
      return;
    }
    this.updateCourse.mutate({ id: course.id, name });
  }
```
New:
```ts
  async onEdit(course: Course): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'edit', name: course.name });
    const outcome = resolveCourseEdit(course.name, name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.updateCourse.mutate({ id: course.id, name: outcome.name });
  }
```

Leave `onDelete` unchanged (its only branch is the framework confirm dialog — nothing pure to extract).

- [ ] **Step 4: Verify no regression and lint/format**

Run: `cd apps/Frontend && bun run test:ci -- courses`
Expected: PASS — existing `CourseDialog` tests (5) plus the new logic/validator specs all pass.

Run: `cd apps/Frontend && bun run lint && bunx prettier --check "src/app/pages/dashboard/client/courses/*.ts"`
Expected: no lint errors; Prettier reports all matched files formatted.

- [ ] **Step 5: Commit**

```bash
git add apps/Frontend/src/app/pages/dashboard/client/courses/courses.ts
git commit -m "refactor(frontend): make the courses component humble, delegating to logic units"
```

---

## Task 4: Extend the coverage gate and exclude humble components

**Files:**
- Modify: `apps/Frontend/scripts/check-critical-coverage.mjs`
- Modify: `apps/Frontend/vitest.config.ts`

- [ ] **Step 1: Add suffix-based critical matching to the gate**

In `check-critical-coverage.mjs`, add a suffix list under the existing `CRITICAL_PREFIXES` array:

```js
const CRITICAL_SUFFIXES = ['.logic.ts', '.validators.ts', '-store.ts'];
```

Then replace `isCritical`.

Old:
```js
function isCritical(relativePath) {
  return CRITICAL_PREFIXES.some((prefix) => relativePath.startsWith(prefix));
}
```
New:
```js
function isCritical(relativePath) {
  return (
    CRITICAL_PREFIXES.some((prefix) => relativePath.startsWith(prefix)) ||
    CRITICAL_SUFFIXES.some((suffix) => relativePath.endsWith(suffix))
  );
}
```

- [ ] **Step 2: Exclude humble page components from coverage**

In `vitest.config.ts`, add one entry to the `coverage.exclude` array (immediately after `'src/testing/**',`). This excludes page/dialog component files while preserving the logic suffixes and the styling variants:

```js
        'src/app/pages/**/!(*.logic|*.validators|*.variants|*-store).ts',
```

- [ ] **Step 3: Run coverage and the gate**

Run: `cd apps/Frontend && bun run test:coverage`
Expected: the run completes; the text-summary no longer lists `courses.ts` (the humble component is now excluded).

Run: `cd apps/Frontend && node scripts/check-critical-coverage.mjs`
Expected: output includes
```
[ok] src/app/pages/dashboard/client/courses/courses.logic.ts  S=100.00%  B=100.00%  F=100.00%  L=100.00%
[ok] src/app/pages/dashboard/client/courses/courses.validators.ts  S=100.00%  B=100.00%  F=100.00%  L=100.00%
```
and ends with `Critical coverage gate passed.`

If the negation glob does not behave as expected (e.g. `courses.ts` still appears, or `courses.logic.ts` disappears), confirm the picomatch extglob form is supported by the installed Vitest; the fallback is to list the component files to exclude explicitly. Do not weaken the gate to make it pass.

- [ ] **Step 4: Commit**

```bash
git add apps/Frontend/scripts/check-critical-coverage.mjs apps/Frontend/vitest.config.ts
git commit -m "build(frontend): gate logic-layer suffixes at 100% and exclude humble components"
```

---

## Task 5: Document the pattern as canonical

**Files:**
- Modify: `apps/Frontend/CLAUDE.md`

- [ ] **Step 1: Add the pattern section under "Testing"**

In `apps/Frontend/CLAUDE.md`, inside the `### Testing` section, add the following subsection after the existing coverage bullets:

```markdown
#### Logic-layer pattern (mirrors the backend service tests)

Backend service tests reach ~100% by keeping logic in framework-free units that are instantiated directly with mocks. Mirror that on the frontend: push decision logic out of components into co-located, **`new`-able** files, and keep the component "humble".

- **`<feature>.validators.ts`** — exported `ValidatorFn`s (e.g. `noDotValidator`). Tested by calling with a fake control (`{ value } as AbstractControl`). No `TestBed`.
- **`<feature>.logic.ts`** — pure functions: decisions, payload building, normalization, derived values. They return **explicit outcome objects** (`{ kind: 'skip' } | { kind: 'update'; name }`) so the component pattern-matches. Tested by direct calls. No `TestBed`.
- **`<feature>-store.ts`** — OPTIONAL `@Injectable` class whose deps come through **constructor parameters** (the sanctioned exception to `inject()`), used only when a feature has stateful orchestration or dependency-touching logic worth owning. Tested with `new Store(...fakes...)` (plain objects / `vi.fn()` implementing the API/notification surface). No `TestBed`.

The component keeps only framework-bound concerns — `injectQuery`/`injectMutation`, Material dialogs, template binding — and delegates decisions to the units above. TanStack orchestration (query keys, invalidation, success toasts) stays in the component and is **not** covered; if a feature's orchestration is genuinely complex and must be covered, that single feature may instead use an `inject()`-based store tested via `TestBed` (the justified exception).

Reference: `pages/dashboard/client/courses` (`courses.validators.ts`, `courses.logic.ts`, humble `courses.ts`).

**Gate:** `scripts/check-critical-coverage.mjs` enforces 100% on any `*.logic.ts` / `*.validators.ts` / `*-store.ts` file (plus the `CRITICAL_PREFIXES`). `vitest.config.ts` excludes humble page components from coverage. Apply this incrementally — when you touch a feature, extract its logic, cover it to 100%, and let it enter the gate; do not rewrite every page at once.
```

- [ ] **Step 2: Verify formatting**

Run: `cd apps/Frontend && bunx prettier --check CLAUDE.md`
Expected: Prettier reports the file is formatted (if it reports changes, run `bunx prettier --write CLAUDE.md` and re-check).

- [ ] **Step 3: Commit**

```bash
git add apps/Frontend/CLAUDE.md
git commit -m "docs(frontend): document the canonical logic-layer testing pattern"
```

---

## Verification (end-to-end)

From `apps/Frontend`:

1. `bun run test:ci` — full suite passes (the original 420 tests + 9 new logic/validator tests).
2. `bun run test:coverage:gate` — coverage runs and the critical gate passes, with `courses.logic.ts` and `courses.validators.ts` reported at 100% and `courses.ts` absent from the report.
3. `bun run lint && bun run format:check` — clean.

## Self-review notes

- **Spec coverage:** three roles (Tasks 1, 2, doc in 5) ✓; humble component (Task 3) ✓; testing-per-layer (Tasks 1-2 direct, store documented in 5) ✓; gate enforcement + component exclusion (Task 4) ✓; migration rule + CLAUDE.md (Task 5) ✓; reference feature `courses` end-to-end (Tasks 1-3) ✓. The `-store` role is documented and gate-wired but intentionally not built for `courses` (it does not warrant one — the spec marks the store optional).
- **Types are consistent:** `resolveCourseCreate` / `resolveCourseEdit` / `coursesSubtitle` / `noDotValidator` names and signatures match between their definition (Tasks 1-2) and use (Task 3).
