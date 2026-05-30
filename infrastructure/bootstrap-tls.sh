#!/usr/bin/env bash
#
# Bootstraps the self-signed PKI used for inter-service gRPC TLS.
# Generates one CA + one server cert per gRPC backend, each with the
# container hostname as the SAN so HttpClient validates the upstream by
# name without any custom callback.
#
# Run once after cloning, and again to rotate. Output goes to
# infrastructure/tls/, which is gitignored — the .key files never leave
# the host.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TLS_DIR="${SCRIPT_DIR}/tls"
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
    "course-management:CourseManagementService" \
    "attendance:AttendanceService"
do
    name="${entry%%:*}"
    hostname="${entry##*:}"
    generate_server_cert "${name}" "${hostname}"
done

chmod 600 "${TLS_DIR}"/*.key
echo ""
echo "TLS bundle ready in ${TLS_DIR}"
echo "Files .key are gitignored; .crt + ca.crt are not committed either (whole tls/ dir is ignored)."
