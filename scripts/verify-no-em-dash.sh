#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

pattern=$'\xE2\x80\x94'

rg_args=(
  --line-number
  --no-heading
  --glob '!.git/**'
  --glob '!**/node_modules/**'
  --glob '!**/bin/**'
  --glob '!**/obj/**'
  "$pattern"
  src
  tests
  docs
  adr
  specs
  plan
  .github
)

for md in AGENTS.md CONVENTIONS.md README.md SCOPE.md VERSIONING.md GOALS.md; do
  if [[ -f "$md" ]]; then
    rg_args+=("$md")
  fi
done

if matches="$(rg "${rg_args[@]}" 2>/dev/null)"; then
  echo "Em dash (U+2014) is forbidden in product sources."
  echo "Use spaced ASCII hyphen ' - ' in prose and comments; use '-' for empty UI placeholders."
  echo "See CONVENTIONS.md (Documentation)."
  echo
  echo "$matches"
  exit 1
fi

echo "No em dash found in product scope."
