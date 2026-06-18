# 3.5 Pruebas y evaluación de calidad

> **Estado:** ✅ Redactado. Introducción de la sección 3.5 (Pruebas y evaluación de calidad) según
> **ISO/IEC 25010:2023**. **Guía:** [3-calidad-iso-25010.md](../3-calidad-iso-25010.md).

---

## Propósito de la sección

Esta sección **verifica y evalúa la calidad de DAMA** frente a lo especificado en 3.2 e
implementado en 3.4. Documenta el plan de pruebas, los casos ejecutados y sus resultados, la
evaluación del producto contra un modelo de calidad reconocido y el cumplimiento de seguridad
mediante el catálogo OWASP. Su función es aportar **evidencia objetiva** de que el sistema hace lo
que debe (verificación) y de que satisface las propiedades de calidad esperadas (validación), con
métricas medibles y reproducibles sobre el repositorio.

En conjunto, la sección recorre el plan de pruebas, los casos de prueba y resultados, la evaluación
por características de calidad y el cumplimiento de seguridad OWASP.

## Convención aplicada — ISO/IEC 25010:2023

**Qué es:** el **modelo de calidad del producto de software** de la familia SQuaRE. La edición
**2023** —que DAMA usa por decisión explícita en lugar de la 2011— define **9 características**:
adecuación funcional, eficiencia de desempeño, compatibilidad, capacidad de interacción, fiabilidad,
seguridad, mantenibilidad, flexibilidad y seguridad física/operacional (*safety*), cada una con sus
subcaracterísticas.

**Cómo se aplica en 3.5:** la norma se emplea como **juez de DAMA en las 9 características** —no como
un *tailoring* que descarte características de entrada—: cada subcaracterística se evalúa marcando su
aplicabilidad (*aplica* con métrica y evidencia, *no aplica* justificado, o *falta contexto*). La
sub-sección 3.5.3 desarrolla esa evaluación característica por característica; 3.5.1 y 3.5.2 aportan
el plan y los casos que la sustentan (suites NUnit y frontend, con cobertura acotada a lógica de
negocio por `.runsettings`); y 3.5.4 cubre la característica de **Security** en detalle mediante el
catálogo OWASP Web/API Top 10. El puente de cada característica a una métrica medible se hace con la
plantilla *característica → métrica → método → esperado → obtenido*.

## Contenido de la sección

### 3.5.1 Plan de pruebas

Define la **estrategia de pruebas**: alcance (lógica de negocio), niveles, herramientas (NUnit,
Vitest) y criterios de cobertura, alineados con las características de 25010.
→ [`5.1-plan-de-pruebas.md`](5.1-plan-de-pruebas.md)

### 3.5.2 Casos de prueba y resultados

Recoge los **casos ejecutados y sus resultados** por dominio, como evidencia verificable de la
adecuación funcional.
→ [`5.2-casos-de-prueba-y-resultados.md`](5.2-casos-de-prueba-y-resultados.md)

### 3.5.3 Evaluación por características de calidad

Evalúa DAMA **característica por característica** según 25010:2023, con métrica, método y
justificación de aplicabilidad para cada subcaracterística.
→ [`5.3-evaluacion-por-caracteristicas-de-calidad.md`](5.3-evaluacion-por-caracteristicas-de-calidad.md)

### 3.5.4 Cumplimiento de seguridad (OWASP Web/API Top 10)

Desarrolla en detalle la característica de **Security** mediante el catálogo OWASP **Web Top 10
(2021)** y **API Security Top 10 (2023)**, ítem por ítem y con citas al código real.
→ [`5.4-cumplimiento-owasp.md`](5.4-cumplimiento-owasp.md)
