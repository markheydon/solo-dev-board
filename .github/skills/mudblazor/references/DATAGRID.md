# MudBlazor Data Grid Patterns

Reference for `MudDataGrid<T>` usage in SoloDevBoard.

---

## Basic Setup

```razor
<MudDataGrid T="LabelDto" Items="@_labels" Filterable="true"
             SortMode="SortMode.Multiple" Hover="true" Dense="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" Sortable="true" />
        <PropertyColumn Property="x => x.Colour" Title="Colour" />
        <TemplateColumn Title="Actions" CellClass="d-flex justify-end" Sortable="false">
            <CellTemplate>
                <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.Edit"
                               Color="Color.Primary"
                               OnClick="@(() => OpenEditDialog(context.Item))" />
                <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.Delete"
                               Color="Color.Error"
                               OnClick="@(() => OpenDeleteDialog(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

---

## Loading State

Wrap the grid with a loading check; use `MudProgressCircular` while data is loading:

```razor
@if (_loading)
{
    <MudProgressCircular Indeterminate="true" Color="Color.Primary" Class="my-4" />
}
else if (_labels.Count == 0)
{
    <MudText Typo="Typo.body1" Class="pa-4">No labels found.</MudText>
}
else
{
    <MudDataGrid ... />
}
```

---

## Server-side / Async Data

Use the `ServerData` delegate for large datasets fetched asynchronously:

```razor
<MudDataGrid T="LabelDto" ServerData="@LoadServerData" @ref="_grid">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
    </Columns>
</MudDataGrid>

@code {
    private MudDataGrid<LabelDto>? _grid;

    private async Task<GridData<LabelDto>> LoadServerData(GridState<LabelDto> state)
    {
        var items = await LabelService.GetLabelsAsync();
        return new GridData<LabelDto>
        {
            Items = items,
            TotalItems = items.Count
        };
    }
}
```

---

## Row Selection

```razor
<MudDataGrid T="LabelDto" Items="@_labels" SelectedItemsChanged="@OnSelectionChanged"
             MultiSelection="true">
    ...
</MudDataGrid>

@code {
    private HashSet<LabelDto> _selected = [];

    private void OnSelectionChanged(HashSet<LabelDto> items) => _selected = items;
}
```

---

## Custom Cell Rendering (colour swatch example)

```razor
<TemplateColumn Title="Colour">
    <CellTemplate>
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            <div style="width:16px;height:16px;border-radius:50%;
                        background-color:@context.Item.Colour;
                        border:1px solid rgba(0,0,0,0.12);" />
            <MudText Typo="Typo.body2">@context.Item.Colour</MudText>
        </MudStack>
    </CellTemplate>
</TemplateColumn>
```

---

## No z-index / stacking issues

MudBlazor's `MudDataGrid` sticky headers use inline CSS and do not conflict with `MudAutocomplete` or `MudSelect` popups. The `MudPopoverProvider` in `MainLayout.razor` ensures popup components render at document body level, above all grid content.
