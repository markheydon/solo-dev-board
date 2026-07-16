using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.BoardRules;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.BoardRules.Pages;

/// <summary>Provides the entry workflow for selecting a repository and project board to visualise.</summary>
public partial class BoardRules : ComponentBase
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

    /// <summary>Gets or sets the hosted authentication recovery service.</summary>
    [Inject]
    public IHostedAuthenticationRecoveryService HostedAuthRecovery { get; set; } = default!;

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

    /// <inheritdoc/>
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

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        errorMessage = null;
        selectedRepository = null;
        availableProjectBoardOptions = [];
        selectedProjectBoardId = string.Empty;
        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;

        try
        {
            availableRepositories = (await RepositoryService.GetActiveRepositoriesAsync())
                .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (HostedAuthenticationRequiredException ex)
        {
            if (HostedAuthRecovery.TryInitiateRecovery(ex))
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
            selectedRepository = null;
            availableProjectBoardOptions = [];
            selectedProjectBoardId = string.Empty;
            selectedBoardRules = null;
            selectedTransition = null;
            selectedRule = null;
            errorMessage = null;
            hasProjectBoardLoadFailure = false;
            hasBoardRulesLoadFailure = false;
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
            return;
        }

        selectedBoardRules = null;
        selectedTransition = null;
        selectedRule = null;
        errorMessage = null;
        hasProjectBoardLoadFailure = false;
        hasBoardRulesLoadFailure = false;

        await LoadProjectBoardOptionsAsync(selectedRepository);
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
            return;
        }

        isLoadingProjectBoards = true;
        hasProjectBoardLoadFailure = false;
        errorMessage = null;
        availableProjectBoardOptions = [];
        selectedProjectBoardId = string.Empty;
        selectedRule = null;

        try
        {
            availableProjectBoardOptions = await BoardRulesService.GetProjectBoardOptionsAsync(owner, repo);
            selectedProjectBoardId = availableProjectBoardOptions.FirstOrDefault()?.Id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(selectedProjectBoardId))
            {
                await LoadBoardRulesDefinitionAsync(owner, repo, selectedProjectBoardId);
            }
        }
        catch (HostedAuthenticationRequiredException ex)
        {
            if (HostedAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            hasProjectBoardLoadFailure = true;
            errorMessage = $"GitHub API request failed while loading project boards. {ex.Message}";
        }
        catch (Exception ex)
        {
            hasProjectBoardLoadFailure = true;
            Logger.LogError(ex, "Failed to load project boards for repository {RepositoryFullName}.", repository.FullName);
            errorMessage = "An unexpected error occurred while loading project boards.";
        }
        finally
        {
            isLoadingProjectBoards = false;
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

        try
        {
            selectedBoardRules = await BoardRulesService.GetBoardRulesAsync(owner, repo, projectBoardId).ConfigureAwait(false);
            selectedTransition = BoardTransitions.FirstOrDefault();
        }
        catch (HostedAuthenticationRequiredException ex)
        {
            if (HostedAuthRecovery.TryInitiateRecovery(ex))
            {
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            hasBoardRulesLoadFailure = true;
            errorMessage = $"GitHub API request failed while loading board rules. {ex.Message}";
        }
        catch (Exception ex)
        {
            hasBoardRulesLoadFailure = true;
            Logger.LogError(ex, "Failed to load board rules for project board {ProjectBoardId} in repository {Owner}/{Repo}.", projectBoardId, owner, repo);
            errorMessage = "An unexpected error occurred while loading board rules.";
        }
        finally
        {
            isLoadingBoardRules = false;
        }
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
        => BoardRuleWarnings.Any(warning => warning.Contains($"'{rule.Name}'", StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<string> BoardRuleWarnings
    {
        get
        {
            if (selectedBoardRules?.Rules is not { Count: > 0 } rules)
            {
                return [];
            }

            var duplicateTriggerWarnings = rules
                .GroupBy(rule => string.IsNullOrWhiteSpace(rule.Trigger) ? string.Empty : rule.Trigger.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .Select(group => $"Rules with the same trigger '{group.Key}' may conflict: {string.Join(", ", group.Select(rule => rule.Name))}.")
                .ToArray();

            var incompleteConfigurationWarnings = rules
                .Where(rule => string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Action))
                .Select(rule => $"Rule '{rule.Name}' has incomplete configuration and may behave unexpectedly.")
                .ToArray();

            return duplicateTriggerWarnings.Concat(incompleteConfigurationWarnings).ToArray();
        }
    }

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
            return;
        }

        var owner = ResolveOwner(selectedRepository!.FullName);
        var repo = ResolveRepositoryName(selectedRepository.FullName);

        await LoadBoardRulesDefinitionAsync(owner, repo, selectedProjectBoardId);
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

    private bool ShowLoadingState => isLoadingRepositories || isLoadingProjectBoards || isLoadingBoardRules;

    private bool HasSelectedRepository => selectedRepository is not null;

    private bool HasSelectedProjectBoard
        => !string.IsNullOrWhiteSpace(selectedProjectBoardId)
            && availableProjectBoardOptions.Any(option => option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal));

    private BoardRulesProjectBoardOptionDto? ActiveProjectBoard
        => availableProjectBoardOptions.FirstOrDefault(option => option.Id.Equals(selectedProjectBoardId, StringComparison.Ordinal));

    private string RepositorySelectorSummary
    {
        get
        {
            var repositoryCount = availableRepositories.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {(selectedRepository is null ? 0 : 1)} selected. Archived repositories are hidden by default.";
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

    private string ErrorTitle
        => hasRepositoryLoadFailure
            ? "Unable to load repositories"
            : hasProjectBoardLoadFailure
                ? "Unable to load project boards"
                : hasBoardRulesLoadFailure
                    ? "Unable to load board rules"
                    : "Unable to load project boards";
}
