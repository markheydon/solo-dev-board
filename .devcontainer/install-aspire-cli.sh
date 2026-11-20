#!/usr/bin/env bash
# Installs Aspire CLI 17.0.0 to ~/.aspire/bin (matches AppHost Aspire.Hosting.* packages and CI).
# The devcontainer aspire feature may leave an older CLI at /usr/local/bin; prepend ~/.aspire/bin on PATH.

set -euo pipefail

ASPIRE_VERSION="17.0.0"
INSTALL_URL="https://aspire.dev/install.sh"

if ! command -v curl >/dev/null 2>&1; then
  echo "install-aspire-cli.sh: curl is required." >&2
  exit 1
fi

echo "Installing Aspire CLI ${ASPIRE_VERSION}..."
curl -sSL "${INSTALL_URL}" | bash -s -- --version "${ASPIRE_VERSION}"

export PATH="${HOME}/.aspire/bin:${PATH}"

if ! command -v aspire >/dev/null 2>&1; then
  echo "install-aspire-cli.sh: aspire not found on PATH after install." >&2
  exit 1
fi

echo "Extracting Aspire CLI bundle (dashboard + DCP)..."
aspire setup

installed="$(aspire --version 2>/dev/null || true)"
echo "Aspire CLI ready: ${installed}"