# ADR-0012: Switch UI Component Library from Fluent UI Blazor to MudBlazor

**Date:** 2026-03-09
**Status:** Accepted — supersedes [ADR-0009](0009-fluent-ui-blazor-component-library.md)

---

## Context

ADR-0009 selected `Microsoft.FluentUI.AspNetCore.Components` as the UI component library for SoloDevBoard. After delivering two UI features (Repositories page, PR #53; Label Manager, PR #62), a pattern of recurring AI-delivery friction has been identified that is attributable to the Fluent UI Blazor library's architecture.

### Observed Problems

| Problem | Root Cause |
|---------|-----------|
| `FluentAutocomplete` dropdown bleeds through `FluentCard` stacking context | Fluent UI is built on FAST web components that create a shadow DOM stacking context. This is a known library limitation with no component-parameter fix. |
| `FluentDataGrid` sticky header z-index hardcoded inside the library | No public API to override; requires `::deep` CSS workarounds on every page using a sticky grid header. |
| No `FluentColorPicker` component | The colour picker in the Label Operation dialog had to be hand-rolled as a custom component, requiring CSS that a library component would have avoided. |
| Agent consistently falls back to raw HTML elements | The Fluent UI Blazor library is underrepresented in AI training data relative to its age. The agent does not reliably choose `<FluentSearch>` over `<input type="search">` without explicit guardrail prompting. |
| Agent-generated CSS corrections on every UI PR | The stacking context and z-index issues require per-PR CSS workarounds rather than being resolved by default component behaviour. |

### The AI-Delivery Constraint

SoloDevBoard is developed entirely through AI agents (GitHub Copilot Delivery Agent) with no human UI engineering. This places a premium on a library that:


Fluent UI Blazor scores poorly on these criteria because of its FAST web component foundation.


## Decision

**Replace `Microsoft.FluentUI.AspNetCore.Components` with `MudBlazor`** as the sole UI component library for SoloDevBoard.


## Rationale

### Pure Blazor Architecture

MudBlazor is built entirely in C# and Razor — no web components, no shadow DOM, no FAST. This eliminates the entire class of stacking context and z-index problems encountered with Fluent UI.

### AI Training Data Coverage

MudBlazor has been the most widely adopted Blazor component library since 2020 and is extensively represented in AI training data. The agent can generate correct `MudAutocomplete`, `MudDataGrid`, and `MudDialog` usage patterns without library-specific guardrail prompting.

### Consistent Component API

Almost every MudBlazor component uses the same parameter conventions: `T` generics, `Value`/`ValueChanged`, `Label`, `Variant`, `Color`, `Size`. The agent can generalise patterns across all components, reducing per-component knowledge requirements.

### Popup Rendering

MudBlazor popups (autocomplete dropdowns, select lists, dialogs) are rendered via a `MudPopoverProvider` at the root, portalling to the document body. They sit above all other content automatically. The z-index category of problems encountered with Fluent UI does not arise.

### Full Component Coverage

MudBlazor provides first-class components for all UI patterns required by the current and planned SoloDevBoard features:

| Pattern | MudBlazor Component |
|---------|---------------------|
| Text input | `MudTextField` |
| Search input | `MudTextField` with `Adornment.End` icon |
| Multi-select autocomplete | `MudAutocomplete` |
| Data grid | `MudDataGrid` |
| Select / dropdown | `MudSelect` |
| Button | `MudButton`, `MudIconButton`, `MudFab` |
| Navigation | `MudNavMenu`, `MudNavLink`, `MudNavGroup` |
| Dialog | `MudDialog` + `IDialogService` |
| Snackbar notification | `MudSnackbar` + `ISnackbar` |
| **Colour picker** | **`MudColorPicker`** (first-class; replaces hand-rolled component) |
| Card / container | `MudPaper`, `MudCard` |
| Layout | `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent` |
| Progress indicator | `MudProgressCircular`, `MudProgressLinear` |
| Tooltip | `MudTooltip` |
| Chip / badge | `MudChip` |

### Licence

MudBlazor is published under the **MIT licence**, which is fully compatible with SoloDevBoard's MIT licence. There is no compliance risk.

### Community Adoption

MudBlazor is the de-facto standard Blazor component library in the community. Contributors joining the open-source project will almost certainly be familiar with it, reducing onboarding friction.

---

## Trade-offs Accepted

- **Material Design aesthetic:** MudBlazor implements Material Design rather than the Fluent Design System. SoloDevBoard will no longer have a Microsoft-native visual appearance. For a solo developer productivity tool this is acceptable.
- **Migration cost:** Two UI features must be refactored (Repositories page, Label Manager + dialog). The logic layer is unchanged; only the Razor component markup changes.
- **Skill artefact disposal:** The `.github/skills/fluentui-blazor/` skill directory, built during Phases 1–2, will be removed and replaced with a new MudBlazor skill. This is sunk cost and does not affect the application.
- **bUnit test adjustments:** Existing bUnit tests register `AddFluentUIComponents()`. These must be updated to register MudBlazor services and remove Fluent UI-specific `JSRuntimeMode.Loose` workarounds.

---

## Consequences

### Immediate Actions (Delivery Agent)

1. **Remove** `Microsoft.FluentUI.AspNetCore.Components` and related packages from `SoloDevBoard.App.csproj`.
2. **Add** `MudBlazor` NuGet package to `SoloDevBoard.App.csproj`.
3. **Update `Program.cs`** — replace `AddFluentUIComponents()` with `builder.Services.AddMudServices()`.
4. **Update `_Imports.razor`** — replace `@using Microsoft.FluentUI.AspNetCore.Components` with `@using MudBlazor`.
5. **Update `MainLayout.razor`** — replace Fluent UI provider components and layout structure with MudBlazor equivalents (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`, `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent`).
6. **Refactor `NavMenu.razor`** — replace `FluentNavMenu`/`FluentNavLink` with `MudNavMenu`/`MudNavLink`.
7. **Refactor `Repositories.razor`** — replace Fluent UI components with MudBlazor equivalents.
8. **Refactor `Labels.razor` and `LabelOperationDialog.razor`** — replace all Fluent UI components, including replacing the hand-rolled colour picker with `MudColorPicker`.
9. **Update bUnit test projects** — replace `AddFluentUIComponents()` with MudBlazor service registration; remove `JSRuntimeMode.Loose` workarounds specific to Fluent UI.
10. **Remove** `.github/skills/fluentui-blazor/` skill directory.
11. **Create** `.github/skills/mudblazor/` skill with MudBlazor-specific guidance.
12. **Replace** `.github/instructions/blazor.instructions.md` with a MudBlazor-aware version.

### Ongoing

- All future Razor component development uses MudBlazor components exclusively.
- The `.github/skills/mudblazor/` skill is the authoritative source of MudBlazor usage patterns for the Delivery Agent.
- ADR-0009 is superseded by this record.
