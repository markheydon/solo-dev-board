using Microsoft.Extensions.Logging;
using NSubstitute;
using SoloDevBoard.Application.Services.ActionsTemplates;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="ActionsTemplateService"/>.</summary>
public sealed class ActionsTemplateServiceTests
{
    private const string BuiltInCiId = "builtin:1";
    private const string CustomTemplateId = "custom:source-owner/template-repo:.github/workflows/deploy.yml";

    private readonly IWorkflowFileRepository _workflowFileRepository = Substitute.For<IWorkflowFileRepository>();
    private readonly ILogger<ActionsTemplateService> _logger = Substitute.For<ILogger<ActionsTemplateService>>();

    [Fact]
    public void Constructor_WorkflowFileRepositoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IWorkflowFileRepository? workflowFileRepository = null;

        // Act
        var action = () => _ = new ActionsTemplateService(workflowFileRepository!, _logger);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_LoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<ActionsTemplateService>? logger = null;

        // Act
        var action = () => _ = new ActionsTemplateService(_workflowFileRepository, logger!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsBuiltInTemplates()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync(cancellationToken: cancellationToken);

        // Assert
        Assert.Null(result.CustomSourceError);
        Assert.Equal(3, result.Templates.Count);
        Assert.Contains(result.Templates, template => template.Name == ".NET CI");
        Assert.Contains(result.Templates, template => template.Name == "Azure CD (Aspire)");
        Assert.Contains(result.Templates, template => template.Name == "Dependabot Auto-Merge");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplateMetadata()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync(cancellationToken: cancellationToken);
        var ciTemplate = result.Templates.Single(template => template.Name == ".NET CI");

        // Assert
        Assert.Equal(BuiltInCiId, ciTemplate.Id);
        Assert.Equal("CI", ciTemplate.Category);
        Assert.Equal("Built-in", ciTemplate.SourceLabel);
        Assert.Equal(".github/workflows/ci.yml", ciTemplate.WorkflowFilePath);
        Assert.Contains("dotnet", ciTemplate.Tags);
        Assert.False(string.IsNullOrWhiteSpace(ciTemplate.TriggerDescription));
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCustomSource_MergesCustomTemplatesWithBuiltIns()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .ListWorkflowFilesAsync("source-owner", "template-repo", cancellationToken)
            .Returns([
                new WorkflowDirectoryEntry
                {
                    Path = ".github/workflows/deploy.yml",
                    Name = "deploy.yml",
                },
            ]);

        _workflowFileRepository
            .GetWorkflowFileAsync("source-owner", "template-repo", ".github/workflows/deploy.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/deploy.yml",
                Content = "name: Deploy\n\njobs:\n  deploy:\n    runs-on: ubuntu-latest",
                Sha = "sha-1",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync("source-owner/template-repo", cancellationToken);

        // Assert
        Assert.Null(result.CustomSourceError);
        Assert.Equal(4, result.Templates.Count);
        Assert.Contains(result.Templates, template => template.Id == BuiltInCiId);
        Assert.Contains(result.Templates, template => template.Id == CustomTemplateId);
        Assert.Equal("Deploy", result.Templates.Single(template => template.Id == CustomTemplateId).Name);
        Assert.Equal("Custom", result.Templates.Single(template => template.Id == CustomTemplateId).Category);
        Assert.Equal("source-owner/template-repo", result.Templates.Single(template => template.Id == CustomTemplateId).SourceLabel);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCustomSource_InfersPlaceholderParametersOnDetail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .ListWorkflowFilesAsync("source-owner", "template-repo", cancellationToken)
            .Returns([
                new WorkflowDirectoryEntry
                {
                    Path = ".github/workflows/deploy.yml",
                    Name = "deploy.yml",
                },
            ]);

        _workflowFileRepository
            .GetWorkflowFileAsync("source-owner", "template-repo", ".github/workflows/deploy.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/deploy.yml",
                Content = "name: Deploy\nenvironment: {{environmentName}}",
                Sha = "sha-1",
            });

        var sut = CreateSut();
        await sut.GetTemplatesAsync("source-owner/template-repo", cancellationToken);

        // Act
        var detail = await sut.GetTemplateDetailAsync(CustomTemplateId, cancellationToken);

        // Assert
        Assert.Single(detail.Parameters);
        Assert.Equal("environmentName", detail.Parameters[0].Name);
        Assert.Equal("environmentName", detail.Parameters[0].Label);
        Assert.True(detail.Parameters[0].IsRequired);
        Assert.Equal(string.Empty, detail.Parameters[0].Description);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCustomSourceFailure_ReturnsBuiltInsAndError()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .ListWorkflowFilesAsync("source-owner", "missing-repo", cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<WorkflowDirectoryEntry>>(new HttpRequestException("GitHub API request failed. Status: 404 (Not Found).")));

        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync("source-owner/missing-repo", cancellationToken);

        // Assert
        Assert.Equal(3, result.Templates.Count);
        Assert.NotNull(result.CustomSourceError);
        Assert.Contains("not found", result.CustomSourceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCustomSourceAndSkippedWorkflowFile_ReturnsWarningAndSkippedPaths()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .ListWorkflowFilesAsync("source-owner", "template-repo", cancellationToken)
            .Returns([
                new WorkflowDirectoryEntry
                {
                    Path = ".github/workflows/deploy.yml",
                    Name = "deploy.yml",
                },
                new WorkflowDirectoryEntry
                {
                    Path = ".github/workflows/missing.yml",
                    Name = "missing.yml",
                },
            ]);

        _workflowFileRepository
            .GetWorkflowFileAsync("source-owner", "template-repo", ".github/workflows/deploy.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/deploy.yml",
                Content = "name: Deploy\n\njobs:\n  deploy:\n    runs-on: ubuntu-latest",
                Sha = "sha-1",
            });

        _workflowFileRepository
            .GetWorkflowFileAsync("source-owner", "template-repo", ".github/workflows/missing.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync("source-owner/template-repo", cancellationToken);

        // Assert
        Assert.Null(result.CustomSourceError);
        Assert.NotNull(result.CustomSourceWarning);
        Assert.Contains("missing.yml", result.CustomSourceWarning, StringComparison.OrdinalIgnoreCase);
        Assert.Single(result.SkippedWorkflowPaths);
        Assert.Equal(".github/workflows/missing.yml", result.SkippedWorkflowPaths[0]);
        Assert.Equal(4, result.Templates.Count);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithEmptyWorkflowDirectory_ReturnsBuiltInsOnly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .ListWorkflowFilesAsync("source-owner", "template-repo", cancellationToken)
            .Returns([]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync("source-owner/template-repo", cancellationToken);

        // Assert
        Assert.Null(result.CustomSourceError);
        Assert.Equal(3, result.Templates.Count);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithInvalidCustomSource_ReturnsBuiltInsAndFormatError()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync("invalid-source", cancellationToken);

        // Assert
        Assert.Equal(3, result.Templates.Count);
        Assert.NotNull(result.CustomSourceError);
        Assert.Contains("owner/repository format", result.CustomSourceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTemplateDetailAsync_ReturnsParametersAndYamlPreview()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplateDetailAsync(BuiltInCiId, cancellationToken);

        // Assert
        Assert.Equal(".NET CI", result.Name);
        Assert.Equal(2, result.Parameters.Count);
        Assert.Contains("main", result.YamlPreview, StringComparison.Ordinal);
        Assert.DoesNotContain("{{mainBranch}}", result.YamlPreview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetTemplateDetailAsync_UnknownTemplate_ThrowsKeyNotFoundException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.GetTemplateDetailAsync("builtin:999", cancellationToken);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(action);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_MissingWorkflowFile_ReturnsNotAppliedStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(ActionsTemplateApplicationStatus.NotApplied, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_MatchingWorkflowFile_ReturnsAppliedStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var canonicalYaml = await GetRenderedCiYamlAsync();

        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = canonicalYaml,
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(ActionsTemplateApplicationStatus.Applied, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_DifferentWorkflowFile_ReturnsDriftedStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = "name: Custom CI",
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(ActionsTemplateApplicationStatus.Drifted, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_RequiredParameterMissing_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.GetRepositoryStatusesAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string> { ["mainBranch"] = "   " }, cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_MissingWorkflowFile_CreatesWorkflowFile()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("Created", result[0].Action);
        Assert.False(result[0].HasError);
        await _workflowFileRepository.Received(1).CreateOrUpdateWorkflowFileAsync(
            Arg.Is("owner"),
            Arg.Is("repo-a"),
            Arg.Is(".github/workflows/ci.yml"),
            Arg.Is<string>(content => content!.Contains("name: CI", StringComparison.Ordinal)),
            Arg.Is<string?>(sha => sha == null),
            Arg.Any<string>(),
            cancellationToken);
    }

    [Fact]
    public async Task ApplyTemplateAsync_CustomTemplateWithoutPlaceholders_WritesYamlAsIs()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        const string yamlContent = "name: Deploy\njobs:\n  deploy:\n    runs-on: ubuntu-latest";

        _workflowFileRepository
            .GetWorkflowFileAsync("source-owner", "template-repo", ".github/workflows/deploy.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/deploy.yml",
                Content = yamlContent,
                Sha = "sha-1",
            });

        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/deploy.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(CustomTemplateId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("Created", result[0].Action);
        await _workflowFileRepository.Received(1).CreateOrUpdateWorkflowFileAsync(
            Arg.Is("owner"),
            Arg.Is("repo-a"),
            Arg.Is(".github/workflows/deploy.yml"),
            Arg.Is(yamlContent),
            Arg.Is<string?>(sha => sha == null),
            Arg.Any<string>(),
            cancellationToken);
    }

    [Fact]
    public async Task ApplyTemplateAsync_MatchingWorkflowFile_SkipsRepository()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var canonicalYaml = await GetRenderedCiYamlAsync();

        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = canonicalYaml,
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(BuiltInCiId, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("Skipped", result[0].Action);
        await _workflowFileRepository.DidNotReceive().CreateOrUpdateWorkflowFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            cancellationToken);
    }

    [Fact]
    public async Task ApplyTemplateAsync_CustomParameterValues_RenderInWorkflowContent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        await sut.ApplyTemplateAsync(
            BuiltInCiId,
            ["owner/repo-a"],
            new Dictionary<string, string>
            {
                ["mainBranch"] = "develop",
                ["dotnetVersion"] = "9.0.x",
            }, cancellationToken);

        // Assert
        await _workflowFileRepository.Received(1).CreateOrUpdateWorkflowFileAsync(
            Arg.Is("owner"),
            Arg.Is("repo-a"),
            Arg.Is(".github/workflows/ci.yml"),
            Arg.Is<string>(content => content!.Contains("- develop", StringComparison.Ordinal) && content.Contains("9.0.x", StringComparison.Ordinal)),
            Arg.Is<string?>(sha => sha == null),
            Arg.Any<string>(),
            cancellationToken);
    }

    [Fact]
    public async Task ApplyTemplateAsync_OneRepositoryFails_ReturnsErrorAndContinues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", cancellationToken)
            .Returns((WorkflowFile?)null);

        _workflowFileRepository
            .GetWorkflowFileAsync("owner", "repo-b", ".github/workflows/ci.yml", cancellationToken)
            .Returns(Task.FromException<WorkflowFile?>(new HttpRequestException("Rate limited")));

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(BuiltInCiId, ["owner/repo-a", "owner/repo-b"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-a" && item.Action == "Created" && !item.HasError);
        Assert.Contains(result, item => item.RepositoryFullName == "owner/repo-b" && item.HasError && item.ErrorMessage == "Rate limited");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyTemplateAsync_EmptyRepositoryList_ThrowsArgumentException(string repository)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.ApplyTemplateAsync(BuiltInCiId, [repository], new Dictionary<string, string>(), cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_InvalidRepositoryFormat_ReturnsErrorResult()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(BuiltInCiId, ["invalid-repo"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].HasError);
        Assert.Contains("owner/repository format", result[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private ActionsTemplateService CreateSut()
        => new(_workflowFileRepository, _logger);

    private static async Task<string> GetRenderedCiYamlAsync()
    {
        var sut = new ActionsTemplateService(
            Substitute.For<IWorkflowFileRepository>(),
            Substitute.For<ILogger<ActionsTemplateService>>());
        var detail = await sut.GetTemplateDetailAsync(BuiltInCiId, TestContext.Current.CancellationToken);
        return detail.YamlPreview;
    }
}
