#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.dev"
ENV_EXAMPLE="$SCRIPT_DIR/.env.example"
COMPOSE_FILE="$SCRIPT_DIR/compose.dev.yaml"
REPO_ROOT="$(realpath "$SCRIPT_DIR/..")"

BOOTSTRAP=0
ARGS=()
for arg in "$@"; do
    if [[ "$arg" == "--bootstrap" ]]; then
        BOOTSTRAP=1
    else
        ARGS+=("$arg")
    fi
done

write_context() {
    local value="$1"
    if grep -qE '^CONTEXT=' "$ENV_FILE"; then
        sed -i.bak -E "s|^CONTEXT=.*|CONTEXT=$value|" "$ENV_FILE" && rm -f "$ENV_FILE.bak"
    else
        printf 'CONTEXT=%s\n%s\n' "$value" "$(cat "$ENV_FILE")" > "$ENV_FILE.new" && mv "$ENV_FILE.new" "$ENV_FILE"
    fi
}

if [[ ! -f "$ENV_FILE" ]]; then
    if [[ -f "$ENV_EXAMPLE" ]]; then
        cp "$ENV_EXAMPLE" "$ENV_FILE"
        echo "Initialized $ENV_FILE from $ENV_EXAMPLE" >&2
        BOOTSTRAP=1
    else
        echo "ERROR: ni $ENV_FILE ni $ENV_EXAMPLE existen. Repo corrupto?" >&2
        exit 1
    fi
fi

CONTEXT_VALUE="$(grep -E '^CONTEXT=' "$ENV_FILE" | tail -n 1 | cut -d= -f2- || true)"

if [[ "$BOOTSTRAP" -eq 1 || -z "$CONTEXT_VALUE" || ! -d "$CONTEXT_VALUE" ]]; then
    if [[ "$BOOTSTRAP" -ne 1 ]]; then
        echo "Detected missing or invalid CONTEXT in $ENV_FILE; auto-bootstrapping from repo root: $REPO_ROOT" >&2
    fi
    write_context "$REPO_ROOT"
    CONTEXT_VALUE="$REPO_ROOT"
    echo "CONTEXT set to $REPO_ROOT in $ENV_FILE" >&2
elif [[ "$CONTEXT_VALUE" != "$REPO_ROOT" ]]; then
    echo "WARNING: CONTEXT=$CONTEXT_VALUE in $ENV_FILE does not match current repo root ($REPO_ROOT)." >&2
    echo "         Re-run with --bootstrap if you cloned the repo to a new location." >&2
fi

if [[ ${#ARGS[@]} -eq 0 ]]; then
    ARGS=("up" "--build")
fi

exec docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "${ARGS[@]}"
