# A06 · Vulnerable and Outdated Components (Web Top 10 2021)

> **Estado:** 🟢 — Toda la plataforma corre sobre **.NET 9** con cada dependencia en versión **fijada** (sin rangos flotantes), un analizador estático (SonarAnalyzer) integrado en cada compilación, y las librerías internas publicadas como NuGet versionado. Es un control de **proceso**: depende de mantener el seguimiento de versiones al día.

## Qué exige OWASP

El riesgo aparece al usar componentes (frameworks, librerías, runtimes) con vulnerabilidades conocidas, sin versionar, sin parchear o sin inventario. OWASP recomienda fijar versiones, mantener un inventario de dependencias, eliminar lo no usado, monitorear avisos de seguridad y actualizar de forma disciplinada.

## Cómo lo cumple DAMA

### Runtime único y moderno: .NET 9

Los cinco backends declaran `TargetFramework` net9.0 — un único runtime soportado, sin *frameworks* heredados conviviendo.

`apps/Auth/Backend/Backend.csproj:4`

```xml
<TargetFramework>net9.0</TargetFramework>
```

### Versiones fijadas, no rangos flotantes

Cada `PackageReference` lleva una versión exacta. No hay comodines (`*`) ni rangos (`[9.0,)`), así que cada *build* es reproducible y el inventario es exacto.

`apps/Auth/Backend/Backend.csproj:13` (extracto):

```xml
<PackageReference Include="FluentValidation" Version="12.1.1" />
<PackageReference Include="JuanCarlosHS.SQLDaosPackage" Version="3.1.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="MySql.Data" Version="9.3.0" />
<PackageReference Include="RabbitMQ.Client" Version="7.0.0" />
<PackageReference Include="Scrutor" Version="6.0.1" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="8.1.1" />
```

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

SonarAnalyzer se incluye como analizador en cada proyecto, con `PrivateAssets=all` (no se propaga a consumidores) y un `SonarLint.xml` por backend. Las reglas corren en cada `dotnet build`.

`apps/Auth/Backend/Backend.csproj:24`

```xml
<PackageReference Include="SonarAnalyzer.CSharp" Version="10.27.0.140913">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<AdditionalFiles Include="SonarLint.xml" />
```

### Librerías internas como NuGet versionado

El código compartido (`packages/outbox`, `packages/unit-of-work`, `grpc-contracts`) se publica como paquetes `DAMA.Software.*` y se consume vía `PackageReference` con versión fija, **nunca** por referencia de proyecto. Así el código compartido también tiene inventario y versión rastreable.

`apps/Auth/Backend/Backend.csproj:10`

```xml
<PackageReference Include="DAMA.Software.GrpcContracts" Version="1.0.0" />
<PackageReference Include="DAMA.Software.MySqlOutbox" Version="2.0.0" />
<PackageReference Include="DAMA.Software.MySqlUnitOfWork" Version="2.0.0" />
```

Cambiar un paquete interno significa subir su `<Version>` y republicar (no editar una referencia de proyecto), lo que fuerza una bump explícita y deja traza.

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
   ▼  actualización disciplinada: bump explícito de Version + republicar paquete interno
```

En el diagrama FossFLOW `extra/fossflow/diagrams/owasp-web-top-10.json`, este ítem es el rectángulo **A06 · Vulnerable & Outdated Components**, que agrupa los nodos **.NET 9 + NuGet fijado**, **SonarAnalyzer** y **Paquetes internos NuGet**.

## Verificación

- Runtime único: `grep -rn "TargetFramework" apps/*/Backend/Backend.csproj` debe arrojar `net9.0` en los cinco.
- Versiones fijas: `grep -rn "Version=\"\*\"\|\[.*,)" apps/*/Backend/Backend.csproj` debe estar vacío (sin comodines/rangos).
- Analizador presente: `grep -rn "SonarAnalyzer" apps/*/Backend/Backend.csproj` en los cinco.
- Auditoría de avisos conocidos: `dotnet list apps/Auth/Backend/Backend.csproj package --vulnerable` (requiere restore con conexión a nuget.org).

## Notas / brechas conocidas

- **Es un control de proceso, no automático en CI.** No hay (en el repo) un *gate* de CI que falle ante un CVE; `dotnet list package --vulnerable` y la bump de versiones se ejecutan manualmente. La fortaleza depende de la disciplina de actualización.
- `JuanCarlosHS.SQLDaosPackage` es una dependencia externa cuyo *source* vive fuera del monorepo; al editar `packages/outbox` o `packages/unit-of-work` la política interna pide subir su dependencia de SQLDaosPackage a la última (3.1.1) — verificar coherencia entre el paquete publicado y el consumido.
- Swashbuckle 8.1.1 permanece como dependencia aunque no se mapea en runtime; conviene revisar si puede retirarse para reducir superficie de dependencias.
- Los paquetes internos los publica el usuario manualmente; un *bump* de `<Version>` en el `.csproj` sin la publicación correspondiente rompería el `restore`.
