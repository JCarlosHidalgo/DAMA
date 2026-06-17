# OWASP en DAMA

Esta carpeta documenta cómo la plataforma **DAMA** (cinco microservicios .NET 9, un frontend Angular 21,
un *api-gateway* nginx, MySQL, RabbitMQ y la integración externa Todotix) cumple los controles de
seguridad de OWASP, ítem por ítem y con referencias al código y la configuración reales del repositorio.

> Los controles descritos aquí son los que existen hoy en `main`, incluyendo el endurecimiento
> introducido en las tres olas de trabajo OWASP (control de acceso, límites de recursos, y
> autenticación/criptografía/logging). Cada documento de ítem cita archivos con `ruta:línea`.

## Qué es OWASP y qué listas existen

[OWASP](https://owasp.org) (Open Worldwide Application Security Project) es una fundación sin ánimo de
lucro que publica estándares y *checklists* de seguridad de aplicaciones. Sus catálogos de riesgo más
conocidos son **listas "Top 10"**, cada una enfocada a un tipo de software:

| Lista OWASP | Enfoque | ¿Aplica a DAMA? |
|---|---|---|
| **Web Application Top 10** (2021) | Aplicaciones web y los servicios que las sirven | **Sí** — documentada aquí |
| **API Security Top 10** (2023) | APIs REST/HTTP entre máquinas | **Sí** — documentada aquí |
| Mobile Top 10 | Apps móviles nativas (iOS/Android) | No — DAMA es una SPA web, no una app móvil nativa |
| LLM / GenAI Top 10 | Aplicaciones que integran modelos de lenguaje | No — DAMA no incorpora IA generativa |
| Otros marcos (ASVS, Proactive Controls, SAMM, Cheat Sheets) | Verificación, controles y guías transversales | Complementarios; no son "Top 10" de riesgo |

DAMA es un backend de microservicios REST detrás de un gateway, consumido por una SPA. Por eso las dos
listas pertinentes son la **Web Top 10 (2021)** y la **API Security Top 10 (2023)**, que se solapan
parcialmente (acceso, autenticación, mala configuración, SSRF) pero difieren en lo específico de APIs
(BOLA, BOPLA, consumo de recursos, inventario, consumo de APIs de terceros). Ambas se cubren por
completo en esta carpeta.

## Estructura de la carpeta

```
extra/guia/academico/5.4-cumplimiento-owasp/
├── README.md                                  # este archivo
├── web-top-10-2021/                           # un .md por cada A01…A10
│   ├── A01-broken-access-control.md
│   ├── A02-cryptographic-failures.md
│   ├── A03-injection.md
│   ├── A04-insecure-design.md
│   ├── A05-security-misconfiguration.md
│   ├── A06-vulnerable-and-outdated-components.md
│   ├── A07-identification-and-authentication-failures.md
│   ├── A08-software-and-data-integrity-failures.md
│   ├── A09-security-logging-and-monitoring-failures.md
│   └── A10-server-side-request-forgery.md
└── api-security-top-10-2023/                  # un .md por cada API1…API10
    ├── API1-broken-object-level-authorization.md
    ├── API2-broken-authentication.md
    ├── API3-broken-object-property-level-authorization.md
    ├── API4-unrestricted-resource-consumption.md
    ├── API5-broken-function-level-authorization.md
    ├── API6-unrestricted-access-to-sensitive-business-flows.md
    ├── API7-server-side-request-forgery.md
    ├── API8-security-misconfiguration.md
    ├── API9-improper-inventory-management.md
    └── API10-unsafe-consumption-of-apis.md
```

Cada documento de ítem sigue la misma estructura: **qué exige OWASP**, **cómo lo cumple DAMA**
(en prosa, con citas `ruta:línea` al código y la configuración reales), **flujo de los componentes
involucrados**, y **notas y brechas conocidas**.

## Diagramas (FossFlow)

El flujo de los componentes de cada lista se grafica con la instancia FossFlow del repo
([`extra/graphics/`](../../../graphics/README.md)). Hay **dos diagramas**, uno por lista:

- `extra/graphics/diagrams/owasp-web-top-10.json` → **OWASP Web Top 10 (2021) – DAMA**
- `extra/graphics/diagrams/owasp-api-top-10.json` → **OWASP API Security Top 10 (2023) – DAMA**

En cada diagrama, **cada ítem de la lista es un rectángulo con un color propio** (A01…A10 / API1…API10)
que encierra los componentes concretos de DAMA que implementan ese control; los conectores dentro del
rectángulo muestran el orden del flujo. Para verlos:

```bash
cd extra/graphics && docker compose up --build   # http://localhost:8088
# Open / Load → "OWASP Web Top 10 (2021) – DAMA"  o  "OWASP API Security Top 10 (2023) – DAMA"
```

## Resumen de cumplimiento

Leyenda: ✅ Cubierto por controles activos · 🟢 Cubierto por diseño/proceso (no requirió código nuevo).

### Web Application Top 10 (2021)

| Ítem | Estado | Documento |
|---|---|---|
| A01 Broken Access Control | ✅ | [A01](web-top-10-2021/A01-broken-access-control.md) |
| A02 Cryptographic Failures | ✅ | [A02](web-top-10-2021/A02-cryptographic-failures.md) |
| A03 Injection | ✅ | [A03](web-top-10-2021/A03-injection.md) |
| A04 Insecure Design | 🟢 | [A04](web-top-10-2021/A04-insecure-design.md) |
| A05 Security Misconfiguration | ✅ | [A05](web-top-10-2021/A05-security-misconfiguration.md) |
| A06 Vulnerable & Outdated Components | 🟢 | [A06](web-top-10-2021/A06-vulnerable-and-outdated-components.md) |
| A07 Identification & Authentication Failures | ✅ | [A07](web-top-10-2021/A07-identification-and-authentication-failures.md) |
| A08 Software & Data Integrity Failures | ✅ | [A08](web-top-10-2021/A08-software-and-data-integrity-failures.md) |
| A09 Security Logging & Monitoring Failures | ✅ | [A09](web-top-10-2021/A09-security-logging-and-monitoring-failures.md) |
| A10 Server-Side Request Forgery | 🟢 | [A10](web-top-10-2021/A10-server-side-request-forgery.md) |

### API Security Top 10 (2023)

| Ítem | Estado | Documento |
|---|---|---|
| API1 Broken Object Level Authorization | ✅ | [API1](api-security-top-10-2023/API1-broken-object-level-authorization.md) |
| API2 Broken Authentication | ✅ | [API2](api-security-top-10-2023/API2-broken-authentication.md) |
| API3 Broken Object Property Level Authorization | ✅ | [API3](api-security-top-10-2023/API3-broken-object-property-level-authorization.md) |
| API4 Unrestricted Resource Consumption | ✅ | [API4](api-security-top-10-2023/API4-unrestricted-resource-consumption.md) |
| API5 Broken Function Level Authorization | ✅ | [API5](api-security-top-10-2023/API5-broken-function-level-authorization.md) |
| API6 Unrestricted Access to Sensitive Business Flows | ✅ | [API6](api-security-top-10-2023/API6-unrestricted-access-to-sensitive-business-flows.md) |
| API7 Server-Side Request Forgery | 🟢 | [API7](api-security-top-10-2023/API7-server-side-request-forgery.md) |
| API8 Security Misconfiguration | ✅ | [API8](api-security-top-10-2023/API8-security-misconfiguration.md) |
| API9 Improper Inventory Management | 🟢 | [API9](api-security-top-10-2023/API9-improper-inventory-management.md) |
| API10 Unsafe Consumption of APIs | ✅ | [API10](api-security-top-10-2023/API10-unsafe-consumption-of-apis.md) |

## Cómo se llegó aquí

El endurecimiento se ejecutó en tres olas, cada una mergeada a `main`:

1. **Control de acceso** — *default-deny* (FallbackPolicy en los cinco backends), `[AllowAnonymous]`
   explícito, y filtrado por tenant movido a la consulta SQL en los pagos QR/suscripción.
2. **Consumo de recursos / mala configuración** — `client_max_body_size`, IP real de Cloudflare +
   rate-limit general en nginx, y topes de paginación.
3. **Autenticación / criptografía / logging** — bloqueo de cuenta (HTTP 423), *re-hash* PBKDF2 al
   nivel OWASP 2023, y auditoría de operaciones sensibles.

Las áreas ya conformes antes de ese trabajo (inyección por *stored procedures* parametrizados,
criptografía RS256/AES-GCM, TLS inter-servicio, idempotencia de consumidores, SSRF acotado) se
mantuvieron intactas y se documentan igualmente en sus ítems.
