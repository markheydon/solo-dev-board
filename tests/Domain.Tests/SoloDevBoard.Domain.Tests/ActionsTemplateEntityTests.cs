using SoloDevBoard.Domain.Entities.ActionsTemplates;
using SoloDevBoard.Domain.Entities.Workflows;

namespace SoloDevBoard.Domain.Tests;

public sealed class ActionsTemplateEntityTests
{
    [Fact]
    public void ActionsTemplate_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var template = new ActionsTemplate
        {
            Id = 5,
            Name = "CI",
            Description = "Continuous integration workflow.",
            Category = "Automation",
            Tags = ["ci", "dotnet"],
            WorkflowFilePath = ".github/workflows/ci.yml",
            TriggerDescription = "On push and pull request",
            YamlContent = "name: CI",
            CreatedAt = createdAt,
        };

        // Assert
        Assert.Equal(5, template.Id);
        Assert.Equal("CI", template.Name);
        Assert.Equal("Continuous integration workflow.", template.Description);
        Assert.Equal("Automation", template.Category);
        Assert.Equal(["ci", "dotnet"], template.Tags);
        Assert.Equal(".github/workflows/ci.yml", template.WorkflowFilePath);
        Assert.Equal("On push and pull request", template.TriggerDescription);
        Assert.Equal("name: CI", template.YamlContent);
        Assert.Equal(createdAt, template.CreatedAt);
    }

    [Fact]
    public void WorkflowFile_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var workflowFile = new WorkflowFile
        {
            Path = ".github/workflows/deploy.yml",
            Content = "name: Deploy",
            Sha = "abc123",
        };

        // Assert
        Assert.Equal(".github/workflows/deploy.yml", workflowFile.Path);
        Assert.Equal("name: Deploy", workflowFile.Content);
        Assert.Equal("abc123", workflowFile.Sha);
    }

    [Fact]
    public void WorkflowRun_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 5, 1, 9, 15, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 5, 1, 9, 20, 0, TimeSpan.Zero);

        // Act
        var workflowRun = new WorkflowRun
        {
            Id = 9001,
            WorkflowName = "CI",
            Status = "completed",
            Conclusion = "success",
            HeadBranch = "main",
            HeadSha = "deadbeef",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            HtmlUrl = "https://github.com/owner/repo/actions/runs/9001",
        };

        // Assert
        Assert.Equal(9001, workflowRun.Id);
        Assert.Equal("CI", workflowRun.WorkflowName);
        Assert.Equal("completed", workflowRun.Status);
        Assert.Equal("success", workflowRun.Conclusion);
        Assert.Equal("main", workflowRun.HeadBranch);
        Assert.Equal("deadbeef", workflowRun.HeadSha);
        Assert.Equal(createdAt, workflowRun.CreatedAt);
        Assert.Equal(updatedAt, workflowRun.UpdatedAt);
        Assert.Equal("https://github.com/owner/repo/actions/runs/9001", workflowRun.HtmlUrl);
    }

    [Fact]
    public void ActionsTemplateParameter_WithInitialisedProperties_ShouldReturnCorrectValues()
    {
        // Arrange & Act
        var parameter = new ActionsTemplateParameter
        {
            Name = "dotnetVersion",
            Label = ".NET version",
            Description = "SDK version used by the workflow.",
            DefaultValue = "10.0.x",
            IsRequired = true,
        };

        // Assert
        Assert.Equal("dotnetVersion", parameter.Name);
        Assert.Equal(".NET version", parameter.Label);
        Assert.Equal("SDK version used by the workflow.", parameter.Description);
        Assert.Equal("10.0.x", parameter.DefaultValue);
        Assert.True(parameter.IsRequired);
    }
}
