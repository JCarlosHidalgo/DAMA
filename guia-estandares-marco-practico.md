# Guía de estándares de software para el Marco Práctico
### Proyecto de grado — Aplicación SaaS web

> **Cómo usar esta guía:** es una guía *práctica*, no teórica. Cada sección te dice
> qué incluir, cómo redactarlo y trae tablas-plantilla que puedes copiar y rellenar.
> Los estándares se **adaptan** (*tailoring*) al alcance de una tesis: no se copian
> completos. Y recuerda: **el reglamento de trabajos de grado de tu universidad
> prevalece** sobre cualquier estándar internacional.

---

## 0. Cómo encajan los 3 estándares en el ciclo del proyecto

Cada estándar cubre una fase distinta de tu proceso. No compiten: se complementan.

| Fase del proceso | Estándar | Pregunta que responde | Artefacto que produces |
|---|---|---|---|
| Análisis / Requisitos | **ISO/IEC/IEEE 29148:2018** | ¿Qué debe construirse? | SRS (Especificación de Requisitos) |
| Diseño | **IEEE 1016-2009** | ¿Cómo se construye? | SDD (Descripción del Diseño) |
| Pruebas / Evaluación | **ISO/IEC 25010:2011** | ¿Qué tan bien quedó? | Plan y resultados de evaluación de calidad |

**Reglas de oro:**
1. **Trazabilidad continua:** Objetivo específico → Requisito (29148) → Elemento de diseño (1016) → Caso de prueba (25010). Esto es lo que un tribunal valora más.
2. **Tailoring:** Selecciona solo las secciones relevantes a tu SaaS. Justifica brevemente lo que omites.
3. **Cita correctamente** cada estándar (ver Sección 5).

---

## 1. ISO/IEC/IEEE 29148:2018 — Requisitos (SRS)

**Qué es:** El estándar internacional vigente de ingeniería de requisitos. **Reemplazó a la
clásica IEEE 830-1998** (SRS), que aún se enseña pero está retirada. Si tu universidad pide
"IEEE 830", puedes usar 29148 y mencionar que es su sucesora.

### 1.1 Subsecciones del SRS a incluir

| # | Subsección | Contenido |
|---|---|---|
| 1 | Propósito y alcance | Qué hace el sistema y qué queda fuera (delimitación). |
| 2 | Perspectiva del producto | Contexto, sistemas con los que interactúa, diagrama de contexto. |
| 3 | Características de los usuarios | **Roles** del SaaS (ej.: super-admin, admin de tenant, usuario final). |
| 4 | Requisitos funcionales (RF) | Lo que el sistema *hace*. Formato: *"El sistema deberá…"*. |
| 5 | Requisitos no funcionales (RNF) | Rendimiento, seguridad, usabilidad, escalabilidad (enlaza con ISO 25010). |
| 6 | Requisitos de interfaz externa | UI, API, hardware, otros sistemas. |
| 7 | Restricciones | Tecnológicas, legales, presupuestarias. |

### 1.2 Cómo escribir un requisito de calidad

Un buen requisito (según 29148) es: **atómico** (una sola cosa), **verificable** (se puede
probar), **inequívoco**, **trazable** y **necesario**.

> ❌ Mal: *"El sistema debe ser rápido y fácil de usar."*
> ✅ Bien: *"RF-012: El sistema deberá responder a la consulta de facturas en menos de 2
> segundos para el 95% de las peticiones bajo carga de 100 usuarios concurrentes."*

**Plantilla de tabla de requisitos:**

| ID | Tipo | Descripción | Prioridad | Origen | Objetivo asociado |
|---|---|---|---|---|---|
| RF-001 | Funcional | El sistema deberá permitir el registro de nuevos tenants. | Alta | Entrevista | OE-1 |
| RNF-001 | No funcional | El sistema deberá cifrar las contraseñas con bcrypt. | Alta | Norma seguridad | OE-3 |

### 1.3 Matriz de trazabilidad (requisito → objetivo)

| Objetivo específico | Requisitos que lo satisfacen |
|---|---|
| OE-1: Gestionar múltiples organizaciones | RF-001, RF-002, RF-015 |
| OE-3: Garantizar la seguridad de los datos | RNF-001, RNF-004, RF-020 |

### 1.4 Particularidades SaaS a no olvidar

- **Multitenancy:** requisitos sobre aislamiento de datos entre tenants.
- **Autenticación y autorización:** roles, permisos, posiblemente SSO/OAuth.
- **Escalabilidad:** comportamiento bajo crecimiento de usuarios/tenants.
- **Planes/suscripciones:** si el SaaS los tiene (límites por plan, facturación).
- **Disponibilidad:** SLA, tiempo de actividad esperado.

---

## 2. IEEE 1016-2009 — Diseño (SDD)

**Qué es:** El estándar para la **Software Design Description (SDD)**. Su idea central son los
**design viewpoints** (puntos de vista): el diseño se documenta desde varias vistas
complementarias, cada una respondiendo a una preocupación distinta.

### 2.1 Vistas a documentar (con UML)

| Vista (viewpoint) | Diagrama UML recomendado | Para qué |
|---|---|---|
| Contexto / funcional | Casos de uso | Qué hace cada actor con el sistema. |
| Arquitectura | Diagrama de componentes / paquetes | Organización en capas o servicios. |
| Estructural | Diagrama de clases | Entidades del dominio y sus relaciones. |
| Datos | Modelo Entidad-Relación + diccionario de datos | Estructura de la base de datos. |
| Dinámica / interacción | Diagrama de secuencia (flujos clave) | Cómo colaboran los objetos en un flujo. |
| Despliegue | Diagrama de despliegue | Servidores, contenedores, nube. |

> **Selecciona los flujos clave** para los diagramas de secuencia (login, alta de tenant,
> operación principal del negocio). No diagrames todo: diagrama lo crítico.

### 2.2 Diseño de la arquitectura
Justifica el patrón elegido (n-capas, cliente-servidor, microservicios, MVC). Para un SaaS
web típico: **separación frontend / backend (API) / base de datos**.

### 2.3 Diseño de interfaz (UI/UX)
- Wireframes o mockups de las pantallas principales.
- Justificación de decisiones de usabilidad (enlaza con la característica *usabilidad* de
  ISO 25010).

### 2.4 Particularidades SaaS web

- **Estilo de API:** REST o GraphQL — documenta los endpoints/recursos principales.
- **Separación frontend/backend:** SPA + API, o renderizado en servidor.
- **Estrategia de tenancy en la BD** (decisión de diseño importante):
  - *Base de datos por tenant* (máximo aislamiento, mayor costo).
  - *Esquema por tenant* (aislamiento medio).
  - *Esquema compartido con columna `tenant_id`* (más simple, menor aislamiento).
  Justifica cuál elegiste y por qué.
- **Despliegue en la nube:** contenedores (Docker), servicios gestionados, CI/CD.

**Plantilla de diccionario de datos (por tabla):**

| Campo | Tipo | Nulo | Clave | Descripción |
|---|---|---|---|---|
| id | UUID | No | PK | Identificador único. |
| tenant_id | UUID | No | FK | Organización propietaria del registro. |

---

## 3. ISO/IEC 25010:2011 — Calidad / Evaluación

**Qué es:** El modelo de calidad del producto de software de la familia **SQuaRE**. Define
**8 características** de calidad. Úsalo para estructurar tu capítulo de pruebas/evaluación de
forma rigurosa y medible.

> Nota: existe una revisión **ISO/IEC 25010:2023** con 9 características (añade
> *seguridad/safety* y reorganiza algunas). Verifica cuál exige tu universidad; 2011 sigue
> siendo la más citada en tesis.

### 3.1 Las 8 características (versión 2011)

| # | Característica | Significado breve |
|---|---|---|
| 1 | Adecuación funcional | ¿Hace lo que debe, de forma correcta y completa? |
| 2 | Eficiencia de desempeño | Tiempos de respuesta, uso de recursos. |
| 3 | Compatibilidad | Coexistencia e interoperabilidad con otros sistemas. |
| 4 | Usabilidad | Facilidad de aprendizaje y uso. |
| 5 | Fiabilidad | Madurez, disponibilidad, tolerancia a fallos. |
| 6 | Seguridad | Confidencialidad, integridad, autenticidad. |
| 7 | Mantenibilidad | Facilidad de modificar/corregir el software. |
| 8 | Portabilidad | Adaptabilidad a distintos entornos. |

**No tienes que evaluar las 8.** Selecciona las relevantes para un SaaS (típicamente:
adecuación funcional, eficiencia, usabilidad, seguridad, fiabilidad) y justifícalo.

### 3.2 De la característica a una métrica medible

Por cada característica que evalúes, define: **métrica + método + resultado esperado vs.
obtenido**.

| Característica | Métrica | Método de medición | Esperado | Obtenido |
|---|---|---|---|---|
| Eficiencia de desempeño | Tiempo de respuesta promedio | Prueba de carga (ej. JMeter, 100 usuarios) | < 2 s | _por llenar_ |
| Usabilidad | Tasa de éxito de tareas | Test de usabilidad con N usuarios | > 90% | _por llenar_ |
| Seguridad | Vulnerabilidades críticas | Escaneo (ej. OWASP ZAP) | 0 | _por llenar_ |
| Adecuación funcional | % de RF implementados y aprobados | Ejecución de casos de prueba | 100% | _por llenar_ |

### 3.3 Plantilla de caso de prueba

| ID | Requisito | Precondición | Pasos | Resultado esperado | Resultado real | Estado |
|---|---|---|---|---|---|---|
| CP-001 | RF-001 | Usuario sin sesión | 1. Ir a registro 2. Completar datos 3. Enviar | Tenant creado y correo enviado | _por llenar_ | ✅/❌ |

---

## 4. Estructura sugerida del capítulo "Marco Práctico"

Índice listo para usar que integra los tres estándares en orden lógico:

```
CAPÍTULO 3 — MARCO PRÁCTICO
3.1 Metodología de desarrollo (Scrum / RUP / XP — la que usaste)
3.2 Análisis y especificación de requisitos        ← ISO/IEC/IEEE 29148
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

---

## 5. Errores comunes y recomendaciones

- **No copies plantillas completas** del estándar. Usa solo lo relevante y justifica las
  omisiones (*tailoring*).
- **Mantén la trazabilidad** de punta a punta: objetivos → requisitos → diseño → pruebas.
  Es el hilo conductor que demuestra rigor.
- **Cita correctamente** cada estándar. Ejemplos:
  - IEEE: `ISO/IEC/IEEE 29148:2018, Systems and software engineering — Life cycle processes — Requirements engineering.`
  - APA 7: `International Organization for Standardization. (2011). ISO/IEC 25010:2011 Systems and software engineering — SQuaRE — System and software quality models.`
- **Verifica versiones** antes de citar (29148:2018, 1016:2009, 25010:2011 — o 25010:2023 si tu universidad la exige).
- **El reglamento de tu universidad prevalece.** Si exige una estructura distinta, ajústate
  a ella y usa estos estándares para enriquecer el contenido, no para sustituir la norma
  institucional.

---

### Estándares referenciados
- **ISO/IEC/IEEE 29148:2018** — Ingeniería de requisitos (sucesora de IEEE 830-1998).
- **IEEE 1016-2009** — Software Design Descriptions (SDD).
- **ISO/IEC 25010:2011** — Modelo de calidad del producto (familia SQuaRE); revisión 2023 disponible.
- Complementarios útiles: **ISO/IEC/IEEE 12207** (procesos del ciclo de vida), **UML / ISO/IEC 19501** (diagramas).
