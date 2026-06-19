# Diseño — Workflows de CI por área (`.github/workflows/`)

Fecha: 2026-06-19

## Objetivo

Implementar CI en GitHub Actions para cada evento de Pull Request hacia `main`,
ejecutando build y test **solo del área de `apps/` que cambió** (el frontend o
cualquiera de los backends), más workflows complementarios para un proceso de CI
satisfactorio.

## Decisiones tomadas

1. **Ejecución**: toolchains nativos del runner (`ubuntu-latest`) — `setup-dotnet`
   y `setup-bun` con caché de NuGet/Bun. No se reutilizan los Dockerfiles de
   `environments-test/`.
2. **Relación build↔test**: test depende de build (gate). Para el área afectada,
   primero corre build; si pasa, corre test.
3. **Un solo archivo `ci.yml`** con dos grupos de jobs (`build-*` y `test-*`); el
   gate se expresa con `needs:`. (Se descartó dos archivos físicos con
   `workflow_run` por fragilidad.)
4. **Sonar/editorconfig estricto**: el build de backend corre con `-warnaserror`,
   de modo que warnings de `SonarAnalyzer.CSharp` rompen el PR.
5. **Extras**: `concurrency` con `cancel-in-progress`, y un workflow `codeql.yml`
   independiente. (Se descartaron PR-title lint y dependency review.)

## Inventario relevante del repo

- 5 backends .NET 9 en `apps/<Service>/Backend/`: Auth, Attendance,
  CourseManagement, Payment, Credentials.
- Test suites NUnit en `apps/<Service>/Test/`: Auth, Attendance,
  CourseManagement, Payment tienen suites reales con su `.runsettings`.
  **Credentials no tiene tests** (dummy de claims) → build sí, test no.
- Cada `Backend/` trae su propio `.editorconfig` y `SonarLint.xml`;
  `SonarAnalyzer.CSharp` ya está referenciado como analizador en cada `.csproj`.
- Paquetes `DAMA.Software.*` y `JuanCarlosHS.SQLDaosPackage` se consumen vía
  NuGet (nuget.org); `dotnet restore` en CI funciona sin acceso a `packages/`.
- Frontend Angular 21 con **Bun** (`bun.lock`). Scripts: `build` (`ng build`),
  `lint` (`ng lint`/eslint), `format:check` (`prettier --check`), `test:ci`
  (`ng test --watch=false`), `test:coverage`.
- No existe `.github/` todavía.

## Arquitectura

### Trigger

```yaml
on:
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]
```

### Job 0 — `changes` (detección por área)

`dorny/paths-filter@v3` calcula un booleano por área. Salidas consumidas por los
`if:` de cada job de build:

| Filtro | Path | Build | Test |
|---|---|---|---|
| `auth` | `apps/Auth/**` | sí | sí |
| `attendance` | `apps/Attendance/**` | sí | sí |
| `coursemanagement` | `apps/CourseManagement/**` | sí | sí |
| `payment` | `apps/Payment/**` | sí | sí |
| `credentials` | `apps/Credentials/**` | sí | **no** |
| `frontend` | `apps/Frontend/**` | sí | sí |

Cambios en `packages/`, `grpc-contracts/`, `infrastructure/` o raíz **no**
disparan builds de backend (libs consumidas como NuGet publicado; no afectan el
restore/build hasta republicar). Si ningún path de `apps/**` cambió, los jobs
quedan en estado *skipped* y el workflow es un no-op verde.

### Grupo build

**Backend** (uno por servicio, `if: needs.changes.outputs.<area> == 'true'`):
1. `actions/checkout@v4`
2. `actions/setup-dotnet@v4` con `dotnet-version: 9.0.x` + caché de NuGet
   (`~/.nuget/packages`, key por hash de los `.csproj`).
3. `dotnet restore apps/<Svc>/Backend/Backend.csproj`
4. `dotnet format apps/<Svc>/Backend/Backend.csproj --verify-no-changes`
   (enforce `.editorconfig`: whitespace + style + analyzers).
5. `dotnet build apps/<Svc>/Backend/Backend.csproj -c Release --no-restore
   -warnaserror` (compila + SonarAnalyzer rompe ante warnings).

**Frontend** (`if: needs.changes.outputs.frontend == 'true'`):
1. `actions/checkout@v4`
2. `oven-sh/setup-bun@v2` + caché de Bun.
3. `bun install --frozen-lockfile` (cwd `apps/Frontend`).
4. `bun run format:check` (prettier).
5. `bun run lint` (eslint).
6. `bun run build` (`ng build`).

### Grupo test (gate: `needs: [changes, build-<area>]`)

**Backend** (Auth/Attendance/CourseManagement/Payment — **no** Credentials):
1. checkout + setup-dotnet + caché NuGet.
2. `dotnet test apps/<Svc>/Test/Test.csproj -s apps/<Svc>/Test/.runsettings
   --logger trx` (solo lógica de negocio, según `.runsettings`).
3. Publicar resultados de test como anotación del PR
   (p. ej. `dorny/test-reporter` o el summary nativo).

**Frontend**:
1. checkout + setup-bun + caché.
2. `bun install --frozen-lockfile`.
3. `bun run test:ci` (con `--coverage`).

### Concurrency

En `ci.yml`:

```yaml
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
```

### `codeql.yml` (workflow aparte)

- `on: pull_request: [main]` + `schedule` semanal (cron).
- Matrix de lenguajes: `csharp` y `javascript-typescript`.
- `github/codeql-action` (init → autobuild/build → analyze). Para `csharp`,
  build manual mínimo de los backends afectados o autobuild.

## Estructura de archivos resultante

```
.github/
  workflows/
    ci.yml        # changes + build-* + test-* (needs)
    codeql.yml    # seguridad C# + JS/TS
```

## Criterios de éxito

- Un PR que toca solo `apps/Auth/**` ejecuta `build-auth` y luego `test-auth`;
  no instancia jobs de otros servicios ni del frontend.
- Un PR que toca `apps/Credentials/**` ejecuta `build-credentials` y **ningún**
  job de test.
- Un PR que toca `apps/Frontend/**` ejecuta build (prettier+eslint+ng build) y
  test (ng test) del frontend.
- Un warning de SonarAnalyzer en un backend tocado **falla** el build.
- Un fallo de build impide que corra el test del mismo área.
- Un push nuevo al PR cancela el run anterior en progreso.
- CodeQL corre en PR a main y semanalmente.

## Fuera de alcance

- Branch protection / required status checks (configuración en GitHub UI, no
  versionable aquí). Se documentará como paso manual.
- Coverage gates con umbral (los tests corren con coverage pero no se enforce un
  porcentaje mínimo en esta iteración).
- PR-title lint y dependency review (descartados).
- Reutilización de `compose.test.yaml` / Dockerfiles de `environments-test`.
