---
name: fluentui-blazor
description: >
  Guide for using the Microsoft Fluent UI Blazor component library
  (Microsoft.FluentUI.AspNetCore.Components NuGet package) in Blazor applications.
  Use this when the user is building a Blazor app with Fluent UI components,
  setting up the library, using FluentUI components like FluentButton, FluentDataGrid,
  FluentDialog, FluentToast, FluentNavMenu, FluentTextField, FluentSelect,
  FluentAutocomplete, FluentDesignTheme, or any component prefixed with "Fluent".
  Also use when troubleshooting missing providers, JS interop issues, or theming.
---

# Fluent UI Blazor — Consumer Usage Guide

This skill teaches how to correctly use the **Microsoft.FluentUI.AspNetCore.Components** (version 4) NuGet package in Blazor applications.

## Critical Rules

### 1. No manual `<script>` or `<link>` tags needed

The library auto-loads all CSS and JS via Blazor's static web assets and JS initializers. **Never tell users to add `<script>` or `<link>` tags for the core library.**

### 2. Providers are mandatory for service-based components

These provider components **MUST** be added to the root layout (e.g. `MainLayout.razor`) for their corresponding services to work. Without them, service calls **fail silently** (no error, no UI).

```razor
<FluentToastProvider />
<FluentDialogProvider />
<FluentMessageBarProvider />
<FluentTooltipProvider />
<FluentKeyCodeProvider />
```

### 3. Service registration in Program.cs

```csharp
builder.Services.AddFluentUIComponents();

// Or with configuration:
builder.Services.AddFluentUIComponents(options =>
{
    options.UseTooltipServiceProvider = true;  // default: true
    options.ServiceLifetime = ServiceLifetime.Scoped; // default
});
```

**ServiceLifetime rules:**
- `ServiceLifetime.Scoped` — for Blazor Server / Interactive (default)
- `ServiceLifetime.Singleton` — for Blazor WebAssembly standalone
- `ServiceLifetime.Transient` — **throws `NotSupportedException`**

### 4. Icons require a separate NuGet package

```
dotnet add package Microsoft.FluentUI.AspNetCore.Components.Icons
```

Usage with a `@using` alias:

```razor
@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons

<FluentIcon Value="@(Icons.Regular.Size24.Save)" />
<FluentIcon Value="@(Icons.Filled.Size20.Delete)" Color="@Color.Error" />
```

Pattern: `Icons.[Variant].[Size].[Name]`
- Variants: `Regular`, `Filled`
- Sizes: `Size12`, `Size16`, `Size20`, `Size24`, `Size28`, `Size32`, `Size48`

Custom image: `Icon.FromImageUrl("/path/to/image.png")`

**Never use string-based icon names** — icons are strongly-typed classes.

### 5. List component binding model

`FluentSelect<TOption>`, `FluentCombobox<TOption>`, `FluentListbox<TOption>`, and `FluentAutocomplete<TOption>` do NOT work like `<InputSelect>`. They use:

- `Items` — the data source (`IEnumerable<TOption>`)
- `OptionText` — `Func<TOption, string?>` to extract display text
- `OptionValue` — `Func<TOption, string?>` to extract the value string
- `SelectedOption` / `SelectedOptionChanged` — for single selection binding
- `SelectedOptions` / `SelectedOptionsChanged` — for multi-selection binding

```razor
<FluentSelect Items="@countries"
              OptionText="@(c => c.Name)"
              OptionValue="@(c => c.Code)"
              @bind-SelectedOption="@selectedCountry"
              Label="Country" />
```

**NOT** like this (wrong pattern):
```razor
@* WRONG — do not use InputSelect pattern *@
<FluentSelect @bind-Value="@selectedValue">
    <option value="1">One</option>
</FluentSelect>
```

### 6. FluentAutocomplete specifics

- Use `ValueText` (NOT `Value` — it's obsolete) for the search input text
- `OnOptionsSearch` is the required callback to filter options
- Default is `Multiple="true"`

```razor
<FluentAutocomplete TOption="Person"
                    OnOptionsSearch="@OnSearch"
                    OptionText="@(p => p.FullName)"
                    @bind-SelectedOptions="@selectedPeople"
                    Label="Search people" />

@code {
    private void OnSearch(OptionsSearchEventArgs<Person> args)
    {
        args.Items = allPeople.Where(p =>
            p.FullName.Contains(args.Text, StringComparison.OrdinalIgnoreCase));
    }
}
```

### 7. Dialog service pattern

**Do NOT toggle visibility of `<FluentDialog>` tags.** The service pattern is:

1. Create a content component implementing `IDialogContentComponent<TData>`:

```csharp
public partial class EditPersonDialog : IDialogContentComponent<Person>
{
    [Parameter] public Person Content { get; set; } = default!;

    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;

    private async Task SaveAsync()
    {
        await Dialog.CloseAsync(Content);
    }

    private async Task CancelAsync()
    {
        await Dialog.CancelAsync();
    }
}
```

2. Show the dialog via `IDialogService`:

```csharp
[Inject] private IDialogService DialogService { get; set; } = default!;

private async Task ShowEditDialog()
{
    var dialog = await DialogService.ShowDialogAsync<EditPersonDialog, Person>(
        person,
        new DialogParameters
        {
            Title = "Edit Person",
            PrimaryAction = "Save",
            SecondaryAction = "Cancel",
            Width = "500px",
            PreventDismissOnOverlayClick = true,
        });

    var result = await dialog.Result;
    if (!result.Cancelled)
    {
        var updatedPerson = result.Data as Person;
    }
}
```

For convenience dialogs:
```csharp
await DialogService.ShowConfirmationAsync("Are you sure?", "Yes", "No");
await DialogService.ShowSuccessAsync("Done!");
await DialogService.ShowErrorAsync("Something went wrong.");
```

### 8. Toast notifications

```csharp
[Inject] private IToastService ToastService { get; set; } = default!;

ToastService.ShowSuccess("Item saved successfully");
ToastService.ShowError("Failed to save");
ToastService.ShowWarning("Check your input");
ToastService.ShowInfo("New update available");
```

`FluentToastProvider` parameters: `Position` (default `TopRight`), `Timeout` (default 7000ms), `MaxToastCount` (default 4).

### 9. Design tokens and themes work only after render

Design tokens rely on JS interop. **Never set them in `OnInitialized`** — use `OnAfterRenderAsync`.

```razor
<FluentDesignTheme Mode="DesignThemeModes.System"
                   OfficeColor="OfficeColor.Teams"
                   StorageName="mytheme" />
```

### 10. FluentEditForm vs EditForm

`FluentEditForm` is only needed inside `FluentWizard` steps (per-step validation). For regular forms, use standard `EditForm` with Fluent form components:

```razor
<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <FluentTextField @bind-Value="@model.Name" Label="Name" Required />
    <FluentSelect Items="@options"
                  OptionText="@(o => o.Label)"
                  @bind-SelectedOption="@model.Category"
                  Label="Category" />
    <FluentValidationSummary />
    <FluentButton Type="ButtonType.Submit" Appearance="Appearance.Accent">Save</FluentButton>
</EditForm>
```

Use `FluentValidationMessage` and `FluentValidationSummary` instead of standard Blazor validation components for Fluent styling.

## Reference files

For detailed guidance on specific topics, see:

- [Setup and configuration](references/SETUP.md)
- [Layout and navigation](references/LAYOUT-AND-NAVIGATION.md)
- [Data grid](references/DATAGRID.md)
- [Theming](references/THEMING.md)

## Advanced UI Debugging Playbook

Use this section when a Fluent component renders but behaves incorrectly in real UI flows (clipping, overlap, invisible popups, incorrect stacking, or partial-width overlays).

### 1. Diagnose with DOM-first inspection.

- Inspect the live element in browser developer tools before changing markup.
- Confirm which element actually renders the popup (for autocomplete this is usually inside `fluent-anchored-region` and `div[role=listbox]`).
- Record computed values for `display`, `position`, `z-index`, `width`, `max-width`, `overflow`, and `background`.

### 2. Check ancestor constraints first.

- Walk up ancestor containers and find any `overflow: hidden`, `transform`, `filter`, `contain`, or `isolation` rules that can create clipping or stacking contexts.
- Prefer removing or relocating the constraining wrapper before introducing deep CSS overrides.
- If a card-like wrapper clips overlays, switch to a simpler container and recreate visual styling explicitly.

### 3. Prefer component parameters before deep CSS.

- Set documented Fluent parameters first (for example `ComponentWidth`, `ListStyleValue`, `Position`, and option templates where applicable).
- Use deep CSS only when parameter configuration cannot solve the issue.
- Keep overrides narrowly scoped to the page/component root to avoid global side effects.

### 4. For list popup readability, enforce opaque surfaces.

- Ensure popup containers and option rows have explicit non-transparent backgrounds.
- Add border/shadow to separate popup from underlying content.
- Validate with underlying high-contrast content (for example a data grid header) to confirm no bleed-through.

### 5. Verify both empty and populated states.

- Test with no table/grid rendered.
- Test with the full table/grid rendered underneath.
- Test with long option lists, scrolling, and narrow viewport widths.

## First-Time-Right Delivery Workflow (Fluent UI)

Apply this exact sequence for user-facing UI tasks.

1. Capture acceptance criteria in observable terms.
    Example: "Popup width matches selector width.", "No bleed-through from grid headers.", "No clipping at card boundaries.".
2. Implement the smallest change that can satisfy all criteria.
3. Perform live browser verification before commit.
    Verify desktop and mobile breakpoints, both empty and data-populated states.
4. Run automated checks.
    Run targeted component tests, then solution build/test.
5. Do not commit until visual acceptance is confirmed by screenshot or explicit user confirmation.

## Specific Guardrails for `FluentAutocomplete`

- Always bind `Items` and `OnOptionsSearch` together.
- Keep `OptionText` and `OptionValue` stable and deterministic.
- Avoid wrappers that unintentionally clip anchored overlays.
- If overlap appears, style both popup container and options as opaque.
- If width collapses, inspect whether listbox is `inline-flex` and explicitly force full-width block/flex behaviour.
