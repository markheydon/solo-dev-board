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
- **Mocking:** NSubstitute
- **Naming convention:** `MethodUnderTest_Scenario_ExpectedOutcome`
- Test projects mirror the structure of source projects.
- Arrange / Act / Assert sections separated by blank lines (no comments required).
- Use `FluentAssertions` for assertion readability where appropriate.

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

- `type/` — feature, bug, chore, documentation, epic
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
