using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services;
using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays repositories available to the authenticated GitHub account.</summary>
public partial class Repositories : ComponentBase
{
    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    private IReadOnlyList<Repository> repositories = [];
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            repositories = await RepositoryService.GetRepositoriesAsync();
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"GitHub API request failed. {ex.Message}";
            repositories = [];
        }
        catch (Exception)
        {
            errorMessage = "An unexpected error occurred while loading repositories.";
            repositories = [];
        }
        finally
        {
            isLoading = false;
        }
    }
}
