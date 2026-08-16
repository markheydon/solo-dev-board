#!/usr/bin/env bash
# Configures ASP.NET HTTPS development certificate trust for dev containers and Codespaces.
# Persists SSL_CERT_DIR so aspire doctor and dotnet tooling find ~/.aspnet/dev-certs/trust.
# See https://aka.ms/aspire-prerequisites#dev-certs

set -euo pipefail

DEV_CERT_TRUST="${HOME}/.aspnet/dev-certs/trust"
MARKER="# solo-dev-board: ASP.NET HTTPS dev certificate trust"
SSL_CERT_EXPORT='export SSL_CERT_DIR="${SSL_CERT_DIR:+${SSL_CERT_DIR}:}/usr/lib/ssl/certs:${HOME}/.aspnet/dev-certs/trust"'

if dotnet dev-certs https --trust >/dev/null 2>&1; then
  echo "ASP.NET HTTPS development certificate trusted."
else
  echo "Warning: dotnet dev-certs https --trust could not complete; continuing startup."
  dotnet dev-certs https >/dev/null 2>&1 || true
fi

for profile in "${HOME}/.bashrc" "${HOME}/.zshrc"; do
  if [[ -f "$profile" ]] && ! grep -qF "$MARKER" "$profile"; then
    {
      echo ""
      echo "$MARKER"
      echo "$SSL_CERT_EXPORT"
    } >> "$profile"
    echo "Persisted SSL_CERT_DIR in ${profile}"
  fi
done

export SSL_CERT_DIR="${SSL_CERT_DIR:+$SSL_CERT_DIR:}/usr/lib/ssl/certs:${DEV_CERT_TRUST}"
