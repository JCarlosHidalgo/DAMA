# graphics — DAMA diagram tooling

Self-contained diagram tooling for the DAMA documentation. It is **not** part of the DAMA stack and
does not touch `infrastructure/`. It hosts two complementary generators, wired in `compose.yaml`:

1. **FossFlow** — a local instance of [FossFlow](https://github.com/stan-smith/FossFLOW) (an
   open-source isometric diagramming tool, a fork of Isoflow) that displays the non-UML isometric
   views (architecture, OWASP control flows, development phases/WBS).
2. **Doxygen + PlantUML** — the published image `juancarloshidalgososa/doxygen:1.17.0` (Doxygen
   1.17.0 with Graphviz `dot` and PlantUML preinstalled), used to (a) auto-generate the UML graphs
   of the backend from source and (b) render the hand-authored UML under `academico/`.

## Contents

```
extra/graphics/
├── Dockerfile                     # thin layer over stnsmith/fossflow:latest that bakes in the diagrams
├── compose.yaml                   # fossflow viewer (port 8088) + doxygen one-shot (profile "docs")
├── .gitignore                     # ignores out/ (generated docs)
├── diagrams/                      # FossFlow isometric views (Isoflow JSON, non-UML)
│   ├── dama-architecture.json     # the stack architecture
│   ├── owasp-web-top-10.json      # OWASP Web Top 10 (2021) controls in DAMA
│   ├── owasp-api-top-10.json      # OWASP API Security Top 10 (2023) controls in DAMA
│   └── desarrollo-fases-wbs.json  # DAMA development phases / WBS with CPM critical path
├── academico/                     # hand-authored UML (PlantUML) for the thesis design chapter
│   ├── casos-de-uso.puml          # use-case diagram (3.3.2)
│   ├── secuencia-login.puml       # sequence: login JWT/refresh (3.3.5)
│   ├── secuencia-asistencia-grpc.puml        # sequence: attendance + gRPC (3.3.5)
│   └── secuencia-pago-todotix-outbox.puml    # sequence: payment Todotix + Outbox (3.3.5)
└── out/                           # generated output (gitignored): out/doxygen, out/academico
```

## Doxygen + PlantUML (UML for the design chapter)

The `doxygen` service uses the published image (Graphviz + PlantUML bundled) to generate every UML
diagram referenced by the academic design sections (3.3.3 classes, 3.3.2 use cases, 3.3.5 sequence).
It is a **one-shot** under the `docs` profile, so it does not start with the FossFlow viewer:

```bash
cd extra/graphics
docker compose --profile docs run --rm doxygen
# Output (gitignored):
#   out/doxygen/html/    — auto-generated UML of the Auth pilot backend (classes, collaboration, dirs)
#   out/academico/       — SVG renders of casos-de-uso.puml and the three secuencia-*.puml
```

The same image also renders the PlantUML files on its own (it bundles `plantuml.jar` at
`/usr/share/plantuml/plantuml.jar`). The academic `.md` files reference these `.puml` sources and
this command; no UML is drawn by hand inside the Markdown.

There are **four** diagrams. Besides the architecture one, two security diagrams accompany
`extra/guia/academico/5.4-cumplimiento-owasp/`: each OWASP list is rendered as one diagram where **every list item is a coloured
rectangle** enclosing the concrete DAMA components that implement that control (one colour per item,
A01…A10 / API1…API10), with connectors showing the order of the control flow inside each item.
Open them the same way (Open / Load → pick the title).

The fourth diagram (**"DAMA – Desarrollo (fases / WBS)"**) accompanies the academic methodology
section `extra/guia/academico/1-metodología-de-desarrollo.md`: it renders the four project phases
(Concepción → Desarrollo → Realización → Despliegue) as coloured zones holding their work packages
(activities A–O), with red connectors highlighting the CPM critical path.

The base image `stnsmith/fossflow:latest` serves the SPA via nginx on port 80 and a Node (Express)
backend on `:3001` behind `/api/`. With `ENABLE_SERVER_STORAGE=true` (the default), that backend
reads/writes diagrams as `*.json` files in `/data/diagrams`. The `Dockerfile` drops
`dama-architecture.json` there, so the diagram travels inside the image.

## Run

```bash
cd extra/graphics
docker compose up --build
```

Then open **http://localhost:8088**.

> **Wait for the container to report `healthy` before opening the page.** The entrypoint runs an
> `npm install` and starts the Node backend (`:3001`) just before nginx, so there is a brief window
> where the SPA loads but `/api/` is not answering yet. If you open during that window, FossFlow
> falls back to browser-local storage and caches that decision for 60 s — so only the diagram cached
> in `localStorage` shows up. The `healthcheck` in `compose.yaml` (it polls `/api/storage/status`)
> exists so `docker compose up` only marks the service healthy once the backend is ready.

> The diagram **does not open by itself** when the page loads (FossFlow remembers the "last opened"
> one in the browser's `localStorage`, which cannot be pre-seeded from the server). All four live in
> the server storage manager: click the **Open / Load** button and pick the title. It opens with a
> single click. If you only see one entry there, the browser cached a stale list from before the
> other three were added — hard-reload (Ctrl+Shift+R), or clear site data for `localhost:8088`.

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
