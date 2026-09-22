#!/usr/bin/env bash
# Run Playwright E2E tests (PAT mode by default; use -Hosted for hosted sign-in gate).
# Usage: ./scripts/test-e2e.sh [-Release] [-Hosted] [extra dotnet test args]
set -euo pipefail

cd "$(dirname "$0")/.."

E2E_PROJECT="tests/E2E/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj"
CONFIG="Debug"
HOSTED=false
EXTRA_ARGS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -Release|--release)
      CONFIG="Release"
      shift
      ;;
    -Hosted|--hosted)
      HOSTED=true
      shift
      ;;
    *)
      EXTRA_ARGS+=("$1")
      shift
      ;;
  esac
done

dotnet build "$E2E_PROJECT" -c "$CONFIG" -p:SkipPlaywrightInstall=true
pwsh "tests/E2E/SoloDevBoard.E2E.Tests/bin/$CONFIG/net10.0/playwright.ps1" install chromium

if [[ "$HOSTED" == true ]]; then
  export E2E_AUTH_MODE=hosted
  FILTER_ARGS=(-- --filter-query "/[(Category=E2E)&(AuthMode=Hosted)]")
else
  export E2E_AUTH_MODE=pat
  FILTER_ARGS=(-- --filter-query "/[Category=E2E]")
fi

dotnet test "$E2E_PROJECT" --no-build -c "$CONFIG" \
  -p:SkipPlaywrightInstall=true \
  "${FILTER_ARGS[@]}" \
  "${EXTRA_ARGS[@]}"
