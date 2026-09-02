using SoloDevBoard.Domain.Entities.ActionsTemplates;

namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Provides built-in workflow templates for browsing, customisation, and apply flows.</summary>
public sealed class ActionsTemplateService : IActionsTemplateService
{
    private const string CustomCategory = "Custom";
    private const string PlaceholderPrefix = "{{";
    private const string PlaceholderSuffix = "}}";
    private const string WorkflowsDirectoryPath = ".github/workflows";

    private static readonly DateTimeOffset BuiltInCreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<ActionsTemplate> BuiltInTemplates = BuildBuiltInTemplates();

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ActionsTemplateParameter>> ParametersByTemplateId =
        BuildParametersByTemplateId();

    private readonly IWorkflowFileRepository _workflowFileRepository;

    /// <summary>Initialises a new instance of the <see cref="ActionsTemplateService"/> class.</summary>
    /// <param name="workflowFileRepository">The repository used to read and write workflow files.</param>
    public ActionsTemplateService(IWorkflowFileRepository workflowFileRepository)
    {
        ArgumentNullException.ThrowIfNull(workflowFileRepository);
        _workflowFileRepository = workflowFileRepository;
    }

    /// <inheritdoc/>
    public async Task<ActionsTemplateCatalogueDto> GetTemplatesAsync(string? customSourceRepository = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var builtInTemplates = BuiltInTemplates
            .Select(MapToDto)
            .ToArray();

        if (string.IsNullOrWhiteSpace(customSourceRepository))
        {
            return new ActionsTemplateCatalogueDto(builtInTemplates, null);
        }

        try
        {
            var customTemplates = await LoadCustomTemplatesAsync(customSourceRepository.Trim(), cancellationToken).ConfigureAwait(false);
            var mergedTemplates = builtInTemplates
                .Concat(customTemplates.Select(MapToDto))
                .ToArray();

            return new ActionsTemplateCatalogueDto(mergedTemplates, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or ArgumentException or FormatException)
        {
            return new ActionsTemplateCatalogueDto(builtInTemplates, MapCustomSourceError(ex, customSourceRepository.Trim()));
        }
    }

    /// <inheritdoc/>
    public async Task<ActionsTemplateDetailDto> GetTemplateDetailAsync(string templateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        cancellationToken.ThrowIfCancellationRequested();

        var template = await ResolveTemplateAsync(templateId, cancellationToken).ConfigureAwait(false);
        var parameters = await GetParametersAsync(template, cancellationToken).ConfigureAwait(false);
        var resolvedValues = ResolveParameterValues(parameters, new Dictionary<string, string>());
        var yamlPreview = ApplyParameterValues(template.YamlContent, resolvedValues);

        return MapToDetailDto(template, parameters, yamlPreview);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionsTemplateRepositoryStatusDto>> GetRepositoryStatusesAsync(
        string templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentNullException.ThrowIfNull(repositoryFullNames);
        ArgumentNullException.ThrowIfNull(parameterValues);

        if (repositoryFullNames.Count == 0)
        {
            return [];
        }

        var template = await ResolveTemplateAsync(templateId, cancellationToken).ConfigureAwait(false);
        var parameters = await GetParametersAsync(template, cancellationToken).ConfigureAwait(false);
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
    public async Task<IReadOnlyList<ActionsTemplateRepositoryResultDto>> ApplyTemplateAsync(
        string templateId,
        IReadOnlyList<string> repositoryFullNames,
        IReadOnlyDictionary<string, string> parameterValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentNullException.ThrowIfNull(repositoryFullNames);
        ArgumentNullException.ThrowIfNull(parameterValues);

        var template = await ResolveTemplateAsync(templateId, cancellationToken).ConfigureAwait(false);
        var parameters = await GetParametersAsync(template, cancellationToken).ConfigureAwait(false);
        var resolvedValues = ResolveParameterValues(parameters, parameterValues);
        ValidateParameterValues(parameters, resolvedValues);

        var normalisedRepositories = NormaliseRepositories(repositoryFullNames);
        var renderedContent = ApplyParameterValues(template.YamlContent, resolvedValues);
        var canonicalContent = NormaliseContent(renderedContent);
        var commitMessage = $"Apply {template.Name} workflow template via SoloDevBoard";

        var results = new List<ActionsTemplateRepositoryResultDto>();

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
                    results.Add(new ActionsTemplateRepositoryResultDto(repositoryFullName, "Skipped", null));
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
                results.Add(new ActionsTemplateRepositoryResultDto(repositoryFullName, action, null));
            }
            catch (Exception ex) when (ex is HttpRequestException or KeyNotFoundException or ArgumentException)
            {
                results.Add(new ActionsTemplateRepositoryResultDto(repositoryFullName, "Failed", ex.Message));
            }
        }

        return results
            .OrderBy(result => result.RepositoryFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<ActionsTemplate>> LoadCustomTemplatesAsync(string repositoryFullName, CancellationToken cancellationToken)
    {
        var sourceRepository = SplitRepositoryFullName(repositoryFullName);
        var directoryEntries = await _workflowFileRepository
            .ListWorkflowFilesAsync(sourceRepository.Owner, sourceRepository.Name, cancellationToken)
            .ConfigureAwait(false);

        var workflowEntries = directoryEntries
            .Where(entry => WorkflowYamlTemplateParser.IsWorkflowYamlFileName(entry.Name))
            .Where(entry => IsTopLevelWorkflowPath(entry.Path))
            .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var templates = new List<ActionsTemplate>(workflowEntries.Length);

        foreach (var entry in workflowEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var workflowFile = await _workflowFileRepository
                .GetWorkflowFileAsync(sourceRepository.Owner, sourceRepository.Name, entry.Path, cancellationToken)
                .ConfigureAwait(false);

            if (workflowFile is null)
            {
                continue;
            }

            templates.Add(new ActionsTemplate
            {
                Id = ActionsTemplateIdFormatter.FormatCustom(repositoryFullName, entry.Path),
                Name = WorkflowYamlTemplateParser.ResolveDisplayName(workflowFile.Content, entry.Name),
                Description = string.Empty,
                Category = CustomCategory,
                Tags = [],
                WorkflowFilePath = entry.Path,
                TriggerDescription = string.Empty,
                YamlContent = workflowFile.Content,
                CreatedAt = BuiltInCreatedAt,
            });
        }

        return templates;
    }

    private async Task<ActionsTemplateRepositoryStatusDto> BuildRepositoryStatusAsync(
        ActionsTemplate template,
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
                return new ActionsTemplateRepositoryStatusDto(
                    repositoryFullName,
                    ActionsTemplateApplicationStatus.NotApplied,
                    "Workflow file is not present in this repository.");
            }

            if (NormaliseContent(existingFile.Content) == canonicalContent)
            {
                return new ActionsTemplateRepositoryStatusDto(
                    repositoryFullName,
                    ActionsTemplateApplicationStatus.Applied,
                    "Workflow file matches the canonical template.");
            }

            return new ActionsTemplateRepositoryStatusDto(
                repositoryFullName,
                ActionsTemplateApplicationStatus.Drifted,
                "Workflow file differs from the canonical template.");
        }
        catch (Exception ex) when (ex is HttpRequestException or ArgumentException)
        {
            return new ActionsTemplateRepositoryStatusDto(
                repositoryFullName,
                ActionsTemplateApplicationStatus.NotApplied,
                $"Unable to inspect workflow file: {ex.Message}");
        }
    }

    private async Task<ActionsTemplate> ResolveTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        if (ActionsTemplateIdFormatter.IsBuiltIn(templateId))
        {
            return ResolveBuiltInTemplate(templateId);
        }

        if (!ActionsTemplateIdFormatter.IsCustom(templateId))
        {
            throw new KeyNotFoundException($"Workflow template '{templateId}' was not found.");
        }

        var (repositoryFullName, workflowFilePath) = ActionsTemplateIdFormatter.ParseCustom(templateId);
        var sourceRepository = SplitRepositoryFullName(repositoryFullName);
        var workflowFile = await _workflowFileRepository
            .GetWorkflowFileAsync(sourceRepository.Owner, sourceRepository.Name, workflowFilePath, cancellationToken)
            .ConfigureAwait(false);

        if (workflowFile is null)
        {
            throw new KeyNotFoundException($"Workflow template '{templateId}' was not found.");
        }

        return new ActionsTemplate
        {
            Id = templateId,
            Name = WorkflowYamlTemplateParser.ResolveDisplayName(workflowFile.Content, Path.GetFileName(workflowFilePath)),
            Description = string.Empty,
            Category = CustomCategory,
            Tags = [],
            WorkflowFilePath = workflowFilePath,
            TriggerDescription = string.Empty,
            YamlContent = workflowFile.Content,
            CreatedAt = BuiltInCreatedAt,
        };
    }

    private static ActionsTemplate ResolveBuiltInTemplate(string templateId)
    {
        var template = BuiltInTemplates.FirstOrDefault(candidate => candidate.Id == templateId);
        if (template is null)
        {
            throw new KeyNotFoundException($"Workflow template '{templateId}' was not found.");
        }

        return template;
    }

    private static Task<IReadOnlyList<ActionsTemplateParameter>> GetParametersAsync(ActionsTemplate template, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ActionsTemplateIdFormatter.IsBuiltIn(template.Id))
        {
            return Task.FromResult(GetBuiltInParameters(template.Id));
        }

        return Task.FromResult<IReadOnlyList<ActionsTemplateParameter>>(WorkflowYamlTemplateParser.InferParameters(template.YamlContent));
    }

    private static IReadOnlyList<ActionsTemplateParameter> GetBuiltInParameters(string templateId)
        => ParametersByTemplateId.TryGetValue(templateId, out var parameters)
            ? parameters
            : [];

    private static string MapCustomSourceError(Exception exception, string repositoryFullName)
        => exception switch
        {
            ArgumentException => exception.Message,
            FormatException => $"Repository '{repositoryFullName}' must be in owner/repository format.",
            HttpRequestException httpRequestException when httpRequestException.Message.Contains("404", StringComparison.Ordinal)
                => $"Repository '{repositoryFullName}' was not found or is not accessible.",
            HttpRequestException httpRequestException when httpRequestException.Message.Contains("403", StringComparison.Ordinal)
                => $"Repository '{repositoryFullName}' is not accessible with the current GitHub token.",
            HttpRequestException httpRequestException => httpRequestException.Message,
            _ => $"Unable to load custom templates from '{repositoryFullName}'.",
        };

    private static bool IsTopLevelWorkflowPath(string path)
    {
        const string prefix = ".github/workflows/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = path[prefix.Length..];
        return !fileName.Contains('/', StringComparison.Ordinal);
    }

    private static ActionsTemplateDto MapToDto(ActionsTemplate template)
        => new(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            template.Tags,
            template.WorkflowFilePath,
            template.TriggerDescription,
            template.CreatedAt);

    private static ActionsTemplateDetailDto MapToDetailDto(
        ActionsTemplate template,
        IReadOnlyList<ActionsTemplateParameter> parameters,
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

    private static ActionsTemplateParameterDto MapParameterToDto(ActionsTemplateParameter parameter)
        => new(
            parameter.Name,
            parameter.Label,
            parameter.Description,
            parameter.DefaultValue,
            parameter.IsRequired);

    private static IReadOnlyDictionary<string, string> ResolveParameterValues(
        IReadOnlyList<ActionsTemplateParameter> parameters,
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
        IReadOnlyList<ActionsTemplateParameter> parameters,
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

    private static IReadOnlyDictionary<string, IReadOnlyList<ActionsTemplateParameter>> BuildParametersByTemplateId()
        => new Dictionary<string, IReadOnlyList<ActionsTemplateParameter>>
        {
            [ActionsTemplateIdFormatter.FormatBuiltIn(1)] =
            [
                new ActionsTemplateParameter
                {
                    Name = "mainBranch",
                    Label = "Main branch",
                    Description = "The branch that triggers CI builds.",
                    DefaultValue = "main",
                    IsRequired = true,
                },
                new ActionsTemplateParameter
                {
                    Name = "dotnetVersion",
                    Label = ".NET version",
                    Description = "The .NET SDK version used by the workflow.",
                    DefaultValue = "10.0.x",
                    IsRequired = false,
                },
            ],
            [ActionsTemplateIdFormatter.FormatBuiltIn(2)] =
            [
                new ActionsTemplateParameter
                {
                    Name = "environmentName",
                    Label = "Deployment environment",
                    Description = "The GitHub environment used for deployment approval.",
                    DefaultValue = "production",
                    IsRequired = true,
                },
            ],
            [ActionsTemplateIdFormatter.FormatBuiltIn(3)] =
            [
                new ActionsTemplateParameter
                {
                    Name = "targetBranch",
                    Label = "Target branch",
                    Description = "The branch that Dependabot pull requests must target.",
                    DefaultValue = "main",
                    IsRequired = true,
                },
            ],
        };

    private static IReadOnlyList<ActionsTemplate> BuildBuiltInTemplates()
        =>
        [
            new ActionsTemplate
            {
                Id = ActionsTemplateIdFormatter.FormatBuiltIn(1),
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
            new ActionsTemplate
            {
                Id = ActionsTemplateIdFormatter.FormatBuiltIn(2),
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
            new ActionsTemplate
            {
                Id = ActionsTemplateIdFormatter.FormatBuiltIn(3),
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
