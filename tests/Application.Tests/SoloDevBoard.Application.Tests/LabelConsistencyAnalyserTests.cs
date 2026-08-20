using SoloDevBoard.Application.Services.Audit;
using SoloDevBoard.Application.Services.Labels;
using SoloDevBoard.Domain.Entities.Labels;

namespace SoloDevBoard.Application.Tests;

/// <summary>Unit tests for <see cref="LabelConsistencyAnalyser"/>.</summary>
public sealed class LabelConsistencyAnalyserTests
{
    [Fact]
    public void Analyse_WhenLabelIsMissing_ReturnsMissingWarning()
    {
        var taxonomy = new[]
        {
            new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        };

        var warnings = LabelConsistencyAnalyser.Analyse("owner/repo", [], taxonomy);

        var warning = Assert.Single(warnings);
        Assert.Equal("owner/repo", warning.RepositoryFullName);
        Assert.Equal("type/bug", warning.LabelName);
        Assert.Equal(LabelConsistencyWarningKind.Missing, warning.Kind);
        Assert.Equal("Missing from the repository.", warning.Detail);
    }

    [Fact]
    public void Analyse_WhenColourDiffersIgnoringHashPrefix_DoesNotWarnWhenValuesMatch()
    {
        var taxonomy = new[]
        {
            new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        };
        var existing = new[]
        {
            new Label { Name = "type/bug", Colour = "#D73A4A", Description = "A bug or unexpected behaviour" },
        };

        var warnings = LabelConsistencyAnalyser.Analyse("owner/repo", existing, taxonomy);

        Assert.Empty(warnings);
    }

    [Fact]
    public void Analyse_WhenDescriptionDiffers_ReturnsDivergentWarning()
    {
        var taxonomy = new[]
        {
            new LabelDto("type/bug", "d73a4a", "A bug or unexpected behaviour", string.Empty),
        };
        var existing = new[]
        {
            new Label { Name = "type/bug", Colour = "d73a4a", Description = "Something else" },
        };

        var warnings = LabelConsistencyAnalyser.Analyse("owner/repo", existing, taxonomy);

        var warning = Assert.Single(warnings);
        Assert.Equal(LabelConsistencyWarningKind.Divergent, warning.Kind);
        Assert.Equal("Description differs from the taxonomy.", warning.Detail);
    }

    [Fact]
    public void Analyse_WhenExtraLabelsExist_IgnoresLabelsNotInTaxonomy()
    {
        var taxonomy = RecommendedLabelTaxonomyCatalog.GitHubDefault.Take(1).ToArray();
        var existing = new[]
        {
            new Label { Name = taxonomy[0].Name, Colour = taxonomy[0].Colour, Description = taxonomy[0].Description },
            new Label { Name = "custom/only-here", Colour = "000000", Description = "Extra" },
        };

        var warnings = LabelConsistencyAnalyser.Analyse("owner/repo", existing, taxonomy);

        Assert.Empty(warnings);
    }

    [Fact]
    public void Analyse_WhenRepositoryFullNameIsMissing_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => LabelConsistencyAnalyser.Analyse(" ", [], []));

        Assert.Equal("repositoryFullName", exception.ParamName);
    }
}
