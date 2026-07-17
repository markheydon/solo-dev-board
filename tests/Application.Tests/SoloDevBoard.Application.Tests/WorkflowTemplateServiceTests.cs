using SoloDevBoard.Application.Services.Workflows;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="WorkflowTemplateService"/>.</summary>
public sealed class WorkflowTemplateServiceTests
{
    [Fact]
    public async Task GetTemplatesAsync_ReturnsBuiltInTemplates()
    {
        // Arrange
        var sut = new WorkflowTemplateService();

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
        var sut = new WorkflowTemplateService();

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
    public async Task ApplyTemplateAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var sut = new WorkflowTemplateService();

        // Act
        var action = () => sut.ApplyTemplateAsync("owner", "repo", 2);

        // Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(action);
        Assert.Contains("not yet implemented", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyTemplateAsync_OwnerIsInvalid_ThrowsArgumentException(string owner)
    {
        // Arrange
        var sut = new WorkflowTemplateService();

        // Act
        var action = () => sut.ApplyTemplateAsync(owner, "repo", 1);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_OwnerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new WorkflowTemplateService();

        // Act
        var action = () => sut.ApplyTemplateAsync(null!, "repo", 1);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyTemplateAsync_RepoIsInvalid_ThrowsArgumentException(string repo)
    {
        // Arrange
        var sut = new WorkflowTemplateService();

        // Act
        var action = () => sut.ApplyTemplateAsync("owner", repo, 1);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task ApplyTemplateAsync_RepoIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new WorkflowTemplateService();

        // Act
        var action = () => sut.ApplyTemplateAsync("owner", null!, 1);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }
}
