# Guía del Marco Práctico de DAMA

Esta carpeta convierte la plantilla raíz [`guia-estandares-marco-practico.md`](../../guia-estandares-marco-practico.md)
en documentación viva del proyecto **DAMA**. Sirve de puente entre la teoría de los estándares
de ingeniería de software y el código real del repositorio, para redactar el capítulo
**"Marco Práctico"** de un proyecto de grado.

> DAMA es un SaaS multitenant para la gestión de academias de baile: cinco microservicios
> .NET 9 (Auth, CourseManagement, Attendance, Payment, Credentials), una SPA Angular 21, un
> *api-gateway* nginx, MySQL por servicio, mensajería RabbitMQ con patrón Outbox y la
> integración externa de pagos Todotix.

## Cómo está organizada

La carpeta tiene dos niveles:

1. **`guia/*.md`** — una *guía por sección* de la plantilla (0–5). Cada archivo reproduce la
   orientación del estándar y añade un bloque **"Cómo aplica a DAMA"** que aterriza la teoría en
   artefactos concretos del repositorio. Es el *qué incluir y cómo redactarlo*.
2. **`guia/academico/`** — el *texto académico ya redactado* del capítulo, un archivo por
   sub-sección hoja del índice 3.x, con los **comandos que demuestran la implementación** sobre
   el repo real. Es el *contenido final* listo para la tesis.

## Índice de la guía (secciones 0–5)

| # | Archivo | Estándar / tema | Pregunta que responde |
|---|---------|-----------------|-----------------------|
| 0 | [0-como-encajan-los-estandares.md](0-como-encajan-los-estandares.md) | Marco general | ¿Cómo se complementan los 3 estándares? |
| 1 | [1-requisitos-iso-29148.md](1-requisitos-iso-29148.md) | ISO/IEC/IEEE 29148:2018 | ¿Qué debe construirse? (SRS) |
| 2 | [2-diseno-ieee-1016.md](2-diseno-ieee-1016.md) | IEEE 1016-2009 | ¿Cómo se construye? (SDD) |
| 3 | [3-calidad-iso-25010.md](3-calidad-iso-25010.md) | ISO/IEC 25010:2023 | ¿Qué tan bien quedó? (calidad) |
| 4 | [4-estructura-marco-practico.md](4-estructura-marco-practico.md) | Estructura del capítulo | ¿Cómo se ordena el Marco Práctico? |
| 5 | [5-errores-comunes-y-citas.md](5-errores-comunes-y-citas.md) | Buenas prácticas | ¿Qué evitar y cómo citar? |

## El capítulo académico (`academico/`)

La sección 4 mapea el índice del capítulo (3.1–3.7) a un archivo por sub-sección hoja. Estado
actual de la redacción:

- ✅ **Redactado:** `3.1 Metodología de desarrollo` (con análisis de Ruta Crítica / CPM y sus
  diagramas) y `3.2 Análisis y especificación de requisitos` (29148: 2.1–2.5, con 35 RF, 24 RNF y
  la matriz de trazabilidad OE → acciones → requisitos → diseño → prueba) y `3.3 Diseño del sistema`
  (1016: 3.1–3.7, con arquitectura, casos de uso, clases, modelo de datos, secuencia, interfaz y
  despliegue; UML autogenerado por Doxygen y UML autorado en PlantUML bajo `../graphics/academico/`)
  y `3.4 Implementación` (stack tecnológico y justificación, estructura del código/módulos y
  desarrollo por iteraciones, datado con `git`) y `3.5 Pruebas y evaluación de calidad`
  (plan, casos y resultados medidos —1 251 pruebas en verde— y evaluación por las 9 características
  de ISO/IEC 25010:2023, con las brechas de carga y accesibilidad declaradas) y `3.6 Despliegue`
  (estrategia dev/prod, despliegue continuo en Dokploy + Cloudflare + TLS automático + respaldos,
  con CI automatizada propuesta vía Jenkins y declarada como no implementada) y `3.7 Resultados`
  (síntesis frente a los cinco objetivos y cierre de la trazabilidad de punta a punta).
- ✅ **Capítulo completo:** las secciones 3.1–3.7 del Marco Práctico están redactadas.

Ver [`academico/README.md`](academico/README.md) para el detalle de archivos y su estado.

## Estrategia de diagramas

Ningún diagrama UML se dibuja a mano en Markdown. Se usan tres fuentes según el propósito:

| Fuente | Para qué | Dónde |
|--------|----------|-------|
| **draw.io** (XML mxGraph) | Diagramas de proceso no-UML (la red CPM de la metodología) | `academico/*.drawio` |
| **FossFlow** (Isoflow JSON) | Vistas isométricas de arquitectura/fases no-UML | `../graphics/diagrams/` + `.md` acompañante |
| **Doxygen** (backends) / **Compodoc** (frontend) | UML autogenerado desde el código (clases, colaboración, directorios, módulos) | Generados desde el código; el `.md` solo referencia el tipo y el comando |
| **PlantUML** (`.puml`) | UML que Doxygen/Compodoc no autogeneran (casos de uso, secuencia) | `../graphics/academico/*.puml`, renderizado por el contenedor `doxygen` (imagen con PlantUML) |

## Referencias del repositorio

- Plantilla original: [`guia-estandares-marco-practico.md`](../../guia-estandares-marco-practico.md)
- Marco metodológico CPM: [`cpm.txt`](../../cpm.txt)
- Diagramas FossFlow: [`../graphics/`](../graphics/)
- Cumplimiento OWASP (insumo de calidad/seguridad): [`../OWASP/`](../OWASP/)
- Documentación de código: `infrastructure/docs/doxygen/` (backends) y
  `infrastructure/docs/compodoc/` (frontend).
