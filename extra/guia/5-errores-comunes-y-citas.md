# 5. Errores comunes y recomendaciones

> Sección 5 de la plantilla.

- **No copies plantillas completas** del estándar. Usa solo lo relevante y justifica las
  omisiones (*tailoring*).
- **Mantén la trazabilidad** de punta a punta: objetivos → requisitos → diseño → pruebas. Es el
  hilo conductor que demuestra rigor. En DAMA, ese hilo termina en evidencia ejecutable del repo
  (suites de pruebas, catálogo OWASP, comandos de demostración).
- **No inventes datos.** En DAMA, las cifras (conteo de pruebas, fechas, duraciones del CPM) se
  derivan del repositorio (`git log`, ejecución de suites), no de estimaciones optimistas
  (recomendación de Lockyer & Gordon en la planificación).
- **No dibujes UML a mano** si la herramienta lo genera. DAMA usa Doxygen (backends) y Compodoc
  (frontend): se referencia el diagrama generado y su comando, no se redibuja.
- **Cita correctamente** cada estándar. Ejemplos:
  - IEEE: `ISO/IEC/IEEE 29148:2018, Systems and software engineering — Life cycle processes — Requirements engineering.`
  - APA 7: `International Organization for Standardization. (2023). ISO/IEC 25010:2023 Systems and software engineering — SQuaRE — Product quality model.`
- **Verifica versiones** antes de citar (29148:2018, 1016:2009, 25010:**2023** — edición vigente
  usada en este trabajo; 25010:2011 sigue siendo válida si la universidad la exige).
- **El reglamento de la universidad prevalece.** Si exige una estructura distinta, ajústate a
  ella y usa estos estándares para enriquecer el contenido.

## Estándares y fuentes referenciados

- **ISO/IEC/IEEE 29148:2018** — Ingeniería de requisitos (sucesora de IEEE 830-1998).
- **IEEE 1016-2009** — Software Design Descriptions (SDD).
- **ISO/IEC 25010:2023** — Modelo de calidad del producto (familia SQuaRE), edición vigente
  (9 características); sucede a la 25010:2011.
- Complementarios: **ISO/IEC/IEEE 12207** (procesos del ciclo de vida), **UML / ISO/IEC 19501**.
- **Gestión de proyecto:** K. Lockyer y J. Gordon, *Project Management and Project Network
  Techniques* — marco del análisis CPM aplicado en `academico/3.1` (ver [`cpm.txt`](../../cpm.txt)).

## Bibliografía de cita (formato sugerido)

```
Lockyer, K., & Gordon, J. Project Management and Project Network Techniques. Pearson Education.
International Organization for Standardization. (2018). ISO/IEC/IEEE 29148:2018.
Institute of Electrical and Electronics Engineers. (2009). IEEE 1016-2009.
International Organization for Standardization. (2023). ISO/IEC 25010:2023.
```
