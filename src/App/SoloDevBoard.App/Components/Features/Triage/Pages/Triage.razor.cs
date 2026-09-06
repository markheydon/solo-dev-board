using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Feedback;
using SoloDevBoard.Application.GitHub;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;

namespace SoloDevBoard.App.Components.Features.Triage.Pages;

/// <summary>Mutually exclusive triage outcomes for the current queue item.</summary>
internal enum TriageItemDisposition
{
    /// <summary>Keep the item open and optionally apply process metadata.</summary>
    Process,

    /// <summary>Close the item as a duplicate.</summary>
    Duplicate,

    /// <summary>Defer the item within the current session without GitHub writes.</summary>
    Skip,
}

/// <summary>Provides the one-at-a-time triage session workflow UI.</summary>
public partial class Triage : ComponentBase
{
    private static readonly MarkdownPipeline markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Regex hrefRegex = new("href=\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex imageRegex = new("<img[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex summaryItemDetailRegex = new(
        "^(?<itemType>Issue|Pull request) #(?<itemNumber>\\d+) [(](?<repository>[^)]+)[)]: (?<description>.+)$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Dictionary<string, object> externalLinkUserAttributes = new()
    {
        ["rel"] = "noopener noreferrer",
    };

    /// <summary>Gets or sets the repository service used to load available repository scope options.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the triage service used to start and progress sessions.</summary>
    [Inject]
    public ITriageService TriageService { get; set; } = default!;

    /// <summary>Gets or sets the label service used to retrieve repository label options.</summary>
    [Inject]
    public ILabelManagerService LabelManagerService { get; set; } = default!;

    /// <summary>Gets or sets the logger for triage diagnostics.</summary>
    [Inject]
    public ILogger<Triage> Logger { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service for transient operation feedback.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private string selectedRepositoryFullName = string.Empty;
    private bool includePullRequests = true;
    private bool isLoadingRepositories = true;
    private bool isStartingSession;
    private bool isApplyingSessionAction;
    private TriageSessionDto? currentSession;
    private IReadOnlyList<string> availableLabelNames = [];
    private IReadOnlyList<TriageMilestoneOptionDto> availableMilestoneOptions = [];
    private IReadOnlyList<TriageProjectBoardOptionDto> availableProjectBoardOptions = [];
    private string selectedQuickActionLabelName = string.Empty;
    private string duplicateReference = string.Empty;
    private string skipReason = string.Empty;
    private int? selectedMilestoneNumber;
    private string selectedProjectBoardId = string.Empty;
    private string selectedProjectBoardStatusOptionId = string.Empty;
    private string? inaccessibleProjectBoardsWarning;
    private bool hasRepositoryLoadFailure;
    private string? repositoryLoadErrorMessage;
    private bool isReloadingFromGitHub;
    private TriageItemDisposition currentDisposition = TriageItemDisposition.Process;

    private void ShowTransientFeedback(string message, Severity severity)
        => SnackbarFeedback.Show(Snackbar, message, severity);

    private bool isLoadingPlanningOptions;

    private bool CanStartSession
        => !isLoadingRepositories
            && !isStartingSession
            && !string.IsNullOrWhiteSpace(selectedRepositoryFullName);

    private IReadOnlyList<string> SelectedRepositoryFullNames
        => string.IsNullOrWhiteSpace(selectedRepositoryFullName)
            ? []
            : [selectedRepositoryFullName];

    private TriageItemDto? CurrentItem => currentSession?.CurrentItem;

    private bool CanCommitCurrentDisposition
        => !isApplyingSessionAction
            && CurrentItem is not null
            && currentDisposition switch
            {
                TriageItemDisposition.Process => true,
                TriageItemDisposition.Duplicate => CanCloseAsDuplicate,
                TriageItemDisposition.Skip => CanSkipCurrentItem,
                _ => false,
            };

    private string PrimaryCommitButtonText
        => currentDisposition switch
        {
            TriageItemDisposition.Process => "Save and next",
            TriageItemDisposition.Duplicate => "Close as duplicate and next",
            TriageItemDisposition.Skip => "Skip and next",
            _ => "Commit",
        };

    private string PrimaryCommitButtonTestId
        => currentDisposition switch
        {
            TriageItemDisposition.Process => "triage-save-and-next-button",
            TriageItemDisposition.Duplicate => "triage-close-duplicate-and-next-button",
            TriageItemDisposition.Skip => "triage-skip-and-next-button",
            _ => "triage-commit-button",
        };

    private bool CanCloseAsDuplicate
        => !isApplyingSessionAction
            && CurrentItem is not null
            && !string.IsNullOrWhiteSpace(duplicateReference);

    private bool CanSkipCurrentItem
        => !isApplyingSessionAction
            && CurrentItem is not null;

    private TriageProjectBoardOptionDto? ActiveProjectBoard
        => availableProjectBoardOptions.FirstOrDefault(option =>
            option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal));

    private bool HasInaccessibleProjectBoardsWarning
        => !string.IsNullOrWhiteSpace(inaccessibleProjectBoardsWarning);

    private IReadOnlyList<TriageProjectBoardStatusOptionDto> ActiveProjectBoardStatusOptions
        => ActiveProjectBoard?.StatusOptions ?? [];

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

    private async Task ReloadRepositoriesAsync()
        => await RetryLoadRepositoriesAsync();

    private async Task ReloadFromGitHubAsync()
    {
        if (isLoadingRepositories || isReloadingFromGitHub || isStartingSession || isApplyingSessionAction)
        {
            return;
        }

        var preservedRepositoryFullName = selectedRepositoryFullName;
        var preservedIncludePullRequests = includePullRequests;
        var preservedProjectBoardId = selectedProjectBoardId;
        var preservedProjectBoardStatusOptionId = selectedProjectBoardStatusOptionId;
        var activeSession = currentSession;

        isReloadingFromGitHub = true;

        try
        {
            await RefreshRepositoriesCatalogueAsync(forceReload: true);

            if (!string.IsNullOrWhiteSpace(preservedRepositoryFullName))
            {
                selectedRepositoryFullName = availableRepositories
                    .Any(repository => repository.FullName.Equals(preservedRepositoryFullName, StringComparison.OrdinalIgnoreCase))
                    ? preservedRepositoryFullName
                    : string.Empty;
            }

            includePullRequests = preservedIncludePullRequests;

            if (activeSession is not null && !string.IsNullOrWhiteSpace(selectedRepositoryFullName))
            {
                selectedProjectBoardId = preservedProjectBoardId;
                selectedProjectBoardStatusOptionId = preservedProjectBoardStatusOptionId;
                await LoadQuickActionLabelsAsync(
                    RepositoryFullName.ResolveOwner(selectedRepositoryFullName),
                    RepositoryFullName.ResolveRepositoryName(selectedRepositoryFullName),
                    forceReload: true);
                await LoadPlanningOptionsAsync(activeSession, preserveProjectBoardSelection: true);
            }
        }
        finally
        {
            isReloadingFromGitHub = false;
        }
    }

    private async Task RetryLoadRepositoriesAsync()
    {
        await RefreshRepositoriesCatalogueAsync(forceReload: true);
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        repositoryLoadErrorMessage = null;

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
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading triage repositories.");
            availableRepositories = [];
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load triage repositories.");
            availableRepositories = [];
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private async Task RefreshRepositoriesCatalogueAsync(bool forceReload)
    {
        hasRepositoryLoadFailure = false;
        repositoryLoadErrorMessage = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync(forceReload: forceReload))
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while refreshing triage repositories.");
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh triage repositories.");
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories.";
        }
    }

    private bool ShowReloadFromGitHubButton => !isLoadingRepositories;

    private bool IsReloadFromGitHubDisabled => isReloadingFromGitHub || isStartingSession || isApplyingSessionAction;

    private Task OnSelectedRepositoryChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var previousRepositoryFullName = selectedRepositoryFullName;

        selectedRepositoryFullName = repositoryFullNames
            .FirstOrDefault(static fullName => !string.IsNullOrWhiteSpace(fullName))
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(previousRepositoryFullName)
            && !string.Equals(previousRepositoryFullName, selectedRepositoryFullName, StringComparison.OrdinalIgnoreCase))
        {
            currentSession = null;
            availableLabelNames = [];
            availableMilestoneOptions = [];
            availableProjectBoardOptions = [];
            selectedQuickActionLabelName = string.Empty;
            duplicateReference = string.Empty;
            skipReason = string.Empty;
            selectedMilestoneNumber = null;
            selectedProjectBoardId = string.Empty;
            selectedProjectBoardStatusOptionId = string.Empty;
            currentDisposition = TriageItemDisposition.Process;

            if (!string.IsNullOrWhiteSpace(selectedRepositoryFullName))
            {
                ShowTransientFeedback("Repository scope changed. Start a new triage session to load items.", Severity.Info);
            }
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
            ShowTransientFeedback("Repository scope must be in owner/repository format.", Severity.Warning);
            return;
        }

        isStartingSession = true;

        try
        {
            currentSession = await TriageService.StartSessionAsync(owner, repo, includePullRequests);
            await LoadQuickActionLabelsAsync(owner, repo);
            await LoadPlanningOptionsAsync(currentSession);
            SyncPlanningSelectionFromCurrentItem();

            ShowTransientFeedback(currentSession.Progress.TotalItems == 0
                ? $"No untriaged items were found in {selectedRepositoryFullName}."
                : $"Started triage session for {selectedRepositoryFullName}.", Severity.Success);
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while starting triage session for {RepositoryScope}.", selectedRepositoryFullName);
            ShowTransientFeedback($"GitHub API request failed while starting triage session. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start triage session for {RepositoryScope}.", selectedRepositoryFullName);
            ShowTransientFeedback("An unexpected error occurred while starting triage session.", Severity.Error);
        }
        finally
        {
            isStartingSession = false;
        }
    }

    private async Task LoadQuickActionLabelsAsync(string owner, string repo, bool forceReload = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        try
        {
            var labelNames = await LabelManagerService.GetLabelsAsync(owner, repo, forceReload: forceReload);

            availableLabelNames = labelNames
                .Select(label => label.Name)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedQuickActionLabelName = string.Empty;

            if (availableLabelNames.Count == 0)
            {
                ShowTransientFeedback("No repository labels are available to apply as quick actions.", Severity.Warning);
            }
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading quick-action labels for {RepositoryScope}.", $"{owner}/{repo}");
            availableLabelNames = [];
            selectedQuickActionLabelName = string.Empty;
            ShowTransientFeedback($"GitHub API request failed while loading labels. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load quick-action labels for {RepositoryScope}.", $"{owner}/{repo}");
            availableLabelNames = [];
            selectedQuickActionLabelName = string.Empty;
            ShowTransientFeedback("An unexpected error occurred while loading labels for quick actions.", Severity.Error);
        }
    }

    private Task OnSelectedQuickActionLabelChangedAsync(string labelName)
    {
        selectedQuickActionLabelName = labelName ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnDuplicateReferenceChangedAsync(string value)
    {
        duplicateReference = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnSkipReasonChangedAsync(string value)
    {
        skipReason = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task<IEnumerable<string>> SearchQuickActionLabelsAsync(string? value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<string> matches = availableLabelNames;

        if (!string.IsNullOrWhiteSpace(value))
        {
            var filter = value.Trim();
            matches = matches.Where(labelName => labelName.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(matches);
    }

    private bool LabelExists(string? labelName)
    {
        if (string.IsNullOrWhiteSpace(labelName))
        {
            return false;
        }

        var candidate = labelName.Trim();

        foreach (var availableLabelName in availableLabelNames)
        {
            if (string.Equals(availableLabelName, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private Task OnDispositionChangedAsync(TriageItemDisposition disposition)
    {
        currentDisposition = disposition;
        return Task.CompletedTask;
    }

    private async Task CommitCurrentDispositionAsync()
    {
        if (!CanCommitCurrentDisposition || currentSession is null || CurrentItem is null)
        {
            return;
        }

        switch (currentDisposition)
        {
            case TriageItemDisposition.Process:
                await SaveAndNextAsync();
                break;
            case TriageItemDisposition.Duplicate:
                await CloseCurrentItemAsDuplicateAsync();
                break;
            case TriageItemDisposition.Skip:
                await SkipCurrentItemAsync();
                break;
            default:
                break;
        }
    }

    private async Task SaveAndNextAsync()
    {
        if (currentSession is null || CurrentItem is null || isApplyingSessionAction)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var processedItemNumber = CurrentItem.Number;
            var itemAtCommitStart = CurrentItem;
            var request = BuildProcessCommitRequest();
            var hadWrites = HasProcessCommitWrites(request, itemAtCommitStart);
            currentSession = await TriageService.ProcessAndAdvanceCurrentItemAsync(currentSession, request);
            SyncPlanningSelectionFromCurrentItem();
            ShowTransientFeedback(currentSession.CurrentItem is null
                ? hadWrites
                    ? $"Saved changes for item #{processedItemNumber}. Reached the end of the current queue."
                    : $"Moved past item #{processedItemNumber} without changes. Reached the end of the current queue."
                : hadWrites
                    ? $"Saved changes for item #{processedItemNumber} and moved to {CurrentPositionText}."
                    : $"Moved past item #{processedItemNumber} without changes and moved to {CurrentPositionText}.", Severity.Success);
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while saving triage item changes.");
            ShowTransientFeedback($"GitHub API request failed while saving changes. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save triage item changes.");
            ShowTransientFeedback("An unexpected error occurred while saving changes for this item.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private TriageProcessCommitRequestDto BuildProcessCommitRequest()
    {
        var labelName = LabelExists(selectedQuickActionLabelName)
            ? selectedQuickActionLabelName.Trim()
            : null;

        var milestoneTitle = availableMilestoneOptions
            .FirstOrDefault(option => option.Number == selectedMilestoneNumber)
            ?.Title;

        string? projectBoardId = null;
        string? projectBoardTitle = null;
        string? statusFieldId = null;
        string? statusOptionId = null;
        string? statusOptionName = null;

        if (!string.IsNullOrWhiteSpace(selectedProjectBoardId)
            && !string.IsNullOrWhiteSpace(selectedProjectBoardStatusOptionId)
            && ActiveProjectBoard is not null)
        {
            var selectedStatusOption = ActiveProjectBoardStatusOptions
                .FirstOrDefault(option => option.Id.Equals(selectedProjectBoardStatusOptionId, StringComparison.Ordinal));

            if (selectedStatusOption is not null)
            {
                projectBoardId = ActiveProjectBoard.Id;
                projectBoardTitle = ActiveProjectBoard.Title;
                statusFieldId = ActiveProjectBoard.StatusFieldId;
                statusOptionId = selectedStatusOption.Id;
                statusOptionName = selectedStatusOption.Name;
            }
        }

        return new TriageProcessCommitRequestDto(
            labelName,
            selectedMilestoneNumber,
            milestoneTitle,
            projectBoardId,
            projectBoardTitle,
            statusFieldId,
            statusOptionId,
            statusOptionName);
    }

    private static bool HasProcessCommitWrites(TriageProcessCommitRequestDto request, TriageItemDto itemAtCommitStart)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(itemAtCommitStart);

        if (!string.IsNullOrWhiteSpace(request.LabelName))
        {
            return true;
        }

        if (request.MilestoneNumber != itemAtCommitStart.MilestoneNumber)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(request.ProjectBoardId)
            && !string.IsNullOrWhiteSpace(request.StatusOptionId);
    }

    private async Task CompleteWithoutChangesAsync()
    {
        if (currentSession is null || currentSession.CurrentItem is null || isApplyingSessionAction)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var completedItemNumber = currentSession.CurrentItem.Number;
            currentSession = await TriageService.AdvanceSessionAsync(currentSession);
            SyncPlanningSelectionFromCurrentItem();
            ShowTransientFeedback(currentSession.CurrentItem is null
                ? $"Moved past item #{completedItemNumber} without changes. Reached the end of the current queue."
                : $"Moved past item #{completedItemNumber} without changes and moved to {CurrentPositionText}.", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to move to the next triage item without changes.");
            ShowTransientFeedback("An unexpected error occurred while moving to the next item without changes.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task CloseCurrentItemAsDuplicateAsync()
    {
        if (currentSession is null || CurrentItem is null || !CanCloseAsDuplicate)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var closedItemNumber = CurrentItem.Number;
            var trimmedDuplicateReference = duplicateReference.Trim();

            var duplicateClosedSession = await TriageService.CloseCurrentItemAsDuplicateAsync(
                currentSession,
                trimmedDuplicateReference);

            var duplicateActionDetail = duplicateClosedSession.ActionHistory.LastOrDefault()?.Detail ?? string.Empty;
            currentSession = await TriageService.AdvanceSessionAsync(duplicateClosedSession);
            SyncPlanningSelectionFromCurrentItem();

            var baseMessage = currentSession.CurrentItem is null
                ? $"Closed item #{closedItemNumber} as a duplicate of '{trimmedDuplicateReference}'. Reached the end of the current queue."
                : $"Closed item #{closedItemNumber} as a duplicate of '{trimmedDuplicateReference}' and moved to {CurrentPositionText}.";

            var duplicateLabelSuffix = string.Empty;
            const string duplicatePrefix = "Closed as duplicate of '";
            if (duplicateActionDetail.StartsWith(duplicatePrefix, StringComparison.Ordinal))
            {
                var separator = ". ";
                var separatorIndex = duplicateActionDetail.IndexOf(separator, StringComparison.Ordinal);
                if (separatorIndex >= 0 && separatorIndex + separator.Length < duplicateActionDetail.Length)
                {
                    duplicateLabelSuffix = duplicateActionDetail[(separatorIndex + separator.Length)..].Trim();
                }
            }

            var duplicateMessage = string.IsNullOrWhiteSpace(duplicateLabelSuffix)
                ? baseMessage
                : $"{baseMessage} {duplicateLabelSuffix}";
            ShowTransientFeedback(duplicateMessage, Severity.Success);
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while closing triage item as duplicate.");
            ShowTransientFeedback($"GitHub API request failed while closing as duplicate. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to close the current triage item as duplicate.");
            ShowTransientFeedback("An unexpected error occurred while closing this item as a duplicate.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task SkipCurrentItemAsync()
    {
        if (currentSession is null || CurrentItem is null || !CanSkipCurrentItem)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var skippedItemNumber = CurrentItem.Number;
            currentSession = await TriageService.SkipCurrentItemAsync(currentSession, skipReason);
            SyncPlanningSelectionFromCurrentItem();

            ShowTransientFeedback(currentSession.CurrentItem is null
                ? $"Skipped item #{skippedItemNumber} for later review. Reached the end of the current queue."
                : $"Skipped item #{skippedItemNumber} for later review and moved to {CurrentPositionText}.", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to skip the current triage item.");
            ShowTransientFeedback("An unexpected error occurred while skipping this item.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private Task OnSelectedMilestoneChangedAsync(int? milestoneNumber)
    {
        selectedMilestoneNumber = milestoneNumber;
        return Task.CompletedTask;
    }

    private Task OnSelectedProjectBoardChangedAsync(string projectBoardId)
    {
        selectedProjectBoardId = projectBoardId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(selectedProjectBoardId))
        {
            selectedProjectBoardStatusOptionId = string.Empty;
            return Task.CompletedTask;
        }

        if (ActiveProjectBoardStatusOptions.Count == 0)
        {
            selectedProjectBoardStatusOptionId = string.Empty;
            return Task.CompletedTask;
        }

        if (!ActiveProjectBoardStatusOptions.Any(option => option.Id.Equals(selectedProjectBoardStatusOptionId, StringComparison.Ordinal)))
        {
            selectedProjectBoardStatusOptionId = ActiveProjectBoardStatusOptions[0].Id;
        }

        return Task.CompletedTask;
    }

    private Task OnSelectedProjectBoardStatusOptionChangedAsync(string statusOptionId)
    {
        selectedProjectBoardStatusOptionId = statusOptionId ?? string.Empty;
        return Task.CompletedTask;
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
            SyncPlanningSelectionFromCurrentItem();
            ShowTransientFeedback("Skipped items were appended to the queue for review.", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revisit skipped triage items.");
            ShowTransientFeedback("An unexpected error occurred while revisiting skipped items.", Severity.Error);
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task LoadPlanningOptionsAsync(TriageSessionDto session, bool preserveProjectBoardSelection = false)
    {
        ArgumentNullException.ThrowIfNull(session);

        isLoadingPlanningOptions = true;
        inaccessibleProjectBoardsWarning = null;

        var preservedProjectBoardId = preserveProjectBoardSelection ? selectedProjectBoardId : string.Empty;
        var preservedProjectBoardStatusOptionId = preserveProjectBoardSelection ? selectedProjectBoardStatusOptionId : string.Empty;

        var milestoneLoadFailed = false;

        try
        {
            availableMilestoneOptions = await TriageService.GetMilestoneOptionsAsync(session);
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading milestone options for triage.");
            availableMilestoneOptions = [];
            milestoneLoadFailed = true;
            ShowTransientFeedback($"GitHub API request failed while loading milestone options. {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load milestone options for triage.");
            availableMilestoneOptions = [];
            milestoneLoadFailed = true;
            ShowTransientFeedback("An unexpected error occurred while loading milestone options.", Severity.Error);
        }

        try
        {
            var discovery = await TriageService.GetProjectBoardOptionsAsync(session);
            availableProjectBoardOptions = discovery.Options;
            inaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);

            if (preserveProjectBoardSelection
                && !string.IsNullOrWhiteSpace(preservedProjectBoardId)
                && discovery.Options.Any(option => option.Id.Equals(preservedProjectBoardId, StringComparison.Ordinal)))
            {
                selectedProjectBoardId = preservedProjectBoardId;
                selectedProjectBoardStatusOptionId = preservedProjectBoardStatusOptionId;
            }

            if (milestoneLoadFailed)
            {
                return;
            }
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GitHub API request failed while loading project-board options for triage.");
            availableProjectBoardOptions = [];
            inaccessibleProjectBoardsWarning = null;
            selectedProjectBoardId = string.Empty;
            selectedProjectBoardStatusOptionId = string.Empty;

            if (!milestoneLoadFailed)
            {
                ShowTransientFeedback($"Milestones loaded, but project-board options could not be loaded. {ex.Message}", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load project-board options for triage.");
            availableProjectBoardOptions = [];
            inaccessibleProjectBoardsWarning = null;
            selectedProjectBoardId = string.Empty;
            selectedProjectBoardStatusOptionId = string.Empty;

            if (!milestoneLoadFailed)
            {
                ShowTransientFeedback("Milestones loaded, but an unexpected error occurred while loading project-board options.", Severity.Warning);
            }
        }
        finally
        {
            isLoadingPlanningOptions = false;
        }
    }

    private void SyncPlanningSelectionFromCurrentItem()
    {
        selectedQuickActionLabelName = string.Empty;
        selectedMilestoneNumber = CurrentItem?.MilestoneNumber;
        duplicateReference = string.Empty;
        skipReason = string.Empty;
        selectedProjectBoardId = string.Empty;
        selectedProjectBoardStatusOptionId = string.Empty;
        currentDisposition = TriageItemDisposition.Process;
    }

    private async Task HandleActionSurfaceKeyDownAsync(KeyboardEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (isApplyingSessionAction || ShouldIgnoreActionSurfaceShortcut(args))
        {
            return;
        }

        switch (args.Key)
        {
            case "Enter":
            case "l":
            case "L":
                await CommitCurrentDispositionAsync();
                break;
            case "d":
            case "D":
                if (currentDisposition == TriageItemDisposition.Duplicate && CanCloseAsDuplicate)
                {
                    await CommitCurrentDispositionAsync();
                }
                else
                {
                    currentDisposition = TriageItemDisposition.Duplicate;
                }

                break;
            case "s":
            case "S":
                if (currentDisposition == TriageItemDisposition.Skip)
                {
                    await CommitCurrentDispositionAsync();
                }
                else
                {
                    await SkipCurrentItemAsync();
                }

                break;
            default:
                break;
        }
    }

    private static bool ShouldIgnoreActionSurfaceShortcut(KeyboardEventArgs args)
        => args.AltKey
            || args.CtrlKey
            || args.MetaKey
            || args.ShiftKey;

    private static bool TryParseRepositoryScope(string repositoryFullName, out string owner, out string repo)
        => RepositoryFullName.TryParse(repositoryFullName, out owner, out repo);

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

    private static bool TryCreateSummaryDetailLink(
        string detail,
        out string linkText,
        out string linkUrl,
        out string remainingDetail)
    {
        linkText = string.Empty;
        linkUrl = string.Empty;
        remainingDetail = string.Empty;

        if (string.IsNullOrWhiteSpace(detail))
        {
            return false;
        }

        Match match;

        try
        {
            match = summaryItemDetailRegex.Match(detail);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }

        if (!match.Success)
        {
            return false;
        }

        var itemType = match.Groups["itemType"].Value;
        var itemNumber = match.Groups["itemNumber"].Value;
        var repository = match.Groups["repository"].Value;
        var description = match.Groups["description"].Value;

        if (string.IsNullOrWhiteSpace(itemType)
            || string.IsNullOrWhiteSpace(itemNumber)
            || string.IsNullOrWhiteSpace(repository)
            || string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var itemPathSegment = string.Equals(itemType, "Pull request", StringComparison.Ordinal)
            ? "pull"
            : "issues";

        linkText = $"{itemType} #{itemNumber}";
        linkUrl = $"https://github.com/{repository}/{itemPathSegment}/{itemNumber}";
        remainingDetail = $" ({repository}): {description}";

        return true;
    }

}
