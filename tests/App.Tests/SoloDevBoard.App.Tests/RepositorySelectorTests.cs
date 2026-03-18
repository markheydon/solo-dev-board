using Bunit;
using MudBlazor;
using MudBlazor.Services;
using SoloDevBoard.App.Components.Shared.Components;

namespace SoloDevBoard.App.Tests;

/// <summary>Component tests for <see cref="RepositorySelector"/> single-selection behaviour.</summary>
public sealed class RepositorySelectorTests
{
    [Fact]
    public async Task RepositorySelector_SingleSelectionOnly_SelectedRepositoryExists_DisablesAutocomplete()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<RepositorySelector>(parameters => parameters
            .Add(selector => selector.SingleSelectionOnly, true)
            .Add(selector => selector.AvailableRepositories, new[] { "owner/repo-a", "owner/repo-b" })
            .Add(selector => selector.SelectedRepositories, new[] { "owner/repo-a" }));

        // Assert
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        Assert.True(autocomplete.Instance.Disabled);
        Assert.NotNull(cut.Find("[data-testid='repository-selector-single-selection-lock-message']"));
    }

    [Fact]
    public async Task RepositorySelector_SingleSelectionOnly_NoSelectedRepository_EnablesAutocomplete()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var cut = ctx.Render<RepositorySelector>(parameters => parameters
            .Add(selector => selector.SingleSelectionOnly, true)
            .Add(selector => selector.AvailableRepositories, new[] { "owner/repo-a", "owner/repo-b" })
            .Add(selector => selector.SelectedRepositories, Array.Empty<string>()));

        // Assert
        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();
        Assert.False(autocomplete.Instance.Disabled);
        Assert.Empty(cut.FindAll("[data-testid='repository-selector-single-selection-lock-message']"));
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
