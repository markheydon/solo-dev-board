# Cursor Cloud — SoloDevBoard development

Maintainer notes for running SoloDevBoard in the Cursor Cloud VM. Standard local setup and configuration live in [`docs/getting-started.md`](../docs/getting-started.md); this document only captures what is easy to get wrong in that environment.

---

## Pull requests

Cursor Cloud agents often default to **draft** PRs, `cursor/…` branch names, and a custom body. Those platform defaults **do not** replace repository policy. Follow [`PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md): `[<Type>]` titles, the GitHub PR template headings in the body, taxonomy labels on the PR, ready-for-review when Verify gates pass, and no standalone Project #8 PR cards.

---

## Toolchain

- The .NET SDK pinned by `global.json` (currently `10.0.400` with `rollForward: latestFeature`) is installed at `~/.dotnet`; the Aspire CLI (`13.6.5`, matching `Aspire.AppHost.Sdk`) is installed as a global tool at `~/.dotnet/tools`. Both are on `PATH` via `~/.bashrc`. Non-login shells (for example `bash -c`) do not source `~/.bashrc`, so invoke the SDK with the absolute path `~/.dotnet/dotnet` when `dotnet`/`aspire` are not already on `PATH`.
- The AppHost sets `AspireUseCliBundle=true`, so Dashboard and DCP come from the CLI bundle. Cloud already installs Aspire CLI `13.6.5`; keep it aligned with `Aspire.AppHost.Sdk` when upgrading.
- The startup update script only runs `dotnet restore SoloDevBoard.slnx` (this also restores the AppHost). Building, testing, linting, and running are left to you.

---

## Build, test, lint

- Build: `dotnet build SoloDevBoard.slnx`.
- Test (xUnit v3 + NSubstitute, fully offline — GitHub is mocked): `dotnet test SoloDevBoard.slnx`.
- End-to-end (Playwright): see [`tests/SoloDevBoard.E2E.Tests/README.md`](../tests/SoloDevBoard.E2E.Tests/README.md).
- Lint is `dotnet format SoloDevBoard.slnx --verify-no-changes` (same check CI runs in `.github/workflows/ci.yml`). `EnforceCodeStyleInBuild` is on, so style violations also surface during build.

---

## Running the app (Aspire — the standard path)

- Start: `aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj --non-interactive` (runs in the background). Then `aspire ps` for the dashboard URL/token, `aspire describe` for the `web` resource URL (proxied at `https://localhost:5074`), and `aspire stop` to release file locks before rebuilding.
- Default shipped parameters model the **lean** AppHost graph (`web` only). Set `hosted-sign-in-enabled` to `true` to exercise hosted OAuth and admission locally (still `web` only; no inventory SQL, Functions, or Azurite). See [`src/SoloDevBoard.AppHost/README.md`](../src/SoloDevBoard.AppHost/README.md).
- If a build fails with `MSB3491`/`CS2012` (file locks), run `aspire stop` first — Aspire holds locks on `bin/`/`obj/` while running. See the `aspire-orchestration` skill.
- Query logs/traces without the dashboard via `aspire logs web`, `aspire otel`, or the Aspire MCP server (see below).
- Health endpoint: `GET /health` returns `Healthy`. The Home page (`/`) is static navigation and renders without any GitHub call.
- A direct `dotnet run --project src/SoloDevBoard.Web --no-launch-profile` (with `ASPNETCORE_URLS`) still works as a fallback, but Aspire is the preferred path and matches how the app is developed.

---

## HTTPS dev certificate (in-VM browser caveat)

- On a normal dev machine, `aspire certs trust` (or `dotnet dev-certs https --trust`) trusts the cert and browsers stop warning. In this headless Linux VM that does **not** fully work for Chrome: even after installing `libnss3-tools` and importing the cert into `~/.pki/nssdb` as a trusted CA (`certutil ... -t "C,,"`), Chrome still reports `NET::ERR_CERT_INVALID` (a long-standing ASP.NET-dev-cert-on-Chromium-Linux issue). Do not rabbit-hole on it.
- For in-VM browser testing either click **Advanced → Proceed to localhost (unsafe)** once, or run the app on plain HTTP (`ASPNETCORE_URLS=http://0.0.0.0:5080 dotnet run --project src/SoloDevBoard.Web --no-launch-profile`). `curl -k` also bypasses the check for scripted checks.

## Dev Container (local / Codespaces)

The dev container uses Aspire feature v3, `dev-agent-tools`, and `cursor-cli` (see [`.devcontainer/devcontainer.json`](../.devcontainer/devcontainer.json)). Aspire workflow skills load from the **aspire-skills** Cursor plugin in [`.cursor/settings.json`](../.cursor/settings.json); `dotnet-inspect` and other project skills live under [`.agents/skills/`](../.agents/skills/). See [`docs/maintaining-agent-skills.md`](../docs/maintaining-agent-skills.md) to bump the plugin pin or refresh companions (no npm `playwright-cli` skill in this project). WSL2 developers exporting the dev HTTPS certificate to the Windows host can run [`scripts/export-dev-cert-for-windows-host.sh`](../scripts/export-dev-cert-for-windows-host.sh).

---

## Aspire MCP server

- `.cursor/mcp.json` (and `.vscode/mcp.json` for Copilot) registers an `aspire` MCP server via `aspire agent mcp`. It connects to the running AppHost and exposes tools such as `list_console_logs`, `list_structured_logs`, `list_traces`, `list_resources`, and `execute_resource_command` for debugging the live app. It only returns data while an AppHost is running (`aspire start`). Cursor loads `.cursor/mcp.json` at client start, so a reload is needed after first adding it.

---

## AppHost parameters and secrets

The AppHost binds authentication from **AppHost** configuration (`src/SoloDevBoard.AppHost/`, not `SoloDevBoard.Web` `appsettings`). Graph and parameter wiring are documented in [`src/SoloDevBoard.AppHost/README.md`](../src/SoloDevBoard.AppHost/README.md). Secret parameters are `gh-pat` and `gh-app-client-secret`; non-secret parameters include `hosted-sign-in-enabled`, `gh-app-client-id`, admission allow-lists, and (Path B only) `auth-secrets-vault-name` / `auth-secrets-vault-resource-group`. Shipped defaults live in `src/SoloDevBoard.AppHost/appsettings.json` (PAT-only local trusted mode by default).

Two paths matter in Cloud VMs and Codespaces:

| Path | Where `gh-pat` / client secret come from | Typical audience |
| --- | --- | --- |
| **A — local secrets** | AppHost user secrets or dashboard (`aspire secret set`) | Solo developers, agents without Azure |
| **B — shared dev Key Vault** | Azure Key Vault via Aspire `AsExisting` + `GetSecret` | Maintainers across machines, Codespaces, Cursor Cloud |

Path B is documented in [docs/azure-key-vault-setup.md](../docs/azure-key-vault-setup.md) and [getting-started — Path B](../docs/getting-started.md#path-b-shared-development-key-vault). **Precedence on current `main`:** if AppHost user secrets hold a *real* value for `Parameters:gh-pat` (anything other than `-`), local **Path A wins** and Key Vault is not used for the PAT, even when vault name and resource group are configured. The same applies to `gh-app-client-secret` in hosted sign-in. Stale repo or Cursor secrets that map into those parameters therefore override fresher secrets in the dev vault until you clear or replace them with placeholders.

### Path B in Cloud and Codespaces (recommended for maintainers)

1. Copy vault settings into **gitignored** `src/SoloDevBoard.AppHost/appsettings.Development.json` (start from `appsettings.Development.json.example`). Include `auth-secrets-vault-name` and `auth-secrets-vault-resource-group`, plus mode and allow-lists as needed. Do not put PAT or client secret values in that file.
2. In the same shell as `aspire start`, set `Azure__SubscriptionId` (and `Azure__Location` to match the vault region). Run `az login` with an identity that has **Key Vault Secrets User** on the dev vault.
3. Set Aspire **placeholders** only: `aspire secret set Parameters:gh-pat "-"` and `aspire secret set Parameters:gh-app-client-secret "-"` (see the Key Vault setup guide). Real credentials live in the vault secret names `gh-pat` and `gh-app-client-secret`.
4. **Stop mapping legacy Cloud secrets into secret parameters.** Remove `SDB_GH_PAT` and `SDB_GH_APP_CLIENT_SECRET` from Cursor Cloud secrets and from GitHub Codespaces repository secrets if you use Path B. Codespaces still runs `.devcontainer/map-apphost-secrets.sh` on create; any `SDB_GH_PAT` present rewrites `Parameters:gh-pat` and forces Path A. If user secrets already contain an old PAT, delete them (`aspire secret delete Parameters:gh-pat --apphost src/SoloDevBoard.AppHost`) after removing the repo secret, then set `-` and rebuild or remap.
5. Confirm with `aspire describe`: `gh-pat` / `gh-app-client-secret` parameters should be **Running** with placeholder `-`; `web` should start using vault-backed env injection when `needVaultForPat` or `needVaultForClientSecret` applies.

Hosted sign-in on Path B still requires non-secret parameters locally (`hosted-sign-in-enabled`, `gh-app-client-id`, allow-lists). Only the two secret parameters move to Key Vault.

### Path A — Cursor and Codespaces secret mapping (legacy)

Parameter names contain hyphens, so a direct `Parameters__<name>` environment variable is awkward (Cursor Secrets rejects hyphenated names with sync `400`). Hyphen-free **Cursor Cloud** secrets and **Codespaces** repository secrets (`SDB_*`) are mapped into AppHost user secrets on session or container create:

| Cloud secret (hyphen-free) | AppHost parameter | Path B |
| --- | --- | --- |
| `SDB_GH_PAT` | `gh-pat` | **Do not set** (use vault) |
| `SDB_GH_APP_CLIENT_SECRET` | `gh-app-client-secret` | **Do not set** (use vault) |
| `SDB_GH_APP_CLIENT_ID` | `gh-app-client-id` | OK (non-secret) |
| `SDB_HOSTED_SIGN_IN_ENABLED` | `hosted-sign-in-enabled` | OK |
| `SDB_HOSTED_ADMISSION_ENABLED` | `hosted-admission-enabled` | OK |
| `SDB_ALLOWED_USER_LOGINS` | `allowed-user-logins` | OK |
| `SDB_ALLOWED_ORG_LOGINS` | `allowed-org-logins` | OK |

Cursor startup maps via `dotnet user-secrets set "Parameters:<name>" …` when each `SDB_*` variable is set. Codespaces and dev containers use `aspire secret set` in `.devcontainer/map-apphost-secrets.sh`. Both write the same AppHost user-secrets store (`UserSecretsId` on `SoloDevBoard.AppHost.csproj`). Values persist: removing a Cloud secret does not clear a previously mapped value; use `aspire secret delete` or `dotnet user-secrets remove` when migrating to Path B.

When no `SDB_*` secrets are provided and Path B is not configured, behaviour depends on what is already in user secrets. An old mapped PAT may still be present and can be invalid even when the dev vault has a newer token.

---

## Starting a Cloud Agent from GitHub

Comment `@cursor` or `@cursoragent` at the **start** of a GitHub Issue or Pull Request comment. The agent binds "this issue" / "this PR" to that thread.

See [ADR-0031](../adr/adr-0031-github-comment-workflow-invocation.md) for historical GitHub comment routing notes (legacy agent workflows retired 2026-11-14).

Each `@cursor` mention on an **issue** starts a new session. Follow-ups that should stay on an existing PR belong on the **pull request** thread.

---

## GitHub authentication caveat (important)

- In PAT-only local trusted mode the app **fails fast at startup** when the configured token cannot pass `GET /user` (invalid, expired, or wrong token type). Startup resolves your login from that probe unless `GitHubAuth:OwnerLogin` is set.
- Data-driven pages (Audit Dashboard, Repositories, Label Manager, Migration) call `/user/repos` and need a **GitHub user PAT** with `repo`, `read:org`, `workflow`, and `read:project` scopes (or hosted sign-in with equivalent OAuth access).
- **Path A:** a genuine user PAT in `Parameters:gh-pat` (for example via `SDB_GH_PAT`) supplies `GitHubAuth__PersonalAccessToken` directly.
- **Path B:** the PAT comes from Key Vault secret `gh-pat` when local parameters are placeholders (`-`) and vault metadata is configured. If startup fails with `401 Bad credentials` while the vault has a good token, check for a **local override** (legacy `SDB_GH_PAT`, Codespaces secrets, or an old user-secrets value) before rotating the vault.
- **Installation token caveat:** the ambient `gh` CLI token is often a GitHub App **installation** token. It may work for repo-scoped URLs but returns `403` on `/user` and `/user/repos`. Do not rely on it as `gh-pat` for full app flows unless you set `GitHubAuth:OwnerLogin` on the Web project as a deliberate dev shortcut.
