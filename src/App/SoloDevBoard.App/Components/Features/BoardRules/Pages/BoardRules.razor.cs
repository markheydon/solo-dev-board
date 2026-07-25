using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.GitHub;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.BoardRules.Pages;

/// <summary>Provides the entry workflow for selecting a repository and project board to visualise.</summary>
public partial class BoardRules : ComponentBase, IAsyncDisposable
{
    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the application service used to retrieve board rules metadata.</summary>
    [Inject]
    public IBoardRulesService BoardRulesService { get; set; } = default!;

    /// <summary>Gets or sets the logger for board rules page diagnostics.</summary>
    [Inject]
    public ILogger<BoardRules> Logger { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    private IReadOnlyList<RepositoryDto> availableRepositories = [];
    private RepositoryDto? selectedRepository;
    private IReadOnlyList<BoardRulesProjectBoardOptionDto> availableProjectBoardOptions = [];
    private string selectedProjectBoardId = string.Empty;
    private BoardRulesDefinitionDto? selectedBoardRules;
    private BoardColumnTransition? selectedTransition;
    private BoardRuleDto? selectedRule;
    private bool isLoadingRepositories = true;
    private bool isLoadingProjectBoards;
    private bool isLoadingBoardRules;
    private bool hasRepositoryLoadFailure;
    private bool hasProjectBoardLoadFailure;
    private bool hasBoardRulesLoadFailure;
    private string? errorMessage;
    private string? inaccessibleProjectBoardsWarning;
    private bool isCompareModeEnabled;
    private RepositoryDto? comparisonRepository;
    private IReadOnlyList<BoardRulesProjectBoardOptionDto> comparisonProjectBoardOptions = [];
    private string comparisonProjectBoardId = string.Empty;
    private BoardRulesDefinitionDto? comparisonBoardRules;
    private bool isLoadingComparisonProjectBoards;
    private bool isLoadingComparisonBoardRules;
    private bool hasComparisonProjectBoardLoadFailure;
    private string? comparisonInaccessibleProjectBoardsWarning;
    private BoardRulesComparisonResultDto? boardRulesComparison;
    private CancellationTokenSource? _primaryProjectBoardsLoadCts;
    private CancellationTokenSource? _primaryBoardRulesLoadCts;
    private CancellationTokenSource? _comparisonProjectBoardsLoadCts;
    private CancellationTokenSource? _comparisonBoardRulesLoadCts;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        CancelPrimaryLoads();
        CancelComparisonLoads();
        await ValueTask.CompletedTask;
    }
    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task ReloadRepositoriesAsync()
    {
        await LoadRepositoriesAsync();
    }

    private async Task ReloadProjectBoardsAsync()
    {
        if (selectedRepository is null)
        {
            return;
        }

        await LoadProjectBoardOptionsAsync(selectedRepository);
    }

    private async Task ReloadComparisonProjectBoardsAsync()
    {
        if (comparisonRepository is null)
        {
            return;
        }

        await LoadComparisonProjectBoardOptionsAsync(comparisonRepository);
    }

    private async Task LoadRepositoriesAsync()
    {
        CancelPrimaryLoads();
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        errorMessage = null;
        selectedRepository = null;
        availableProjectBoardOptions = [];
        selectedProjectBoardId = string.Empty;
        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;
        ClearComparisonState();

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
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
            hasRepositoryLoadFailure = true;
            errorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            hasRepositoryLoadFailure = true;
            Logger.LogError(ex, "Failed to load repositories for the Board Rules Visualiser.");
            errorMessage = "An unexpected error occurred while loading repositories.";
        }
        finally
        {
            isLoadingRepositories = false;
        }
    }

    private async Task OnSelectedRepositoriesChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var selectedFullName = repositoryFullNames
            .FirstOrDefault(fullName => !string.IsNullOrWhiteSpace(fullName));

        if (string.IsNullOrWhiteSpace(selectedFullName))
        {
            CancelPrimaryLoads();
            selectedRepository = null;
            availableProjectBoardOptions = [];
            selectedProjectBoardId = string.Empty;
            selectedBoardRules = null;
            selectedTransition = null;
            selectedRule = null;
            errorMessage = null;
            hasProjectBoardLoadFailure = false;
            hasBoardRulesLoadFailure = false;
            inaccessibleProjectBoardsWarning = null;
            UpdateBoardRulesComparison();
            return;
        }

        selectedRepository = availableRepositories
            .FirstOrDefault(repository => repository.FullName.Equals(selectedFullName, StringComparison.OrdinalIgnoreCase));

        if (selectedRepository is null)
        {
            availableProjectBoardOptions = [];
            selectedProjectBoardId = string.Empty;
            selectedBoardRules = null;
            selectedTransition = null;
            selectedRule = null;
            errorMessage = null;
            hasProjectBoardLoadFailure = false;
            hasBoardRulesLoadFailure = false;
            inaccessibleProjectBoardsWarning = null;
            UpdateBoardRulesComparison();
            return;
        }

        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;
        errorMessage = null;
        hasProjectBoardLoadFailure = false;
        hasBoardRulesLoadFailure = false;
        inaccessibleProjectBoardsWarning = null;

        await LoadProjectBoardOptionsAsync(selectedRepository);
    }

    private async Task OnComparisonRepositoriesChangedAsync(IReadOnlyList<string> repositoryFullNames)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);

        var selectedFullName = repositoryFullNames
            .FirstOrDefault(fullName => !string.IsNullOrWhiteSpace(fullName));

        if (string.IsNullOrWhiteSpace(selectedFullName))
        {
            CancelComparisonLoads();
            comparisonRepository = null;
            comparisonProjectBoardOptions = [];
            comparisonProjectBoardId = string.Empty;
            comparisonBoardRules = null;
            hasComparisonProjectBoardLoadFailure = false;
            comparisonInaccessibleProjectBoardsWarning = null;
            UpdateBoardRulesComparison();
            return;
        }

        comparisonRepository = availableRepositories
            .FirstOrDefault(repository => repository.FullName.Equals(selectedFullName, StringComparison.OrdinalIgnoreCase));

        if (comparisonRepository is null)
        {
            CancelComparisonLoads();
            comparisonProjectBoardOptions = [];
            comparisonProjectBoardId = string.Empty;
            comparisonBoardRules = null;
            hasComparisonProjectBoardLoadFailure = false;
            comparisonInaccessibleProjectBoardsWarning = null;
            UpdateBoardRulesComparison();
            return;
        }

        comparisonBoardRules = null;
        hasComparisonProjectBoardLoadFailure = false;
        comparisonInaccessibleProjectBoardsWarning = null;

        await LoadComparisonProjectBoardOptionsAsync(comparisonRepository);
    }

    private async Task LoadProjectBoardOptionsAsync(RepositoryDto repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var owner = ResolveOwner(repository.FullName);
        var repo = ResolveRepositoryName(repository.FullName);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            availableProjectBoardOptions = [];
            selectedProjectBoardId = string.Empty;
            hasProjectBoardLoadFailure = true;
            errorMessage = "The selected repository name is not in owner/name form.";
            UpdateBoardRulesComparison();
            return;
        }

        isLoadingProjectBoards = true;
        hasProjectBoardLoadFailure = false;
        errorMessage = null;
        inaccessibleProjectBoardsWarning = null;
        availableProjectBoardOptions = [];
        selectedProjectBoardId = string.Empty;
        selectedRule = null;

        var loadCts = BeginPrimaryProjectBoardsLoad();
        var cancellationToken = loadCts.Token;

        try
        {
            var discovery = await BoardRulesService
                .GetProjectBoardOptionsAsync(owner, repo, cancellationToken)
                .ConfigureAwait(false);

            if (IsStalePrimaryProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            availableProjectBoardOptions = discovery.Options;
            inaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);
            selectedProjectBoardId = availableProjectBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(selectedProjectBoardId))
            {
                await LoadBoardRulesDefinitionAsync(owner, repo, selectedProjectBoardId);
            }
            else
            {
                selectedBoardRules = null;
                UpdateBoardRulesComparison();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
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
            if (IsStalePrimaryProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            hasProjectBoardLoadFailure = true;
            errorMessage = $"GitHub API request failed while loading project boards. {ex.Message}";
            UpdateBoardRulesComparison();
        }
        catch (Exception ex)
        {
            if (IsStalePrimaryProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            hasProjectBoardLoadFailure = true;
            Logger.LogError(ex, "Failed to load project boards for repository {RepositoryFullName}.", repository.FullName);
            errorMessage = "An unexpected error occurred while loading project boards.";
            UpdateBoardRulesComparison();
        }
        finally
        {
            if (ReferenceEquals(_primaryProjectBoardsLoadCts, loadCts))
            {
                isLoadingProjectBoards = false;
            }
        }
    }

    private async Task LoadComparisonProjectBoardOptionsAsync(RepositoryDto repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var owner = ResolveOwner(repository.FullName);
        var repo = ResolveRepositoryName(repository.FullName);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            comparisonProjectBoardOptions = [];
            comparisonProjectBoardId = string.Empty;
            hasComparisonProjectBoardLoadFailure = true;
            UpdateBoardRulesComparison();
            return;
        }

        isLoadingComparisonProjectBoards = true;
        hasComparisonProjectBoardLoadFailure = false;
        comparisonInaccessibleProjectBoardsWarning = null;
        comparisonProjectBoardOptions = [];
        comparisonProjectBoardId = string.Empty;

        var loadCts = BeginComparisonProjectBoardsLoad();
        var cancellationToken = loadCts.Token;

        try
        {
            var discovery = await BoardRulesService
                .GetProjectBoardOptionsAsync(owner, repo, cancellationToken)
                .ConfigureAwait(false);

            if (IsStaleComparisonProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            comparisonProjectBoardOptions = discovery.Options;
            comparisonInaccessibleProjectBoardsWarning = LinkedProjectBoardVisibility.BuildInaccessibleProjectsWarning(
                discovery.TotalLinkedProjectCount,
                discovery.InaccessibleLinkedProjectCount);
            comparisonProjectBoardId = comparisonProjectBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(comparisonProjectBoardId))
            {
                await LoadComparisonBoardRulesDefinitionAsync(owner, repo, comparisonProjectBoardId);
            }
            else
            {
                comparisonBoardRules = null;
                UpdateBoardRulesComparison();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException)
        {
            if (IsStaleComparisonProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            hasComparisonProjectBoardLoadFailure = true;
            UpdateBoardRulesComparison();
        }
        catch (Exception ex)
        {
            if (IsStaleComparisonProjectBoardsLoad(loadCts, repository))
            {
                return;
            }

            hasComparisonProjectBoardLoadFailure = true;
            Logger.LogError(ex, "Failed to load comparison project boards for repository {RepositoryFullName}.", repository.FullName);
            UpdateBoardRulesComparison();
        }
        finally
        {
            if (ReferenceEquals(_comparisonProjectBoardsLoadCts, loadCts))
            {
                isLoadingComparisonProjectBoards = false;
            }
        }
    }

    private async Task LoadBoardRulesDefinitionAsync(string owner, string repo, string projectBoardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectBoardId);

        isLoadingBoardRules = true;
        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;
        hasBoardRulesLoadFailure = false;
        errorMessage = null;

        var loadCts = BeginPrimaryBoardRulesLoad();
        var cancellationToken = loadCts.Token;

        try
        {
            var boardRules = await BoardRulesService
                .GetBoardRulesAsync(owner, repo, projectBoardId, cancellationToken)
                .ConfigureAwait(false);

            if (IsStalePrimaryBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            selectedBoardRules = boardRules;
            selectedTransition = BoardTransitions.FirstOrDefault();
            UpdateBoardRulesComparison();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
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
            if (IsStalePrimaryBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            hasBoardRulesLoadFailure = true;
            errorMessage = $"GitHub API request failed while loading board rules. {ex.Message}";
            UpdateBoardRulesComparison();
        }
        catch (Exception ex)
        {
            if (IsStalePrimaryBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            hasBoardRulesLoadFailure = true;
            Logger.LogError(ex, "Failed to load board rules for project board {ProjectBoardId} in repository {Owner}/{Repo}.", projectBoardId, owner, repo);
            errorMessage = "An unexpected error occurred while loading board rules.";
            UpdateBoardRulesComparison();
        }
        finally
        {
            if (ReferenceEquals(_primaryBoardRulesLoadCts, loadCts))
            {
                isLoadingBoardRules = false;
            }
        }
    }

    private async Task LoadComparisonBoardRulesDefinitionAsync(string owner, string repo, string projectBoardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectBoardId);

        isLoadingComparisonBoardRules = true;
        comparisonBoardRules = null;
        hasComparisonProjectBoardLoadFailure = false;

        var loadCts = BeginComparisonBoardRulesLoad();
        var cancellationToken = loadCts.Token;

        try
        {
            var boardRules = await BoardRulesService
                .GetBoardRulesAsync(owner, repo, projectBoardId, cancellationToken)
                .ConfigureAwait(false);

            if (IsStaleComparisonBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            comparisonBoardRules = boardRules;
            UpdateBoardRulesComparison();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex) when (ex is HostedAuthenticationRequiredException or GitHubPatConnectivityRequiredException)
        {
            if (GitHubAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException)
        {
            if (IsStaleComparisonBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            hasComparisonProjectBoardLoadFailure = true;
            UpdateBoardRulesComparison();
        }
        catch (Exception ex)
        {
            if (IsStaleComparisonBoardRulesLoad(loadCts, projectBoardId))
            {
                return;
            }

            hasComparisonProjectBoardLoadFailure = true;
            Logger.LogError(ex, "Failed to load comparison board rules for project board {ProjectBoardId} in repository {Owner}/{Repo}.", projectBoardId, owner, repo);
            UpdateBoardRulesComparison();
        }
        finally
        {
            if (ReferenceEquals(_comparisonBoardRulesLoadCts, loadCts))
            {
                isLoadingComparisonBoardRules = false;
            }
        }
    }

    private void UpdateBoardRulesComparison()
    {
        if (!isCompareModeEnabled || selectedBoardRules is null || comparisonBoardRules is null)
        {
            boardRulesComparison = null;
            return;
        }

        boardRulesComparison = BoardRulesService.CompareBoardRules(selectedBoardRules, comparisonBoardRules);
    }

    private async Task OnCompareModeToggledAsync(bool enabled)
    {
        isCompareModeEnabled = enabled;

        if (!enabled)
        {
            ClearComparisonState();
            return;
        }

        UpdateBoardRulesComparison();
        await Task.CompletedTask;
    }

    private void ClearComparisonState()
    {
        CancelComparisonLoads();
        comparisonRepository = null;
        comparisonProjectBoardOptions = [];
        comparisonProjectBoardId = string.Empty;
        comparisonBoardRules = null;
        isLoadingComparisonProjectBoards = false;
        isLoadingComparisonBoardRules = false;
        hasComparisonProjectBoardLoadFailure = false;
        comparisonInaccessibleProjectBoardsWarning = null;
        boardRulesComparison = null;
    }

    private IReadOnlyList<BoardColumnTransition> BoardTransitions
        => selectedBoardRules is null
            ? []
            : selectedBoardRules.Columns
                .Select((column, index) => (column, index))
                .Where(pair => pair.index < selectedBoardRules.Columns.Count - 1)
                .Select(pair => new BoardColumnTransition(
                    pair.index,
                    pair.column.Name,
                    selectedBoardRules.Columns[pair.index + 1].Name))
                .ToArray();

    private void SelectTransition(BoardColumnTransition transition)
    {
        selectedTransition = transition;
        selectedRule = null;
    }

    private void SelectRule(BoardRuleDto rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        selectedRule = rule;
    }

    private Variant GetTransitionButtonVariant(BoardColumnTransition transition)
        => selectedTransition?.Id == transition.Id ? Variant.Filled : Variant.Outlined;

    private Variant GetRuleChipVariant(BoardRuleDto rule)
        => selectedRule?.Id == rule.Id ? Variant.Filled : Variant.Outlined;

    private Color GetRuleChipColor(BoardRuleDto rule)
        => selectedRule?.Id == rule.Id ? Color.Primary
            : IsRuleWarning(rule) ? Color.Warning
            : rule.IsEnabled ? Color.Secondary
            : Color.Default;

    private bool IsRuleWarning(BoardRuleDto rule)
        => BoardRulesWarningAnalyser.IsRuleWarning(rule, BoardRuleWarnings);

    private IReadOnlyList<string> BoardRuleWarnings
        => BoardRulesWarningAnalyser.AnalyseWarnings(selectedBoardRules);

    private bool HasRuleWarnings => BoardRuleWarnings.Count > 0;

    private string DetailPanelTitle
        => selectedRule is not null ? "Selected rule" : "Transition detail";

    private string GetTransitionButtonAriaLabel(BoardColumnTransition transition)
        => $"Inspect transition from {transition.FromColumnName} to {transition.ToColumnName}";

    private sealed record BoardColumnTransition(int Id, string FromColumnName, string ToColumnName);

    private async Task OnSelectedProjectBoardChangedAsync(string projectBoardId)
    {
        selectedProjectBoardId = projectBoardId ?? string.Empty;
        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;
        hasBoardRulesLoadFailure = false;
        errorMessage = null;

        if (!HasSelectedRepository || string.IsNullOrWhiteSpace(selectedProjectBoardId))
        {
            UpdateBoardRulesComparison();
            return;
        }

        var owner = ResolveOwner(selectedRepository!.FullName);
        var repo = ResolveRepositoryName(selectedRepository.FullName);

        await LoadBoardRulesDefinitionAsync(owner, repo, selectedProjectBoardId);
    }

    private async Task OnComparisonProjectBoardChangedAsync(string projectBoardId)
    {
        comparisonProjectBoardId = projectBoardId ?? string.Empty;
        comparisonBoardRules = null;
        hasComparisonProjectBoardLoadFailure = false;

        if (!HasComparisonRepository || string.IsNullOrWhiteSpace(comparisonProjectBoardId))
        {
            UpdateBoardRulesComparison();
            return;
        }

        var owner = ResolveOwner(comparisonRepository!.FullName);
        var repo = ResolveRepositoryName(comparisonRepository.FullName);

        await LoadComparisonBoardRulesDefinitionAsync(owner, repo, comparisonProjectBoardId);
    }

    private static string ResolveOwner(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private static string ResolveRepositoryName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private bool HasInaccessibleProjectBoardsWarning
        => !string.IsNullOrWhiteSpace(inaccessibleProjectBoardsWarning);

    private bool HasComparisonInaccessibleProjectBoardsWarning
        => !string.IsNullOrWhiteSpace(comparisonInaccessibleProjectBoardsWarning);

    private bool ShowLoadingState
        => isLoadingRepositories
            || isLoadingProjectBoards
            || isLoadingBoardRules
            || (isCompareModeEnabled && (isLoadingComparisonProjectBoards || isLoadingComparisonBoardRules));

    private bool HasSelectedRepository => selectedRepository is not null;

    private bool HasComparisonRepository => comparisonRepository is not null;

    private bool HasSelectedProjectBoard
        => !string.IsNullOrWhiteSpace(selectedProjectBoardId)
            && availableProjectBoardOptions.Any(option => option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal));

    private bool HasComparisonProjectBoard
        => !string.IsNullOrWhiteSpace(comparisonProjectBoardId)
            && comparisonProjectBoardOptions.Any(option => option.Id.Equals(comparisonProjectBoardId, StringComparison.Ordinal));

    private bool ShowComparisonResults
        => isCompareModeEnabled
            && selectedBoardRules is not null
            && comparisonBoardRules is not null
            && boardRulesComparison is not null;

    private string RepositorySelectorSummary
    {
        get
        {
            var repositoryCount = availableRepositories.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {(selectedRepository is null ? 0 : 1)} selected. Archived repositories are hidden by default.";
        }
    }

    private string ComparisonRepositorySelectorSummary
    {
        get
        {
            var repositoryCount = availableRepositories.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {(comparisonRepository is null ? 0 : 1)} selected for comparison.";
        }
    }

    private IReadOnlyList<string> availableRepositoryFullNames
        => availableRepositories
            .Select(repository => repository.FullName)
            .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> selectedRepositoryFullNames
        => selectedRepository is null
            ? []
            : [selectedRepository.FullName];

    private IReadOnlyList<string> comparisonRepositoryFullNames
        => comparisonRepository is null
            ? []
            : [comparisonRepository.FullName];

    private string ErrorTitle
        => hasRepositoryLoadFailure
            ? "Unable to load repositories"
            : hasProjectBoardLoadFailure
                ? "Unable to load project boards"
                : hasBoardRulesLoadFailure
                    ? "Unable to load board rules"
                    : "Unable to load project boards";

    private CancellationTokenSource BeginPrimaryProjectBoardsLoad()
    {
        CancelPrimaryLoads();
        _primaryProjectBoardsLoadCts = new CancellationTokenSource();
        return _primaryProjectBoardsLoadCts;
    }

    private CancellationTokenSource BeginPrimaryBoardRulesLoad()
    {
        _primaryBoardRulesLoadCts?.Cancel();
        _primaryBoardRulesLoadCts?.Dispose();
        _primaryBoardRulesLoadCts = new CancellationTokenSource();
        return _primaryBoardRulesLoadCts;
    }

    private CancellationTokenSource BeginComparisonProjectBoardsLoad()
    {
        CancelComparisonLoads();
        _comparisonProjectBoardsLoadCts = new CancellationTokenSource();
        return _comparisonProjectBoardsLoadCts;
    }

    private CancellationTokenSource BeginComparisonBoardRulesLoad()
    {
        _comparisonBoardRulesLoadCts?.Cancel();
        _comparisonBoardRulesLoadCts?.Dispose();
        _comparisonBoardRulesLoadCts = new CancellationTokenSource();
        return _comparisonBoardRulesLoadCts;
    }

    private void CancelPrimaryLoads()
    {
        _primaryProjectBoardsLoadCts?.Cancel();
        _primaryProjectBoardsLoadCts?.Dispose();
        _primaryProjectBoardsLoadCts = null;
        _primaryBoardRulesLoadCts?.Cancel();
        _primaryBoardRulesLoadCts?.Dispose();
        _primaryBoardRulesLoadCts = null;
    }

    private void CancelComparisonLoads()
    {
        _comparisonProjectBoardsLoadCts?.Cancel();
        _comparisonProjectBoardsLoadCts?.Dispose();
        _comparisonProjectBoardsLoadCts = null;
        _comparisonBoardRulesLoadCts?.Cancel();
        _comparisonBoardRulesLoadCts?.Dispose();
        _comparisonBoardRulesLoadCts = null;
    }

    private bool IsStalePrimaryProjectBoardsLoad(CancellationTokenSource loadCts, RepositoryDto repository)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_primaryProjectBoardsLoadCts, loadCts)
            || !string.Equals(selectedRepository?.FullName, repository.FullName, StringComparison.OrdinalIgnoreCase);

    private bool IsStalePrimaryBoardRulesLoad(CancellationTokenSource loadCts, string projectBoardId)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_primaryBoardRulesLoadCts, loadCts)
            || !string.Equals(selectedProjectBoardId, projectBoardId, StringComparison.Ordinal);

    private bool IsStaleComparisonProjectBoardsLoad(CancellationTokenSource loadCts, RepositoryDto repository)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_comparisonProjectBoardsLoadCts, loadCts)
            || !string.Equals(comparisonRepository?.FullName, repository.FullName, StringComparison.OrdinalIgnoreCase);

    private bool IsStaleComparisonBoardRulesLoad(CancellationTokenSource loadCts, string projectBoardId)
        => loadCts.IsCancellationRequested
            || !ReferenceEquals(_comparisonBoardRulesLoadCts, loadCts)
            || !string.Equals(comparisonProjectBoardId, projectBoardId, StringComparison.Ordinal);
}
