---
name: mudblazor
description: Guide for using the MudBlazor component library in Blazor Server applications. Use this when building or refactoring Blazor pages and components for SoloDevBoard. Covers setup, layout, component usage patterns, dialog and snackbar services, data grids, forms, colour pickers, theming, and bUnit testing with MudBlazor. Also use when troubleshooting z-index, popup rendering, or styling issues.
---

# MudBlazor — Consumer Usage Guide

**MudBlazor** is a Material Design component library for Blazor built entirely in pure C#/Razor — no web components, no shadow DOM. This is the sole UI component library for SoloDevBoard (see ADR-0012).

**Official docs:** https://mudblazor.com/  
**Component demos:** https://mudblazor.com/components/

---

## Decision Order

When building or refactoring UI in SoloDevBoard, make decisions in this order:

1. Use an existing MudBlazor component and its parameters.
2. Compose MudBlazor layout primitives such as `MudStack`, `MudGrid`, `MudItem`, `MudPaper`, `MudContainer`, and `MudSpacer`.
3. Apply MudBlazor utility classes in the component `Class` attribute for spacing, alignment, display, and sizing.
4. Use theme configuration or built-in component properties such as `Color`, `Variant`, `Typo`, `Elevation`, `Dense`, and `GutterSize`.
5. Only then consider isolated `.razor.css`, and only when the requirement cannot be achieved by the options above.

Custom CSS is an exception, not a normal implementation tool. Raw HTML should be limited to framework-owned host elements or cases where MudBlazor genuinely has no suitable equivalent.

---

## Setup

### NuGet Package

```xml
<PackageReference Include="MudBlazor" />
```

This repository uses Central Package Management. Define or update the `MudBlazor` version in `Directory.Packages.props` via `<PackageVersion Include="MudBlazor" Version="..." />`.

### Program.cs

```csharp
builder.Services.AddMudServices();
// Or with configuration:
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
});
```

### _Imports.razor

```razor
@using MudBlazor
```

### MainLayout.razor — Required Providers

The following components **MUST** appear in `MainLayout.razor` for services to work:

```razor
<MudThemeProvider />
<MudPopoverProvider />   <!-- required for dropdowns, autocomplete, select -->
<MudDialogProvider />
<MudSnackbarProvider />
```

Without `MudPopoverProvider`, popup components (autocomplete, select) will render nothing.

---

## Layout Structure

The current SoloDevBoard baseline follows the official MudBlazor template shape:

```razor
<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit"
                       Edge="Edge.Start" OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h6">SoloDevBoard</MudText>
        <MudSpacer />
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" Elevation="2" ClipMode="DrawerClipMode.Always">
        <MudNavMenu>
            <MudNavLink Href="/" Match="NavLinkMatch.All"
                        Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
            <MudNavLink Href="/labels" Icon="@Icons.Material.Filled.Label">Labels</MudNavLink>
            <MudNavLink Href="/repositories"
                        Icon="@Icons.Material.Filled.Storage">Repositories</MudNavLink>
        </MudNavMenu>
    </MudDrawer>

    <MudMainContent Class="pt-16 pa-4">
        @Body
    </MudMainContent>
</MudLayout>
```

Use page-level `MudContainer`, `MudPaper`, `MudStack`, and `MudGrid` composition inside `@Body` rather than reworking `MainLayout` with bespoke wrappers or CSS.

## Styling Guidance

- Prefer component parameters and composition over styling.
- Prefer MudBlazor utility classes such as `pa-4`, `pt-16`, `mt-4`, `d-flex`, `justify-end`, and `align-center` before writing CSS.
- Prefer `MudStack` or `MudGrid` over raw `<div>` elements used only for layout.
- Prefer `MudText`, `MudAlert`, `MudChip`, `MudPaper`, and `MudDivider` over styled HTML elements.
- Do not add `<style>` blocks to Razor files.
- Only create or extend `.razor.css` when the requirement cannot be met through components, parameters, theme settings, or utility classes. Keep the CSS isolated and minimal.

---

## Component Quick Reference

See [references/COMPONENT-CHOOSER.md](references/COMPONENT-CHOOSER.md) for the full mapping.

### Text Input

```razor
<MudTextField @bind-Value="_value" Label="Name" Variant="Variant.Outlined" />
<MudTextField @bind-Value="_search" Label="Search" Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.Search" />
<MudTextField @bind-Value="_multi" Label="Description" Lines="4" />
```

### Select

```razor
<MudSelect @bind-Value="_selected" Label="Repository" Variant="Variant.Outlined">
    @foreach (var repo in _repos)
    {
        <MudSelectItem Value="@repo">@repo.Name</MudSelectItem>
    }
</MudSelect>
```

### Autocomplete (multi-select)

```razor
<MudAutocomplete T="string" Label="Repositories" @bind-Value="_repo"
                 SearchFunc="@SearchRepos" Variant="Variant.Outlined" />
```

### Checkbox

```razor
<MudCheckBox @bind-Value="_checked" Label="Apply to all repositories" />
```

### Button

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="@Save">Save</MudButton>
<MudButton Variant="Variant.Text" Color="Color.Secondary" OnClick="@Cancel">Cancel</MudButton>
<MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error" OnClick="@Delete" />
```

### Colour Picker

```razor
<MudColorPicker @bind-Text="_colour" Label="Label colour"
                ColorPickerMode="ColorPickerMode.HEX"
                Variant="Variant.Outlined" />
```

`@bind-Text` binds to a hex string (e.g. `"#d73a4a"`). Use `@bind-Value` to bind to a `MudColor` value object instead.

### Data Grid

See [references/DATAGRID.md](references/DATAGRID.md) for full grid patterns.

```razor
<MudDataGrid Items="@_labels" Filterable="true" SortMode="SortMode.Multiple"
             Hover="true" Striped="true" Dense="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.Colour" Title="Colour" />
        <TemplateColumn Title="Actions" CellClass="d-flex justify-end">
            <CellTemplate>
                <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.Edit"
                               OnClick="@(() => Edit(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

---

## Dialog Service Pattern

### Service injection

```csharp
[Inject] private IDialogService DialogService { get; set; } = default!;
```

### Opening a dialog

```csharp
var parameters = new DialogParameters<MyDialog>
{
    { x => x.Label, _selectedLabel }
};
var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
var dialog = await DialogService.ShowAsync<MyDialog>("Edit Label", parameters, options);
var result = await dialog.Result;

if (!result.Canceled)
{
    // handle confirmed result
}
```

### Dialog component

```razor
@* MyDialog.razor *@
<MudDialog>
    <TitleContent>Edit Label</TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_name" Label="Name" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="@Submit">Save</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public LabelDto Label { get; set; } = default!;

    private string _name = string.Empty;

    protected override void OnParametersSet() => _name = Label.Name;

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(_name));
}
```

---

## Snackbar / Notification Pattern

```csharp
[Inject] private ISnackbar Snackbar { get; set; } = default!;

// Usage:
Snackbar.Add("Label created successfully.", Severity.Success);
Snackbar.Add("Failed to create label.", Severity.Error);
Snackbar.Add("No repositories selected.", Severity.Warning);
```

---

## Loading State Pattern

```razor
@if (_loading)
{
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
}
else
{
    @* content *@
}
```

---

## Icons

MudBlazor includes Material Icons:

```razor
Icon="@Icons.Material.Filled.Label"        @* filled variant *@
Icon="@Icons.Material.Outlined.Delete"     @* outlined variant *@
Icon="@Icons.Material.TwoTone.Settings"    @* two-tone variant *@
```

---

## Theming

See [references/THEMING.md](references/THEMING.md) for custom palette setup.

```razor
@* MainLayout.razor *@
<MudThemeProvider Theme="_theme" />

@code {
    private MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976D2",
            Secondary = "#424242",
            AppbarBackground = "#1976D2"
        }
    };
}
```

---

## Known Pitfalls

See [references/KNOWN-PITFALLS.md](references/KNOWN-PITFALLS.md) for full details.

### MudPopoverProvider missing

Symptom: `MudAutocomplete` or `MudSelect` dropdown renders nothing.  
Fix: Add `<MudPopoverProvider />` to `MainLayout.razor`.

### MudDialogProvider missing

Symptom: `DialogService.ShowAsync` call silently does nothing.  
Fix: Add `<MudDialogProvider />` to `MainLayout.razor`.

### MudSnackbarProvider missing

Symptom: `Snackbar.Add(...)` silently does nothing.  
Fix: Add `<MudSnackbarProvider />` to `MainLayout.razor`.

### Two-way binding on MudColorPicker

Use `@bind-Text` for a hex string, not `@bind-Value` when the model field is a `string`.

### Custom CSS introduced too early

Symptom: A component adds a new `.razor.css` file or bespoke selectors for spacing, alignment, or simple layout.
Fix: Replace the styling with MudBlazor layout primitives or utility classes first. Only keep CSS that cannot be expressed with built-in component structure, parameters, theming, or utilities.

---

## bUnit Testing

See [references/BUNIT.md](references/BUNIT.md) for full test setup patterns.

```csharp
// Register MudBlazor services in the test context
ctx.Services.AddMudServices();

// Suppress MudBlazor JS interop calls that surface in unit tests
ctx.JSInterop.Mode = JSRuntimeMode.Loose;
```
