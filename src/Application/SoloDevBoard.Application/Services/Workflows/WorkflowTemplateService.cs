using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Provides built-in workflow templates for browsing, customisation, and apply flows.</summary>
public sealed class WorkflowTemplateService : IWorkflowTemplateService
{
    private const string PlaceholderPrefix = "{{";
    private const string PlaceholderSuffix = "}}";

    private static readonly DateTimeOffset BuiltInCreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<WorkflowTemplate> BuiltInTemplates = BuildBuiltInTemplates();

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<WorkflowTemplateParameter>> ParametersByTemplateId =
        BuildParametersByTemplateId();

    private readonly IWorkflowFileRepository _workflowFileRepository;

    /// <summary>Initialises a new instance of the <see cref="WorkflowTemplateService"/> class.</summary>
    /// <param name="workflowFileRepository">The repository used to read and write workflow files.</param>
    public WorkflowTemplateService(IWorkflowFileRepository workflowFileRepository)
    {
        ArgumentNullException.ThrowIfNull(workflowFileRepository);
        _workflowFileRepository = workflowFileRepository;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<WorkflowTemplateDto> result = BuiltInTemplates
            .Select(MapToDto)
            .ToArray();

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<WorkflowTemplateDetailDto> GetTemplateDetailAsync(int templateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var template = ResolveTemplate(templateId);
        var parameters = GetParameters(templateId);
        var resolvedValues = ResolveParameterValues(parameters, new Dictionary<string, string>());
        var yamlPreview = ApplyParameterValues(template.YamlContent, resolvedValues);

        return Task.FromResult(MapToDetailDto(template, parameters, yamlPreview));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowTemplateRepositoryStatusDto>> GetRepositoryStatusesAsync(
        int templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);
        ArgumentNullException.ThrowIfNull(parameterValues);

        if (repositoryFullNames.Count == 0)
        {
            return [];
        }

        var template = ResolveTemplate(templateId);
        var parameters = GetParameters(templateId);
        var resolvedValues = ResolveParameterValues(parameters, parameterValues);
        ValidateParameterValues(parameters, resolvedValues);
        var canonicalContent = NormaliseContent(ApplyParameterValues(template.YamlContent, resolvedValues));

        var normalisedRepositories = NormaliseRepositories(repositoryFullNames);
        var statusTasks = normalisedRepositories
            .Select(repositoryFullName => BuildRepositoryStatusAsync(template, repositoryFullName, canonicalContent, cancellationToken))
            .ToArray();

        var statuses = await Task.WhenAll(statusTasks).ConfigureAwait(false);
        return statuses
            .OrderBy(status => status.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkflowTemplateRepositoryResultDto>> ApplyTemplateAsync(
        int templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryFullNames);
        ArgumentNullException.ThrowIfNull(parameterValues);

        var template = ResolveTemplate(templateId);
        var parameters = GetParameters(templateId);
        var resolvedValues = ResolveParameterValues(parameters, parameterValues);
        ValidateParameterValues(parameters, resolvedValues);

        var normalisedRepositories = NormaliseRepositories(repositoryFullNames);
        var renderedContent = ApplyParameterValues(template.YamlContent, resolvedValues);
        var canonicalContent = NormaliseContent(renderedContent);
        var commitMessage = $"Apply {template.Name} workflow template via SoloDevBoard";

        var results = new List<WorkflowTemplateRepositoryResultDto>();

        foreach (var repositoryFullName in normalisedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var repository = SplitRepositoryFullName(repositoryFullName);
                var existingFile = await _workflowFileRepository
                    .GetWorkflowFileAsync(repository.Owner, repository.Name, template.WorkflowFilePath, cancellationToken)
                    .ConfigureAwait(false);

                if (existingFile is not null && NormaliseContent(existingFile.Content) == canonicalContent)
                {
                    results.Add(new WorkflowTemplateRepositoryResultDto(repositoryFullName, "Skipped", null));
                    continue;
                }

                await _workflowFileRepository
                    .CreateOrUpdateWorkflowFileAsync(
                        repository.Owner,
                        repository.Name,
                        template.WorkflowFilePath,
                        renderedContent,
                        existingFile?.Sha,
                        commitMessage,
                        cancellationToken)
                    .ConfigureAwait(false);

                var action = existingFile is null ? "Created" : "Updated";
                results.Add(new WorkflowTemplateRepositoryResultDto(repositoryFullName, action, null));
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new WorkflowTemplateRepositoryResultDto(repositoryFullName, "Failed", ex.Message));
            }
        }

        return results
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<WorkflowTemplateRepositoryStatusDto> BuildRepositoryStatusAsync(
        WorkflowTemplate template,
        string repositoryFullName,
        string canonicalContent,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = SplitRepositoryFullName(repositoryFullName);
            var existingFile = await _workflowFileRepository
                .GetWorkflowFileAsync(repository.Owner, repository.Name, template.WorkflowFilePath, cancellationToken)
                .ConfigureAwait(false);

            if (existingFile is null)
            {
                return new WorkflowTemplateRepositoryStatusDto(
                    repositoryFullName,
                    WorkflowTemplateApplicationStatus.NotApplied,
                    "Workflow file is not present in this repository.");
            }

            if (NormaliseContent(existingFile.Content) == canonicalContent)
            {
                return new WorkflowTemplateRepositoryStatusDto(
                    repositoryFullName,
                    WorkflowTemplateApplicationStatus.Applied,
                    "Workflow file matches the canonical template.");
            }

            return new WorkflowTemplateRepositoryStatusDto(
                repositoryFullName,
                WorkflowTemplateApplicationStatus.Drifted,
                "Workflow file differs from the canonical template.");
        }
        catch (Exception ex) when (ex is HttpRequestException or ArgumentException)
        {
            return new WorkflowTemplateRepositoryStatusDto(
                repositoryFullName,
                WorkflowTemplateApplicationStatus.NotApplied,
                $"Unable to inspect workflow file: {ex.Message}");
        }
    }

    private static WorkflowTemplate ResolveTemplate(int templateId)
    {
        var template = BuiltInTemplates.FirstOrDefault(candidate => candidate.Id == templateId);
        if (template is null)
        {
            throw new KeyNotFoundException($"Workflow template '{templateId}' was not found.");
        }

        return template;
    }

    private static IReadOnlyList<WorkflowTemplateParameter> GetParameters(int templateId)
        => ParametersByTemplateId.TryGetValue(templateId, out var parameters)
            ? parameters
            : [];

    private static WorkflowTemplateDto MapToDto(WorkflowTemplate template)
        => new(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            template.Tags,
            template.WorkflowFilePath,
            template.TriggerDescription,
            template.CreatedAt);

    private static WorkflowTemplateDetailDto MapToDetailDto(
        WorkflowTemplate template,
        IReadOnlyList<WorkflowTemplateParameter> parameters,
        string yamlPreview)
        => new(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            template.Tags,
            template.WorkflowFilePath,
            template.TriggerDescription,
            template.CreatedAt,
            yamlPreview,
            parameters.Select(MapParameterToDto).ToArray());

    private static WorkflowTemplateParameterDto MapParameterToDto(WorkflowTemplateParameter parameter)
        => new(
            parameter.Name,
            parameter.Label,
            parameter.Description,
            parameter.DefaultValue,
            parameter.IsRequired);

    private static IReadOnlyDictionary<string, string> ResolveParameterValues(
        IReadOnlyList<WorkflowTemplateParameter> parameters,
        IReadOnlyDictionary<string, string> parameterValues)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters)
        {
            if (parameterValues.TryGetValue(parameter.Name, out var providedValue))
            {
                resolved[parameter.Name] = string.IsNullOrWhiteSpace(providedValue)
                    ? string.Empty
                    : providedValue.Trim();
                continue;
            }

            resolved[parameter.Name] = parameter.DefaultValue;
        }

        return resolved;
    }

    private static void ValidateParameterValues(
        IReadOnlyList<WorkflowTemplateParameter> parameters,
        IReadOnlyDictionary<string, string> resolvedValues)
    {
        foreach (var parameter in parameters.Where(parameter => parameter.IsRequired))
        {
            if (!resolvedValues.TryGetValue(parameter.Name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Parameter '{parameter.Label}' is required.");
            }
        }
    }

    private static string ApplyParameterValues(string yamlContent, IReadOnlyDictionary<string, string> resolvedValues)
    {
        var rendered = yamlContent;

        foreach (var (name, value) in resolvedValues)
        {
            rendered = rendered.Replace($"{PlaceholderPrefix}{name}{PlaceholderSuffix}", value, StringComparison.Ordinal);
        }

        return rendered;
    }

    private static string NormaliseContent(string content)
        => content.ReplaceLineEndings("\n").TrimEnd();

    private static IReadOnlyList<string> NormaliseRepositories(IReadOnlyList<string> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var normalised = repositories
            .Select(repository => repository?.Trim())
            .Where(repository => !string.IsNullOrWhiteSpace(repository))
            .Select(repository => repository!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalised.Length == 0)
        {
            throw new ArgumentException("At least one repository must be provided.", nameof(repositories));
        }

        return normalised;
    }

    private static RepositoryCoordinates SplitRepositoryFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name must be provided.", nameof(fullName));
        }

        var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Repository '{fullName}' must be in owner/repository format.", nameof(fullName));
        }

        return new RepositoryCoordinates(parts[0], parts[1]);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<WorkflowTemplateParameter>> BuildParametersByTemplateId()
        => new Dictionary<int, IReadOnlyList<WorkflowTemplateParameter>>
        {
            [1] =
            [
                new WorkflowTemplateParameter
                {
                    Name = "mainBranch",
                    Label = "Main branch",
                    Description = "The branch that triggers CI builds.",
                    DefaultValue = "main",
                    IsRequired = true,
                },
                new WorkflowTemplateParameter
                {
                    Name = "dotnetVersion",
                    Label = ".NET version",
                    Description = "The .NET SDK version used by the workflow.",
                    DefaultValue = "10.0.x",
                    IsRequired = false,
                },
            ],
            [2] =
            [
                new WorkflowTemplateParameter
                {
                    Name = "environmentName",
                    Label = "Deployment environment",
                    Description = "The GitHub environment used for deployment approval.",
                    DefaultValue = "production",
                    IsRequired = true,
                },
            ],
            [3] =
            [
                new WorkflowTemplateParameter
                {
                    Name = "targetBranch",
                    Label = "Target branch",
                    Description = "The branch that Dependabot pull requests must target.",
                    DefaultValue = "main",
                    IsRequired = true,
                },
            ],
        };

    private static IReadOnlyList<WorkflowTemplate> BuildBuiltInTemplates()
        =>
        [
            new WorkflowTemplate
            {
                Id = 1,
                Name = ".NET CI",
                Description = "Build and test a .NET solution on every push and pull request to the main branch.",
                Category = "CI",
                Tags = ["dotnet", "github-actions", "build", "test"],
                WorkflowFilePath = ".github/workflows/ci.yml",
                TriggerDescription = "Push and pull request to main",
                YamlContent = """
                    name: CI

                    on:
                      push:
                        branches:
                          - {{mainBranch}}
                      pull_request:
                        branches:
                          - {{mainBranch}}

                    jobs:
                      build-and-test:
                        runs-on: ubuntu-latest
                        steps:
                          - uses: actions/checkout@v7
                          - uses: actions/setup-dotnet@v5
                            with:
                              dotnet-version: '{{dotnetVersion}}'
                          - run: dotnet build
                          - run: dotnet test
                    """,
                CreatedAt = BuiltInCreatedAt,
            },
            new WorkflowTemplate
            {
                Id = 2,
                Name = "Azure CD (Aspire)",
                Description = "Deploy the application to Azure Container Apps using Aspire after operator approval.",
                Category = "CD",
                Tags = ["aspire", "azure", "deploy", "container-apps"],
                WorkflowFilePath = ".github/workflows/cd.yml",
                TriggerDescription = "Manual workflow dispatch",
                YamlContent = """
                    name: CD - Deploy to Azure

                    on:
                      workflow_dispatch:

                    jobs:
                      deploy:
                        runs-on: ubuntu-latest
                        environment: {{environmentName}}
                        steps:
                          - uses: actions/checkout@v7
                          - uses: actions/setup-dotnet@v5
                          - run: dotnet tool install --global aspire.cli
                          - run: aspire deploy
                    """,
                CreatedAt = BuiltInCreatedAt,
            },
            new WorkflowTemplate
            {
                Id = 3,
                Name = "Dependabot Auto-Merge",
                Description = "Automatically enable auto-merge for low-risk Dependabot pull requests after metadata checks.",
                Category = "Maintenance",
                Tags = ["dependabot", "security", "automation"],
                WorkflowFilePath = ".github/workflows/dependabot-auto-merge.yml",
                TriggerDescription = "Dependabot pull requests targeting main",
                YamlContent = """
                    name: Dependabot Auto-Merge

                    on:
                      pull_request_target:
                        branches:
                          - {{targetBranch}}

                    jobs:
                      enable-auto-merge:
                        if: github.actor == 'dependabot[bot]'
                        runs-on: ubuntu-latest
                        steps:
                          - uses: dependabot/fetch-metadata@v3
                          - run: gh pr merge --auto --squash "$PR_URL"
                    """,
                CreatedAt = BuiltInCreatedAt,
            },
        ];

    private sealed record RepositoryCoordinates(string Owner, string Name);
}
