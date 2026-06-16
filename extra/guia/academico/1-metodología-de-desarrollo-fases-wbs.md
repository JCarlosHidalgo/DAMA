# Diagrama FossFlow — Fases del WBS de DAMA

> Diagrama complementario de la sección [`1-metodología-de-desarrollo.md`](1-metodología-de-desarrollo.md).
> Vista **isométrica** (no-UML) de las fases del WBS y las dependencias entre actividades.
> No reemplaza la red CPM en draw.io ([`1-metodología-de-desarrollo-cpm-ruta-critica.drawio`](1-metodología-de-desarrollo-cpm-ruta-critica.drawio)),
> que es la fuente de los tiempos (EST/EFT/LST/LFT) y la ruta crítica.

## Qué muestra

Las **cuatro fases** del proyecto como zonas de color, con sus paquetes de trabajo (actividades
A–O) dentro, y conectores que representan las dependencias Fin a Inicio:

- **1. Concepción** (azul): A, B.
- **2. Desarrollo** (naranja): C, D.
- **3. Realización** (verde): E, F, G, H, I, J, K, L.
- **4. Endurecimiento y despliegue** (morado): M, N, O.

Los conectores **rojos sólidos** marcan las dependencias de la **ruta crítica**; los **grises
punteados**, las dependencias con holgura. Es coherente 1:1 con la tabla CPM y con el diagrama
draw.io.

## Cómo abrirlo

El diagrama vive en formato Isoflow en
[`../../fossflow/diagrams/desarrollo-fases-wbs.json`](../../fossflow/diagrams/desarrollo-fases-wbs.json).
Para visualizarlo en la instancia local de FossFlow:

```bash
cd extra/fossflow
docker compose up --build
```

Luego abrir **http://localhost:8088**, pulsar **Open / Load** y seleccionar
**"DAMA – Desarrollo (fases / WBS)"**.

> El ciclo de vida de los contenedores lo gestiona el usuario; este documento no levanta el stack.

## Contenido del diagrama (Isoflow JSON)

```json
{
  "name": "DAMA – Desarrollo (fases / WBS)",
  "title": "DAMA – Desarrollo (fases / WBS)",
  "version": "1.0",
  "fitToScreen": true,
  "icons": [],
  "colors": [
    { "id": "c-blue", "value": "#3b82f6" },
    { "id": "c-orange", "value": "#f59e0b" },
    { "id": "c-green", "value": "#10b981" },
    { "id": "c-purple", "value": "#8b5cf6" },
    { "id": "c-grey", "value": "#94a3b8" },
    { "id": "c-red", "value": "#ef4444" }
  ],
  "items": [
    { "id": "A", "name": "A · Viabilidad y alcance", "icon": "document" },
    { "id": "B", "name": "B · Requisitos (SRS) + stack", "icon": "document" },
    { "id": "C", "name": "C · Arquitectura + gRPC", "icon": "loadbalancer" },
    { "id": "D", "name": "D · Diseño de datos", "icon": "storage" },
    { "id": "E", "name": "E · Backend Auth", "icon": "lock" },
    { "id": "F", "name": "F · CourseManagement", "icon": "server" },
    { "id": "G", "name": "G · Attendance", "icon": "server" },
    { "id": "H", "name": "H · Payment", "icon": "paymentcard" },
    { "id": "I", "name": "I · Credentials", "icon": "document" },
    { "id": "J", "name": "J · Outbox + RabbitMQ", "icon": "queue" },
    { "id": "K", "name": "K · Frontend Angular 21", "icon": "desktop" },
    { "id": "L", "name": "L · Infraestructura", "icon": "cloud" },
    { "id": "M", "name": "M · Pruebas + uniformidad", "icon": "server" },
    { "id": "N", "name": "N · OWASP + ISO + docs", "icon": "lock" },
    { "id": "O", "name": "O · Despliegue producción", "icon": "cloud" }
  ]
}
```

> El JSON completo (con la vista, posiciones, zonas y conectores) es el archivo
> `../../fossflow/diagrams/desarrollo-fases-wbs.json`. Aquí se muestra el modelo de nodos por
> brevedad; la fuente de verdad es ese archivo.
