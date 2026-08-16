# Cursor Cloud — SoloDevBoard development

Maintainer notes for running SoloDevBoard in the Cursor Cloud VM. Standard local setup and configuration live in [`docs/getting-started.md`](../docs/getting-started.md); this document only captures what is easy to get wrong in that environment.

---

## Pull requests

Cursor Cloud agents often default to **draft** PRs, `cursor/…` branch names, and a custom body. Those platform defaults **do not** replace repository policy. Follow [`PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md): `[<Type>]` titles, the GitHub PR template headings in the body, taxonomy labels on the PR, ready-for-review when Verify gates pass, and no standalone Project #8 PR cards.

---

## Toolchain

- The .NET SDK pinned by `global.json` (`10.0.300`) is installed at `~/.dotnet`; the Aspire CLI (`13.4.6`, matching `Aspire.AppHost.Sdk`) is installed as a global tool at `~/.dotnet/tools`. Both are on `PATH` via `~/.bashrc`. Non-login shells (for example `bash -c`) do not source `~/.bashrc`, so invoke the SDK with the absolute path `~/.dotnet/dotnet` when `dotnet`/`aspire` are not already on `PATH`.
- The startup update script only runs `dotnet restore SoloDevBoard.slnx` (this also restores the AppHost). Building, testing, linting, and running are left to you.

---

## Build, test, lint

- Build: `dotnet build SoloDevBoard.slnx`.
- Test (xUnit v3 + NSubstitute, fully offline — GitHub is mocked): `dotnet test SoloDevBoard.slnx`.
- End-to-end (Playwright): see [`tests/E2E/README.md`](../tests/E2E/README.md).
- Lint is `dotnet format SoloDevBoard.slnx --verify-no-changes` (same check CI runs in `.github/workflows/ci.yml`). `EnforceCodeStyleInBuild` is on, so style violations also surface during build.

---

## Running the app (Aspire — the standard path)

- Start: `aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj --non-interactive` (runs in the background). Then `aspire ps` for the dashboard URL/token, `aspire describe` for the `app` resource URL (proxied at `https://localhost:5074`), and `aspire stop` to release file locks before rebuilding.
- If a build fails with `MSB3491`/`CS2012` (file locks), run `aspire stop` first — Aspire holds locks on `bin/`/`obj/` while running. See the `aspire-orchestration` skill.
- Query logs/traces without the dashboard via `aspire logs app`, `aspire otel`, or the Aspire MCP server (see below).
- Health endpoint: `GET /health` returns `Healthy`. The Home page (`/`) is static navigation and renders without any GitHub call.
- A direct `dotnet run --project src/App/SoloDevBoard.App --no-launch-profile` (with `ASPNETCORE_URLS`) still works as a fallback, but Aspire is the preferred path and matches how the app is developed.

---

## HTTPS dev certificate (in-VM browser caveat)

- On a normal dev machine, `aspire certs trust` (or `dotnet dev-certs https --trust`) trusts the cert and browsers stop warning. In this headless Linux VM that does **not** fully work for Chrome: even after installing `libnss3-tools` and importing the cert into `~/.pki/nssdb` as a trusted CA (`certutil ... -t "C,,"`), Chrome still reports `NET::ERR_CERT_INVALID` (a long-standing ASP.NET-dev-cert-on-Chromium-Linux issue). Do not rabbit-hole on it.
- For in-VM browser testing either click **Advanced → Proceed to localhost (unsafe)** once, or run the app on plain HTTP (`ASPNETCORE_URLS=http://0.0.0.0:5080 dotnet run --project src/App/SoloDevBoard.App --no-launch-profile`). `curl -k` also bypasses the check for scripted checks.

---

## Aspire MCP server

- `.cursor/mcp.json` (and `.vscode/mcp.json` for Copilot) registers an `aspire` MCP server via `aspire agent mcp`. It connects to the running AppHost and exposes tools such as `list_console_logs`, `list_structured_logs`, `list_traces`, `list_resources`, and `execute_resource_command` for debugging the live app. It only returns data while an AppHost is running (`aspire start`). Cursor loads `.cursor/mcp.json` at client start, so a reload is needed after first adding it.

---

## AppHost parameters and secrets

- The AppHost defines these parameters (see `src/SoloDevBoard.AppHost/AppHost.cs`): `hosted-sign-in-enabled`, `gh-pat` (secret), `gh-app-client-id`, `gh-app-client-secret` (secret), `hosted-admission-enabled`, `allowed-user-logins`, `allowed-org-logins`. Non-secret defaults live in `src/SoloDevBoard.AppHost/appsettings.json` (PAT mode by default).
- **Cloud Secrets → Aspire parameters mapping.** The parameter names contain hyphens, and a direct `Parameters__<name>` env var is impossible because environment-variable names cannot contain hyphens (the Cursor Secrets panel rejects them with a sync `400`). Instead, add Cursor secrets with hyphen-free names and let the startup update script map them into AppHost user secrets:

  | Cursor secret (hyphen-free) | AppHost parameter |
  |---|---|
  | `SDB_GH_PAT` | `gh-pat` |
  | `SDB_GH_APP_CLIENT_ID` | `gh-app-client-id` |
  | `SDB_GH_APP_CLIENT_SECRET` | `gh-app-client-secret` |
  | `SDB_HOSTED_SIGN_IN_ENABLED` | `hosted-sign-in-enabled` |
  | `SDB_HOSTED_ADMISSION_ENABLED` | `hosted-admission-enabled` |
  | `SDB_ALLOWED_USER_LOGINS` | `allowed-user-logins` |
  | `SDB_ALLOWED_ORG_LOGINS` | `allowed-org-logins` |

  The update script runs `dotnet user-secrets set "Parameters:<name>" "$SDB_…"` for each variable that is set (guarded, idempotent), so provided secrets are picked up automatically on every session. Values persist in AppHost user secrets; removing a Cursor secret does not clear a previously-mapped value, so run `dotnet user-secrets remove "Parameters:<name>" --project src/SoloDevBoard.AppHost` if you need to clear one. You can also set values manually with `aspire secret set Parameters:<name> "…"`.
- When no `SDB_*` secrets are provided, the persisted AppHost user secrets fall back to placeholder values (`gh-pat` = the ambient `gh` installation token, `gh-app-client-secret` = `-`) so `aspire start` still boots. When `SDB_GH_PAT` is set, the update script maps it into `Parameters:gh-pat`, giving the app a genuine user PAT for full functionality.

---

## GitHub authentication caveat (important)

- In the default PAT mode the app **fails fast at startup** unless a token is configured, and it resolves your login from the token via `/user` unless `GitHubAuth:OwnerLogin` is set.
- All data-driven pages (Audit Dashboard, Repositories, Label Manager, Migration) list repositories through the authenticated `/user/repos` endpoint. This needs a **GitHub user PAT** with `repo`, `read:org`, `workflow`, and `read:project` scopes.
- When `SDB_GH_PAT` holds a genuine user PAT, the login auto-resolves from the token via `/user`, no App-level `GitHubAuth:OwnerLogin` override is required, and all data-driven pages work end-to-end.
- Fallback caveat: the ambient `gh` CLI token in the VM is often a GitHub App **installation** token — it can reach repo-scoped endpoints (`/repos/{owner}/{repo}/...`) but returns `403 "Resource not accessible by integration"` on `/user` and `/user/repos`. If `SDB_GH_PAT` is removed and `gh-pat` falls back to that installation token, the app still boots but **cannot** exercise the repo-list flows; set `GitHubAuth:OwnerLogin` on `src/App/SoloDevBoard.App` (or restore a real user PAT via `SDB_GH_PAT`) to unblock demos.
