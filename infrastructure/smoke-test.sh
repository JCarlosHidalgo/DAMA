#!/usr/bin/env bash
# Smoke test for DAMA — runs Bruno collections against a stack already up.
#
# Preconditions:
#   - Stack levantado:   docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.dev.yaml up --build
#   - bru CLI disponible en PATH (npm install -g @usebruno/cli)
#   - jq disponible para parsear los login responses
#   - Seed cargado con los usuarios "Client Example", "Teacher Example",
#     "Student Example" y "w7rudO521J2adG" (admin) con password "Admin123"
#
# Target gateway:
#   - Default: http://localhost:8100 (stack local dev).
#   - Override: export API_PATH=https://api.dama-software.org  # apunta a prod.
#     El script propaga API_PATH al CLI de Bruno, sobreescribiendo el default
#     del env file ("DAMA envs.yml") sólo para esta ejecución.
#
# Cobertura: Auth, Attendance, CourseManagement, Credentials
# Excluye:   Payment (depende de Todotix externo)
#
# El script obtiene un JWT fresco para cada rol via /api/auth/login y lo
# inyecta como --env-var en cada bru run; los valores en DAMA envs.yml quedan
# como fallback pero nunca se usan durante el smoke. El env file no se modifica.
#
# Exit code 0 si todas las collections corrieron OK; >0 si alguna falló.

set -u

readonly REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WORKSPACE_ROOT="$REPO_ROOT/api-endpoints"
readonly ENV_FILE="$WORKSPACE_ROOT/environments/DAMA envs.yml"
readonly GATEWAY_URL="${API_PATH:-http://localhost:8100}"
readonly COLLECTIONS=(Auth Attendance CourseManagement Credentials)
readonly SEED_PASSWORD="Admin123"

red()    { printf '\033[31m%s\033[0m\n' "$*"; }
green()  { printf '\033[32m%s\033[0m\n' "$*"; }
yellow() { printf '\033[33m%s\033[0m\n' "$*"; }

declare -a BRU_CMD=()
CLIENT_JWT=""
ADMIN_JWT=""
STUDENT_JWT=""
TEACHER_JWT=""
SCHEDULED_CLASS_ID_TODAY=""

require_bru() {
    local bru_path
    bru_path="$(command -v bru || true)"
    if [[ -z "$bru_path" ]]; then
        red "bru CLI not found. Install with: npm install -g @usebruno/cli"
        exit 2
    fi
    # bru's shebang is `#!/usr/bin/env node`; fall back to bun when node is absent.
    if command -v node >/dev/null 2>&1; then
        BRU_CMD=("$bru_path")
    elif command -v bun >/dev/null 2>&1; then
        BRU_CMD=(bun "$bru_path")
    else
        red "Neither node nor bun is available to run bru."
        exit 2
    fi
}

require_jq() {
    if ! command -v jq >/dev/null 2>&1; then
        red "jq not found. Install with: sudo apt install jq"
        exit 2
    fi
}

require_gateway_up() {
    local probe_url="$GATEWAY_URL/api/auth/login"
    if ! curl -s -o /dev/null --max-time 5 "$probe_url"; then
        red "Gateway $GATEWAY_URL not reachable. Bring the stack up first:"
        red "  docker compose --env-file infrastructure/.env.dev -f infrastructure/compose.dev.yaml up --build"
        exit 2
    fi
}

# Fetch a JWT via /api/auth/login for the given role. Echoes the token on
# stdout (empty if login failed). Errors go to stderr so they do not contaminate
# the captured token.
#
# The gateway throttles /api/auth/login with `rate=5r/m burst=2 nodelay`, so
# four consecutive logins blow the burst. On HTTP 429 we wait one bucket cycle
# (13s) and retry. Other HTTP codes (401/400/500) abort immediately because
# they are not transient.
fetch_token() {
    local role="$1" username="$2"
    local payload raw body http_code token attempt
    payload=$(jq -nc --arg u "$username" --arg p "$SEED_PASSWORD" '{username: $u, password: $p}')
    for attempt in 1 2 3; do
        raw=$(curl -s --max-time 10 -w '\n%{http_code}' \
            -X POST "$GATEWAY_URL/api/auth/login" \
            -H 'Content-Type: application/json' \
            --data-binary "$payload")
        http_code=$(printf '%s' "$raw" | tail -n1)
        body=$(printf '%s' "$raw" | sed '$d')
        if [[ "$http_code" == "200" ]]; then
            token=$(printf '%s' "$body" | jq -r '.accessToken // empty' 2>/dev/null)
            if [[ -n "$token" ]]; then
                echo "$token"
                return 0
            fi
        fi
        if [[ "$http_code" == "429" ]]; then
            yellow "  [$role] gateway rate-limit hit, waiting 13s (attempt $attempt/3)..." >&2
            sleep 13
            continue
        fi
        break
    done
    yellow "  [$role] login failed for user '$username'. Last status: $http_code" >&2
    yellow "  [$role] body: $body" >&2
    return 1
}

refresh_jwts() {
    yellow "==> Refreshing JWTs"
    CLIENT_JWT=$(fetch_token "Client"  "Client Example"  || true)
    ADMIN_JWT=$(fetch_token   "Admin"   "w7rudO521J2adG" || true)
    STUDENT_JWT=$(fetch_token "Student" "Student Example" || true)
    TEACHER_JWT=$(fetch_token "Teacher" "Teacher Example" || true)
    echo
}

# Pick a seeded ScheduledClass whose DayOfWeekIndex matches today (in La_Paz),
# so Mark Scheduled Attendance hits a real class. The MySQL stored proc filters
# by DayOfWeekIndex = WEEKDAY(classDate)+1 (Mon=1..Sun=7). Sunday has no seeded
# class for Teacher Example, so the smoke logs a warning and the request 404s.
pick_scheduled_class_for_today() {
    local dow_iso
    dow_iso=$(TZ=America/La_Paz date '+%u')
    case "$dow_iso" in
        1) SCHEDULED_CLASS_ID_TODAY="a44397bc-0fdd-4c57-a666-f76154b3434c" ;;
        2) SCHEDULED_CLASS_ID_TODAY="4f2a2c33-65cf-4fcf-afb1-8e14e8b3c0b8" ;;
        3) SCHEDULED_CLASS_ID_TODAY="3df9e8c1-f191-4dee-9178-d0ffc226f648" ;;
        4) SCHEDULED_CLASS_ID_TODAY="cbf3b72a-16c9-4856-97ec-029d10d509e8" ;;
        5) SCHEDULED_CLASS_ID_TODAY="c5fcfe98-1113-4d80-a396-c49b21df4c2e" ;;
        6) SCHEDULED_CLASS_ID_TODAY="56e05b0d-5b8a-40da-9397-245cb0c0cb9c" ;;
        *)
            SCHEDULED_CLASS_ID_TODAY="00000000-0000-0000-0000-000000000000"
            yellow "==> No seeded ScheduledClass for Sunday — Mark Scheduled Attendance will 404"
            ;;
    esac
}

run_collection() {
    local name="$1"
    local collection_absolute="$WORKSPACE_ROOT/collections/$name"

    if [[ ! -d "$collection_absolute" ]]; then
        red "[$name] collection directory missing: $collection_absolute"
        return 1
    fi

    yellow "==> $name"
    # bru requires cwd at the collection root; the env file is referenced absolutely.
    # Requests tagged "smoke-skip" are excluded: the Auth collection's "Public- Login *"
    # files duplicate refresh_jwts and would blow the gateway's login throttle
    # (rate=5r/m burst=2) — refresh_jwts is the canonical login coverage instead.
    (cd "$collection_absolute" && "${BRU_CMD[@]}" run . -r \
        --exclude-tags smoke-skip \
        --env-file "$ENV_FILE" \
        --env-var "API_PATH=$GATEWAY_URL" \
        --env-var "Client_JWT=$CLIENT_JWT" \
        --env-var "Admin_JWT=$ADMIN_JWT" \
        --env-var "Student_JWT=$STUDENT_JWT" \
        --env-var "Teacher_JWT=$TEACHER_JWT" \
        --env-var "ScheduledClassIdToday=$SCHEDULED_CLASS_ID_TODAY")
}

main() {
    require_bru
    require_jq
    require_gateway_up
    refresh_jwts
    pick_scheduled_class_for_today

    local -a failed=()
    for collection in "${COLLECTIONS[@]}"; do
        if ! run_collection "$collection"; then
            failed+=("$collection")
        fi
        echo
    done

    echo "============================="
    if (( ${#failed[@]} == 0 )); then
        green "All collections passed."
        exit 0
    fi
    red "Failed collections: ${failed[*]}"
    exit 1
}

main "$@"
