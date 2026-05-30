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
if [[ ${#ARGS[@]} -eq 0 ]]; then
    ARGS=("down")
fi

exec docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "${ARGS[@]}"
