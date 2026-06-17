# 3. ISO/IEC 25010:2023 — Calidad / Evaluación

> Sección 3 de la plantilla, aterrizada en DAMA. **Se usa la edición vigente 2023** (no la 2011),
> por decisión explícita: el objetivo es evaluar DAMA cumpliendo estrictamente la norma actual.

**Qué es:** el modelo de calidad del producto de software de la familia **SQuaRE**. La edición
**2023** define **9 características** (la 2011 definía 8). Cambios clave respecto a 2011:

- **Usabilidad → Interaction Capability** (capacidad de interacción).
- **Portabilidad → Flexibility** (flexibilidad), que además incorpora **Scalability**.
- **Se añade Safety** (seguridad física/operacional), característica nueva.
- **Reliability** renombra *Maturity* → *Faultlessness*; **Security** añade *Resistance*.

> Fuente del modelo: portal oficial SQuaRE (iso25000.com) y resumen de ISO/IEC 25010:2023. Verifica
> la nomenclatura final contra el ejemplar de la norma que exija tu universidad antes de citar.

## 3.1 Las 9 características y sus subcaracterísticas (2023)

| # | Característica | Subcaracterísticas (2023) |
|---|---|---|
| 1 | **Functional Suitability** (adecuación funcional) | Functional completeness · correctness · appropriateness |
| 2 | **Performance Efficiency** (eficiencia de desempeño) | Time behaviour · Resource utilization · Capacity |
| 3 | **Compatibility** (compatibilidad) | Co-existence · Interoperability |
| 4 | **Interaction Capability** (capacidad de interacción) | Appropriateness recognizability · Learnability · Operability · User error protection · User engagement · Inclusivity · User assistance · Self-descriptiveness |
| 5 | **Reliability** (fiabilidad) | Faultlessness · Availability · Fault tolerance · Recoverability |
| 6 | **Security** (seguridad) | Confidentiality · Integrity · Non-repudiation · Accountability · Authenticity · Resistance |
| 7 | **Maintainability** (mantenibilidad) | Modularity · Reusability · Analysability · Modifiability · Testability |
| 8 | **Flexibility** (flexibilidad) | Adaptability · Scalability · Installability · Replaceability |
| 9 | **Safety** (seguridad física/operacional) | Operational constraint · Risk identification · Fail safe · Hazard warning · Safe integration |

## 3.2 Enfoque de evaluación para DAMA

A diferencia de un *tailoring* que descarta características de entrada, aquí la norma 2023 se usa
como **juez de DAMA en las 9 características**: cada subcaracterística se evalúa y se marca su
**aplicabilidad** con justificación (no se omite sin más):

- **Aplica** — se evalúa con métrica y evidencia del repo.
- **No aplica (justificado)** — se documenta por qué una subcaracterística no es pertinente al
  dominio de DAMA. (Nota: en DAMA incluso **Safety** aplica, reinterpretada como integridad
  económica de los cobros; ver `academico/5.3`.)
- **Falta contexto** — aplica, pero requiere un insumo aún no disponible (p. ej. resultados de
  prueba de carga, evaluación de accesibilidad).

El detalle por característica vive en
[`academico/5.3-evaluacion-por-caracteristicas-de-calidad.md`](academico/5.3-evaluacion-por-caracteristicas-de-calidad.md);
el plan y los casos, en `academico/5.1` y `academico/5.2`.

## 3.3 De la característica a una métrica medible (plantilla → `academico/5.3`)

| Característica | Métrica | Método | Esperado | Obtenido |
|---|---|---|---|---|
| Functional Suitability | % de RF con prueba que pasa | Suites NUnit + frontend | 100% | _por llenar_ |
| Security | Subcaracterísticas con control verificado | Catálogo OWASP `academico/5.4-cumplimiento-owasp/` | 6/6 cubiertas | _por llenar_ |
| Maintainability | Violaciones de complejidad | *Gates* SonarAnalyzer | 0 | _por llenar_ |
| Performance Efficiency | Tiempo de respuesta p95 / capacidad | Prueba de carga | _definir_ | _por llenar_ |

## 3.4 Insumos del repositorio (comandos)

- Pruebas backend: `dotnet test apps/<Servicio>/Test/<Proyecto>.csproj`
- Pruebas frontend: `cd apps/Frontend && bun run test:ci`
- Cobertura con *gate*: `cd apps/Frontend && bun run test:coverage:gate`
- Estilo/complejidad: `dotnet format <Project>.csproj --verify-no-changes`
- Seguridad: catálogo OWASP en [`academico/5.4-cumplimiento-owasp/`](academico/5.4-cumplimiento-owasp/) (Web Top 10 2021 + API Top 10 2023).

> Alcance de las suites: por `.runsettings` cubren **lógica de negocio**, no infraestructura.
> Credentials es un *dummy* de claims sin casos de prueba.
