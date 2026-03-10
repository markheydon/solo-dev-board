---
description: 'Blazor component and application patterns for SoloDevBoard (MudBlazor)'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

## UI Library — MudBlazor

This project uses **MudBlazor** as its sole UI component library (see ADR-0012).

- **Never use raw HTML form elements** where a MudBlazor component exists. Use `<MudTextField>`, `<MudSelect>`, `<MudAutocomplete>`, `<MudCheckBox>`, `<MudSwitch>`, `<MudColorPicker>`, etc.
- **Never use raw `<input>`, `<select>`, `<textarea>`, `<button>`** in Razor components — always use the `<Mud*>` equivalent.
- **Prefer MudBlazor structural components** such as `<MudStack>`, `<MudGrid>`, `<MudItem>`, `<MudPaper>`, `<MudContainer>`, `<MudSpacer>`, `<MudDivider>`, and `<MudHidden>` instead of raw layout wrappers where the library already covers the pattern.
- **Prefer MudBlazor utility classes** in `Class` attributes for spacing, alignment, display, and sizing before creating any custom CSS.
- **Never use `<style>` blocks** inside `.razor` files.
- **Treat `.razor.css` as a last resort.** Only add or extend an isolated stylesheet when the requirement cannot be met with MudBlazor components, component parameters, theming, or utility classes.
- For any pattern where you cannot find a MudBlazor component or utility-class-based solution, add a brief comment explaining the gap and keep the scoped `.razor.css` fallback minimal.
- Consult the **mudblazor skill** (`.github/skills/mudblazor/SKILL.md`) for component usage patterns, layout structure, and known pitfalls before implementing any new Razor component.

## Blazor Code Style

- Write idiomatic C# and Razor markup.
- Prefer code-behind files (`.razor.cs`) for any component logic beyond simple one-liners; keep `.razor` files focused on markup.
- Business logic must not appear in Razor components — call Application-layer services instead.
- Use `async`/`await` for all service calls and lifecycle methods that perform I/O.
- Use `EventCallback<T>` for component events; never use `Action` or `Func` for cross-component communication.
- Use `@bind` and `@bind:get`/`@bind:set` for two-way binding.
- When styling is unavoidable, scoped CSS lives in `.razor.css` files alongside the component — never in a shared stylesheet unless the style genuinely applies globally.

## Naming Conventions

- PascalCase for component names, public members, and method names.
- camelCase for private fields and local variables.
- Prefix interface names with `I` (e.g. `ILabelManagerService`).
- Component files: `<FeatureName>.razor`, `<FeatureName>.razor.cs`, `<FeatureName>.razor.css`.

## Lifecycle and State

- Initialise data in `OnInitializedAsync`, not in constructors or field initialisers.
- Use `StateHasChanged()` only when the render cycle cannot detect changes automatically (e.g. after a callback from a non-UI thread).
- Do not call `StateHasChanged()` inside `OnInitializedAsync` — it is called automatically.
- Use `[Parameter]` for inputs, `[CascadingParameter]` for ambient context (e.g. theme, auth state).

## Error Handling

- Wrap service calls in `try`/`catch` in code-behind methods; display errors via `ISnackbar` notifications.
- Use `<ErrorBoundary>` for page-level error containment.
- Log errors via the injected `ILogger<T>`.

## MudBlazor Layout

The application layout uses the standard MudBlazor shell:

```
MudLayout
  MudAppBar (top navigation bar)
  MudDrawer (side navigation, MudNavMenu + MudNavLink items)
  MudMainContent
    Page content composed with MudBlazor components and utility classes
```

Do not deviate from this structure without an ADR.

## MudBlazor Dialog Pattern

- Inject `IDialogService` and call `await DialogService.ShowAsync<TDialog>(...)` to open dialogs.
- Dialog components inherit from `ComponentBase` and receive parameters via `[Parameter]`.
- Use `MudDialog` with `TitleContent`, `DialogContent`, and `DialogActions` fragments inside dialog components.
- Do not use inline modals or JS interop modals.

## MudBlazor Snackbar / Notification Pattern

- Inject `ISnackbar` and call `Snackbar.Add(message, Severity.X)` for user notifications.
- Use `Severity.Success`, `Severity.Error`, `Severity.Warning`, `Severity.Info`.

## Testing

- bUnit tests register MudBlazor services via `ctx.Services.AddMudServices()`.
- Set `ctx.JSInterop.Mode = JSRuntimeMode.Loose` as the default for MudBlazor component tests.
- Use `ctx.JSInterop.SetupVoid(...)` only when a test must assert specific JS interop calls.
- Tests must register MudBlazor services only and must not reference retired pre-migration UI service registrations or namespaces.
- Follow the naming convention `MethodUnderTest_Scenario_ExpectedOutcome`.
