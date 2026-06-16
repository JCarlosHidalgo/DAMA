# 3.7 Resultados

> **Estado:** ✅ Redactado. **Cierre del capítulo Marco Práctico**: síntesis de lo logrado frente a
> los objetivos y cierre de la trazabilidad de punta a punta.
> **Guía:** [4-estructura-marco-practico.md](../4-estructura-marco-practico.md).
> **Sintetiza:** 3.1 (metodología), 3.2 (requisitos), 3.3 (diseño), 3.4 (implementación),
> 3.5 (pruebas y calidad) y 3.6 (despliegue).

---

## 3.7.1 Síntesis frente a los objetivos específicos

DAMA se construyó para cumplir cinco objetivos específicos (OE-1…OE-5, definidos en 2.5). El
resultado por objetivo, con su evidencia, es:

| OE | Resultado | Evidencia |
|----|-----------|-----------|
| **OE-1** — Multitenancy, autenticación y suscripciones | ✅ **Logrado.** Servicio Auth con token firmado RSA, aislamiento por academia y niveles de suscripción; Credentials refleja la identidad. | Suite Auth (116 pruebas) · 3.3.1/3.3.3/3.3.4 · 3.5.3 §6 |
| **OE-2** — Oferta académica (cursos, clases, grupos, horarios por rol) | ✅ **Logrado.** CourseManagement con clases recurrentes/puntuales, grupos con validación de solape y horarios por rol; SPA por rol. | Suite CourseManagement (193 pruebas) · 3.3.5 · 3.5.3 §1 |
| **OE-3** — Asistencia (registro por código y clases restantes) | ✅ **Logrado.** Attendance con registro por QR, cálculo de clases restantes y mensajería asíncrona confiable (Bandeja de Salida + consumidor idempotente). | Suite Attendance (103 pruebas) · 3.3.5 · 3.5.3 §5 |
| **OE-4** — Cobros con pasarela externa, integridad económica | ✅ **Logrado.** Payment integrado con Todotix, credenciales cifradas por academia, pago por QR y cuatro libros de registro que preservan la integridad de los cobros. | Suite Payment (186 pruebas) · 3.3.4 · 3.5.3 §9 |
| **OE-5** — Seguridad, aislamiento de datos y calidad | ✅ **Logrado.** Gateway único, TLS inter-servicio, tres olas de endurecimiento OWASP, evaluación ISO/IEC 25010:2023 y despliegue en producción con respaldos. | Catálogo OWASP (20 ítems) · 3.5.3 · 3.6 |

**Los cinco objetivos se alcanzaron**, cada uno respaldado por una suite de pruebas en verde y por la
evidencia de diseño y despliegue correspondiente.

## 3.7.2 Resultados de calidad (ISO/IEC 25010:2023)

La evaluación con el modelo de calidad de producto de la norma 2023 (3.5.3) arroja: de las **nueve
características**, **siete se cumplen con evidencia** (Functional Suitability, Compatibility,
Reliability, Security, Maintainability, Safety, y Flexibility en lo esencial) y **dos se cumplen en
lo cualitativo pero arrastran una brecha de insumo medido**:

- **Performance Efficiency** — la evaluación cualitativa (optimizaciones existentes) está redactada;
  faltan los números de **carga** (p95, capacidad), que requieren una corrida instrumentada
  (JMeter/k6) con el stack levantado.
- **Interaction Capability → Inclusivity / User assistance** — la accesibilidad **no fue un objetivo
  formal**; existe la base de Angular Material/CDK, sin objetivo WCAG ni auditoría.

Ambas brechas se **declaran con honestidad** (rigor ISO: reportar el estado real, no asumir
conformidad), no se encubren como cumplimiento.

## 3.7.3 Cobertura de seguridad (OWASP)

La seguridad (OE-5) se trabajó en **tres olas de endurecimiento** —control de acceso denegado por
defecto, límites de recursos en el gateway, y reforzamiento de autenticación/criptografía/auditoría—
y se documentó frente a los catálogos **OWASP Web Top 10 (2021)** y **API Security Top 10 (2023)**:
**veinte ítems** revisados en total (`extra/OWASP/`). Esta cobertura es la evidencia principal del
veredicto de la característica *Security* (3.5.3 §6).

## 3.7.4 Resultados cuantitativos

| Métrica | Valor | Fuente |
|---------|-------|--------|
| Microservicios .NET 9 desplegados | 5 (Auth, CourseManagement, Attendance, Payment, Credentials) | 3.3.1 |
| Requisitos funcionales especificados | 35 (RF-001…RF-035) | 2.3 |
| Requisitos no funcionales especificados | 24 (RNF-001…RNF-024) | 2.4 |
| Pruebas automatizadas en verde | **1 251** (598 backend + 653 frontend), 0 fallos, 0 omitidas | 5.2 |
| Archivos de prueba | 199 | 5.2 |
| Características de calidad evaluadas (25010:2023) | 9 (7 cumplen + 2 con brecha) | 5.3 |
| Ítems OWASP revisados | 20 (Web 10 + API 10) | `extra/OWASP/` |
| Ventana de desarrollo | 30 de mayo – 16 de junio de 2026 | `git log` |
| Estado de despliegue | En producción (Dokploy + Cloudflare) | 3.6 |

## 3.7.5 Cierre de la trazabilidad de punta a punta

Con el capítulo completo, la cadena de trazabilidad de 2.5 queda **cerrada con todas sus columnas
redactadas**: cada objetivo se sigue hasta su evidencia de prueba y de vuelta.

| OE | Requisito (3.2) | Diseño (3.3) | Implementación (3.4) | Prueba (3.5) | Resultado (3.7) |
|----|-----------------|--------------|----------------------|--------------|-----------------|
| OE-1 | RF-001…012, 035 · RNF-001…005 | 3.3.1/3.3.3/3.3.4 | Auth (WBS E) | Suite Auth ✅ | OE-1 logrado |
| OE-2 | RF-013…019 | 3.3.3/3.3.5 | CourseManagement (WBS F) | Suite CM ✅ | OE-2 logrado |
| OE-3 | RF-020…025 · RNF-015/016 | 3.3.5/3.3.7 | Attendance (WBS G) | Suite Attendance ✅ | OE-3 logrado |
| OE-4 | RF-026…034 · RNF-009 | 3.3.4/3.3.5 | Payment (WBS H) | Suite Payment ✅ | OE-4 logrado |
| OE-5 | (transversal) · RNF-006…024 | 3.3.1/3.3.7 | Infra + OWASP (WBS L,N) | OWASP + 25010 ✅ | OE-5 logrado |

El hilo de ejemplo de 2.5.4 —**OE-1 → RF-001 → diseño `Tenant`/`TenantDomain` → RNF-002 (aislamiento)
→ prueba de la suite Auth → veredicto de *Functional completeness* y *Confidentiality*— se recorre
ahora con **todas las secciones redactadas**: la trazabilidad no es una promesa, es verificable de
extremo a extremo sobre el repositorio.

## 3.7.6 Brechas honestas y trabajo futuro

Para no presentar el resultado como una conformidad total que no se midió, se consolidan las brechas
declaradas a lo largo del capítulo:

| Brecha | Estado | Acción futura propuesta |
|--------|--------|-------------------------|
| Desempeño bajo carga (Performance / Capacity / Scalability medida) | ⚠️ Sin medir | Corrida JMeter/k6 contra el gateway con el stack levantado (p95, capacidad, CPU/memoria). |
| Accesibilidad formal (Inclusivity / User assistance) | ⚠️ No fue objetivo | Auditoría axe/Lighthouse contra WCAG 2.1 AA; `alt=`, `aria-label`, ayuda contextual. |
| Integración continua automatizada (CI) | ⚠️ No implementada | **Jenkins** reutilizando las barreras de 3.5 como puerta previa al despliegue (propuesta de 3.6.7). |
| Ensayo de restauración de respaldos | ⚠️ No documentado | Restaurar el backup S3 a una BD limpia y registrar RTO/RPO (solo producción). |

Ninguna de estas brechas invalida un objetivo: los cinco OE están logrados; las brechas señalan
**oportunidades de elevación de calidad**, no funcionalidades faltantes.

## 3.7.7 Comandos de demostración

```bash
# Volumen y ventana del proyecto
git rev-list --count HEAD
git log --pretty=format:'%ad' --date=short | sort | sed -n '1p;$p'

# Resultado de pruebas (1 251 en verde) — desde la raíz
for s in Auth Attendance CourseManagement Payment; do ( cd apps/$s/Test && dotnet test -c Release --nologo ); done
cd apps/Frontend && bun run test:ci

# Cobertura OWASP (20 ítems) y suites por servicio (cierre de trazabilidad)
ls extra/OWASP/web-top-10-2021 extra/OWASP/api-security-top-10-2023
ls apps/*/Test

# Acciones por objetivo: integraciones del WBS (3.1/4.3)
git log --merges --reverse --pretty=format:'%ad | %s' --date=short
```

## 3.7.8 Conclusión del capítulo

El Marco Práctico de DAMA recorre la cadena completa de la ingeniería de software: una **metodología**
iterativa-incremental planificada con CPM (3.1), unos **requisitos** especificados según 29148 (3.2),
un **diseño** documentado según IEEE 1016 con UML autogenerado (3.3), una **implementación** sobre un
stack .NET 9 / Angular 21 organizada por el árbol WBS (3.4), una **evaluación de calidad** según
ISO/IEC 25010:2023 con 1 251 pruebas en verde (3.5) y un **despliegue** en producción sobre Dokploy y
Cloudflare (3.6). Los **cinco objetivos específicos se alcanzaron**, con la trazabilidad cerrada de
extremo a extremo y las pocas brechas (carga, accesibilidad, CI, ensayo de restauración) declaradas
con honestidad como trabajo futuro. El resultado es una plataforma SaaS multitenant **funcional,
probada, segura y desplegada**, construida y documentada conforme a los estándares de ingeniería de
software adoptados.
