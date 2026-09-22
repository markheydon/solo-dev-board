#!/usr/bin/env bash
# Full CI parity: unit/component tests, then E2E PAT, then E2E hosted (sequential, fail fast).
# Usage: ./scripts/test-all.sh [-Release] [extra args passed to unit test only]
set -euo pipefail

SCRIPT_DIR="$(dirname "$0")"
RELEASE_FLAG=()
EXTRA_ARGS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -Release|--release)
      RELEASE_FLAG=(-Release)
      shift
      ;;
    *)
      EXTRA_ARGS+=("$1")
      shift
      ;;
  esac
done

"$SCRIPT_DIR/test-unit.sh" "${EXTRA_ARGS[@]}"
"$SCRIPT_DIR/test-e2e.sh" "${RELEASE_FLAG[@]}"
"$SCRIPT_DIR/test-e2e.sh" "${RELEASE_FLAG[@]}" -Hosted
