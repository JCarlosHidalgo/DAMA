#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.dev"
COMPOSE_FILE="$SCRIPT_DIR/compose.dev.yaml"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: $ENV_FILE no existe; no hay stack que bajar." >&2
    exit 1
fi

CONTEXT_VALUE="$(grep -E '^CONTEXT=' "$ENV_FILE" | tail -n 1 | cut -d= -f2- || true)"
if [[ -z "$CONTEXT_VALUE" || ! -d "$CONTEXT_VALUE" ]]; then
    echo "ERROR: CONTEXT inválido en $ENV_FILE (vacío o ruta inexistente)." >&2
    echo "  Editar el archivo o ejecutar: $SCRIPT_DIR/compose-up.sh --bootstrap" >&2
    exit 1
fi

ARGS=("$@")
# Default teardown removes volumes (-v) and orphan containers as well; `docker
# compose down` already drops the compose-defined networks. Applies when called
# with no args or with a bare `down`; any explicit args are respected verbatim
# (e.g. `down auth-service` or `down --rmi all` will NOT auto-add -v).
if [[ ${#ARGS[@]} -eq 0 || ( ${#ARGS[@]} -eq 1 && "${ARGS[0]}" == "down" ) ]]; then
    ARGS=("down" "-v" "--remove-orphans")
fi

exec docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "${ARGS[@]}"
