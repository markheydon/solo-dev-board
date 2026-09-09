using SoloDevBoard.Application.Services.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="LabelRemapSuggestionHelper"/>.</summary>
public sealed class LabelRemapSuggestionHelperTests
{
    private static readonly string[] SoloDevBoardNames = RecommendedLabelTaxonomyCatalog.SoloDevBoard
        .Select(label => label.Name)
        .ToArray();

    [Fact]
    public void SuggestDestination_WhenLeafMatchesTypePrefix_ReturnsTaxonomyName()
    {
        var destination = LabelRemapSuggestionHelper.SuggestDestination("story", SoloDevBoardNames);

        Assert.Equal("type/story", destination);
    }

    [Fact]
    public void SuggestDestination_WhenDocumentationDefault_ReturnsTypeDocumentation()
    {
        var destination = LabelRemapSuggestionHelper.SuggestDestination("documentation", SoloDevBoardNames);

        Assert.Equal("type/documentation", destination);
    }

    [Fact]
    public void SuggestDestination_WhenDependenciesDefault_ReturnsTypeChore()
    {
        var destination = LabelRemapSuggestionHelper.SuggestDestination("dependencies", SoloDevBoardNames);

        Assert.Equal("type/chore", destination);
    }

    [Fact]
    public void SuggestDestination_WhenAmbiguousEnhancement_ReturnsNull()
    {
        var destination = LabelRemapSuggestionHelper.SuggestDestination("enhancement", SoloDevBoardNames);

        Assert.Null(destination);
    }

    [Fact]
    public void BuildDestinationOptions_ExcludesSourceAndReturnsSortedDistinctNames()
    {
        var options = LabelRemapSuggestionHelper.BuildDestinationOptions(
            "story",
            ["type/story", "type/bug"],
            ["custom", "type/story"]);

        Assert.Equal(["custom", "type/bug", "type/story"], options);
        Assert.DoesNotContain("story", options);
    }

    [Fact]
    public void BuildDestinationOptions_ExcludesOutgoingLabelNames()
    {
        var options = LabelRemapSuggestionHelper.BuildDestinationOptions(
            "dependencies",
            ["type/bug", "type/story"],
            ["type/test", "status/ice-box"],
            ["blocked", "bug", "dependencies", "enhancement"]);

        Assert.Equal(["status/ice-box", "type/bug", "type/story", "type/test"], options);
        Assert.DoesNotContain("blocked", options);
        Assert.DoesNotContain("bug", options);
        Assert.DoesNotContain("dependencies", options);
        Assert.DoesNotContain("enhancement", options);
    }
}
