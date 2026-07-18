using Moq;
using SoloDevBoard.Application.Services.Workflows;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="WorkflowTemplateService"/>.</summary>
public sealed class WorkflowTemplateServiceTests
{
    private readonly Mock<IWorkflowFileRepository> _workflowFileRepositoryMock = new();

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
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, template => template.Name == ".NET CI");
        Assert.Contains(result, template => template.Name == "Azure CD (Aspire)");
        Assert.Contains(result, template => template.Name == "Dependabot Auto-Merge");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplateMetadata()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplatesAsync();
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
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetTemplateDetailAsync(1);

        // Assert
        Assert.Equal(".NET CI", result.Name);
        Assert.Equal(2, result.Parameters.Count);
        Assert.Contains("main", result.YamlPreview, StringComparison.Ordinal);
        Assert.DoesNotContain("{{mainBranch}}", result.YamlPreview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetTemplateDetailAsync_UnknownTemplate_ThrowsKeyNotFoundException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.GetTemplateDetailAsync(999);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(action);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_MissingWorkflowFile_ReturnsNotAppliedStatus()
    {
        // Arrange
        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.NotApplied, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_MatchingWorkflowFile_ReturnsAppliedStatus()
    {
        // Arrange
        var canonicalYaml = await GetRenderedCiYamlAsync();

        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = canonicalYaml,
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.Applied, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_DifferentWorkflowFile_ReturnsDriftedStatus()
    {
        // Arrange
        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = "name: Custom CI",
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkflowTemplateApplicationStatus.Drifted, result[0].Status);
    }

    [Fact]
    public async Task GetRepositoryStatusesAsync_RequiredParameterMissing_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.GetRepositoryStatusesAsync(1, ["owner/repo-a"], new Dictionary<string, string> { ["mainBranch"] = "   " });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_MissingWorkflowFile_CreatesWorkflowFile()
    {
        // Arrange
        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.Equal("Created", result[0].Action);
        Assert.False(result[0].HasError);
        _workflowFileRepositoryMock.Verify(
            repository => repository.CreateOrUpdateWorkflowFileAsync(
                "owner",
                "repo-a",
                ".github/workflows/ci.yml",
                It.Is<string>(content => content.Contains("name: CI", StringComparison.Ordinal)),
                null,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyTemplateAsync_MatchingWorkflowFile_SkipsRepository()
    {
        // Arrange
        var canonicalYaml = await GetRenderedCiYamlAsync();

        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowFile
            {
                Path = ".github/workflows/ci.yml",
                Content = canonicalYaml,
                Sha = "abc123",
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.Equal("Skipped", result[0].Action);
        _workflowFileRepositoryMock.Verify(
            repository => repository.CreateOrUpdateWorkflowFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyTemplateAsync_CustomParameterValues_RenderInWorkflowContent()
    {
        // Arrange
        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowFile?)null);

        var sut = CreateSut();

        // Act
        await sut.ApplyTemplateAsync(
            1,
            ["owner/repo-a"],
            new Dictionary<string, string>
            {
                ["mainBranch"] = "develop",
                ["dotnetVersion"] = "9.0.x",
            });

        // Assert
        _workflowFileRepositoryMock.Verify(
            repository => repository.CreateOrUpdateWorkflowFileAsync(
                "owner",
                "repo-a",
                ".github/workflows/ci.yml",
                It.Is<string>(content => content.Contains("- develop", StringComparison.Ordinal) && content.Contains("9.0.x", StringComparison.Ordinal)),
                null,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyTemplateAsync_OneRepositoryFails_ReturnsErrorAndContinues()
    {
        // Arrange
        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-a", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowFile?)null);

        _workflowFileRepositoryMock
            .Setup(repository => repository.GetWorkflowFileAsync("owner", "repo-b", ".github/workflows/ci.yml", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Rate limited"));

        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(1, ["owner/repo-a", "owner/repo-b"], new Dictionary<string, string>());

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
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.ApplyTemplateAsync(1, [repository], new Dictionary<string, string>());

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_InvalidRepositoryFormat_ReturnsErrorResult()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ApplyTemplateAsync(1, ["invalid-repo"], new Dictionary<string, string>());

        // Assert
        Assert.Single(result);
        Assert.True(result[0].HasError);
        Assert.Contains("owner/repository format", result[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private WorkflowTemplateService CreateSut()
        => new(_workflowFileRepositoryMock.Object);

    private static async Task<string> GetRenderedCiYamlAsync()
    {
        var sut = new WorkflowTemplateService(new Mock<IWorkflowFileRepository>().Object);
        var detail = await sut.GetTemplateDetailAsync(1);
        return detail.YamlPreview;
    }
}
