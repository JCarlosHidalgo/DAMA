# 3. ISO/IEC 25010:2011 — Calidad / Evaluación

> Sección 3 de la plantilla, aterrizada en DAMA.

**Qué es:** el modelo de calidad del producto de software de la familia **SQuaRE**. Define
**8 características** de calidad. Úsalo para estructurar el capítulo de pruebas/evaluación de
forma rigurosa y medible.

> Existe una revisión **ISO/IEC 25010:2023** con 9 características. Verifica cuál exige la
> universidad; 2011 sigue siendo la más citada en tesis.

## 3.1 Las 8 características (versión 2011)

| # | Característica | Significado breve |
|---|---|---|
| 1 | Adecuación funcional | ¿Hace lo que debe, de forma correcta y completa? |
| 2 | Eficiencia de desempeño | Tiempos de respuesta, uso de recursos. |
| 3 | Compatibilidad | Coexistencia e interoperabilidad. |
| 4 | Usabilidad | Facilidad de aprendizaje y uso. |
| 5 | Fiabilidad | Madurez, disponibilidad, tolerancia a fallos. |
| 6 | Seguridad | Confidencialidad, integridad, autenticidad. |
| 7 | Mantenibilidad | Facilidad de modificar/corregir el software. |
| 8 | Portabilidad | Adaptabilidad a distintos entornos. |

**No hay que evaluar las 8.** Se seleccionan las relevantes y se justifica.

---

## Cómo aplica a DAMA

Características seleccionadas para evaluar (con evidencia ya existente en el repo):

| Característica | Por qué aplica a DAMA | Evidencia / método en el repo |
|---|---|---|
| **Adecuación funcional** | El sistema debe cumplir los RF de cada dominio | Suites NUnit en Auth/Attendance/CourseManagement/Payment (~450 pruebas de lógica de negocio) + suite del frontend. |
| **Seguridad** | SaaS multitenant con datos y pagos | Catálogo OWASP completo en [`../OWASP/`](../OWASP/) (Web Top 10 2021 y API Security Top 10 2023): control de acceso *default-deny*, *account lockout*, rehash transparente, auditoría. |
| **Eficiencia de desempeño** | Consultas y reportes bajo carga | Optimizaciones reales: conteo en SQL en Attendance, *batch* concurrente del Outbox, compresión gzip de respuestas JSON en el gateway. |
| **Fiabilidad** | Servicio siempre disponible | Health checks `/health/ready` profundos, patrón Outbox + consumidor idempotente, reintentos no aplicados a operaciones no idempotentes (Todotix). |
| **Mantenibilidad** | Cinco servicios uniformes | *Gates* de complejidad con SonarAnalyzer en todos los backends, uniformidad estructural entre servicios, capa de lógica pura en el frontend con cobertura al 100%. |

> Compatibilidad y portabilidad pueden documentarse de forma breve (contenedores Docker,
> ejecución idéntica dev/prod) y justificarse como secundarias para el alcance de la tesis.

### De la característica a una métrica medible (plantilla → `academico/5.3`)

| Característica | Métrica | Método de medición | Esperado | Obtenido |
|---|---|---|---|---|
| Adecuación funcional | % de RF con prueba que pasa | Ejecución de suites NUnit + frontend | 100% | _por llenar_ |
| Seguridad | Ítems OWASP cubiertos | Revisión del catálogo `../OWASP/` | 10/10 + 10/10 | _por llenar_ |
| Eficiencia | Tiempo de respuesta p95 | Prueba de carga (ej. JMeter, 100 usuarios) | < 2 s | _por llenar_ |
| Mantenibilidad | Cero violaciones de complejidad | Build con *gates* SonarAnalyzer | 0 | _por llenar_ |

### Comandos de evidencia (se detallan en `academico/5.x`)

- Pruebas backend: `dotnet test apps/<Servicio>/Test/<Proyecto>.csproj`
- Pruebas frontend: `cd apps/Frontend && bun run test:ci`
- Cobertura con *gate*: `cd apps/Frontend && bun run test:coverage:gate`
- Estilo/complejidad: `dotnet format <Project>.csproj --verify-no-changes`

> Recuerda el alcance de las suites: por configuración de `.runsettings`, las pruebas cubren
> **lógica de negocio**, no infraestructura. Credentials es un *dummy* de solo-claims y no tiene
> casos de prueba.
