using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Identity;
using SoloDevBoard.Application.Services.Repositories;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.App.Components.Features.Workflows.Pages;

/// <summary>Provides the workflow template browser for built-in GitHub Actions templates.</summary>
public partial class Workflows : ComponentBase
{
    private const string AllCategoriesLabel = "All";

    /// <summary>Gets or sets the application service used to retrieve workflow templates.</summary>
    [Inject]
    public IWorkflowTemplateService WorkflowTemplateService { get; set; } = default!;

    /// <summary>Gets or sets the application service used to retrieve repositories.</summary>
    [Inject]
    public IRepositoryService RepositoryService { get; set; } = default!;

    /// <summary>Gets or sets the logger for workflow page diagnostics.</summary>
    [Inject]
    public ILogger<Workflows> Logger { get; set; } = default!;

    /// <summary>Gets or sets the hosted authentication recovery service.</summary>
    [Inject]
    public IHostedAuthenticationRecoveryService HostedAuthRecovery { get; set; } = default!;

    private IReadOnlyList<WorkflowTemplateDto> templates = [];
    private WorkflowTemplateDetailDto? selectedTemplateDetail;
    private IReadOnlyList<WorkflowTemplateRepositoryStatusDto> repositoryStatuses = [];
    private IReadOnlyList<WorkflowTemplateRepositoryResultDto> applyResults = [];
    private IReadOnlyList<string> repositoryOptions = [];
    private HashSet<string> selectedRepositories = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> parameterValues = new(StringComparer.OrdinalIgnoreCase);
    private string searchText = string.Empty;
    private string selectedCategory = AllCategoriesLabel;
    private int? selectedTemplateId;
    private bool isLoadingTemplates = true;
    private bool isLoadingRepositories = true;
    private bool isLoadingStatuses;
    private bool isApplyingTemplate;
    private bool hasLoadFailure;
    private bool hasRepositoryLoadFailure;
    private string? repositoryLoadErrorMessage;
    private string? operationMessage;
    private Severity operationSeverity = Severity.Info;

    private bool ShowLoadingState => isLoadingTemplates;

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

    private IReadOnlyList<WorkflowTemplateDto> FilteredTemplates
        => templates
            .Where(MatchesSelectedCategory)
            .Where(MatchesSearchText)
            .OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<string> SelectedRepositoryFullNames
        => selectedRepositories
            .OrderBy(repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
        await Task.WhenAll(LoadTemplatesAsync(), LoadRepositoriesAsync());
    }

    private async Task LoadTemplatesAsync()
    {
        isLoadingTemplates = true;
        hasLoadFailure = false;

        try
        {
            templates = await WorkflowTemplateService.GetTemplatesAsync();
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
        }
    }

    private async Task OnSelectedRepositoriesChangedAsync(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        selectedRepositories = repositories
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        applyResults = [];
        operationMessage = null;
        await RefreshRepositoryStatusesAsync();
    }

    private async Task SelectCategory(string category)
    {
        selectedCategory = category;
        await Task.CompletedTask;
    }

    private async Task SelectTemplate(WorkflowTemplateDto template)
    {
        selectedTemplateId = template.Id;
        applyResults = [];
        operationMessage = null;

        try
        {
            selectedTemplateDetail = await WorkflowTemplateService.GetTemplateDetailAsync(template.Id);
            parameterValues = selectedTemplateDetail.Parameters
                .ToDictionary(parameter => parameter.Name, parameter => parameter.DefaultValue, StringComparer.OrdinalIgnoreCase);
            await RefreshRepositoryStatusesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load workflow template detail for template {TemplateId}.", template.Id);
            selectedTemplateDetail = null;
            operationSeverity = Severity.Error;
            operationMessage = "Unable to load template details.";
        }
    }

    private async Task OnParameterValueChangedAsync(string parameterName, string? value)
    {
        parameterValues[parameterName] = value ?? string.Empty;
        applyResults = [];
        operationMessage = null;
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
            repositoryStatuses = await WorkflowTemplateService.GetRepositoryStatusesAsync(
                selectedTemplateDetail.Id,
                SelectedRepositoryFullNames,
                parameterValues);
        }
        catch (ArgumentException ex)
        {
            operationSeverity = Severity.Warning;
            operationMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load workflow template repository statuses.");
            operationSeverity = Severity.Error;
            operationMessage = "Unable to load repository workflow status.";
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
        operationMessage = null;
        applyResults = [];

        try
        {
            applyResults = await WorkflowTemplateService.ApplyTemplateAsync(
                selectedTemplateDetail.Id,
                SelectedRepositoryFullNames,
                parameterValues);

            var failedCount = applyResults.Count(result => result.HasError);
            var createdCount = applyResults.Count(result => result.Action == "Created");
            var updatedCount = applyResults.Count(result => result.Action == "Updated");
            var skippedCount = applyResults.Count(result => result.Action == "Skipped");

            if (failedCount == 0)
            {
                operationSeverity = Severity.Success;
                operationMessage = $"Applied template successfully. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.";
            }
            else
            {
                operationSeverity = Severity.Warning;
                operationMessage = $"Applied template with {failedCount} repository errors. Created {createdCount}, updated {updatedCount}, skipped {skippedCount}.";
            }

            await RefreshRepositoryStatusesAsync();
        }
        catch (ArgumentException ex)
        {
            operationSeverity = Severity.Error;
            operationMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply workflow template {TemplateId}.", selectedTemplateDetail.Id);
            operationSeverity = Severity.Error;
            operationMessage = "An unexpected error occurred while applying the workflow template.";
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

    private bool MatchesSelectedCategory(WorkflowTemplateDto template)
        => selectedCategory.Equals(AllCategoriesLabel, StringComparison.OrdinalIgnoreCase)
            || template.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase);

    private bool MatchesSearchText(WorkflowTemplateDto template)
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

    private static string GetTemplateHeadingId(WorkflowTemplateDto template)
        => $"workflow-template-heading-{template.Id}";

    private static Color GetStatusColour(WorkflowTemplateApplicationStatus status)
        => status switch
        {
            WorkflowTemplateApplicationStatus.Applied => Color.Success,
            WorkflowTemplateApplicationStatus.Drifted => Color.Warning,
            _ => Color.Default,
        };

    private static string GetStatusLabel(WorkflowTemplateApplicationStatus status)
        => status switch
        {
            WorkflowTemplateApplicationStatus.Applied => "Applied",
            WorkflowTemplateApplicationStatus.Drifted => "Drifted",
            _ => "Not applied",
        };
}
