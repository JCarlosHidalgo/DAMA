# 2. IEEE 1016-2009 — Diseño (SDD)

> Sección 2 de la plantilla, aterrizada en DAMA.

**Qué es:** el estándar para la **Software Design Description (SDD)**. Su idea central son los
**design viewpoints** (puntos de vista): el diseño se documenta desde varias vistas
complementarias, cada una respondiendo a una preocupación distinta.

## 2.1 Vistas a documentar

| Vista (viewpoint) | Diagrama recomendado | Para qué |
|---|---|---|
| Contexto / funcional | Casos de uso | Qué hace cada actor con el sistema. |
| Arquitectura | Componentes / paquetes | Organización en capas o servicios. |
| Estructural | Clases | Entidades del dominio y sus relaciones. |
| Datos | Modelo Entidad-Relación + diccionario | Estructura de la base de datos. |
| Dinámica / interacción | Secuencia (flujos clave) | Cómo colaboran los objetos en un flujo. |
| Despliegue | Despliegue | Servidores, contenedores, nube. |

> **Selecciona los flujos clave** para los diagramas de secuencia. No diagrames todo: diagrama lo
> crítico.

---

## Cómo aplica a DAMA

### Arquitectura (vista de arquitectura → `academico/3.1`)

Patrón elegido: **microservicios** detrás de un *api-gateway* nginx, con SPA Angular 21 como
cliente. Justificación: aislamiento de dominios (autenticación, cursos, asistencia, pagos,
credenciales), despliegue independiente y límites de seguridad claros en el borde. La vista
isométrica del stack ya existe en [`../fossflow/diagrams/dama-architecture.json`](../fossflow/diagrams/dama-architecture.json).

### Estrategia de tenancy en la BD (decisión de diseño clave → `academico/3.4`)

DAMA usa **esquema compartido con columna `TenantId`** (la opción más simple operativamente).
Justificación: una sola instancia por servicio, menor costo y complejidad de despliegue que
"BD por tenant"; el aislamiento se garantiza filtrando **siempre** por `TenantId` (leído de
`IClaimContext`) y verificándolo en pruebas (OWASP A01). Aristas conocidas: los *stored
procedures* deben calificar columnas con alias de tabla para evitar colisión
`WHERE TenantId = tenantId`.

### Estilo de API y separación frontend/backend

- **API REST** por servicio, expuesta tras el gateway con rutas kebab-case en minúsculas.
- **SPA Angular 21** que consume el gateway; URLs públicas inyectadas por entorno (build-arg para
  el bundle, envsubst de runtime para CORS del gateway).
- **gRPC con TLS** para contratos internos (`class_existence`/`course_existence` entre
  CourseManagement↔Attendance, `tenant_subscription` Payment→Auth).
- **Mensajería asíncrona:** patrón Outbox + RabbitMQ + consumidor idempotente.

### Diagramas: cómo se producen (regla transversal)

DAMA **no dibuja UML a mano**. Los diagramas estructurales/de componentes se generan desde el
código con la herramienta de documentación de cada stack, y el documento académico solo
**referencia el tipo de diagrama y el comando que lo genera**:

| Vista | Herramienta | Diagrama concreto |
|-------|-------------|-------------------|
| Componentes / paquetes (backend) | **Doxygen** (`infrastructure/docs/doxygen/Doxyfile`) | Grafo de directorios (`DIRECTORY_GRAPH`) e *include graph*. |
| Componentes / módulos (frontend) | **Compodoc** (`infrastructure/docs/compodoc/tsconfig.doc.json`) | Grafo de dependencias de módulos. |
| Clases (estructural) | **Doxygen** | Grafo de clases/colaboración (`UML_LOOK`) y jerarquía de herencia (`GRAPHICAL_HIERARCHY`). |
| Despliegue / arquitectura de servicios | **FossFlow** | Vista isométrica `dama-architecture.json`. |

Solo casos de uso y secuencia no se autogeneran; su fuente se define en la ola correspondiente
(ver los esqueletos `academico/3.2` y `academico/3.5`).

### Diccionario de datos (rellenar en `academico/3.4`)

La fuente de verdad del esquema son los `infrastructure/environments/<svc>/init.sql` (tablas +
*stored procedures*). El diccionario se redacta tabla por tabla:

| Campo | Tipo | Nulo | Clave | Descripción |
|---|---|---|---|---|
| Id | … | No | PK | Identificador único del registro. |
| TenantId | … | No | — | Academia propietaria del registro (aislamiento multitenant). |
