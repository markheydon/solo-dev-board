# AI Collaborator Instructions — SoloDevBoard

Platform-neutral standards for GitHub Copilot, Cursor, and other AI agents working in this repository.

## Project Overview

**SoloDevBoard** is a .NET 10 Blazor Server application that provides a single pane of glass for solo developers to manage GitHub workloads across multiple repositories.

---

## Language & Framework

- **Language:** C# 14
- **Framework:** .NET 10 / Blazor Server
- **Target runtime:** `net10.0`

---

## UK English Requirement

> **All code comments, string literals, user-facing text, documentation, and commit messages MUST be written in UK English.**

> **All bullet point list items that form complete sentences MUST end with a full stop (`.`).** This applies to all documentation: planning files, user guides, ADRs, agent definitions, prompt files, and inline code comments.

Use the following spellings consistently:
- `colour` (not color)
- `organise` (not organize)
- `recognise` (not recognize)
- `licence` as a noun, `license` as a verb
- `analyse` (not analyze)
- `prioritise` (not prioritize)
- `behaviour` (not behavior)
- `centre` (not center)
- `favour` (not favor)

---

## Architecture

The solution follows a **clean/layered architecture** with the following projects:

```
SoloDevBoard.App             → Blazor Server UI (presentation layer)
SoloDevBoard.Application     → Application logic, use cases, service interfaces
SoloDevBoard.Domain          → Domain entities, value objects, domain events
SoloDevBoard.Infrastructure  → GitHub API clients, persistence, external integrations
```

### Rules
- **Domain** has no external dependencies.
- **Application** depends on Domain only.
- **Infrastructure** depends on Application and Domain (implements interfaces).
- **App** depends on Application (calls use cases via services/mediators).
- Use constructor injection throughout; avoid service locator patterns.

### Boundary Data Shapes (ADR-0011)

Two explicit rules govern what types cross each boundary:

1. **Repository boundary (Infrastructure ↔ Application):** `IRepository` interfaces return and accept **domain entity records** (`Label`, `Repository`, etc.). Infrastructure translates external API responses to domain records before crossing this boundary.
2. **Application→App boundary:** All public Application service interfaces (`I*Service`, `I*Manager`) return and accept **DTO records** (`LabelDto`, `RepositoryDto`, etc.) defined in `SoloDevBoard.Application`. **Domain entities must never appear in the public signature of these interfaces.**

DTOs are `sealed record` types named `<Entity>Dto`, co-located in `SoloDevBoard.Application` alongside the service interface that uses them. Mapping from domain entity to DTO happens in the Application service implementation — not in Razor components, not via AutoMapper.

---

## Coding Conventions

- **Nullable reference types:** enabled in all projects (`<Nullable>enable</Nullable>`).
- **Implicit usings:** enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
- **Records** for domain entities and value objects wherever immutability is appropriate.
- **File-scoped namespaces** for all `.cs` files.
- **Primary constructors** where they improve readability.
- Prefer `IReadOnlyList<T>` and `IReadOnlyDictionary<TKey, TValue>` over mutable collections in public APIs.
- All public members must have XML doc comments (`///`).
- Use `ArgumentNullException.ThrowIfNull()` for guard clauses.

---

## Testing

- **Framework:** xUnit
- **Mocking:** Moq
- **Naming convention:** `MethodUnderTest_Scenario_ExpectedOutcome`
- Test projects mirror the structure of source projects.
- Arrange / Act / Assert sections separated by blank lines (no comments required).
- Use xUnit's built-in `Assert.*` methods for all assertions. **Do not add FluentAssertions** — it requires a commercial licence and is prohibited in this open-source project (see ADR-0008).

---

## When Adding a New Feature

When asked to add a feature, **must** perform the following steps in order:

1. **Update `plan/BACKLOG.md`** — add the feature to the relevant epic, formatted as a user story.
2. **Update `plan/SCOPE.md`** — if the feature changes scope, update the in-scope or out-of-scope sections.
3. **Create a wireframe in `plan/wireframes/`** and update `plan/wireframes/README.md` if the feature will result in a new page, a new major page region, or a substantive page refresh.
4. **Create or update an ADR** in `adr/` if the feature requires an architectural decision.
5. **Create a stub in `docs/user-guide/`** if the feature is user-facing.
6. **Update `docs/index.md`** quick links if a new doc page is added.
7. **Open a GitHub Issue** (or instruct the developer to do so) following the label strategy in `plan/LABEL_STRATEGY.md`.
8. **Implement the feature** following the architecture and conventions above.
9. **Add or update tests** in the appropriate test project.

---

## Label Strategy

When referencing or suggesting labels for GitHub Issues and PRs, follow the taxonomy defined in **`plan/LABEL_STRATEGY.md`**. Key label groups:

- `type/` — epic, feature, story, enabler, test, bug, chore, documentation
- `priority/` — critical, high, medium, low
- `status/` — todo, in-progress, blocked, in-review, done
- `area/` — dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs
- `size/` — xs, s, m, l, xl

---

## Infrastructure

- Production deployment uses **Aspire** (`aspire deploy`) from `SoloDevBoard.AppHost` to **Azure Container Apps** (ADR-0018).
- Aspire generates and applies deployment Bicep at deploy time; do not maintain hand-authored `infra/*.bicep` for production hosting.
- Secrets for hosted deployments are supplied via AppHost parameters from GitHub Environment secrets at deploy time.
- See `docs/deployment.md` for operator deployment instructions.

---

## Open Source & Security

This repository is **open source and public**. The following guidelines ensure security and code quality:

### Secrets Management

- **Golden Rule:** Never commit secrets, credentials, API keys, or personal data to this repository.
- **GitHub Tokens for app runtime:** Must be stored in **GitHub Environment secrets** (production Aspire deploy parameters), **Aspire user secrets** (local AppHost), or .NET User Secrets (legacy `dotnet run` path). Never commit tokens to source control.
- **GitHub Tokens for repository automation:** Repository-scoped GitHub Actions bridge tokens may be stored in GitHub Secrets when they are used only for repository/project automation workflows (for example, the SoloDevBoard roadmap bridge) and cannot be replaced by `GITHUB_TOKEN`.
- **Local Development:** Use `dotnet user-secrets` to manage sensitive configuration. See `docs/getting-started.md` for setup instructions.
- **App Settings Files:** `appsettings.json` and related files leave sensitive fields empty and instantiate with environment variables or secrets at runtime.
- **CI/CD:** GitHub Actions workflows use OIDC authentication with Azure (no long-lived credentials) and GitHub Environment secrets for AppHost parameters.

### Contributing & Pull Requests

- All contributions are welcome under the MIT license.
- Ensure no secrets appear in your commits before submitting a PR.
- Use `.gitignore` to exclude local secrets (`.env`, `secrets.json`, etc.).
- Review [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.

### .gitignore Protections

The repository includes patterns to prevent accidental secret commits:
- `*.env` — environment variable files
- `**/secrets.json` — .NET User Secrets
- `*.user` — Visual Studio user-specific files

---

## Documentation Sync

When code changes are made, ensure the following are kept in sync:

| Change | Doc to update |
|--------|--------------|
| New feature | `docs/user-guide/<feature>.md`, `docs/index.md`, `plan/BACKLOG.md` |
| New ADR | `adr/README.md` |
| Scope change | `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md` |
| New env variable | `docs/getting-started.md`, `docs/deployment.md`, `src/SoloDevBoard.AppHost/README.md` |
| New release | `plan/RELEASE_PLAN.md` |

---

## AI Collaborator Behaviour

- Always respond with UK English spelling.
- When suggesting code, follow the architecture rules above — do not place business logic in Razor components.
- For Blazor UI work, use MudBlazor components and the official MudBlazor layout patterns first; do not reintroduce bespoke layout structures when the library already provides an equivalent.
- Prefer MudBlazor layout primitives and utility classes in `Class` attributes for spacing, alignment, sizing, and visibility before creating or extending `.razor.css` files.
- Treat raw HTML and custom CSS as exceptional escape hatches only. If no MudBlazor component, parameter, or utility class can satisfy the requirement, keep the fallback minimal and explain the reason in the implementation summary or PR notes.
- When asked to "add a feature", follow the full checklist above before writing any code.
- When reviewing a PR diff, flag any non-UK English spelling in comments or strings.
- Do not generate any secrets or credentials in generated files.

---

## Skills and Workflow

The **`.agents/`** directory holds agent-agnostic AI collaboration artefacts — shared by GitHub Copilot, Cursor, and other tools without tying them to a single vendor layout.

### Skills

Canonical skills live in **`.agents/skills/`**. See **`.agents/skills/_REGISTRY.md`** for the active skill set, optional companions, and workflow order.

- Skills prefixed with **`repo-`** are SoloDevBoard-specific (GitHub project board, issue lifecycle, PM workflow, and related repository operations).
- Unprefixed skills are reusable implementation guidance (for example `dotnet-best-practices`, `mudblazor`, `aspire`).
- Skills formerly lived in **`.github/skills/`**; that path is no longer used.

Default workflow order for feature delivery:

1. Orchestration: `repo-pm-feature-workflow`
2. Planning: `breakdown-plan`
3. Issue lifecycle: `repo-github-issues` (and `repo-github-gh-cli` for bulk operations), then `repo-github-project` to sync the project board
4. Test planning: `breakdown-test`
5. Implementation: `dotnet-best-practices` and `mudblazor` as needed
6. Architecture decision capture: `create-architectural-decision-record` when required
7. Documentation updates: **Tech Writer agent** (uses `documentation-writer` skill internally)

### Execution gates

1. Do not start coding before planning and issue creation are complete.
2. For page-producing UI work, do not start implementation before the planning wireframe exists in `plan/wireframes/` and is referenced by the relevant planning artefacts or issues.
3. Do not close feature work before tests and documentation updates are complete.
4. Scope-impacting changes must update `plan/SCOPE.md` and `plan/BACKLOG.md` (via Tech Writer agent).

### PM operating system

- **Runbook:** `plan/PM_RUNBOOK.md`
- **Agents:** `.github/agents/*.agent.md` (Copilot)
- **Prompts:** `.github/prompts/*.prompt.md` (Copilot)
- **Cursor commands:** [`.cursor/commands/implement-issue.md`](.cursor/commands/implement-issue.md) — type `/implement-issue` in Agent chat after planning (Copilot handles PM prompts)
- **Copilot stack baseline:** `.github/copilot-instructions.md` and `.github/instructions/*.md`

---

## Cursor Cloud specific instructions

Durable, non-obvious notes for running SoloDevBoard in the Cursor Cloud VM. Standard commands live in `docs/getting-started.md`; this section only captures what is easy to get wrong here.

### Toolchain

- The .NET SDK pinned by `global.json` (`10.0.300`) is installed at `~/.dotnet`; the Aspire CLI (`13.4.6`, matching `Aspire.AppHost.Sdk`) is installed as a global tool at `~/.dotnet/tools`. Both are on `PATH` via `~/.bashrc`. Non-login shells (for example `bash -c`) do not source `~/.bashrc`, so invoke the SDK with the absolute path `~/.dotnet/dotnet` when `dotnet`/`aspire` are not already on `PATH`.
- The startup update script only runs `dotnet restore SoloDevBoard.slnx` (this also restores the AppHost). Building, testing, linting, and running are left to you.

### Build, test, lint

- Build: `dotnet build SoloDevBoard.slnx`.
- Test (xUnit, fully offline — GitHub is mocked): `dotnet test SoloDevBoard.slnx`.
- Lint is `dotnet format SoloDevBoard.slnx --verify-no-changes` (same check CI runs in `.github/workflows/ci.yml`). `EnforceCodeStyleInBuild` is on, so style violations also surface during build.

### Running the app (Aspire — the standard path)

- Start: `aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj --non-interactive` (runs in the background). Then `aspire ps` for the dashboard URL/token, `aspire describe` for the `app` resource URL (proxied at `https://localhost:5074`), and `aspire stop` to release file locks before rebuilding.
- If a build fails with `MSB3491`/`CS2012` (file locks), run `aspire stop` first — Aspire holds locks on `bin/`/`obj/` while running. See the `aspire-orchestration` skill.
- Query logs/traces without the dashboard via `aspire logs app`, `aspire otel`, or the Aspire MCP server (see below).
- Health endpoint: `GET /health` returns `Healthy`. The Home page (`/`) is static navigation and renders without any GitHub call.
- A direct `dotnet run --project src/App/SoloDevBoard.App --no-launch-profile` (with `ASPNETCORE_URLS`) still works as a fallback, but Aspire is the preferred path and matches how the app is developed.

### HTTPS dev certificate (in-VM browser caveat)

- On a normal dev machine, `aspire certs trust` (or `dotnet dev-certs https --trust`) trusts the cert and browsers stop warning. In this headless Linux VM that does **not** fully work for Chrome: even after installing `libnss3-tools` and importing the cert into `~/.pki/nssdb` as a trusted CA (`certutil ... -t "C,,"`), Chrome still reports `NET::ERR_CERT_INVALID` (a long-standing ASP.NET-dev-cert-on-Chromium-Linux issue). Do not rabbit-hole on it.
- For in-VM browser testing either click **Advanced → Proceed to localhost (unsafe)** once, or run the app on plain HTTP (`ASPNETCORE_URLS=http://0.0.0.0:5080 dotnet run --project src/App/SoloDevBoard.App --no-launch-profile`). `curl -k` also bypasses the check for scripted checks.

### Aspire MCP server

- `.cursor/mcp.json` (and `.vscode/mcp.json` for Copilot) registers an `aspire` MCP server via `aspire agent mcp`. It connects to the running AppHost and exposes tools such as `list_console_logs`, `list_structured_logs`, `list_traces`, `list_resources`, and `execute_resource_command` for debugging the live app. It only returns data while an AppHost is running (`aspire start`). Cursor loads `.cursor/mcp.json` at client start, so a reload is needed after first adding it.

### AppHost parameters and secrets

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
- The persisted AppHost user secrets in this VM hold placeholder values (`gh-pat` = the ambient `gh` installation token, `gh-app-client-secret` = `-`) so `aspire start` boots without extra setup. Real functionality needs a genuine user PAT via `SDB_GH_PAT` (below).

### GitHub authentication caveat (important)

- In the default PAT mode the app **fails fast at startup** unless a token is configured, and it resolves your login from the token via `/user` unless `GitHubAuth:OwnerLogin` is set.
- All data-driven pages (Audit Dashboard, Repositories, Label Manager, Migration) list repositories through the authenticated `/user/repos` endpoint. This needs a **GitHub user PAT** with `repo`, `read:org`, `workflow`, and `read:project` scopes.
- The ambient `gh` CLI token in this VM is a GitHub App **installation** token: it can reach repo-scoped endpoints (`/repos/{owner}/{repo}/...`) but returns `403 "Resource not accessible by integration"` on `/user` and `/user/repos`. Because it cannot resolve `/user`, an app-level user secret `GitHubAuth:OwnerLogin=markheydon` (on `src/App/SoloDevBoard.App`) is set so the app boots for demos; it is harmless with a real owner PAT and can be removed once a genuine user PAT is supplied (the login then auto-resolves). The installation token boots the app but **cannot** exercise the repo-list flows — supply a real user PAT (via the `Parameters__gh-pat` secret) for end-to-end feature testing.
