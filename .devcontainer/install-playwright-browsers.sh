#!/usr/bin/env bash
# Installs Chromium for C# Playwright E2E (Microsoft.Playwright via NuGet playwright.ps1).
# No Node.js or npm — see tests/SoloDevBoard.E2E.Tests/README.md.

set -euo pipefail

cd "$(dirname "$0")/.."

E2E_PROJECT="tests/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj"
CONFIG="Debug"
TFM="net10.0"
PLAYWRIGHT_SCRIPT="tests/SoloDevBoard.E2E.Tests/bin/${CONFIG}/${TFM}/playwright.ps1"

if ! command -v pwsh >/dev/null 2>&1; then
  echo "install-playwright-browsers.sh: pwsh is required to run Microsoft.Playwright install." >&2
  exit 1
fi

echo "Building E2E project (Debug) for Playwright driver..."
dotnet restore "$E2E_PROJECT"
dotnet build "$E2E_PROJECT" -c "$CONFIG" --no-restore -p:SkipPlaywrightInstall=true

echo "Installing Chromium for Playwright E2E..."
pwsh "$PLAYWRIGHT_SCRIPT" install chromium

echo "Installing Chromium system dependencies (apt)..."
pwsh "$PLAYWRIGHT_SCRIPT" install-deps chromium

echo "Playwright Chromium install complete."