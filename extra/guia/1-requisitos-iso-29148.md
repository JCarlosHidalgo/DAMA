# 1. ISO/IEC/IEEE 29148:2018 — Requisitos (SRS)

> Sección 1 de la plantilla, aterrizada en DAMA.

**Qué es:** el estándar internacional vigente de ingeniería de requisitos. Reemplazó a la
clásica **IEEE 830-1998**. Si la universidad pide "IEEE 830", puede usarse 29148 mencionando que
es su sucesora.

## 1.1 Subsecciones del SRS a incluir

| # | Subsección | Contenido |
|---|---|---|
| 1 | Propósito y alcance | Qué hace el sistema y qué queda fuera. |
| 2 | Perspectiva del producto | Contexto, sistemas con los que interactúa, diagrama de contexto. |
| 3 | Características de los usuarios | **Roles** del SaaS. |
| 4 | Requisitos funcionales (RF) | Lo que el sistema *hace*. Formato: *"El sistema deberá…"*. |
| 5 | Requisitos no funcionales (RNF) | Rendimiento, seguridad, usabilidad, escalabilidad. |
| 6 | Requisitos de interfaz externa | UI, API, otros sistemas. |
| 7 | Restricciones | Tecnológicas, legales, presupuestarias. |

## 1.2 Cómo escribir un requisito de calidad

Un buen requisito es **atómico**, **verificable**, **inequívoco**, **trazable** y **necesario**.

> ❌ Mal: *"El sistema debe ser rápido y seguro."*
> ✅ Bien: *"RNF-003: El sistema deberá filtrar toda consulta de datos por el `TenantId` del
> usuario autenticado, de modo que ningún tenant acceda a registros de otro."*

---

## Cómo aplica a DAMA

### Roles del SaaS (subsección 3)

DAMA es multitenant; sus roles reales (definidos por constantes `UserRoles` por servicio y leídos
vía `IClaimContext`) son:

| Rol | Alcance | Ejemplos de capacidades |
|-----|---------|--------------------------|
| **Admin** | Operador **global** de tenants | Listar, crear y renombrar academias (tenants); gestionar planes de suscripción. |
| **Client** | Dueño/gestor de **su** academia | Configurar credenciales Todotix, gestionar clases y grupos, ver reportes del tenant, cambiar zona horaria. |
| **Teacher** | Personal de la academia | Ver su horario, tomar asistencia. |
| **Student** | Usuario final | Ver horario, marcar asistencia (QR), consultar y pagar deudas. |

> Detalle: solo Admin opera sobre cualquier tenant; cambiar la zona horaria es del Client sobre
> su propio tenant. Esto se desarrolla en `academico/2.2-roles-de-usuario.md`.

### Particularidades SaaS a documentar (subsección 5/7)

- **Multitenancy:** aislamiento de datos vía esquema compartido con columna `TenantId` (decisión
  de diseño justificada en la [sección 2](2-diseno-ieee-1016.md)).
- **Autenticación y autorización:** JWT firmado con RSA, *refresh tokens*, autorización
  *default-deny* por rol en el gateway y los servicios.
- **Planes/suscripciones:** niveles de suscripción por tenant (*core-services pyramid*) que
  habilitan o restringen funciones; Payment consulta la suscripción a Auth vía gRPC
  (`tenant_subscription`).
- **Integración externa:** registro y cobro de deudas a través de **Todotix** (credenciales
  cifradas por tenant).
- **Seguridad como RNF de primera clase:** ver el catálogo de controles ya implementados en
  [`../OWASP/`](../OWASP/), que sirve de fuente directa para los RNF de seguridad.

### Plantilla de requisitos (rellenar en `academico/2.3` y `academico/2.4`)

| ID | Tipo | Descripción | Prioridad | Origen | Objetivo asociado |
|---|---|---|---|---|---|
| RF-001 | Funcional | El sistema deberá permitir al Admin registrar nuevas academias (tenants). | Alta | Alcance | OE-1 |
| RNF-001 | No funcional | El sistema deberá cifrar las contraseñas con un algoritmo de *hash* adaptativo y rehash transparente. | Alta | OWASP A02 | OE-3 |

> Los requisitos definitivos se redactan en los archivos `academico/2.3-requisitos-funcionales.md`
> y `academico/2.4-requisitos-no-funcionales.md`, y la matriz objetivo↔requisito en
> `academico/2.5-matriz-de-trazabilidad.md`.
