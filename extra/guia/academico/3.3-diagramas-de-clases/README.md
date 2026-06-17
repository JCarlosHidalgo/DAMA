# 3.3.3 — Diagramado detallado de clases por servicio

Carpeta de apoyo de la sección [3.3.3 Diagrama de clases](../3.3-diagrama-de-clases.md). Mientras
aquel archivo da la **vista general** (entidades y patrones por servicio), aquí se documenta el
diagramado **clase por clase** de cada servicio, apoyándose en los grafos que **Doxygen** genera
desde el código (jerarquía gráfica, herencia y colaboración).

## Enfoque

Cada archivo de servicio tiene dos partes:

- **a) Jerarquía gráfica** — cómo se organiza el código por *namespaces* y patrones estructurales,
  con un título de figura y su función por cada grupo estructural.
- **b) Diagramas de herencia y colaboración** — una lista numerada con una entrada por cada
  clase/interfaz **implementada** en el servicio; cada entrada titula su diagrama de herencia y su
  diagrama de colaboración y explica, en viñetas, las relaciones que muestran (sin describir métodos).

La estructura completa y las reglas están en [`_plantilla.md`](_plantilla.md).

## Estado

| Archivo | Servicio | Sección | Estado |
|---------|----------|---------|--------|
| [`_plantilla.md`](_plantilla.md) | (plantilla reutilizable) | — | ✅ |
| [`credentials.md`](credentials.md) | Credentials | 3.3.3.6 | ✅ Redactado (piloto) |
| [`auth.md`](auth.md) | Auth | 3.3.3.7 | ✅ Redactado |
| [`course-management.md`](course-management.md) | CourseManagement | 3.3.3.8 | ✅ Redactado |
| [`attendance.md`](attendance.md) | Attendance | 3.3.3.9 | ✅ Redactado |
| [`payment.md`](payment.md) | Payment | 3.3.3.10 | ✅ Redactado |
| [`frontend.md`](frontend.md) | Frontend (Angular / Compodoc) | 3.3.3.11 | ✅ Redactado |

> Las imágenes se generan con: `cd extra/graphics && docker compose --profile docs run --rm doxygen`.
