using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.Repositories.Pages;

/// <summary>Displays repositories available to the authenticated GitHub account.</summary>
public partial class Repositories : ComponentBase
{
    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the logger for repository page diagnostics.</summary>
    [Inject]
    public ILogger<Repositories> Logger { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> repositories = [];
    private bool isLoading = true;
    private string? errorMessage;
    private string? repositorySearchTerm;

    private IReadOnlyList<RepositoryDto> FilteredRepositories =>
        string.IsNullOrWhiteSpace(repositorySearchTerm)
            ? repositories
            : repositories
                .Where(repository =>
                    repository.Name.Contains(repositorySearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    repository.FullName.Contains(repositorySearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

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
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load repositories.");
            errorMessage = "An unexpected error occurred while loading repositories.";
            repositories = [];
        }
        finally
        {
            isLoading = false;
        }
    }
}
