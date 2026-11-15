> Coding and design conventions for this repository.

# Conventions

**Project:** SoloDevBoard  
**Last updated:** 2026-11-15

When in doubt, follow this file. Significant architecture changes need an ADR.

---

## Project structure

Projects live under `src/` and `tests/` with `SoloDevBoard.*` directory names. `SoloDevBoard.slnx` groups them into solution folders (`src/Layers`, `src/Aspire`, `tests/Unit`, `tests/E2E`) for navigation only; paths on disk are flat.

```
src/
├── SoloDevBoard.Web/              # Blazor Server UI (presentation)
├── SoloDevBoard.Application/
├── SoloDevBoard.Composition/      # DI composition root (`AddSoloDevBoard`)
├── SoloDevBoard.Domain/
├── SoloDevBoard.Infrastructure/
├── SoloDevBoard.AppHost/          # Aspire orchestration and Azure deploy model
└── SoloDevBoard.ServiceDefaults/

tests/
├── SoloDevBoard.Web.Tests/        # bUnit component tests
├── SoloDevBoard.Application.Tests/, SoloDevBoard.Infrastructure.Tests/, ...
├── SoloDevBoard.Composition.Tests/, SoloDevBoard.ServiceDefaults.Tests/
└── SoloDevBoard.E2E.Tests/        # Playwright journeys
```

Page-producing UI work should align with wireframes in [`plan/wireframes/`](plan/wireframes/) when the change introduces or materially alters a user-facing page.

---

## Architecture

Clean/layered boundaries (see [`adr/adr-0004-layered-architecture.md`](adr/adr-0004-layered-architecture.md)):

- **Domain** has no external dependencies.
- **Application** depends on Domain only.
- **Infrastructure** implements Application interfaces and depends on Application and Domain.
- **Composition** is the sole DI composition root (`AddSoloDevBoard`).
- **Web** depends on Application and Composition only; it must not reference Infrastructure (not in `.csproj`, not in `using` statements).

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

Consult the `mudblazor` skill before new UI. Prefer MudBlazor components and utility classes before `.razor.css`. Business logic belongs in Application services, not Razor components. Use code-behind (`.razor.cs`) for non-trivial logic. Use `ISnackbar` for transient feedback and inline alerts for persistent state ([`adr/adr-0035-transient-feedback-via-snackbar.md`](adr/adr-0035-transient-feedback-via-snackbar.md)).

**Theme and chrome tokens** ([`THEMING.md`](.agents/skills/mudblazor/references/THEMING.md) in the mudblazor skill):

- Brand and semantic colours live only in [`src/SoloDevBoard.Web/Themes/SoloDevBoardTheme.cs`](src/SoloDevBoard.Web/Themes/SoloDevBoardTheme.cs), wired through `AppThemeProvider`. Do not hard-code hex in Razor except GitHub label colours returned from the API.
- Default grey for new GitHub labels (data, not brand) is [`LabelDefaults.DefaultColour`](src/SoloDevBoard.Web/Components/Features/Labels/LabelDefaults.cs) in the Labels feature.
- Shell toolbar icons (menu, search, overflow, warning, theme toggle) use [`AppIcons`](src/SoloDevBoard.Web/Theming/AppIcons.cs). Drawer nav icons in `NavMenu` remain ad hoc `Icons.Material.Filled.*` until branding centralises them. The theme toggle is the only deliberate non-Filled icon set (`Rounded` / `Outlined` via `ThemePreferenceExtensions`).
- Isolated CSS is limited to Blazor circuit reconnect, the skip link, and `.sdb-responsive-grid` unless MudBlazor adds a replacement. Fallback hex in reconnect CSS and theme flash JavaScript must match `SoloDevBoardTheme`; [`SoloDevBoardThemeFallbackTests`](tests/SoloDevBoard.Web.Tests/SoloDevBoardThemeFallbackTests.cs) and [`ThemePreferenceConstantsTests`](tests/SoloDevBoard.Web.Tests/ThemePreferenceConstantsTests.cs) lock those values. Do not override `.mud-*` selectors when a component parameter, theme token, or utility class suffices.

---

## Testing

- **xUnit v3** with Microsoft.Testing.Platform; **NSubstitute** for mocks; **built-in `Assert` only** (no Moq, FluentAssertions, NUnit, or MSTest).
- **bUnit** in `SoloDevBoard.Web.Tests` only; **Playwright** E2E in `tests/SoloDevBoard.E2E.Tests`.
- Fast path: `dotnet test SoloDevBoard.UnitTests.slnf` or `./scripts/test-unit.sh`. Full parity: `./scripts/test-all.sh`.
- Do not test AppHost modelling or orchestration.
- Naming: `MethodUnderTest_Scenario_ExpectedOutcome`; Arrange / Act / Assert with blank lines.

End-user docs ([`website/content/docs/`](website/content/docs/)) and Playwright E2E must stay aligned per [`tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`](tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md).

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
| New end-user feature | `website/content/docs/`, matching E2E tests, `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`, `tests/SoloDevBoard.E2E.Tests/CRITICAL_JOURNEYS.md` |
| End-user behaviour change | User guide pages and matching Playwright tests |
| New developer / operator guidance | `docs/` |
| New decision | `adr/` |
| Scope change | `plan/SCOPE.md`, `plan/BACKLOG.md` |
| New env variable | `docs/getting-started.md`, `docs/deployment.md`, `src/SoloDevBoard.AppHost/README.md` |
| MudBlazor version bump | `.agents/skills/mudblazor/` |
| Theme tokens, shell icons, or allowed `.razor.css` exceptions | `CONVENTIONS.md` (Blazor), `.agents/skills/mudblazor/references/THEMING.md`, theme fallback tests when hex changes |
| New release | `plan/RELEASE_PLAN.md`, `CHANGELOG.md` |

---

## Security (open source)

- Never commit secrets, tokens, or credentials.
- Runtime tokens: Aspire user secrets, GitHub Environment secrets at deploy, or .NET user secrets on the legacy run path.
- Docs-capture screenshots: public catalogues only ([`adr/adr-0020-public-only-docs-capture-mode.md`](adr/adr-0020-public-only-docs-capture-mode.md)).