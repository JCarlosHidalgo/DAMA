# 3.4 Implementación

> **Estado:** ✅ Redactado. Introducción de la sección 3.4 (Implementación). **Sin norma de
> producto asociada**: corresponde al proceso de *Implementation* de **ISO/IEC/IEEE 12207:2017**.
> **Guía:** [4-estructura-marco-practico.md](../4-estructura-marco-practico.md) (correspondencia con
> el ciclo de vida) y [0-como-encajan-los-estandares.md](../0-como-encajan-los-estandares.md).

---

## Propósito de la sección

Esta sección **describe cómo se construyó efectivamente DAMA**: con qué tecnologías, con qué
organización del código y a lo largo de qué iteraciones. A diferencia de 3.2 (qué debe hacer) y 3.3
(cómo está diseñado), 3.4 documenta la **realización del diseño en código fuente** —el stack
elegido y su justificación, la estructura de módulos de los cinco backends y el frontend, y la
progresión del desarrollo por olas—. Su función es evidenciar que el diseño se materializó de forma
disciplinada y trazable, tomando como fuentes los manifiestos de dependencias, la documentación
generada del código y el historial de versiones.

En conjunto, la sección recorre el stack tecnológico y su justificación, la estructura del código y
módulos, y el desarrollo por iteraciones/sprints.

## Convención aplicada — proceso de Implementación (ISO/IEC/IEEE 12207:2017)

**Qué es:** a diferencia de 3.2, 3.3 y 3.5, la implementación **no se rige por una norma de
producto** (no hay un estándar análogo a 29148, 1016 o 25010 que dicte su contenido). En el mapeo
con el ciclo de vida del software, esta sección corresponde al **proceso técnico de Implementación**
(y de Integración) de **ISO/IEC/IEEE 12207:2017**, que cubre la transformación del diseño en
componentes ejecutables y su combinación en el sistema.

**Cómo se aplica en 3.4:** en lugar de una norma de producto, la sección se ancla en **convenciones
y fuentes verificables del repositorio**: los manifiestos `.csproj` y `package.json` como fuente del
stack y sus versiones (3.4.1); la documentación de estructura **generada desde el código** con
Doxygen (backends) y Compodoc (frontend), más las convenciones de estilo Microsoft .NET impuestas
por `.editorconfig`, como evidencia de la organización en módulos (3.4.2); y el `git log` —con sus
olas de trabajo mergeadas a `main`— como registro del desarrollo iterativo (3.4.3).

## Contenido de la sección

### 3.4.1 Stack tecnológico y justificación

Detalla las tecnologías de la plataforma (.NET 9, Angular 21, MySQL, RabbitMQ, nginx, gRPC) y
**justifica cada elección**, tomando las versiones reales de los `.csproj` y `package.json`.
→ [`4.1-stack-tecnologico-y-justificacion.md`](4.1-stack-tecnologico-y-justificacion.md)

### 3.4.2 Estructura del código / módulos

Describe la **organización del código** de los backends y el frontend —capas, módulos y patrones
transversales—, referenciando los grafos de dependencias generados con Doxygen y Compodoc.
→ [`4.2-estructura-del-codigo-y-modulos.md`](4.2-estructura-del-codigo-y-modulos.md)

### 3.4.3 Desarrollo por iteraciones/sprints

Reconstruye la **progresión del desarrollo** por olas a partir del historial de Git, evidenciando
el carácter iterativo-incremental de la construcción.
→ [`4.3-desarrollo-por-iteraciones.md`](4.3-desarrollo-por-iteraciones.md)
