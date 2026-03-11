using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays open issue and pull request summary counts across repositories.</summary>
public partial class Audit : ComponentBase
{
    /// <summary>Gets or sets the audit dashboard application service.</summary>
    [Inject]
    public IAuditDashboardService AuditDashboardService { get; set; } = default!;

    /// <summary>Gets or sets the logger for audit page diagnostics.</summary>
    [Inject]
    public ILogger<Audit> Logger { get; set; } = default!;

    private IReadOnlyList<RepositoryAuditSummaryDto> repositorySummaries = [];
    private int totalOpenIssues;
    private int totalOpenPullRequests;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadAuditSummaryAsync();
    }

    private async Task LoadAuditSummaryAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var summary = await AuditDashboardService.GetRepositorySummaryAsync();
            repositorySummaries = summary
                .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            totalOpenIssues = repositorySummaries.Sum(result => result.OpenIssueCount);
            totalOpenPullRequests = repositorySummaries.Sum(result => result.OpenPullRequestCount);
        }
        catch (HttpRequestException ex)
        {
            repositorySummaries = [];
            totalOpenIssues = 0;
            totalOpenPullRequests = 0;
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load audit dashboard summary.");
            repositorySummaries = [];
            totalOpenIssues = 0;
            totalOpenPullRequests = 0;
            errorMessage = "An unexpected error occurred while loading the audit dashboard.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private static string BuildRepositoryUrl(string repositoryFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        return $"https://github.com/{repositoryFullName}";
    }
}
