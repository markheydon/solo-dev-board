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
