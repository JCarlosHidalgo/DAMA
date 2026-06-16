# Capítulo "Marco Práctico" — texto académico de DAMA

Esta carpeta contiene el **texto académico** del capítulo, un archivo por sub-sección **hoja** del
índice 3.x (ver el mapeo completo en [`../4-estructura-marco-practico.md`](../4-estructura-marco-practico.md)).
Cada archivo trae el contenido para la tesis y los **comandos que demuestran la implementación**
sobre el repositorio real.

## Estado de la redacción

| Archivo | Sub-sección | Estado |
|---------|-------------|--------|
| [1-metodología-de-desarrollo.md](1-metodología-de-desarrollo.md) | 3.1 Metodología (CPM) | ✅ Redactado |
| [2.1-descripcion-general-del-sistema.md](2.1-descripcion-general-del-sistema.md) | 3.2.1 | ✅ Redactado |
| [2.2-roles-de-usuario.md](2.2-roles-de-usuario.md) | 3.2.2 | ✅ Redactado |
| [2.3-requisitos-funcionales.md](2.3-requisitos-funcionales.md) | 3.2.3 | ✅ Redactado |
| [2.4-requisitos-no-funcionales.md](2.4-requisitos-no-funcionales.md) | 3.2.4 | ✅ Redactado |
| [2.5-matriz-de-trazabilidad.md](2.5-matriz-de-trazabilidad.md) | 3.2.5 | ✅ Redactado |
| [3.1-arquitectura.md](3.1-arquitectura.md) | 3.3.1 | ⬜ |
| [3.2-casos-de-uso.md](3.2-casos-de-uso.md) | 3.3.2 | ⬜ |
| [3.3-diagrama-de-clases.md](3.3-diagrama-de-clases.md) | 3.3.3 | ⬜ |
| [3.4-modelo-de-datos-y-diccionario.md](3.4-modelo-de-datos-y-diccionario.md) | 3.3.4 | ⬜ |
| [3.5-diagramas-de-secuencia.md](3.5-diagramas-de-secuencia.md) | 3.3.5 | ⬜ |
| [3.6-diseno-de-interfaz.md](3.6-diseno-de-interfaz.md) | 3.3.6 | ⬜ |
| [3.7-diagrama-de-despliegue.md](3.7-diagrama-de-despliegue.md) | 3.3.7 | ⬜ |
| [4.1-stack-tecnologico-y-justificacion.md](4.1-stack-tecnologico-y-justificacion.md) | 3.4.1 | ⬜ |
| [4.2-estructura-del-codigo-y-modulos.md](4.2-estructura-del-codigo-y-modulos.md) | 3.4.2 | ⬜ |
| [4.3-desarrollo-por-iteraciones.md](4.3-desarrollo-por-iteraciones.md) | 3.4.3 | ⬜ |
| [5.1-plan-de-pruebas.md](5.1-plan-de-pruebas.md) | 3.5.1 | ⬜ |
| [5.2-casos-de-prueba-y-resultados.md](5.2-casos-de-prueba-y-resultados.md) | 3.5.2 | ⬜ |
| [5.3-evaluacion-por-caracteristicas-de-calidad.md](5.3-evaluacion-por-caracteristicas-de-calidad.md) | 3.5.3 | ⬜ |
| [6-despliegue.md](6-despliegue.md) | 3.6 | ⬜ |
| [7-resultados.md](7-resultados.md) | 3.7 | ⬜ |

## Archivos de diagramas del piloto (3.1)

- [`1-metodología-de-desarrollo-cpm-ruta-critica.drawio`](1-metodología-de-desarrollo-cpm-ruta-critica.drawio) — red CPM (draw.io).
- [`1-metodología-de-desarrollo-fases-wbs.md`](1-metodología-de-desarrollo-fases-wbs.md) — diagrama FossFlow de fases/WBS.

## Convenciones

- **Idioma:** español.
- **Diagramas UML / de componentes:** no se dibujan en el `.md`; se referencia el diagrama que
  generan **Doxygen** (backends, `infrastructure/docs/doxygen/Doxyfile`) o **Compodoc** (frontend,
  `infrastructure/docs/compodoc/tsconfig.doc.json`) y el comando que lo produce.
- **Sin datos inventados:** cada cifra se respalda con un comando reproducible del repo.
