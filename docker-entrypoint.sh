#!/bin/bash
set -e

# ---------------------------------------------------------------------------
# docker-entrypoint.sh — Trust the Kestrel self-signed certificate
# ---------------------------------------------------------------------------
# When services communicate over HTTPS inside Docker using a self-signed
# certificate, outbound HttpClient calls fail with "UntrustedRoot" because
# the certificate is not in the container's CA trust store.
#
# This script extracts the public certificate from the Kestrel PFX file and
# adds it to the OS trust store before starting the application.
# ---------------------------------------------------------------------------

CERT_PFX="${ASPNETCORE_Kestrel__Certificates__Default__Path}"
CERT_PASS="${ASPNETCORE_Kestrel__Certificates__Default__Password}"

if [ -n "$CERT_PFX" ] && [ -f "$CERT_PFX" ]; then
    echo "[entrypoint] Importing Kestrel certificate into trusted CA store..."
    openssl pkcs12 -in "$CERT_PFX" -clcerts -nokeys -passin "pass:${CERT_PASS}" 2>/dev/null \
        | openssl x509 -out /usr/local/share/ca-certificates/aspnetapp.crt 2>/dev/null
    update-ca-certificates --fresh > /dev/null 2>&1
    echo "[entrypoint] Certificate imported successfully."
else
    echo "[entrypoint] No Kestrel PFX found at '${CERT_PFX}' — skipping certificate import."
fi

exec "$@"
