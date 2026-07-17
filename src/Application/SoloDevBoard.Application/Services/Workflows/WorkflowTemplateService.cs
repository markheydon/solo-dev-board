using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Services.Workflows;

/// <summary>Provides built-in workflow templates for browsing and future apply flows.</summary>
public sealed class WorkflowTemplateService : IWorkflowTemplateService
{
    private static readonly DateTimeOffset BuiltInCreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<WorkflowTemplate> BuiltInTemplates = BuildBuiltInTemplates();

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
    public Task<WorkflowTemplateDto> ApplyTemplateAsync(string owner, string repo, int templateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        cancellationToken.ThrowIfCancellationRequested();

        var template = BuiltInTemplates.FirstOrDefault(candidate => candidate.Id == templateId)
            ?? throw new InvalidOperationException($"Workflow template '{templateId}' was not found.");

        return Task.FromResult(MapToDto(template));
    }

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
                          - main
                      pull_request:
                        branches:
                          - main

                    jobs:
                      build-and-test:
                        runs-on: ubuntu-latest
                        steps:
                          - uses: actions/checkout@v7
                          - uses: actions/setup-dotnet@v5
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
                        environment: production
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
                          - main

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
}
