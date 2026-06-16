# 4. Estructura del capítulo "Marco Práctico"

> Sección 4 de la plantilla. Aquí se materializa la carpeta [`academico/`](academico/): el índice
> del capítulo se mapea a un archivo por sub-sección **hoja**.

## Índice del capítulo

```
CAPÍTULO 3 — MARCO PRÁCTICO
3.1 Metodología de desarrollo                       ← (gestión de proyecto: CPM)
3.2 Análisis y especificación de requisitos         ← ISO/IEC/IEEE 29148
    3.2.1 Descripción general del sistema
    3.2.2 Roles de usuario (multitenancy)
    3.2.3 Requisitos funcionales
    3.2.4 Requisitos no funcionales
    3.2.5 Matriz de trazabilidad
3.3 Diseño del sistema                              ← IEEE 1016
    3.3.1 Arquitectura
    3.3.2 Casos de uso
    3.3.3 Diagrama de clases
    3.3.4 Modelo de datos y diccionario de datos
    3.3.5 Diagramas de secuencia (flujos clave)
    3.3.6 Diseño de interfaz (UI/UX)
    3.3.7 Diagrama de despliegue
3.4 Implementación
    3.4.1 Stack tecnológico y justificación
    3.4.2 Estructura del código / módulos
    3.4.3 Desarrollo por iteraciones/sprints
3.5 Pruebas y evaluación de calidad                 ← ISO/IEC 25010
    3.5.1 Plan de pruebas
    3.5.2 Casos de prueba y resultados
    3.5.3 Evaluación por características de calidad
3.6 Despliegue (CI/CD, nube)
3.7 Resultados
```

## Mapeo a archivos de `academico/`

El prefijo del archivo es el número de sub-sección sin el "3." (3.1 → `1-`, 3.2.1 → `2.1-`,
3.6 → `6-`).

| Sub-sección | Archivo | Estado | Estándar / fuente |
|---|---|---|---|
| 3.1 | [`academico/1-metodología-de-desarrollo.md`](academico/1-metodología-de-desarrollo.md) | ✅ Redactado | CPM (cpm.txt) |
| 3.2.1 | [`academico/2.1-descripcion-general-del-sistema.md`](academico/2.1-descripcion-general-del-sistema.md) | ✅ Redactado | 29148 |
| 3.2.2 | [`academico/2.2-roles-de-usuario.md`](academico/2.2-roles-de-usuario.md) | ✅ Redactado | 29148 |
| 3.2.3 | [`academico/2.3-requisitos-funcionales.md`](academico/2.3-requisitos-funcionales.md) | ✅ Redactado | 29148 |
| 3.2.4 | [`academico/2.4-requisitos-no-funcionales.md`](academico/2.4-requisitos-no-funcionales.md) | ✅ Redactado | 29148 |
| 3.2.5 | [`academico/2.5-matriz-de-trazabilidad.md`](academico/2.5-matriz-de-trazabilidad.md) | ✅ Redactado | 29148 |
| 3.3.1 | [`academico/3.1-arquitectura.md`](academico/3.1-arquitectura.md) | ⬜ | 1016 |
| 3.3.2 | [`academico/3.2-casos-de-uso.md`](academico/3.2-casos-de-uso.md) | ⬜ | 1016 |
| 3.3.3 | [`academico/3.3-diagrama-de-clases.md`](academico/3.3-diagrama-de-clases.md) | ⬜ | 1016 / Doxygen |
| 3.3.4 | [`academico/3.4-modelo-de-datos-y-diccionario.md`](academico/3.4-modelo-de-datos-y-diccionario.md) | ⬜ | 1016 |
| 3.3.5 | [`academico/3.5-diagramas-de-secuencia.md`](academico/3.5-diagramas-de-secuencia.md) | ⬜ | 1016 |
| 3.3.6 | [`academico/3.6-diseno-de-interfaz.md`](academico/3.6-diseno-de-interfaz.md) | ⬜ | 1016 / 25010 |
| 3.3.7 | [`academico/3.7-diagrama-de-despliegue.md`](academico/3.7-diagrama-de-despliegue.md) | ⬜ | 1016 / FossFlow |
| 3.4.1 | [`academico/4.1-stack-tecnologico-y-justificacion.md`](academico/4.1-stack-tecnologico-y-justificacion.md) | ⬜ | — |
| 3.4.2 | [`academico/4.2-estructura-del-codigo-y-modulos.md`](academico/4.2-estructura-del-codigo-y-modulos.md) | ⬜ | — / Doxygen·Compodoc |
| 3.4.3 | [`academico/4.3-desarrollo-por-iteraciones.md`](academico/4.3-desarrollo-por-iteraciones.md) | ⬜ | — |
| 3.5.1 | [`academico/5.1-plan-de-pruebas.md`](academico/5.1-plan-de-pruebas.md) | ⬜ | 25010 |
| 3.5.2 | [`academico/5.2-casos-de-prueba-y-resultados.md`](academico/5.2-casos-de-prueba-y-resultados.md) | ⬜ | 25010 |
| 3.5.3 | [`academico/5.3-evaluacion-por-caracteristicas-de-calidad.md`](academico/5.3-evaluacion-por-caracteristicas-de-calidad.md) | ⬜ | 25010 |
| 3.6 | [`academico/6-despliegue.md`](academico/6-despliegue.md) | ⬜ | — |
| 3.7 | [`academico/7-resultados.md`](academico/7-resultados.md) | ⬜ | — |

## Nota sobre 3.1 (Metodología) vs. los estándares

La metodología de desarrollo y la planificación del proyecto son transversales: no las cubre
ninguno de los tres estándares de producto. Por eso `academico/3.1` usa el **Método de la Ruta
Crítica (CPM)** —según el marco de [`cpm.txt`](../../cpm.txt)— para describir y planificar el
proceso real de construcción de DAMA, complementando (no sustituyendo) la metodología
iterativa-incremental adoptada.
