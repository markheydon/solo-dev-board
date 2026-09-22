#!/usr/bin/env bash
# Run unit and component tests (excludes Playwright E2E).
set -euo pipefail

cd "$(dirname "$0")/.."
dotnet test SoloDevBoard.UnitTests.slnf "$@"
