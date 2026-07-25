using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Triage;

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
    private string? operationMessage;
    // Persistent page feedback uses MudAlert via operationMessage; snackbars are not duplicated here
    // because animated overlays can fail axe colour-contrast scans and duplicate inline alerts.
    private string? inaccessibleProjectBoardsWarning;
    private Severity operationSeverity = Severity.Info;
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

    private bool CanApplySelectedLabel
        => !isApplyingSessionAction
            && CurrentItem is not null
            && LabelExists(selectedQuickActionLabelName);

    private bool CanCloseAsDuplicate
        => !isApplyingSessionAction
            && CurrentItem is not null
            && !string.IsNullOrWhiteSpace(duplicateReference);

    private bool CanSkipCurrentItem
        => !isApplyingSessionAction
            && CurrentItem is not null;

    private bool CanAssignMilestone
        => !isApplyingSessionAction
            && CurrentItem is not null
            && !isLoadingPlanningOptions
            && (selectedMilestoneNumber.HasValue || CurrentItem.MilestoneNumber.HasValue);

    private bool CanAddToProjectBoard
        => !isApplyingSessionAction
            && CurrentItem is not null
            && !isLoadingPlanningOptions
            && !string.IsNullOrWhiteSpace(selectedProjectBoardId)
            && !string.IsNullOrWhiteSpace(selectedProjectBoardStatusOptionId)
            && ActiveProjectBoard is not null;

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
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while loading repositories. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load triage repositories.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while loading repositories.";
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
            availableLabelNames = [];
            availableMilestoneOptions = [];
            availableProjectBoardOptions = [];
            selectedQuickActionLabelName = string.Empty;
            duplicateReference = string.Empty;
            skipReason = string.Empty;
            selectedMilestoneNumber = null;
            selectedProjectBoardId = string.Empty;
            selectedProjectBoardStatusOptionId = string.Empty;
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
            return;
        }

        isStartingSession = true;
        operationMessage = null;

        try
        {
            currentSession = await TriageService.StartSessionAsync(owner, repo, includePullRequests);
            await LoadQuickActionLabelsAsync(owner, repo);
            await LoadPlanningOptionsAsync(currentSession);
            SyncPlanningSelectionFromCurrentItem();

            operationSeverity = Severity.Success;
            operationMessage = currentSession.Progress.TotalItems == 0
                ? $"No untriaged items were found in {selectedRepositoryFullName}."
                : $"Started triage session for {selectedRepositoryFullName}.";
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
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while starting triage session. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start triage session for {RepositoryScope}.", selectedRepositoryFullName);
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while starting triage session.";
        }
        finally
        {
            isStartingSession = false;
        }
    }

    private async Task LoadQuickActionLabelsAsync(string owner, string repo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);

        try
        {
            var labelNames = await LabelManagerService.GetLabelsAsync(owner, repo);

            availableLabelNames = labelNames
                .Select(label => label.Name)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedQuickActionLabelName = string.Empty;

            if (availableLabelNames.Count == 0)
            {
                operationSeverity = Severity.Warning;
                operationMessage = "No repository labels are available to apply as quick actions.";
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
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while loading labels. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load quick-action labels for {RepositoryScope}.", $"{owner}/{repo}");
            availableLabelNames = [];
            selectedQuickActionLabelName = string.Empty;
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while loading labels for quick actions.";
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

    private async Task ApplySelectedLabelAsync()
    {
        if (currentSession is null || CurrentItem is null || !CanApplySelectedLabel)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var appliedItemNumber = CurrentItem.Number;
            var labelName = selectedQuickActionLabelName.Trim();

            var labelledSession = await TriageService.ApplyLabelToCurrentItemAsync(currentSession, labelName);
            currentSession = await TriageService.AdvanceSessionAsync(labelledSession);
            SyncPlanningSelectionFromCurrentItem();

            operationSeverity = Severity.Success;
            operationMessage = currentSession.CurrentItem is null
                ? $"Applied label '{labelName}' to item #{appliedItemNumber}. Reached the end of the current queue."
                : $"Applied label '{labelName}' to item #{appliedItemNumber} and moved to {CurrentPositionText}.";
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
            Logger.LogError(ex, "GitHub API request failed while applying label to triage item.");
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while applying the label. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply label to the current triage item.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while applying the selected label.";
        }
        finally
        {
            isApplyingSessionAction = false;
        }
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
            operationSeverity = Severity.Info;
            operationMessage = currentSession.CurrentItem is null
                ? $"Moved past item #{completedItemNumber} without changes. Reached the end of the current queue."
                : $"Moved past item #{completedItemNumber} without changes and moved to {CurrentPositionText}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to move to the next triage item without changes.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while moving to the next item without changes.";
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

            operationSeverity = Severity.Success;
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

            operationMessage = string.IsNullOrWhiteSpace(duplicateLabelSuffix)
                ? baseMessage
                : $"{baseMessage} {duplicateLabelSuffix}";
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
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while closing as duplicate. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to close the current triage item as duplicate.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while closing this item as a duplicate.";
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

            operationSeverity = Severity.Info;
            operationMessage = currentSession.CurrentItem is null
                ? $"Skipped item #{skippedItemNumber} for later review. Reached the end of the current queue."
                : $"Skipped item #{skippedItemNumber} for later review and moved to {CurrentPositionText}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to skip the current triage item.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while skipping this item.";
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

    private async Task AssignSelectedMilestoneAsync()
    {
        if (currentSession is null || CurrentItem is null || !CanAssignMilestone)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var selectedMilestoneTitle = availableMilestoneOptions
                .FirstOrDefault(option => option.Number == selectedMilestoneNumber)
                ?.Title;

            currentSession = await TriageService.AssignMilestoneToCurrentItemAsync(
                currentSession,
                selectedMilestoneNumber,
                selectedMilestoneTitle);

            SyncPlanningSelectionFromCurrentItem();

            operationSeverity = Severity.Success;
            operationMessage = selectedMilestoneNumber is null
                ? $"Cleared milestone assignment for item #{CurrentItem.Number}."
                : $"Assigned milestone '{selectedMilestoneTitle}' to item #{CurrentItem.Number}.";
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
            Logger.LogError(ex, "GitHub API request failed while assigning milestone to triage item.");
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while assigning milestone. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to assign milestone to the current triage item.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while assigning the selected milestone.";
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task AddCurrentItemToProjectBoardAsync()
    {
        if (currentSession is null || CurrentItem is null || !CanAddToProjectBoard || ActiveProjectBoard is null)
        {
            return;
        }

        var selectedStatusOption = ActiveProjectBoardStatusOptions
            .FirstOrDefault(option => option.Id.Equals(selectedProjectBoardStatusOptionId, StringComparison.Ordinal));

        if (selectedStatusOption is null)
        {
            return;
        }

        isApplyingSessionAction = true;

        try
        {
            var currentItemNumber = CurrentItem.Number;
            currentSession = await TriageService.AddCurrentItemToProjectBoardAsync(
                currentSession,
                ActiveProjectBoard.Id,
                ActiveProjectBoard.Title,
                ActiveProjectBoard.StatusFieldId,
                selectedStatusOption.Id,
                selectedStatusOption.Name);

            operationSeverity = Severity.Success;
            operationMessage = $"Added item #{currentItemNumber} to '{ActiveProjectBoard.Title}' with status '{selectedStatusOption.Name}'.";
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
            Logger.LogError(ex, "GitHub API request failed while adding triage item to project board.");
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while adding to project board. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add the current triage item to a project board.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while adding this item to a project board.";
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
            SyncPlanningSelectionFromCurrentItem();
            operationSeverity = Severity.Info;
            operationMessage = "Skipped items were appended to the queue for review.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to revisit skipped triage items.");
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while revisiting skipped items.";
        }
        finally
        {
            isApplyingSessionAction = false;
        }
    }

    private async Task LoadPlanningOptionsAsync(TriageSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        isLoadingPlanningOptions = true;
        inaccessibleProjectBoardsWarning = null;

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
            operationSeverity = Severity.Error;
            operationMessage = $"GitHub API request failed while loading milestone options. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load milestone options for triage.");
            availableMilestoneOptions = [];
            milestoneLoadFailed = true;
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while loading milestone options.";
        }

        try
        {
            var discovery = await TriageService.GetProjectBoardOptionsAsync(session);
            availableProjectBoardOptions = discovery.Options;
            inaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);

            if (!availableProjectBoardOptions.Any(option => option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal)))
            {
                selectedProjectBoardId = availableProjectBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
            }

            if (ActiveProjectBoardStatusOptions.Count == 0)
            {
                selectedProjectBoardStatusOptionId = string.Empty;
                return;
            }

            if (!ActiveProjectBoardStatusOptions.Any(option => option.Id.Equals(selectedProjectBoardStatusOptionId, StringComparison.Ordinal)))
            {
                selectedProjectBoardStatusOptionId = ActiveProjectBoardStatusOptions[0].Id;
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
                operationSeverity = Severity.Warning;
                operationMessage = $"Milestones loaded, but project-board options could not be loaded. {ex.Message}";
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
                operationSeverity = Severity.Warning;
                operationMessage = "Milestones loaded, but an unexpected error occurred while loading project-board options.";
            }
        }
        finally
        {
            isLoadingPlanningOptions = false;
        }
    }

    private void SyncPlanningSelectionFromCurrentItem()
    {
        selectedMilestoneNumber = CurrentItem?.MilestoneNumber;
        duplicateReference = string.Empty;
        skipReason = string.Empty;

        if (!availableProjectBoardOptions.Any(option => option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal)))
        {
            selectedProjectBoardId = availableProjectBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
        }

        if (!ActiveProjectBoardStatusOptions.Any(option => option.Id.Equals(selectedProjectBoardStatusOptionId, StringComparison.Ordinal)))
        {
            selectedProjectBoardStatusOptionId = ActiveProjectBoardStatusOptions.FirstOrDefault()?.Id ?? string.Empty;
        }
    }

    private async Task HandleActionSurfaceKeyDownAsync(KeyboardEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (isApplyingSessionAction)
        {
            return;
        }

        switch (args.Key)
        {
            case "l":
            case "L":
                await ApplySelectedLabelAsync();
                break;
            case "n":
            case "N":
                await CompleteWithoutChangesAsync();
                break;
            case "d":
            case "D":
                await CloseCurrentItemAsDuplicateAsync();
                break;
            case "s":
            case "S":
                await SkipCurrentItemAsync();
                break;
            default:
                break;
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
