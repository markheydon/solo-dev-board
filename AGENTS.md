# AI Collaborator Instructions — SoloDevBoard

Platform-neutral standards for GitHub Copilot, Cursor, and other AI agents working in this repository.

## Project Overview

**SoloDevBoard** is a .NET 10 Blazor Server application that provides a single pane of glass for solo developers to manage GitHub workloads across multiple repositories.

---

## Language & Framework

- **Language:** C# 14
- **Framework:** .NET 10 / Blazor Server
- **Target runtime:** `net10.0`

Path-scoped rules live in `.github/instructions/` and are loaded by matching tools when relevant files are in context. Cursor mirrors each instruction file with a rule in `.cursor/rules/`:

| Instruction file | Scope | Cursor rule |
|------------------|-------|-------------|
| `blazor.instructions.md` | `**/*.razor`, `**/*.razor.cs`, `**/*.razor.css` | `.cursor/rules/blazor-mudblazor.mdc` |
| `dotnet-framework.instructions.md` | `**/*.cs`, `**/*.csproj` | `.cursor/rules/dotnet.mdc` |
| `github-actions-ci-cd-best-practices.instructions.md` | `.github/workflows/**` | `.cursor/rules/github-actions-ci-cd-best-practices.mdc` |

---

## UK English Requirement

> **All code comments, string literals, user-facing text, documentation, and commit messages MUST be written in UK English.**

> **All bullet point list items that form complete sentences MUST end with a full stop (`.`).** This applies to all documentation: planning files, user guides, decision log, agent definitions, prompt files, and inline code comments.

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

### Boundary Data Shapes (DEC-008)

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
- Use xUnit's built-in `Assert.*` methods for all assertions. **Do not add FluentAssertions** — it requires a commercial licence and is prohibited in this open-source project (see [DEC-006](plan/DECISIONS.md#dec-006-no-fluentassertions--xunit-built-in-assertions-only)).

---

## When Adding a New Feature

Apply the tier that matches the request:

### Always (any code change)

- Follow architecture, coding conventions, UK English, and testing rules in this file and path-scoped instructions.
- Do not commit secrets or temporary files to the repository.

### Planned feature work (issue exists or user requests full PM workflow)

Follow [`.agents/workflows/plan-next-issue.md`](.agents/workflows/plan-next-issue.md) and [`.agents/contracts/delivery.md`](.agents/contracts/delivery.md). Gates:

- Planning and GitHub issue with acceptance criteria before coding.
- Wireframe in `plan/wireframes/` before page-producing UI implementation.
- User guide, decision log or constitution update, tests, and docs sync per the Documentation Sync table below.

### Greenfield "add a feature" (no issue yet)

Create or update planning artefacts and a GitHub issue before implementation — see `plan-next-issue` workflow. Do not implement directly without scope validation.

### Ad-hoc fix or small change

No backlog or issue ceremony required unless scope grows beyond the original request.

---

## Label Strategy

Follow the taxonomy in [`plan/LABEL_STRATEGY.md`](plan/LABEL_STRATEGY.md). Apply at least one `type/` and one `priority/` label to new issues and PRs.

---

## Infrastructure

- Production deployment uses **Aspire** (`aspire deploy`) from `SoloDevBoard.AppHost` to **Azure Container Apps** ([DEC-015](plan/DECISIONS.md#dec-015-aspire-azure-container-apps-deployment)).
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
- **Local Development:** Use `dotnet user-secrets` for the legacy `dotnet run` path, or `aspire secret set` for Aspire AppHost. See `docs/getting-started.md` for setup instructions.
- **App Settings Files:** `appsettings.json` and related files leave sensitive fields empty and instantiate with environment variables or secrets at runtime.
- **Aspire Deployments:** Secrets are supplied as AppHost parameters from GitHub Environment secrets and injected into the `app` resource at deploy time. See `src/SoloDevBoard.AppHost/README.md` and `docs/deployment.md` for parameter mappings.
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
| New feature | `docs/user-guide/<feature>.md`, `docs/index.md`, GitHub Issue + Project #8 sync |
| New decision | `plan/DECISIONS.md` (+ constitution if cross-cutting) |
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
- When asked to "add a feature", apply the matching tier in **When Adding a New Feature** above.
- When reviewing a PR diff, flag any non-UK English spelling in comments or strings.
- For infrastructure changes, edit the AppHost in `src/SoloDevBoard.AppHost` or add configuration to `src/SoloDevBoard.App`; do not create hand-authored Bicep files (Aspire generates deployment Bicep at deploy time).
- Do not generate any secrets or credentials in generated files.
- Do not generate temporary or disposable files in the repository. If a temporary file is required for any reason including (but not limited to) temporary output from tools or testing, it must be created in a temp-style directory away from the repo and cleaned up after use. The rationale is not to restrict an AI's ability to do its work, but to prevent accidental commits of temporary files to the repository.

---

## Skills and Workflow

The **`.agents/`** directory holds agent-agnostic AI collaboration artefacts — shared by GitHub Copilot, Cursor, and other tools without tying them to a single vendor layout.

### Skills

Canonical skills live in [`.agents/skills/`](.agents/skills/). See [`.agents/skills/_REGISTRY.md`](.agents/skills/_REGISTRY.md) for the active set, optional companions, and default workflow order.

### Execution gates

1. Do not start coding before planning and issue creation are complete.
2. For page-producing UI work, do not start implementation before the planning wireframe exists in `plan/wireframes/` and is referenced by the relevant planning artefacts or issues.
3. Do not close feature work before tests and documentation updates are complete.
4. Scope-impacting changes must update `plan/SCOPE.md` and `plan/IMPLEMENTATION_PLAN.md` (via Tech Writer agent). New or changed work items must be created or updated as GitHub Issues and synced to Project #8.

### PM operating system

- **Runbook:** [`plan/PM_RUNBOOK.md`](plan/PM_RUNBOOK.md)
- **Workflows:** [`.agents/workflows/README.md`](.agents/workflows/README.md) (canonical entry points)
- **Contracts:** [`.agents/contracts/`](.agents/contracts/) (role boundaries)
- **Tool mirrors:** [`.github/prompts/`](.github/prompts/) and [`.cursor/commands/`](.cursor/commands/) — thin pointers only; never duplicate workflow content

Consult the workflow or contract index when entering a PM or delivery mode. Do not load every skill up front.

---

## Environment-specific guidance

For Cursor Cloud VM development (Aspire lifecycle, secrets mapping, MCP, HTTPS caveats), see [`plan/CURSOR_CLOUD.md`](plan/CURSOR_CLOUD.md). Standard local setup remains in [`docs/getting-started.md`](docs/getting-started.md).
