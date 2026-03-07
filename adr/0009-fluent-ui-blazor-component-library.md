# ADR-0009: Use Microsoft Fluent UI Blazor Component Library

**Date:** 2026-03-07
**Status:** Accepted

---

## Context

SoloDevBoard requires a UI component library for its Blazor Server front-end. The application presents data-heavy views (repository tables, issue grids, audit dashboards) and needs accessible, visually consistent components without requiring custom CSS engineering.

The following options were considered:

| Option | Description |
|--------|-------------|
| **Raw HTML + Bootstrap** | Standard CSS framework; UI components hand-rolled as Razor markup. |
| **MudBlazor** | Feature-rich Blazor component library following Material Design; MIT licensed. |
| **Radzen Blazor** | Component library with commercial support tier; community edition available. |
| **Microsoft FluentUI Blazor** | Microsoft's official Blazor implementation of the Fluent Design System; MIT licensed. |

---

## Decision

**Use `Microsoft.FluentUI.AspNetCore.Components`** as the UI component library for SoloDevBoard.

---

## Rationale

### Licence Alignment

`Microsoft.FluentUI.AspNetCore.Components` is published under the **MIT licence**, which is fully compatible with SoloDevBoard's open-source MIT licence. There is no compliance risk for contributors or forks. This was a decisive factor given the lesson learned in [ADR-0008](0008-remove-fluentassertions.md) concerning FluentAssertions' commercial licence.

### First-Party Microsoft Library

The library is developed and maintained by Microsoft. It ships with .NET Aspire templates and receives active investment aligned with the .NET roadmap. Long-term compatibility with .NET 10 and beyond is assured.

### Accessibility

Fluent UI components are built to WCAG 2.1 AA standards. ARIA attributes, keyboard navigation, and screen reader support are built-in properties rather than concerns that must be hand-engineered.

### Design Language

GitHub — the platform SoloDevBoard integrates with — is part of the Microsoft ecosystem and shares visual design sensibilities with the Fluent Design System. Using Fluent UI produces a coherent interface that feels familiar to developers who use GitHub and other Microsoft tooling daily.

### Component Coverage

The library provides all components required by Phase 1–3 of SoloDevBoard:

| Component | Used for |
|-----------|----------|
| `FluentDataGrid` | Repository listing, issue tables, label grids |
| `FluentCard` | Error states, empty states, feature panels |
| `FluentButton` | Actions, retry buttons |
| `FluentProgressRing` | Loading states |
| `FluentToastProvider` | User notifications |
| `FluentDialogProvider` | Confirmation dialogs |
| `FluentNavMenu` | Application navigation |

---

## Trade-offs Accepted

- **JavaScript Interop:** Some Fluent UI components (notably `FluentDataGrid`) use JavaScript for column resizing and internal rendering. This limits the depth of automated bUnit component testing for grid interactions (see [ADR-0010](0010-bunit-component-testing.md)).
- **Fluent Design Lock-in:** Choosing Fluent UI commits all future UI development to the Fluent Design System. Migrating to another library would require component-by-component replacement.
- **Bundle Size:** The library adds web components JavaScript to the application bundle. For a Blazor Server application this is negligible.

---

## Consequences

- `Microsoft.FluentUI.AspNetCore.Components` is added to `SoloDevBoard.App.csproj`.
- `AddFluentUIComponents()` is called in `Program.cs` to register required services.
- `@using Microsoft.FluentUI.AspNetCore.Components` is added to `_Imports.razor`.
- Five provider components (`FluentToastProvider`, `FluentDialogProvider`, `FluentMessageBarProvider`, `FluentTooltipProvider`, `FluentKeyCodeProvider`) are added to `MainLayout.razor` to enable notification and interaction services.
- All Blazor pages use `Fluent*`-prefixed components rather than raw HTML for interactive UI elements.
- bUnit test projects register `AddFluentUIComponents()` and use `JSRuntimeMode.Loose` to handle Fluent UI's JavaScript interop silently in component tests (see [ADR-0010](0010-bunit-component-testing.md)).
