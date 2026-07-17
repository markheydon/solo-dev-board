using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.App.Components.Features.Workflows.Pages;

/// <summary>Provides the workflow template browser for built-in GitHub Actions templates.</summary>
public partial class Workflows : ComponentBase
{
    private const string AllCategoriesLabel = "All";

    /// <summary>Gets or sets the application service used to retrieve workflow templates.</summary>
    [Inject]
    public IWorkflowTemplateService WorkflowTemplateService { get; set; } = default!;

    private IReadOnlyList<WorkflowTemplateDto> templates = [];
    private string searchText = string.Empty;
    private string selectedCategory = AllCategoriesLabel;
    private int? selectedTemplateId;
    private bool isLoadingTemplates = true;
    private bool hasLoadFailure;

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

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadTemplatesAsync();
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

    private void SelectCategory(string category)
    {
        selectedCategory = category;
    }

    private void SelectTemplate(WorkflowTemplateDto template)
    {
        selectedTemplateId = template.Id;
    }

    private static string GetTemplateHeadingId(WorkflowTemplateDto template)
        => $"workflow-template-heading-{template.Id}";
}
