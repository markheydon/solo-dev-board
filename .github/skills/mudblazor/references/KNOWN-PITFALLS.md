# MudBlazor Known Pitfalls

Operational diagnosis guidance for common MudBlazor issues in SoloDevBoard.

---

## Pitfall 1: Popup components render nothing (missing MudPopoverProvider)

**Symptom:** `MudAutocomplete`, `MudSelect`, `MudDatePicker`, or `MudTimePicker` dropdown opens but is empty, or opens but immediately closes, or renders no dropdown at all.

**Root cause:** `MudPopoverProvider` is missing from `MainLayout.razor`.

**Fix:**

```razor
@* MainLayout.razor — ensure this is present alongside the other providers *@
<MudThemeProvider />
<MudPopoverProvider />   <!-- ← this is the fix -->
<MudDialogProvider />
<MudSnackbarProvider />
```

---

## Pitfall 2: Dialog service calls silently do nothing (missing MudDialogProvider)

**Symptom:** `await DialogService.ShowAsync<T>(...)` returns immediately with a cancelled result and no dialog appears.

**Root cause:** `MudDialogProvider` is missing from `MainLayout.razor`.

**Fix:** Add `<MudDialogProvider />` to `MainLayout.razor` (see Pitfall 1 for full provider list).

---

## Pitfall 3: Snackbar notifications never appear (missing MudSnackbarProvider)

**Symptom:** `Snackbar.Add(...)` is called but no notification appears.

**Root cause:** `MudSnackbarProvider` is missing from `MainLayout.razor`.

**Fix:** Add `<MudSnackbarProvider />` to `MainLayout.razor`.

---

## Pitfall 4: MudColorPicker — binding a hex string

**Symptom:** Two-way binding on `MudColorPicker` fails or the value does not update.

**Cause:** Confusion between `@bind-Value` (binds `MudColor` object) and `@bind-Text` (binds `string`).

**Fix:** When the model field is a `string` (e.g. `"#d73a4a"`), use `@bind-Text`:

```razor
<MudColorPicker @bind-Text="_hexColour" Label="Colour"
                ColorPickerMode="ColorPickerMode.HEX"
                Variant="Variant.Outlined" />
```

To convert between `MudColor` and `string`: `new MudColor(hexString)` or `mudColor.ToString("#")`.

---

## Pitfall 5: MudDataGrid filterable requires column filterable setup

**Symptom:** Grid has `Filterable="true"` but no filter UI appears on columns.

**Cause:** Filterable grid requires `FilterMode` and optionally `FilterDefinition`.

**Fix:** For simple column filtering, ensure `Filterable="true"` is on the grid and columns use `PropertyColumn` (which infers filter type automatically):

```razor
<MudDataGrid T="LabelDto" Items="@_items" Filterable="true" FilterMode="DataGridFilterMode.ColumnFilterRow">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
    </Columns>
</MudDataGrid>
```

---

## Pitfall 6: bUnit tests — MudBlazor JS interop errors

**Symptom:** bUnit tests throw `JSException` or similar during component render.

**Cause:** MudBlazor uses JS interop for some interactions (ripple effects, popover positioning). bUnit's strict JS interop mode throws on unexpected calls.

**Fix:**

```csharp
// In test setup:
ctx.JSInterop.Mode = JSRuntimeMode.Loose;
ctx.Services.AddMudServices();
```

---

## Pitfall 7: MudDrawer not toggling (missing @bind-Open)

**Symptom:** Hamburger menu button does nothing; drawer state doesn't update.

**Fix:** Use `@bind-Open` on `MudDrawer` and maintain a `bool _drawerOpen` field:

```razor
<MudDrawer @bind-Open="_drawerOpen">...</MudDrawer>

@code {
    private bool _drawerOpen = true;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
```

---

## Pitfall 8: MudAutocomplete — SearchFunc must be async

**Symptom:** `MudAutocomplete` filter does not work or throws.

**Fix:** `SearchFunc` must be `Func<string, CancellationToken, Task<IEnumerable<T>>>`:

```csharp
private async Task<IEnumerable<string>> SearchRepos(string value, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(value))
        return _repos.Select(r => r.Name);
    return _repos.Where(r => r.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                 .Select(r => r.Name);
}
```

---

## Pitfall 9: MudMenu and MudPopover clipped or hidden

**Symptom:** Menu or popover content appears cut off, hidden behind other surfaces, or not visible near container edges.

**Cause:** Missing provider setup, restrictive parent overflow styling, or assumptions about local z-index stacking.

**Fix:**

- Confirm `MudPopoverProvider` is present in `MainLayout.razor`.
- Avoid custom container styles such as `overflow: hidden` around menu and popover triggers unless clipping is intentional.
- Prefer MudBlazor layout primitives and defaults over custom z-index adjustments.

---

## Pitfall 10: MudForm validation state not updating as expected

**Symptom:** Submit actions proceed while invalid fields are present, or validation messages do not appear until too late.

**Cause:** Form-level validation is bypassed and controls are validated ad hoc.

**Fix:** Keep validation coordinated through `MudForm`.

```razor
<MudForm @ref="_form">
    <MudTextField @bind-Value="_name" Label="Name" Required="true" />
    <MudButton OnClick="@SubmitAsync">Save</MudButton>
</MudForm>

@code {
    private MudForm? _form;
    private string _name = string.Empty;

    private async Task SubmitAsync()
    {
        await _form!.Validate();
        if (!_form.IsValid)
            return;

        // Continue with save.
    }
}
```

---

## Pitfall 11: File upload accepted with no guardrails

**Symptom:** Unexpected file types or oversized files are processed.

**Cause:** File selection is treated as trusted input.

**Fix:** Validate uploaded files explicitly before processing.

- Validate file count, file extension, and content type.
- Enforce maximum file size in code.
- Provide clear snackbar or inline validation feedback.

---

## Pitfall 12: Tabs lose intended active panel after rerender

**Symptom:** Active tab jumps back unexpectedly after state changes.

**Cause:** Active tab selection is not controlled when tab collection or conditional visibility changes.

**Fix:** Bind and manage active panel explicitly when tabs are dynamic.

```razor
<MudTabs @bind-ActivePanelIndex="_activeTab">
    <MudTabPanel Text="Overview" />
    <MudTabPanel Text="Details" />
</MudTabs>

@code {
    private int _activeTab;
}
```
