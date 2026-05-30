#!/usr/bin/env bash
set -euo pipefail

GATEWAY="${GATEWAY:-http://localhost:8100}"

SERVICES=(
    "auth"
    "course-management"
    "attendance"
    "credentials"
    "payment"
)

failed=0

for service in "${SERVICES[@]}"; do
    url="${GATEWAY}/api/${service}/"
    status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$url" || echo "000")

    case "$status" in
        502|504)
            printf "FAIL  %-20s %s -> %s (upstream unreachable)\n" "$service" "$url" "$status"
            failed=1
            ;;
        000)
            printf "FAIL  %-20s %s -> connection refused\n" "$service" "$url"
            failed=1
            ;;
        *)
            printf "OK    %-20s %s -> %s\n" "$service" "$url" "$status"
            ;;
    esac
done

if [[ $failed -ne 0 ]]; then
    echo
    echo "One or more gateway routes failed. Check upstream and location blocks in"
    echo "infrastructure/environments-dev/api-gateway/nginx.conf."
    exit 1
fi
