#!/usr/bin/env bash
#
# Bootstraps the self-signed PKI used for inter-service gRPC TLS.
# Generates one CA + one server cert per gRPC *server* backend, each with
# the container hostname as the SAN so HttpClient validates the upstream by
# name without any custom callback. There are two gRPC servers: CourseManagement
# (consumed by Attendance) and Auth (consumed by Payment for the synchronous
# tenant-subscription update). Their clients (Attendance, Payment) only need
# ca.crt in their trust store, no cert of their own. Each SAN hostname comes
# from the matching *_HOST_NAME var (passed by the tls-init compose service),
# so it tracks the same var as the gRPC URL and container_name; changing it
# requires deleting the dama-tls volume to regenerate (the existing cert is
# otherwise kept by the idempotency check).
#
# Output goes to TLS_DIR (default infrastructure/tls/, gitignored — the .key
# files never leave the host). The tls-init compose service reuses this exact
# script with TLS_DIR=/tls to populate the dama-tls volume on every deploy.
# Idempotent: re-running skips anything already present, so it is safe to run
# on every `docker compose up`; delete a cert to force regeneration (rotation).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TLS_DIR="${TLS_DIR:-${SCRIPT_DIR}/tls}"
mkdir -p "${TLS_DIR}"
cd "${TLS_DIR}"

CA_DAYS=3650
SERVER_DAYS=825

generate_ca() {
    if [[ -f ca.crt && -f ca.key ]]; then
        echo "ca.crt + ca.key already exist; skipping CA generation"
        return
    fi
    openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out ca.key
    openssl req -x509 -new -key ca.key -days "${CA_DAYS}" -out ca.crt \
        -subj "/CN=DAMA Internal CA" \
        -addext "basicConstraints=critical,CA:TRUE" \
        -addext "keyUsage=critical,keyCertSign,cRLSign"
    echo "generated ca.crt (${CA_DAYS} days)"
}

generate_server_cert() {
    local name="$1"        # file prefix, e.g. payment
    local hostname="$2"    # SAN value, e.g. PaymentService

    if [[ -f "${name}.crt" && -f "${name}.key" ]]; then
        echo "${name}.crt + ${name}.key already exist; skipping"
        return
    fi
    openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "${name}.key"
    openssl req -new -key "${name}.key" -out "${name}.csr" \
        -subj "/CN=${hostname}"
    openssl x509 -req -in "${name}.csr" \
        -CA ca.crt -CAkey ca.key -CAcreateserial \
        -out "${name}.crt" -days "${SERVER_DAYS}" -sha256 \
        -extfile <(cat <<EOF
basicConstraints = critical, CA:FALSE
keyUsage = critical, digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = DNS:${hostname}
EOF
)
    rm -f "${name}.csr"
    echo "generated ${name}.crt -> SAN=${hostname} (${SERVER_DAYS} days)"
}

generate_ca

for entry in \
    "course-management:${COURSE_MANAGEMENT_HOST_NAME:-CourseManagementService}" \
    "auth:${AUTH_HOST_NAME:-AuthService}"
do
    name="${entry%%:*}"
    hostname="${entry##*:}"
    generate_server_cert "${name}" "${hostname}"
done

chmod 600 "${TLS_DIR}"/*.key
echo ""
echo "TLS bundle ready in ${TLS_DIR}"
echo "Files .key are gitignored; .crt + ca.crt are not committed either (whole tls/ dir is ignored)."
