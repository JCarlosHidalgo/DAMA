#!/bin/sh
set -e

if [ -f /etc/dama/tls/ca.crt ]; then
    cp /etc/dama/tls/ca.crt /usr/local/share/ca-certificates/dama-ca.crt
    update-ca-certificates >/dev/null 2>&1 || true
fi

exec dotnet Backend.dll
