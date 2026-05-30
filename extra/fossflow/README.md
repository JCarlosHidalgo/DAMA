# FossFlow — DAMA architecture diagram

A local instance of [FossFlow](https://github.com/stan-smith/FossFLOW) (an open-source isometric
diagramming tool, a fork of Isoflow) that displays the architecture diagram of the DAMA stack.

It is **not** part of the DAMA stack: it is a self-contained visualization tool that does not touch
`infrastructure/`.

## Contents

```
extra/fossflow/
├── Dockerfile                     # thin layer over stnsmith/fossflow:latest that bakes in the diagram
├── compose.yaml                   # local build + port 8088 + persistent volume
└── diagrams/
    └── dama-architecture.json     # the diagram model (Isoflow format)
```

The base image `stnsmith/fossflow:latest` serves the SPA via nginx on port 80 and a Node (Express)
backend on `:3001` behind `/api/`. With `ENABLE_SERVER_STORAGE=true` (the default), that backend
reads/writes diagrams as `*.json` files in `/data/diagrams`. The `Dockerfile` drops
`dama-architecture.json` there, so the diagram travels inside the image.

## Run

```bash
cd extra/fossflow
docker compose up --build
```

Then open **http://localhost:8088**.

> The diagram **does not open by itself** when the page loads (FossFlow remembers the "last opened"
> one in the browser's `localStorage`, which cannot be pre-seeded from the server). To view it:
> click the **Open / Load** button (the server storage manager) and select
> **"DAMA – Arquitectura"**. It opens with a single click.

Port `8088` is configurable in `compose.yaml` (`"8088:80"`).

## Edit and export

Once open, you can move nodes, add components (the **+** button), create connections, and edit text.
You can also **Export** to download the JSON, or **Import** to load another one.

## Persistence

A **bind-mount** of `./diagrams` to `/data/diagrams` is used. This means:

- The repo file (`diagrams/dama-architecture.json`) always wins. If you edit it by hand, the change
  is reflected on page reload — **without** resetting anything or rebuilding the image.
- Edits made **inside the UI** (moving nodes, saving) are written **over that same repo file**. Keep
  this in mind if you do not want to version visual changes.

> Note: with a bind-mount, the file baked into the image by the `Dockerfile` is shadowed by the host
> folder; it only serves as a fallback for anyone running the image without mounting `./diagrams`.

## The diagram

It reflects the real flow of the stack:

- `User → Frontend (Angular 21) → api-gateway (nginx)` and from there to the 5 .NET 9 backends.
- Each backend with a database (`Auth`, `CourseManagement`, `Attendance`, `Payment`) connects
  to its own `mysql:9` instance; `Credentials` has no database.
- Outbox publishing to **RabbitMQ** from `Auth`, `CourseManagement` and `Payment` (dashed lines);
  consumption from `Attendance` and `Payment`.
- **gRPC TLS** edge: `Attendance → CourseManagement` (dotted line).
- External integration: `Payment → Todotix` (HTTPS).

### JSON format (quick reference)

`diagrams/dama-architecture.json` uses the Isoflow model:

- `name` / `title` — name shown in the list and the diagram title.
- `colors[]` — `{ id, value }`; connectors and rectangles reference `color` by id.
- `icons: []` — empty: the app injects its default icon set (`@isoflow/isopacks`). Each `item.icon`
  references a real id from that set (`server`, `storage`, `queue`, `lock`, `paymentcard`,
  `loadbalancer`, `desktop`, `document`, `cloud`, `user`, …).
- `items[]` — model nodes: `{ id, name, icon, description }`.
- `views[0]` — the view:
  - `items[]` — position of each node on the grid: `{ id, tile: { x, y } }` (x grows to the right,
    y downward).
  - `connectors[]` — `{ id, color, style: SOLID|DOTTED|DASHED, description?, anchors: [2] }`; each
    anchor references a node via `ref.item`.
  - `rectangles[]` — zones: `{ id, color, from:{x,y}, to:{x,y} }`.
  - `textBoxes[]` — labels: `{ id, tile:{x,y}, content, fontSize? }`.
