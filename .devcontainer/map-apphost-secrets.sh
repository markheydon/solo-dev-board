#!/usr/bin/env bash
# Maps Codespaces / devcontainer SDB_* environment variables to AppHost user secrets.
# Secret names mirror the Cursor Cloud Environment mapping in plan/CURSOR_CLOUD.md.

set -euo pipefail

PROJECT="src/SoloDevBoard.AppHost"

map_parameter() {
  local env_name="$1"
  local param_name="$2"
  local value="${!env_name:-}"

  if [[ -n "$value" ]]; then
    dotnet user-secrets set "Parameters:${param_name}" "$value" --project "$PROJECT"
    echo "Mapped ${env_name} -> Parameters:${param_name}"
  fi
}

map_parameter SDB_GH_PAT gh-pat
map_parameter SDB_GH_APP_CLIENT_ID gh-app-client-id
map_parameter SDB_GH_APP_CLIENT_SECRET gh-app-client-secret
map_parameter SDB_HOSTED_SIGN_IN_ENABLED hosted-sign-in-enabled
map_parameter SDB_HOSTED_ADMISSION_ENABLED hosted-admission-enabled
map_parameter SDB_ALLOWED_USER_LOGINS allowed-user-logins
map_parameter SDB_ALLOWED_ORG_LOGINS allowed-org-logins
