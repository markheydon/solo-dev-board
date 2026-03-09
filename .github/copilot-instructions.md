# GitHub Copilot Instructions — SoloDevBoard

<!-- Inspired by GitHub Awesome Copilot patterns -->

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

When Copilot Chat is asked to add a feature, it **must** perform the following steps in order:

1. **Update `plan/BACKLOG.md`** — add the feature to the relevant epic, formatted as a user story.
2. **Update `plan/SCOPE.md`** — if the feature changes scope, update the in-scope or out-of-scope sections.
3. **Create or update an ADR** in `adr/` if the feature requires an architectural decision.
4. **Create a stub in `docs/user-guide/`** if the feature is user-facing.
5. **Update `docs/index.md`** quick links if a new doc page is added.
6. **Open a GitHub Issue** (or instruct the developer to do so) following the label strategy in `plan/LABEL_STRATEGY.md`.
7. **Implement the feature** following the architecture and conventions above.
8. **Add or update tests** in the appropriate test project.

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

- Azure infrastructure is defined using **Bicep** in the `infra/` directory.
- All Bicep parameters must have `@description` decorators.
- Modules are organised under `infra/modules/`.
- Secrets (e.g. GitHub tokens) must be stored in **Azure Key Vault**, never in app settings files.

---

## Open Source & Security

This repository is **open source and public**. The following guidelines ensure security and code quality:

### Secrets Management

- **Golden Rule:** Never commit secrets, credentials, API keys, or personal data to this repository.
- **GitHub Tokens:** Must be stored exclusively in **Azure Key Vault** (production/Azure deployment) or .NET User Secrets (local development).
- **Local Development:** Use `dotnet user-secrets` to manage sensitive configuration. See `docs/getting-started.md` for setup instructions.
- **App Settings Files:** `appsettings.json` and related files leave sensitive fields empty and instantiate with environment variables or secrets at runtime.
- **CI/CD:** GitHub Actions workflows use OIDC authentication with Azure (no long-lived secrets) and GitHub Secrets context for sensitive data.
- **Bicep Deployments:** Infrastructure as Code uses Key Vault references (`@Microsoft.KeyVault(...)`) with RBAC-based access control.

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
| New env variable | `docs/getting-started.md`, `infra/README.md` |
| New release | `plan/RELEASE_PLAN.md` |

---

## AI Collaborator Instructions

- Always respond with UK English spelling.
- When suggesting code, follow the architecture rules above — do not place business logic in Razor components.
- When asked to "add a feature", follow the full checklist above before writing any code.
- When reviewing a PR diff, flag any non-UK English spelling in comments or strings.
- When generating Bicep, always add `@description` decorators and use symbolic resource names.
- Do not generate any secrets or credentials in generated files.

---

## Skill Trigger Matrix

Use the following active skill set for this repository:

- `pm-feature-workflow`
- `breakdown-plan`
- `github-issues`
- `github-project`
- `gh-cli`
- `breakdown-test`
- `create-architectural-decision-record`
- `documentation-writer`
- `dotnet-best-practices`
- `mudblazor`

Optional companion skills:

- `csharp-xunit`
- `csharp-docs`

Default workflow order for feature delivery:

1. Orchestration: `pm-feature-workflow`
2. Planning: `breakdown-plan`
3. Issue lifecycle: `github-issues` (and `gh-cli` for bulk operations), then `github-project` to sync the project board
4. Test planning: `breakdown-test`
5. Implementation: `dotnet-best-practices` and `mudblazor` as needed
6. Architecture decision capture: `create-architectural-decision-record` when required
7. Documentation updates: **Tech Writer agent** (invoked by PM Orchestrator or Delivery Agent; uses `documentation-writer` skill internally)

Execution gates:

1. Do not start coding before planning and issue creation are complete.
2. Do not close feature work before tests and documentation updates are complete.
3. Scope-impacting changes must update `plan/SCOPE.md` and `plan/BACKLOG.md` (via Tech Writer agent).

---

## PM Daily Workflow

For daily product management operations, use the **PM operating system** defined in:
- **Runbook:** `plan/PM_RUNBOOK.md` — daily/weekly workflow guide
- **Agents:** `.github/agents/*.agent.md` — specialised execution modes
- **Prompts:** `.github/prompts/*.prompt.md` — reusable workflow patterns

### Custom Agents

- **PM Orchestrator** (`.github/agents/pm-orchestrator.agent.md`) — backlog selection, scope validation, technical planning, issue creation
- **Delivery Agent** (`.github/agents/delivery.agent.md`) — implementation, tests, in-code XML docs
- **Tech Writer** (`.github/agents/tech-writer.agent.md`) — BACKLOG/SCOPE/ADR/user guide documentation, UK English enforcement
- **Review Agent** (`.github/agents/review.agent.md`) — quality gates, PR creation, issue closure

### Workflow Prompts

- **`daily-start.prompt.md`** — morning status check + next action recommendation
- **`plan-next-issue.prompt.md`** — select from backlog + create technical plan + setup GitHub issues
- **`execute-feature.prompt.md`** — implement code + tests + docs
- **`review-and-close.prompt.md`** — validate quality + create PR + close issue
- **`weekly-pm-review.prompt.md`** — milestone health + release confidence + priority recommendations

**Workflow reference:** See `plan/PM_RUNBOOK.md` for daily operating rhythm, decision tree, and command quick reference.
