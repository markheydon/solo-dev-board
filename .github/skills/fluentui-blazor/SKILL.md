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

### 4. Distinguish between transparency bleed-through and stacking-order bleed-through.

These look identical to the eye but have entirely different root causes and fixes. Applying the wrong fix wastes every attempt.

**Transparency bleed-through:** The overlay has `opacity < 1`, `background: transparent`, or no background colour at all. Underlying content is literally visible through the surface. Fix: add `background-color: var(--neutral-layer-1, #fff)` to the popup container and option elements.

**Stacking-order bleed-through (far more common):** The overlay IS fully opaque, but an underlying element with `position: sticky`, `position: fixed`, or a higher `z-index` is painted on top of it. Adding `background-color` or `opacity` to the overlay does absolutely nothing because the intruding element is rendered after it in the compositing order.

**Diagnosis test — apply this before any fix attempt:** Temporarily set `background-color: red !important` on the popup container. If the bleed-through area still shows the grid headers (not red), the cause is stacking order, not transparency. Stop all transparency fixes immediately and address z-index hierarchy instead.

**Fix for stacking-order bleed-through:**
- Lower the `z-index` of the intruding element (for example DataGrid sticky headers).
- Or raise the `z-index` of the popup stacking context above the intruder.
- Never try to fix it with `background-color`, `opacity`, or `backdrop-filter` — these have no effect on compositing order.

For `FluentDataGrid` headers specifically: the library CSS sets `.sticky-header` and `.header` to `z-index: 3`, and `tr[row-type='sticky-header'] > th` to `position: sticky; z-index: 2`. These always paint over any anchored overlay unless explicitly suppressed. See the **Known Component Interaction Risks** section for the ready-made fix pattern.

### 5. Verify both empty and populated states.

- Test with no table/grid rendered.
- Test with the full table/grid rendered underneath.
- Test with long option lists, scrolling, and narrow viewport widths.

## Iteration Failure Protocol

**If the same visual bug is reported as unfixed after two attempts, STOP all further CSS changes immediately.** Further variations of the same failed approach waste effort — if `background-color` alone did not fix bleed-through, neither will `opacity` or `backdrop-filter`. These are all the same class of fix and the same class will fail for the same reason.

Follow this protocol before making any more edits:

### Step 1 — State the hypothesis explicitly.

Write out: "I believe the element at `[selector]` is visible because `[specific reason]`." If you cannot complete this sentence with certainty, proceed to step 2 before guessing.

### Step 2 — Inspect the live DOM.

Check the following in browser developer tools:

- Where in the DOM is the popup element? Is `fluent-anchored-region` a child of the selector container, or is it appended to `<body>`? (Portalled elements break parent stacking context assumptions entirely.)
- What is the **computed** `z-index` on the popup? (Not the declared CSS value — browser DevTools computed tab shows the effective value.)
- What is the computed `z-index` on the intruding element?
- Does any ancestor have `contain`, `isolation`, `transform`, or `filter` creating an unexpected stacking context?
- Does the CSS override actually apply? (Check whether the selector matches what is in the DOM — specificity failures cause silent no-ops.)

### Step 3 — Identify the new hypothesis.

Based solely on evidence from step 2, write a new hypothesis before touching any code.

### Step 4 — Apply one targeted change.

Address exactly the new hypothesis. Do not multi-change. Do not commit until the visual result is confirmed.

### Step 5 — If the change is structural (markup changes, wrapping elements), report findings to the user first.

Structural changes carry more risk — confirm the diagnosis before proceeding.

**Never retry a variation of a failed approach.** Change the class of fix, not the value of the same property.

## Known Component Interaction Risks

This section documents specific component combinations that will cause visible layout or stacking issues unless addressed upfront. Apply the mitigation patterns at the start of implementation — do not wait for the bug to appear.

### FluentAutocomplete + FluentDataGrid on the same page

**Risk:** FluentDataGrid sticky headers will paint over the FluentAutocomplete popup dropdown when both appear on the same page. The datagrid column headings appear to "bleed through" the open selector. This is a stacking-order problem, not a transparency problem.

**Root cause:** The Fluent library CSS sets `.sticky-header { z-index: 3 }`, `.header { z-index: 3 }`, and `tr[row-type='sticky-header'] > th { position: sticky; z-index: 2 }`. These values exceed the default stacking context of `fluent-anchored-region`, so the grid headers composite above the popup.

**Required pattern — apply from the start:**

Markup: place the selector in a higher-elevation container than the grid.

```razor
<section class="selector-panel">
    <FluentAutocomplete ... />
</section>

<div class="results-surface">
    <FluentDataGrid ... />
</div>
```

Page `.razor.css`:

```css
.selector-panel {
    position: relative;
    z-index: 2;
    overflow: visible; /* must NOT be hidden */
}

.results-surface {
    position: relative;
    z-index: 1;
}

/* Suppress grid sticky-header elevation so overlay wins */
.results-surface ::deep .header,
.results-surface ::deep .sticky-header,
.results-surface ::deep tr[row-type='sticky-header'] > th {
    z-index: 0 !important;
}
```

### FluentCard as a container for popup-rendering controls

**Risk:** `FluentCard` may set `overflow: hidden` or create a stacking context depending on version, which clips anchored overlays. Popups rendered by `FluentAutocomplete`, `FluentSelect`, or `FluentCombobox` placed inside `FluentCard` will appear truncated or cut off.

**Required pattern:** Do not use `FluentCard` as a wrapper for any component that renders a popup. Use a plain `<section>` or `<div>` with explicit card styling:

```css
.my-panel {
    background: var(--neutral-layer-1, #fff);
    border: 1px solid var(--neutral-stroke-layer-rest, #d1d1d1);
    border-radius: 0.625rem;
    box-shadow: 0 1px 2px rgb(0 0 0 / 8%);
    padding: 1rem;
    overflow: visible; /* must be explicit */
    position: relative;
    z-index: 2;
}
```

## First-Time-Right Delivery Workflow (Fluent UI)

Apply this exact sequence for user-facing UI tasks.

**Step 0 — Pre-implementation component risk audit (do this before writing any code).**

Answer these questions for the page being built:

- Does the page combine a popup control (`FluentAutocomplete`, `FluentSelect`, `FluentCombobox`) with `FluentDataGrid`? If yes, apply the DataGrid stacking mitigation pattern from the Known Interaction Risks section upfront.
- Is any popup control inside or adjacent to `FluentCard`? If yes, replace `FluentCard` with a plain container and apply card styling manually.
- Are there any `overflow: hidden` or `isolation: isolate` containers in ancestor markup that the popup must render above?

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
- Do not place inside `FluentCard` — use a plain container with explicit card styling instead.
- Do not place inside any ancestor with `overflow: hidden` or `transform` — these clip the anchored overlay.
- If the popup appears at the wrong width, inspect whether the listbox is rendering as `inline-flex` and explicitly force full-width `flex` or `block` behaviour with `::deep` overrides.
- If bleed-through is visible when a `FluentDataGrid` is on the same page, apply the stacking mitigation pattern from the Known Interaction Risks section.
- Applying `background-color` or `opacity` to the popup will NOT fix bleed-through caused by stacking order — identify and lower the z-index of the intruding element instead.
