using Microsoft.AspNetCore.Components;
using Markdig;
using MudBlazor;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace SoloDevBoard.App.Components.Features.Triage.Pages;

/// <summary>Provides the one-at-a-time triage session workflow UI.</summary>
public partial class Triage : ComponentBase
{
    private static readonly MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Regex hrefRegex = new("href=\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex imageRegex = new("<img[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>Gets or sets the repository service used to load available repository scope options.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the triage service used to start and progress sessions.</summary>
    [Inject]
    public ITriageService TriageService { get; set; } = default!;

    /// <summary>Gets or sets the logger for triage diagnostics.</summary>
    [Inject]
    public ILogger<Triage> Logger { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service used for non-blocking notifications.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private string selectedRepositoryFullName = string.Empty;
    private bool includePullRequests = true;
    private bool isLoadingRepositories = true;
    private bool isStartingSession;
    private bool isApplyingSessionAction;
    private string skipReason = string.Empty;
    private TriageSessionDto? currentSession;
    private string? operationMessage;
    private Severity operationSeverity = Severity.Info;

    private bool CanStartSession
        => !isLoadingRepositories
            && !isStartingSession
            && !string.IsNullOrWhiteSpace(selectedRepositoryFullName);

    private IReadOnlyList<string> SelectedRepositoryFullNames
        => string.IsNullOrWhiteSpace(selectedRepositoryFullName)
            ? []
            : [selectedRepositoryFullName];

    private TriageItemDto? CurrentItem => currentSession?.CurrentItem;

    private MarkupString CurrentItemBodyMarkup
        => string.IsNullOrWhiteSpace(CurrentItem?.Body)
            ? new MarkupString(string.Empty)
            : new MarkupString(RenderMarkdownForDisplay(CurrentItem.Body));

    private string ScopeSummaryText
        => string.IsNullOrWhiteSpace(selectedRepositoryFullName)
            ? "Select one repository to scope this triage session."
            : $"Scope: {selectedRepositoryFullName}";

    private string CurrentPositionText
    {
        get
        {
            if (currentSession is null)
            {
                return "Session not started";
            }

            if (currentSession.Progress.TotalItems == 0)
            {
                return "No items in queue";
            }

            var position = Math.Min(currentSession.CurrentIndex + 1, currentSession.Progress.TotalItems);
            return $"Item {position} of {currentSession.Progress.TotalItems}";
        }
    }

    private double SessionProgressPercent
    {
        get
        {
            if (currentSession is null || currentSession.Progress.TotalItems == 0)
            {
                return 0;
            }

            return Math.Clamp(
                (currentSession.Progress.ProcessedItems / (double)currentSession.Progress.TotalItems) * 100d,
                0d,
                100d);
        }
    }

    private string RemainingCountText
        => currentSession is null
            ? "Remaining: 0 items"
            : $"Remaining: {currentSession.Progress.RemainingItems} {GetItemCountLabel(currentSession.Progress.RemainingItems)}";

    private string SkippedCountText
        => currentSession is null
            ? "Skipped: 0 items"
            : $"Skipped: {currentSession.Progress.SkippedItems} {GetItemCountLabel(currentSession.Progress.SkippedItems)}";

    private string CurrentItemTypeText
        => CurrentItem?.ItemType == TriageItemTypeDto.PullRequest
            ? "Pull request"
            : "Issue";

    private string SessionCompleteSummaryText
        => currentSession is null
            ? string.Empty
            : $"Processed {currentSession.Summary.ProcessedItems} of {currentSession.Summary.TotalItems} items. {currentSession.Summary.SkippedItems} skipped item(s) are available to revisit.";

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        operationMessage = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (!availableRepositories.Any(repository => repository.FullName.Equals(selectedRepositoryFullName, StringComparison.OrdinalIgnoreCase)))
            {
                selectedRepositoryFullName = string.Empty;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading triage repositories.");
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
            Snackbar.Add("Failed to load repositories for triage session scope.", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load triage repositories.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while loading repositories.";
            Snackbar.Add("An unexpected error occurred while loading triage repositories.", Severity.Error);
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private Task OnSelectedRepositoryChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var previousRepositoryFullName = selectedRepositoryFullName;

        selectedRepositoryFullName = repositoryFullNames
            .FirstOrDefault(static fullName => !string.IsNullOrWhiteSpace(fullName))
            ?? string.Empty;

        if (!string.Equals(previousRepositoryFullName, selectedRepositoryFullName, StringComparison.OrdinalIgnoreCase))
        {
            currentSession = null;
            skipReason = string.Empty;
            operationSeverity = Severity.Info;
            operationMessage = string.IsNullOrWhiteSpace(selectedRepositoryFullName)
                ? null
                : "Repository scope changed. Start a new triage session to load items.";
        }

        return Task.CompletedTask;
    }

    private Task OnIncludePullRequestsChangedAsync(bool value)
    {
        includePullRequests = value;
        return Task.CompletedTask;
    }

    private async Task StartSessionAsync()
    {
        if (!CanStartSession)
        {
            return;
        }

        if (!TryParseRepositoryScope(selectedRepositoryFullName, out var owner, out var repo))
        {
            operationSeverity = Severity.Warning;
            operationMessage = "Repository scope must be in owner/repository format.";
            Snackbar.Add("Select a valid repository scope before starting triage.", Severity.Warning);
            return;
        }

        isStartingSession = true;
        operationMessage = null;

        try
        {
            currentSession = await TriageService.StartSessionAsync(owner, repo, includePullRequests);
            skipReason = string.Empty;

            operationSeverity = Severity.Success;
            operationMessage = currentSession.Progress.TotalItems == 0
                ? $"No untriaged items were found in {selectedRepositoryFullName}."
                : $"Started triage session for {selectedRepositoryFullName}.";
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while starting triage session for {RepositoryScope}.", selectedRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while starting triage session. {ex.Message}";
            Snackbar.Add("Failed to start triage session due to a GitHub API error.", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start triage session for {RepositoryScope}.", selectedRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while starting triage session.";
            Snackbar.Add("An unexpected error occurred while starting triage session.", Severity.Error);
        }
        finally
        {
            isStartingSession = false;
        }
    }

    private async Task AdvanceSessionAsync()
    {
        if (currentSession is null || currentSession.CurrentItem is null || isApplyingSessionAction)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            currentSession = await TriageService.AdvanceSessionAsync(currentSession);
            operationSeverity = Severity.Info;
            operationMessage = currentSession.CurrentItem is null
                ? "Reached the end of the current queue."
                : $"Moved to {CurrentPositionText}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to move to the next triage item.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while moving to the next item.";
            Snackbar.Add("Could not move to the next triage item.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task SkipCurrentItemAsync()
    {
        if (currentSession is null || currentSession.CurrentItem is null || isApplyingSessionAction)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            currentSession = await TriageService.SkipCurrentItemAsync(currentSession, skipReason);
            skipReason = string.Empty;
            operationSeverity = Severity.Info;
            operationMessage = "Skipped current item and deferred it for later review.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to skip the current triage item.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while skipping the item.";
            Snackbar.Add("Could not skip the current triage item.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task RevisitSkippedItemsAsync()
    {
        if (currentSession is null || currentSession.SkippedItems.Count == 0 || isApplyingSessionAction)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            currentSession = await TriageService.RevisitSkippedItemsAsync(currentSession);
            operationSeverity = Severity.Info;
            operationMessage = "Skipped items were appended to the queue for review.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revisit skipped triage items.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while revisiting skipped items.";
            Snackbar.Add("Could not revisit skipped triage items.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private static bool TryParseRepositoryScope(string repositoryFullName, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (string.IsNullOrWhiteSpace(repositoryFullName))
        {
            return false;
        }

        var segments = repositoryFullName.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        owner = segments[0];
        repo = segments[1];
        return true;
    }

    /// <summary>Converts markdown body content into HTML suitable for safe display in the triage UI.</summary>
    /// <param name="body">The original issue or pull-request body content.</param>
    /// <returns>Rendered HTML with unsafe protocols and image tags removed.</returns>
    private static string RenderMarkdownForDisplay(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var html = Markdown.ToHtml(body, markdownPipeline);

        try
        {
            html = imageRegex.Replace(html, string.Empty);

            // Keep rendered links clickable while preventing unsafe protocols.
            html = hrefRegex.Replace(
                html,
                static match =>
                {
                    var url = match.Groups["url"].Value;
                    return IsAllowedLink(url)
                        ? match.Value
                        : "href=\"#\"";
                });
        }
        catch (RegexMatchTimeoutException)
        {
            // Fall back to escaped plain text if regex replacement times out.
            return $"<p>{HtmlEncoder.Default.Encode(body)}</p>";
        }

        return html;
    }

    private static bool IsAllowedLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith("#", StringComparison.Ordinal))
        {
            return true;
        }

        if (url.StartsWith("/", StringComparison.Ordinal))
        {
            return !url.StartsWith("//", StringComparison.Ordinal);
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https" or "mailto";
        }

        if (!Uri.TryCreate(url, UriKind.Relative, out _))
        {
            return false;
        }

        // Block scheme-like values masquerading as relative URLs.
        var schemeSeparatorIndex = url.IndexOf(':', StringComparison.Ordinal);
        return schemeSeparatorIndex <= 0;
    }

    private static string GetItemCountLabel(int count) => count == 1 ? "item" : "items";

}
