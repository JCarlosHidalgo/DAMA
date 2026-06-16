# 3.6 Despliegue (CI/CD, nube)

> ⬜ **Pendiente de redacción.** **Cubre:** Despliegue.
> **Guía:** [4-estructura-marco-practico.md](../4-estructura-marco-practico.md).

## Qué debe contener
- Estrategia de despliegue: producción en **Dokploy** (bases gestionadas, `expose` en vez de
  `ports`), TLS público vía **Cloudflare Tunnels**, TLS inter-servicio automático (`tls-init`),
  administración de esquema con DbGate tras el gateway.
- Diferencias dev/prod y el runbook de `infrastructure/environments.md`.

## Comandos de demostración (sugeridos)
```bash
sed -n '1,40p' infrastructure/compose.prod.yaml
grep -n "dbgate\|tls-init\|dokploy-network" infrastructure/compose.prod.yaml
```
