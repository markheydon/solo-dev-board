# ADR-0010: Use bUnit for Blazor Component Testing

**Date:** 2026-03-07
**Status:** Accepted

---

## Context

SoloDevBoard is developed and extended by AI coding agents (see `.github/agents/delivery.agent.md`). For AI-driven delivery to be reliable, every code change must be validated by automated tests before a human reviews the work. The application has Blazor Server UI components that contain meaningful conditional rendering logic — loading states, error states, empty states, and data states — all of which must be verified automatically.

The application already uses xUnit for unit testing service and infrastructure layers (see [ADR-0002](0002-testing-framework.md), [ADR-0006](0006-moq-mocking-library.md)). The question is how to extend that automated validation to cover Blazor component behaviour.

The following approaches were considered:

| Approach | Runs via `dotnet test`? | Requires running browser/server? | AI-manageable? |
|----------|------------------------|----------------------------------|----------------|
| **No component tests** | N/A | N/A | ❌ Human review only |
| **bUnit** | ✅ Yes | ❌ No | ✅ Yes |
| **Playwright** | ❌ No (standalone runner) | ✅ Yes | ⚠️ Difficult |
| **Selenium** | ❌ No (standalone runner) | ✅ Yes | ⚠️ Difficult |

---

## Decision

**Use bUnit** for automated Blazor component testing in SoloDevBoard.

---

## Rationale

### AI Delivery Agent Compatibility

AI delivery agents validate their work via `dotnet test`. bUnit tests run via `dotnet test` without a browser or running server, making them fully compatible with the agent's existing build-and-verify loop. End-to-end tools like Playwright require a running application with a configured test environment, which the agent cannot orchestrate within a standard pull request workflow.

### xUnit Integration

bUnit integrates natively with xUnit. Tests follow the same Arrange/Act/Assert pattern, use the same `[Fact]`/`[Theory]` attributes, and use the same `Assert.*` methods as existing service and infrastructure tests. No new testing philosophy or separate tooling is introduced.

### In-Memory Rendering

bUnit renders Blazor components in-memory using a virtual DOM. There is:
- No browser required
- No network required
- Sub-millisecond render time
- Deterministic execution (no timing-dependent failures)

### Component State Verification

bUnit allows the delivery agent to verify:
- All conditional rendering states (loading, error, empty, data)
- Markup content and structure
- Event interactions (button clicks, form submissions)
- Service call depth and argument verification (via Moq integration)

### Licence

bUnit is published under the **MIT licence**, consistent with SoloDevBoard's open-source licence policy (see [ADR-0008](0008-remove-fluentassertions.md)).

---

## Limitations and Mitigations

| Limitation | Mitigation |
|------------|------------|
| Fluent UI components use JavaScript interop | Use `JSRuntimeMode.Loose` to silently handle all JS calls in tests |
| `FluentDataGrid` may limit deep row-level DOM assertions | Assert on text content in rendered markup rather than grid-specific DOM structure |
| Component tests do not validate visual appearance, layout, or responsive behaviour | These remain the responsibility of manual human review before PR approval |
| End-to-end flow (GitHub API → UI) cannot be tested | Covered by infrastructure unit tests (`GitHubServiceTests`) and developer testing |

---

## Consequences

- `bunit` NuGet package is added to `SoloDevBoard.App.Tests.csproj`.
- `Microsoft.AspNetCore.App` FrameworkReference is added to `SoloDevBoard.App.Tests.csproj` (required for Blazor component types such as `ComponentBase`).
- `SoloDevBoard.App` project reference is added to `SoloDevBoard.App.Tests.csproj`.
- Component test classes inherit from `Bunit.BunitContext` and call `Services.AddFluentUIComponents()` with `JSInterop.Mode = JSRuntimeMode.Loose` in the constructor. Components are rendered with `Render<T>()` (bUnit 2.x API).
- Test class naming follows the standard convention: `{ComponentName}Tests` with `sealed` modifier.
- The `PlaceholderTests.cs` file in `SoloDevBoard.App.Tests` is retained until component tests cover each page.
- End-to-end testing (Playwright or similar) is deferred as a post-Phase 1 decision. Manual human review covers visual, UX, and browser-compatibility concerns in the interim.
- As each new Blazor page is implemented, a corresponding component test class is added to `tests/App.Tests/SoloDevBoard.App.Tests/` covering at minimum: loading state, error state, empty state, and data state.
