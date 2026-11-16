using SoloDevBoard.Application.Services.OpenSource;

namespace SoloDevBoard.Application.Tests.OpenSource;

public sealed class FundingFileComparerTests
{
    [Fact]
    public void CompareUniformCrlfMatchesAfterNormalisation()
    {
        const string standard = "github: user\r\n";
        const string repository = "github: user\r\n";

        var result = FundingFileComparer.Compare(repository, standard);

        Assert.Equal(FundingCompareOutcome.Matches, result.Outcome);
    }

    [Fact]
    public void MixedLineEndingsDiffers()
    {
        const string standard = "github: user\n";
        const string repository = "github: user\r\nother: line\n";

        var result = FundingFileComparer.Compare(repository, standard);

        Assert.Equal(FundingCompareOutcome.Differs, result.Outcome);
    }

    [Fact]
    public void LeadingBomDiffers()
    {
        const string standard = "github: user\n";
        const string repository = "\uFEFFgithub: user\n";

        var result = FundingFileComparer.Compare(repository, standard);

        Assert.Equal(FundingCompareOutcome.Differs, result.Outcome);
    }
}