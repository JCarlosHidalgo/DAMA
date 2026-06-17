# 0. Cómo encajan los estándares en el ciclo del proyecto

> Sección 0 de la plantilla, aterrizada en DAMA. Guía general antes de entrar a cada estándar.

Cada estándar cubre una fase distinta del proceso. No compiten: se complementan.

| Fase del proceso | Estándar | Pregunta que responde | Artefacto que produces |
|---|---|---|---|
| Análisis / Requisitos | **ISO/IEC/IEEE 29148:2018** | ¿Qué debe construirse? | SRS (Especificación de Requisitos) |
| Diseño | **IEEE 1016-2009** | ¿Cómo se construye? | SDD (Descripción del Diseño) |
| Pruebas / Evaluación | **ISO/IEC 25010:2023** | ¿Qué tan bien quedó? | Plan y resultados de evaluación de calidad |

**Reglas de oro:**

1. **Trazabilidad continua:** Objetivo específico → Requisito (29148) → Elemento de diseño
   (1016) → Caso de prueba (25010). Es lo que un tribunal valora más.
2. **Tailoring:** Selecciona solo las secciones relevantes al SaaS. Justifica brevemente lo que
   omites.
3. **Cita correctamente** cada estándar (ver [sección 5](5-errores-comunes-y-citas.md)).

---

## Cómo aplica a DAMA

DAMA es un SaaS web multitenant; encaja de lleno en el flujo de los tres estándares:

- **Requisitos (29148).** El sistema tiene objetivos claros (multitenancy, control de acceso por
  rol, gestión de clases y asistencia, cobro de deudas vía Todotix). Estos se traducen en
  requisitos funcionales y no funcionales — ver [sección 1](1-requisitos-iso-29148.md) y los
  archivos `academico/2.x`.
- **Diseño (1016).** La arquitectura ya está materializada: cinco microservicios .NET 9 detrás de
  un *api-gateway* nginx, SPA Angular 21, MySQL por servicio, RabbitMQ con patrón Outbox y gRPC
  con TLS entre servicios. El diseño se documenta desde varias vistas — ver
  [sección 2](2-diseno-ieee-1016.md) y los archivos `academico/3.x`.
- **Calidad (25010).** Hay evidencia objetiva medible: suites NUnit en cuatro backends, suite del
  frontend con *gate* de cobertura, *gates* de complejidad con SonarAnalyzer, y el endurecimiento
  documentado en [`academico/5.4-cumplimiento-owasp/`](academico/5.4-cumplimiento-owasp/) (OWASP Web Top 10 2021 y API Security Top 10 2023). Ver
  [sección 3](3-calidad-iso-25010.md) y los archivos `academico/5.x`.

### Cadena de trazabilidad de ejemplo (DAMA)

```
OE: "Garantizar el aislamiento de datos entre academias"
  └─ RNF (29148): "El sistema deberá filtrar todo acceso a datos por TenantId."
       └─ Diseño (1016): tenancy de esquema compartido con columna TenantId + IClaimContext
            └─ Prueba (25010 · Seguridad): casos que verifican que un tenant no lee datos de otro
                 └─ Evidencia: OWASP A01 (Broken Access Control) en academico/5.4-cumplimiento-owasp/web-top-10-2021/
```

Este hilo —objetivo → requisito → diseño → prueba → evidencia en el repo— es el patrón que
recorre todo el capítulo académico.

---

## Antes de continuar

- El **reglamento de trabajos de grado de la universidad prevalece** sobre cualquier estándar
  internacional. Estos estándares enriquecen el contenido; no sustituyen la norma institucional.
- La estructura completa del capítulo está en la [sección 4](4-estructura-marco-practico.md).
