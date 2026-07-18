# ADR-0004: Use Layered / Clean Architecture

**Date:** 2025-01-01
**Status:** Accepted

---

## Context

SoloDevBoard needs a solution structure that:
- Separates concerns clearly (UI, business logic, data access, external integrations).
- Allows the GitHub API client and other infrastructure concerns to be swapped or mocked in tests.
- Scales gracefully as features are added over time without accumulating spaghetti dependencies.
- Is familiar to .NET developers and well-supported by AI coding tools.

The following architectural patterns were considered:

| Option | Description |
|--------|-------------|
| **Layered / Clean Architecture** | Explicit layers: App → Application → Domain + Infrastructure. Dependencies point inward. |
| **Vertical Slice Architecture** | Each feature is a self-contained slice. Minimal shared abstractions. |
| **Modular Monolith** | Feature modules with internal layering. |
| **Minimal / Script-style** | Single project, no layering. Pragmatic for a small solo tool. |

---

## Decision

**Use a layered / clean architecture with four projects:**

```
SoloDevBoard.App             → Blazor Server UI (presentation layer)
SoloDevBoard.Application     → Application logic, use cases, service interfaces
SoloDevBoard.Domain          → Domain entities, value objects, domain events
SoloDevBoard.Infrastructure  → GitHub API clients, external integrations
```

---

## Rationale

- **Testability:** By defining service interfaces in `Application` and implementing them in `Infrastructure`, the GitHub API can be substituted with NSubstitute mocks in unit tests without making real HTTP calls.
- **Maintainability:** As features are added, the clear separation of concerns prevents business logic from leaking into Razor components.
- **Familiar pattern:** Clean Architecture is widely understood by .NET developers and well-supported by GitHub Copilot, which generates accurate code for this pattern.
- **Domain integrity:** The `Domain` project has no external dependencies, making it easy to reason about and test in isolation.

### Why Not Vertical Slice?

Vertical Slice Architecture is a good choice for larger teams where feature ownership is distinct. For a solo developer building a cohesive tool, the overhead of per-slice duplication (each slice reimplementing its own query/command/handler infrastructure) is not justified. The shared `Application` layer's service interfaces provide sufficient abstraction.

---

## Dependency Rules

- **Domain** → no dependencies on other projects or external NuGet packages (only the BCL).
- **Application** → depends on `Domain` only.
- **Infrastructure** → depends on `Application` and `Domain`; implements interfaces defined in `Application`.
- **App** → depends on `Application`; uses Blazor Server; does not reference `Infrastructure` directly (uses DI).

Dependency injection is configured in `SoloDevBoard.App/Program.cs`, where `Infrastructure` implementations are registered against `Application` interfaces.

---

## Consequences

- The solution contains four projects plus one or more test projects.
- All domain entities are defined as C# records in `SoloDevBoard.Domain`.
- All service interfaces (e.g. `IGitHubService`, `ILabelRepository`) are defined in `SoloDevBoard.Application`.
- Razor components in `SoloDevBoard.App` only call application services — they do not contain business logic.
- New features follow the same structure: Domain record → Application interface → Infrastructure implementation → App component.
