using SoloDevBoard.Domain.Entities.BoardRules;

namespace SoloDevBoard.Domain.Tests;

public sealed class BoardRulesDefinitionTests
{
    [Fact]
    public void BoardRulesDefinition_WithUnsupportedDetails_ShouldReportPartialVisibility()
    {
        // Arrange & Act
        var definition = new BoardRulesDefinition
        {
            ProjectId = "PVT_123",
            ProjectTitle = "Roadmap",
            OwnerLogin = "owner",
            Columns =
            [
                new BoardColumn { Id = 1, Name = "Todo", Order = 0, LabelFilters = ["type/bug"] },
            ],
            Rules =
            [
                new BoardRule
                {
                    Id = 1,
                    Name = "Auto-close",
                    Trigger = "item.closed",
                    Action = "move_to_done",
                    IsEnabled = true,
                },
            ],
            UnsupportedDetails = ["Automation rules are partially visible."],
        };

        // Assert
        Assert.Equal("PVT_123", definition.ProjectId);
        Assert.Equal("Roadmap", definition.ProjectTitle);
        Assert.Equal("owner", definition.OwnerLogin);
        Assert.Single(definition.Columns);
        Assert.Equal("Todo", definition.Columns[0].Name);
        Assert.Equal(0, definition.Columns[0].Order);
        Assert.Equal(["type/bug"], definition.Columns[0].LabelFilters);
        Assert.Single(definition.Rules);
        Assert.Equal("Auto-close", definition.Rules[0].Name);
        Assert.Equal("item.closed", definition.Rules[0].Trigger);
        Assert.Equal("move_to_done", definition.Rules[0].Action);
        Assert.True(definition.Rules[0].IsEnabled);
        Assert.False(definition.HasFullVisibility);
    }

    [Fact]
    public void BoardRulesDefinition_WithoutUnsupportedDetails_ShouldReportFullVisibility()
    {
        // Arrange & Act
        var definition = new BoardRulesDefinition
        {
            ProjectId = "PVT_456",
            ProjectTitle = "Delivery",
            OwnerLogin = "owner",
        };

        // Assert
        Assert.True(definition.HasFullVisibility);
    }
}
