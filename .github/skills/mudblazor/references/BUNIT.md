# MudBlazor bUnit Testing

Reference for testing MudBlazor components with bUnit in SoloDevBoard.

---

## Test Context Setup

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Moq;
using Xunit;

public class LabelsPageTests : TestContext
{
    public LabelsPageTests()
    {
        // Register MudBlazor services
        Services.AddMudServices();

        // MudBlazor uses JS interop; use Loose mode to suppress unknown calls
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
```

---

## Rendering a Page Component

```csharp
[Fact]
public void LabelsPage_RendersDataGrid_WhenLabelsLoaded()
{
    // Arrange
    var mockService = new Mock<ILabelManagerService>();
    mockService.Setup(s => s.GetLabelsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([new LabelDto("bug", "#d73a4a", "Something is broken", [])]);
    Services.AddSingleton(mockService.Object);

    // Act
    var cut = RenderComponent<Labels>();

    // Assert
    cut.WaitForElement(".mud-table-root");
    Assert.Contains("bug", cut.Markup);
}
```

---

## Testing Dialog Interactions

MudBlazor dialogs are opened via `IDialogService`. In bUnit tests, add `MudDialogProvider` as a sibling or use a wrapping layout:

```csharp
[Fact]
public async Task CreateButton_OpensCreateDialog()
{
    // Register dialog service (already covered by AddMudServices)
    var cut = RenderComponent<Labels>(parameters => parameters
        .Add(p => p.SomeParam, value));

    cut.Find("[data-testid='create-button']").Click();

    cut.WaitForElement(".mud-dialog");
    Assert.Contains("Create Label", cut.Markup);
}
```

---

## Testing Snackbar Messages

`ISnackbar` is registered by `AddMudServices()`. You can verify calls via mock injection if needed:

```csharp
var snackbarMock = new Mock<ISnackbar>();
Services.AddSingleton(snackbarMock.Object);

// ... trigger action ...

snackbarMock.Verify(s => s.Add(
    It.Is<string>(m => m.Contains("created")),
    Severity.Success,
    It.IsAny<Action<SnackbarOptions>>(),
    It.IsAny<string>()),
    Times.Once);
```

---

## WaitForState and Async Rendering

MudBlazor components often trigger async re-renders. Use bUnit's `WaitForState` / `WaitForElement`:

```csharp
var cut = RenderComponent<Labels>();

// Wait for loading state to complete
cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular"), TimeSpan.FromSeconds(2));

Assert.Contains("mud-data-grid", cut.Markup);
```

---

## Common Test Patterns

### Assert component renders without exception

```csharp
var cut = RenderComponent<Dashboard>();
Assert.NotNull(cut);
```

### Assert navigation link exists

```csharp
var cut = RenderComponent<NavMenu>();
Assert.NotEmpty(cut.FindAll("a[href='/labels']"));
```

### Assert data grid row count

```csharp
var rows = cut.FindAll(".mud-table-row");
Assert.Equal(expectedCount, rows.Count);
```
