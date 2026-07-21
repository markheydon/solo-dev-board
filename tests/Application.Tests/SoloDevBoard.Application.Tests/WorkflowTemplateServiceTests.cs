using NSubstitute;
using SoloDevBoard.Application.Services.Workflows;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="WorkflowTemplateService"/>.</summary>
public sealed class WorkflowTemplateServiceTests
{
    private readonly IWorkflowFileRepository _workflowFileRepository = Substitute.For<IWorkflowFileRepository>();

    [Fact]
    public void Constructor_WorkflowFileRepositoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IWorkflowFileRepository? workflowFileRepository = null;

        // Act
        var action = () => _ = new WorkflowTemplateService(workflowFileRepository!);

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
        var result = await sut.GetTemplatesAsync(cancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, template => template.Name == ".NET CI");
        Assert.Contains(result, template => template.Name == "Azure CD (Aspire)");
        Assert.Contains(result, template => template.Name == "Dependabot Auto-Merge");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplateMetadata()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync(cancellationToken);
        var ciTemplate = result.Single(template => template.Name == ".NET CI");

        // Assert
        Assert.Equal("CI", ciTemplate.Category);
        Assert.Equal(".github/workflows/ci.yml", ciTemplate.WorkflowFilePath);
        Assert.Contains("dotnet", ciTemplate.Tags);
        Assert.False(string.IsNullOrWhiteSpace(ciTemplate.TriggerDescription));
    }

    [Fact]
    public async Task GetTemplateDetailAsync_ReturnsParametersAndYamlPreview()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplateDetailAsync(1, cancellationToken);

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
        var action = () => sut.GetTemplateDetailAsync(999, cancellationToken);

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
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.NotApplied, result[0].Status);
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
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.Applied, result[0].Status);
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
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.Drifted, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_RequiredParameterMissing_ThrowsArgumentException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string> { ["mainBranch"] = "   " }, cancellationToken);

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
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

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
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a"], new Dictionary<string, string>(), cancellationToken);

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
            1,
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
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a", "owner/repo-b"], new Dictionary<string, string>(), cancellationToken);

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
        var action = () => sut.ApplyTemplateAsync(1, [repository], new Dictionary<string, string>(), cancellationToken);

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
        var result = await sut.ApplyTemplateAsync(1, ["invalid-repo"], new Dictionary<string, string>(), cancellationToken);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].HasError);
        Assert.Contains("owner/repository format", result[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private WorkflowTemplateService CreateSut()
        => new(_workflowFileRepository);

    private static async Task<string> GetRenderedCiYamlAsync()
    {
        var sut = new WorkflowTemplateService(Substitute.For<IWorkflowFileRepository>());
        var detail = await sut.GetTemplateDetailAsync(1, TestContext.Current.CancellationToken);
        return detail.YamlPreview;
    }
}
