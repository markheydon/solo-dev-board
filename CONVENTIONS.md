> Coding and design conventions for this repository.

# Conventions

**Project:** SoloDevBoard  
**Last updated:** 2026-11-14

When in doubt, follow this file. Significant architecture changes need an ADR.

---

## Project structure

```
src/
├── App/SoloDevBoard.App/
├── Application/SoloDevBoard.Application/
├── Composition/SoloDevBoard.Composition/
├── Domain/SoloDevBoard.Domain/
├── Infrastructure/SoloDevBoard.Infrastructure/
├── SoloDevBoard.AppHost/
└── SoloDevBoard.ServiceDefaults/

tests/
├── Application.Tests/, App.Tests/, Composition.Tests/, Domain.Tests/
├── Infrastructure.Tests/, ServiceDefaults.Tests/
└── E2E/SoloDevBoard.E2E.Tests/
```

Page-producing UI work should align with wireframes in [`plan/wireframes/`](plan/wireframes/) when the change introduces or materially alters a user-facing page.

---

## Architecture

Clean/layered boundaries (see [`adr/adr-0004-layered-architecture.md`](adr/adr-0004-layered-architecture.md)):

- **Domain** has no external dependencies.
- **Application** depends on Domain only.
- **Infrastructure** implements Application interfaces and depends on Application and Domain.
- **Composition** is the sole DI composition root (`AddSoloDevBoard`).
- **App** depends on Application and Composition only; it must not reference Infrastructure (not in `.csproj`, not in `using` statements).

**Boundary data shapes** ([`adr/adr-0011-boundary-data-shapes.md`](adr/adr-0011-boundary-data-shapes.md)): repository interfaces return domain entity records; public Application service interfaces (`I*Service`, `I*Manager`) return and accept **DTO records** only. Mapping happens in Application services, not Razor components and not via AutoMapper.

Use constructor injection throughout; avoid service locator patterns. All public members require XML documentation (`///`). Use `ArgumentNullException.ThrowIfNull()` for guard clauses.

---

## Target framework

- **`net10.0`** (.NET 10 LTS); pin the SDK band in [`global.json`](global.json).
- C# 14; nullable reference types and implicit usings enabled.
- Treat warnings as errors (`Directory.Build.props`).
- When changing the `MudBlazor` `<PackageVersion>` in `Directory.Packages.props`, refresh [`.agents/skills/mudblazor/`](.agents/skills/mudblazor/) in the same change.

---

## C# patterns

- **Async/await** for I/O; propagate `CancellationToken` on public async APIs.
- **Records** for domain entities and DTOs where immutability fits.
- **File-scoped namespaces** and **primary constructors** where they improve readability.
- Prefer `IReadOnlyList<T>` / `IReadOnlyDictionary<,>` in public APIs.
- Do not use sync-over-async (`.Result`, `.Wait()`).
- **Options pattern** for configuration; never commit secrets (see [`docs/getting-started.md`](docs/getting-started.md)).

---

## Blazor and MudBlazor

Sole UI library: MudBlazor ([`adr/adr-0012-switch-to-mudblazor-component-library.md`](adr/adr-0012-switch-to-mudblazor-component-library.md), [`adr/adr-0039-mudblazor-visual-language-for-page-chrome.md`](adr/adr-0039-mudblazor-visual-language-for-page-chrome.md)).

Consult the `mudblazor` and `blazor-blueprint` skills before new UI. Prefer MudBlazor components and utility classes before `.razor.css`. Business logic belongs in Application services, not Razor components. Use code-behind (`.razor.cs`) for non-trivial logic. Use `ISnackbar` for transient feedback and inline alerts for persistent state ([`adr/adr-0035-transient-feedback-via-snackbar.md`](adr/adr-0035-transient-feedback-via-snackbar.md)).

---

## Testing

- **xUnit v3** with Microsoft.Testing.Platform; **NSubstitute** for mocks; **built-in `Assert` only** (no Moq, FluentAssertions, NUnit, or MSTest).
- **bUnit** in App test project only; **Playwright** E2E in `tests/E2E/SoloDevBoard.E2E.Tests`.
- Fast path: `dotnet test SoloDevBoard.UnitTests.slnf` or `./scripts/test-unit.sh`. Full parity: `./scripts/test-all.sh`.
- Do not test AppHost modelling or orchestration.
- Naming: `MethodUnderTest_Scenario_ExpectedOutcome`; Arrange / Act / Assert with blank lines.

End-user docs ([`website/content/docs/`](website/content/docs/)) and Playwright E2E must stay aligned per [`tests/E2E/USER_DOCS_ALIGNMENT.md`](tests/E2E/USER_DOCS_ALIGNMENT.md).

---

## GitHub Actions

- One workflow file per concern with a descriptive name ([`adr/adr-0030-self-documenting-ci-workflow-split.md`](adr/adr-0030-self-documenting-ci-workflow-split.md)).
- Use OIDC for Azure in CD workflows; no long-lived cloud credentials in source control.

---

## Documentation

- UK English in docs, comments, and user-facing strings (`colour`, `organise`, `behaviour`, etc.).
- Do not use the em dash (Unicode U+2014); use spaced ASCII hyphen ` - ` in prose. Run [`scripts/verify-no-em-dash.sh`](scripts/verify-no-em-dash.sh) before large doc edits.
- User-facing product docs: Hugo site under `website/`. Developer/operator docs: `docs/`.
- New technical decisions: add an ADR under `adr/` using the `create-architectural-decision-record` skill.

### Documentation sync (feature work)

| Change | Doc to update |
| --- | --- |
| New end-user feature | `website/content/docs/`, matching E2E tests, `tests/E2E/USER_DOCS_ALIGNMENT.md`, `tests/E2E/CRITICAL_JOURNEYS.md` |
| End-user behaviour change | User guide pages and matching Playwright tests |
| New developer / operator guidance | `docs/` |
| New decision | `adr/` |
| Scope change | `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md` |
| New env variable | `docs/getting-started.md`, `docs/deployment.md`, `src/SoloDevBoard.AppHost/README.md` |
| MudBlazor version bump | `.agents/skills/mudblazor/` |
| New release | `plan/RELEASE_PLAN.md`, `CHANGELOG.md` |

---

## Security (open source)

- Never commit secrets, tokens, or credentials.
- Runtime tokens: Aspire user secrets, GitHub Environment secrets at deploy, or .NET user secrets on the legacy run path.
- Docs-capture screenshots: public catalogues only ([`adr/adr-0020-public-only-docs-capture-mode.md`](adr/adr-0020-public-only-docs-capture-mode.md)).