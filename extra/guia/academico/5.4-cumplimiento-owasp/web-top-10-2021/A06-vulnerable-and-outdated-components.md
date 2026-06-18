# A06 · Vulnerable and Outdated Components (Web Top 10 2021)

> **Estado:** 🟢 — Toda la plataforma corre sobre **.NET 9** con cada dependencia en versión **fijada** (sin rangos flotantes), un analizador estático (SonarAnalyzer) integrado en cada compilación, y las librerías internas publicadas como NuGet versionado. Es un control de **proceso**: depende de mantener el seguimiento de versiones al día.

## Introducción

Esta ficha documenta cómo DAMA gestiona el riesgo de **componentes vulnerables y desactualizados** (A06), que surge al usar frameworks, librerías o runtimes con vulnerabilidades conocidas, sin versionar o sin inventario. El documento expone la evidencia técnica del control, de naturaleza fundamentalmente de proceso: el runtime único y moderno .NET 9, las versiones de cada `PackageReference` fijadas sin rangos flotantes, el inventario de versiones, el analizador estático SonarAnalyzer integrado en cada compilación y las librerías internas publicadas como NuGet versionado. Incluye el flujo de gestión de dependencias, los comandos de verificación y las brechas conocidas (notablemente la ausencia de una barrera de CI que falle ante un CVE).

## Qué exige OWASP

El riesgo aparece al usar componentes (frameworks, librerías, runtimes) con vulnerabilidades conocidas, sin versionar, sin parchear o sin inventario. OWASP recomienda fijar versiones, mantener un inventario de dependencias, eliminar lo no usado, monitorear avisos de seguridad y actualizar de forma disciplinada.

## Cómo lo cumple DAMA

### Runtime único y moderno: .NET 9

Los cinco backends declaran `TargetFramework` net9.0 — un único runtime soportado, sin *frameworks* heredados conviviendo (`apps/Auth/Backend/Backend.csproj:4`).

### Versiones fijadas, no rangos flotantes

Cada `PackageReference` lleva una versión exacta. No hay comodines (`*`) ni rangos (`[9.0,)`), así que cada compilación es reproducible y el inventario es exacto. El extracto del `.csproj` de Auth fija versiones concretas para FluentValidation, JuanCarlosHS.SQLDaosPackage, Microsoft.AspNetCore.Authentication.JwtBearer, MySql.Data, RabbitMQ.Client, Scrutor y Swashbuckle.AspNetCore (`apps/Auth/Backend/Backend.csproj:13`).

Inventario de versiones verificado hoy en los `.csproj` reales:

| Paquete | Versión | Servicios |
|---|---|---|
| FluentValidation (+ DependencyInjectionExtensions) | 12.1.1 | todos los que validan |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | todos |
| MySql.Data | 9.3.0 | todos con DB |
| RabbitMQ.Client | 7.0.0 | productores/consumidores |
| Scrutor | 6.0.1 | todos |
| JuanCarlosHS.SQLDaosPackage | 3.1.0 | todos con DB |
| AutoMapper | 16.1.1 | Auth, Payment (entre otros) — `apps/Payment/Backend/Backend.csproj:10` |
| Microsoft.Extensions.Http.Resilience | 9.0.0 | Payment (resiliencia gRPC/HTTP) — `apps/Payment/Backend/Backend.csproj:18` |
| System.IdentityModel.Tokens.Jwt | 8.3.0 | Auth (emisión de tokens) |
| Swashbuckle.AspNetCore | 8.1.1 | presente como paquete, **no** mapeado en runtime (ver A05/API8) |
| SonarAnalyzer.CSharp | 10.27.0.140913 | todos |

### Análisis estático en cada compilación (SonarAnalyzer)

SonarAnalyzer se incluye como analizador en cada proyecto, con `PrivateAssets=all` (no se propaga a consumidores) y un `SonarLint.xml` por backend declarado como `AdditionalFiles`. Las reglas corren en cada `dotnet build` (`apps/Auth/Backend/Backend.csproj:24`).

### Librerías internas como NuGet versionado

El código compartido (`packages/outbox`, `packages/unit-of-work`, `grpc-contracts`) se publica como paquetes `DAMA.Software.*` (`DAMA.Software.GrpcContracts`, `DAMA.Software.MySqlOutbox`, `DAMA.Software.MySqlUnitOfWork`) y se consume vía `PackageReference` con versión fija, **nunca** por referencia de proyecto. Así el código compartido también tiene inventario y versión rastreable (`apps/Auth/Backend/Backend.csproj:10`).

Cambiar un paquete interno significa subir su `<Version>` y republicar (no editar una referencia de proyecto), lo que fuerza un incremento explícito de versión y deja traza.

## Flujo de los componentes

```
proceso de gestión de dependencias
   │
   ▼  .csproj por backend
   │     · TargetFramework net9.0           (runtime único soportado)
   │     · PackageReference con Version=x.y.z fija (sin rangos)
   │     · DAMA.Software.* internos vía NuGet versionado
   │
   ▼  dotnet build
   │     · SonarAnalyzer.CSharp + SonarLint.xml → reglas en cada compilación
   │
   ▼  actualización disciplinada: incremento explícito de Version + republicar paquete interno
```

En el diagrama FossFlow `extra/graphics/diagrams/owasp-web-top-10.json`, este ítem es el rectángulo **A06 · Vulnerable & Outdated Components**, que agrupa los nodos **.NET 9 + NuGet fijado**, **SonarAnalyzer** y **Paquetes internos NuGet**.

## Verificación

Runtime único: debe arrojar `net9.0` en los cinco backends.

```bash
grep -rn "TargetFramework" apps/*/Backend/Backend.csproj
```

Versiones fijas: el resultado debe estar vacío (sin comodines ni rangos).

```bash
grep -rn "Version=\"\*\"\|\[.*,)" apps/*/Backend/Backend.csproj
```

Analizador presente: SonarAnalyzer debe aparecer en los cinco.

```bash
grep -rn "SonarAnalyzer" apps/*/Backend/Backend.csproj
```

Auditoría de avisos conocidos (requiere restore con conexión a nuget.org):

```bash
dotnet list apps/Auth/Backend/Backend.csproj package --vulnerable
```

## Notas y brechas conocidas

- **Es un control de proceso, no automático en CI.** No hay (en el repo) una barrera de CI que falle ante un CVE; `dotnet list package --vulnerable` y el incremento de versiones se ejecutan manualmente. La fortaleza depende de la disciplina de actualización.
- `JuanCarlosHS.SQLDaosPackage` es una dependencia externa cuyo código fuente vive fuera del monorepo; al editar `packages/outbox` o `packages/unit-of-work` la política interna pide subir su dependencia de SQLDaosPackage a la última (3.1.1) — verificar coherencia entre el paquete publicado y el consumido.
- Swashbuckle 8.1.1 permanece como dependencia aunque no se mapea en runtime; conviene revisar si puede retirarse para reducir superficie de dependencias.
- Los paquetes internos los publica el usuario manualmente; un incremento de `<Version>` en el `.csproj` sin la publicación correspondiente rompería el `restore`.
