using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.App.Feedback;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.ActionsTemplates;
using SoloDevBoard.Application.Services.Repositories;

namespace SoloDevBoard.App.Components.Features.ActionsTemplates.Pages;

/// <summary>Provides the workflow template browser for built-in and custom GitHub Actions templates.</summary>
public partial class ActionsTemplates : ComponentBase
{
    private const string AllCategoriesLabel = "All";
    private const string LastUsedSourceCaptionText = "Last-used source restored from browser storage.";

    /// <summary>Gets or sets the application service used to retrieve workflow templates.</summary>
    [Inject]
    public IActionsTemplateService ActionsTemplateService { get; set; } = default!;

    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the browser storage for the last-used custom template source.</summary>
    [Inject]
    public IActionsTemplateSourceStorage ActionsTemplateSourceStorage { get; set; } = default!;

    /// <summary>Gets or sets the logger for workflow page diagnostics.</summary>
    [Inject]
    public ILogger<ActionsTemplates> Logger { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication recovery service.</summary>
    [Inject]
    public IGitHubAuthenticationRecoveryService GitHubAuthRecovery { get; set; } = default!;

    /// <summary>Gets or sets the snackbar service for transient operation feedback.</summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private IReadOnlyList<ActionsTemplateDto> templates = [];
    private ActionsTemplateDetailDto? selectedTemplateDetail;
    private IReadOnlyList<ActionsTemplateRepositoryStatusDto> repositoryStatuses = [];
    private IReadOnlyList<ActionsTemplateRepositoryResultDto> applyResults = [];
    private IReadOnlyList<string> repositoryOptions = [];
    private HashSet<string> selectedRepositories = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> selectedCustomSourceRepositories = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> parameterValues = new(StringComparer.OrdinalIgnoreCase);
    private string searchText = string.Empty;
    private string selectedCategory = AllCategoriesLabel;
    private string? selectedTemplateId;
    private bool isLoadingTemplates = true;
    private bool isLoadingRepositories = true;
    private bool isLoadingStatuses;
    private bool isApplyingTemplate;
    private bool hasLoadFailure;
    private bool hasRepositoryLoadFailure;
    private string? repositoryLoadErrorMessage;
    private string? customSourceError;
    private string? customSourceWarning;
    private string customSourceRepository = string.Empty;
    private string? loadedCustomSourceRepository;
    private bool isLoadingCustomSource;
    private bool restoredLastUsedSource;

    private void ShowSnackbarFeedback(string message, Severity severity)
        => SnackbarFeedback.Show(Snackbar, message, severity);

    private bool ShowLoadingState => isLoadingTemplates;

    private bool IsLoadCustomSourceDisabled
        => ShowLoadingState
            || isLoadingCustomSource
            || string.IsNullOrWhiteSpace(customSourceRepository);

    private bool ShowLastUsedSourceCaption
        => restoredLastUsedSource && !string.IsNullOrWhiteSpace(customSourceRepository);

    private string CustomSourceRepository
    {
        get => customSourceRepository;
        set
        {
            var normalisedValue = value ?? string.Empty;
            if (customSourceRepository == normalisedValue)
            {
                return;
            }

            customSourceRepository = normalisedValue;
            restoredLastUsedSource = false;
            SyncCustomSourceSelectorFromManualField();
            StateHasChanged();
        }
    }

    private string SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
            {
                return;
            }

            searchText = value;
            StateHasChanged();
        }
    }

    private IReadOnlyList<string> AvailableCategories
        => [AllCategoriesLabel, .. templates.Select(template => template.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category)];

    private IReadOnlyList<ActionsTemplateDto> FilteredTemplates
        => templates
            .Where(MatchesSelectedCategory)
            .Where(MatchesSearchText)
            .OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> SelectedRepositoryFullNames
        => selectedRepositories
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> SelectedCustomSourceRepositoryFullNames
        => selectedCustomSourceRepositories
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private string CustomSourceRepositorySelectorSummary
    {
        get
        {
            var repositoryCount = repositoryOptions.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun} from your catalogue.";
        }
    }

    private string RepositorySelectorSummary
    {
        get
        {
            var repositoryCount = repositoryOptions.Count;
            var repositoryNoun = repositoryCount == 1 ? "repository" : "repositories";

            return $"Showing {repositoryCount} active {repositoryNoun}. {selectedRepositories.Count} selected. Archived repositories are hidden by default.";
        }
    }

    private bool CanApplyTemplate => selectedTemplateDetail is not null
        && selectedRepositories.Count > 0
        && AreRequiredParametersValid()
        && !isApplyingTemplate
        && !isLoadingStatuses;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await RestoreLastUsedCustomSourceAsync();
        await Task.WhenAll(LoadTemplatesAsync(loadedCustomSourceRepository), LoadRepositoriesAsync());
    }

    private async Task RestoreLastUsedCustomSourceAsync()
    {
        var lastUsedSource = await ActionsTemplateSourceStorage.GetLastUsedSourceAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(lastUsedSource))
        {
            return;
        }

        customSourceRepository = lastUsedSource.Trim();
        loadedCustomSourceRepository = customSourceRepository;
        restoredLastUsedSource = true;
    }

    private void ApplyRestoredCustomSourceToPickers()
    {
        if (string.IsNullOrWhiteSpace(customSourceRepository))
        {
            return;
        }

        SyncCustomSourceSelectorFromManualField();
    }

    private void SyncCustomSourceSelectorFromManualField()
    {
        var trimmedSource = customSourceRepository.Trim();
        if (string.IsNullOrWhiteSpace(trimmedSource))
        {
            selectedCustomSourceRepositories.Clear();
            return;
        }

        if (repositoryOptions.Contains(trimmedSource, StringComparer.OrdinalIgnoreCase))
        {
            selectedCustomSourceRepositories = new HashSet<string>([trimmedSource], StringComparer.OrdinalIgnoreCase);
            return;
        }

        selectedCustomSourceRepositories.Clear();
    }

    private async Task LoadCustomSourceAsync()
    {
        if (string.IsNullOrWhiteSpace(customSourceRepository))
        {
            return;
        }

        var trimmedSource = customSourceRepository.Trim();
        loadedCustomSourceRepository = trimmedSource;
        restoredLastUsedSource = false;
        isLoadingCustomSource = true;

        try
        {
            await ActionsTemplateSourceStorage.SetLastUsedSourceAsync(trimmedSource).ConfigureAwait(false);
            await LoadTemplatesAsync(trimmedSource).ConfigureAwait(false);
        }
        finally
        {
            isLoadingCustomSource = false;
        }
    }

    private async Task LoadTemplatesAsync(string? customSource = null)
    {
        isLoadingTemplates = true;
        hasLoadFailure = false;
        customSourceError = null;
        customSourceWarning = null;
        selectedTemplateId = null;
        selectedTemplateDetail = null;
        parameterValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        repositoryStatuses = [];
        applyResults = [];

        try
        {
            var catalogue = await ActionsTemplateService.GetTemplatesAsync(customSource).ConfigureAwait(false);
            templates = catalogue.Templates;
            customSourceError = catalogue.CustomSourceError;
            customSourceWarning = catalogue.CustomSourceWarning;
        }
        catch
        {
            templates = [];
            hasLoadFailure = true;
        }
        finally
        {
            isLoadingTemplates = false;
        }
    }

    private async Task LoadRepositoriesAsync()
    {
        isLoadingRepositories = true;
        hasRepositoryLoadFailure = false;
        repositoryLoadErrorMessage = null;

        try
        {
            var repositories = await RepositoryService.GetActiveRepositoriesAsync();
            repositoryOptions = repositories
                .Select(repository => repository.FullName)
                .OrderBy(fullName => fullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            selectedRepositories.Clear();
            repositoryStatuses = [];
            applyResults = [];
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
            repositoryLoadErrorMessage = $"GitHub API request failed. {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load workflow template repositories.");
            hasRepositoryLoadFailure = true;
            repositoryLoadErrorMessage = "An unexpected error occurred while loading repositories.";
        }
        finally
        {
            isLoadingRepositories = false;
            ApplyRestoredCustomSourceToPickers();
        }
    }

    private Task OnCustomSourceRepositoriesChangedAsync(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        selectedCustomSourceRepositories = repositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        customSourceRepository = selectedCustomSourceRepositories.FirstOrDefault() ?? string.Empty;
        restoredLastUsedSource = false;

        return Task.CompletedTask;
    }

    private async Task OnSelectedRepositoriesChangedAsync(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        selectedRepositories = repositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        applyResults = [];
        await RefreshRepositoryStatusesAsync();
    }

    private async Task SelectCategory(string category)
    {
        selectedCategory = category;
        await Task.CompletedTask;
    }

    private async Task SelectTemplate(ActionsTemplateDto template)
    {
        selectedTemplateId = template.Id;
        applyResults = [];

        try
        {
            selectedTemplateDetail = await ActionsTemplateService.GetTemplateDetailAsync(template.Id);
            parameterValues = selectedTemplateDetail.Parameters
                .ToDictionary(parameter => parameter.Name, parameter => parameter.DefaultValue, StringComparer.OrdinalIgnoreCase);
            await RefreshRepositoryStatusesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load workflow template detail for template {TemplateId}.", template.Id);
            selectedTemplateDetail = null;
            ShowSnackbarFeedback("Unable to load template details.", Severity.Error);
        }
    }

    private async Task OnParameterValueChangedAsync(string parameterName, string? value)
    {
        parameterValues[parameterName] = value ?? string.Empty;
        applyResults = [];
        await RefreshRepositoryStatusesAsync();
    }

    private async Task RefreshRepositoryStatusesAsync()
    {
        repositoryStatuses = [];

        if (selectedTemplateDetail is null || selectedRepositories.Count == 0)
        {
            return;
        }

        isLoadingStatuses = true;

        try
        {
            repositoryStatuses = await ActionsTemplateService.GetRepositoryStatusesAsync(
                selectedTemplateDetail.Id,
                SelectedRepositoryFullNames,
                parameterValues);
        }
        catch (ArgumentException ex)
        {
            ShowSnackbarFeedback(ex.Message, Severity.Warning);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load workflow template repository statuses.");
            ShowSnackbarFeedback("Unable to load repository workflow status.", Severity.Error);
        }
        finally
        {
            isLoadingStatuses = false;
        }
    }

    private async Task ApplyTemplateAsync()
    {
        if (selectedTemplateDetail is null || selectedRepositories.Count == 0)
        {
            return;
        }

        isApplyingTemplate = true;
        applyResults = [];

        try
        {
            applyResults = await ActionsTemplateService.ApplyTemplateAsync(
                selectedTemplateDetail.Id,
                SelectedRepositoryFullNames,
                parameterValues);

            var failedCount = applyResults.Count(result => result.HasError);
            var createdCount = applyResults.Count(result => result.Action == "Created");
            var updatedCount = applyResults.Count(result => result.Action == "Updated");
            var skippedCount = applyResults.Count(result => result.Action == "Skipped");

            if (failedCount == 0)
            {
                ShowSnackbarFeedback($"Applied template successfully. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.", Severity.Success);
            }
            else
            {
                ShowSnackbarFeedback($"Applied template with {failedCount} repository errors. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.", Severity.Warning);
            }

            await RefreshRepositoryStatusesAsync();
        }
        catch (ArgumentException ex)
        {
            ShowSnackbarFeedback(ex.Message, Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply workflow template {TemplateId}.", selectedTemplateDetail.Id);
            ShowSnackbarFeedback("An unexpected error occurred while applying the workflow template.", Severity.Error);
        }
        finally
        {
            isApplyingTemplate = false;
        }
    }

    private bool AreRequiredParametersValid()
    {
        if (selectedTemplateDetail is null)
        {
            return false;
        }

        return selectedTemplateDetail.Parameters
            .Where(parameter => parameter.IsRequired)
            .All(parameter => parameterValues.TryGetValue(parameter.Name, out var value) && !string.IsNullOrWhiteSpace(value));
    }

    private bool MatchesSelectedCategory(ActionsTemplateDto template)
        => selectedCategory.Equals(AllCategoriesLabel, StringComparison.OrdinalIgnoreCase)
            || template.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase);

    private bool MatchesSearchText(ActionsTemplateDto template)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return template.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || template.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || template.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || template.Tags.Any(tag => tag.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCategorySelected(string category)
        => selectedCategory.Equals(category, StringComparison.OrdinalIgnoreCase);

    private static string GetTemplateHeadingId(ActionsTemplateDto template)
        => $"actions-templates-heading-{template.Id}";

    private static Color GetStatusColour(ActionsTemplateApplicationStatus status)
        => status switch
        {
            ActionsTemplateApplicationStatus.Applied => Color.Success,
            ActionsTemplateApplicationStatus.Drifted => Color.Warning,
            _ => Color.Default,
        };

    private static string GetStatusLabel(ActionsTemplateApplicationStatus status)
        => status switch
        {
            ActionsTemplateApplicationStatus.Applied => "Applied",
            ActionsTemplateApplicationStatus.Drifted => "Drifted",
            _ => "Not applied",
        };
}
