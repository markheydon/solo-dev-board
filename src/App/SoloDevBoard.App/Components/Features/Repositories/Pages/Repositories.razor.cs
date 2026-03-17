using Microsoft.AspNetCore.Components;
using MudBlazor;
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

    /// <summary>Gets or sets the snackbar service used for transient page notifications.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> repositories = [];
    private bool isLoading = true;
    private string? errorMessage;
    private string feedbackMessage = "Loading repositories...";
    private Severity feedbackSeverity = Severity.Info;
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
        SetFeedback("Refreshing repositories.", Severity.Info);
        await LoadRepositoriesAsync();
    }

    private void AddRepository()
    {
        ShowPlaceholderFeedback("Add repository will be available in a future milestone.");
    }

    private void RemoveSelectedRepositories()
    {
        ShowPlaceholderFeedback("Remove repositories will be available in a future milestone.");
    }

    private void OpenBulkActions()
    {
        ShowPlaceholderFeedback("Bulk actions will be available in a future milestone.");
    }

    private void EditRepository(RepositoryDto repository)
    {
        ShowPlaceholderFeedback($"Edit repository '{repository.Name}' will be available in a future milestone.");
    }

    private void OpenRepositoryMoreActions(RepositoryDto repository)
    {
        ShowPlaceholderFeedback($"More actions for '{repository.Name}' will be available in a future milestone.");
    }

    private static string GetRepositoryStatusText(RepositoryDto repository)
        => repository.IsArchived ? "Archived" : "Connected";

    private static Color GetRepositoryStatusColour(RepositoryDto repository)
        => repository.IsArchived ? Color.Warning : Color.Success;

    private void SetFeedback(string message, Severity severity)
    {
        feedbackMessage = message;
        feedbackSeverity = severity;
    }

    private void ShowPlaceholderFeedback(string message)
    {
        SetFeedback(message, Severity.Info);
        Snackbar.Add(message, Severity.Info);
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoading = true;
        errorMessage = null;
        SetFeedback("Loading repositories...", Severity.Info);

        try
        {
            repositories = await RepositoryService.GetRepositoriesAsync();
            SetFeedback(
                repositories.Count == 0
                    ? "No repositories are connected yet."
                    : $"Loaded {repositories.Count} repositories.",
                repositories.Count == 0 ? Severity.Info : Severity.Success);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"GitHub API request failed. {ex.Message}";
            SetFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load repositories.");
            errorMessage = "An unexpected error occurred while loading repositories.";
            SetFeedback(errorMessage, Severity.Error);
            repositories = [];
        }
        finally
        {
            isLoading = false;
        }
    }
}
