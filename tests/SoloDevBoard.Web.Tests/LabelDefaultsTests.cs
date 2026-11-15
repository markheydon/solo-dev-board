using SoloDevBoard.Web.Components.Features.Labels;

namespace SoloDevBoard.Web.Tests;

/// <summary>Tests for <see cref="LabelDefaults"/>.</summary>
public sealed class LabelDefaultsTests
{
    [Fact]
    public void DefaultColourLiteral_ExistsOnlyInLabelDefaults()
    {
        var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var repositoryRoot = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", ".."));
        var webSrc = Path.Combine(repositoryRoot, "src", "SoloDevBoard.Web");
        var offenders = new List<string>();

        foreach (var path in Directory.EnumerateFiles(webSrc, "*.*", SearchOption.AllDirectories))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(path);
            if (!source.Contains("#ededed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.EndsWith("LabelDefaults.cs", StringComparison.Ordinal))
            {
                continue;
            }

            offenders.Add(Path.GetRelativePath(repositoryRoot, path));
        }

        Assert.Empty(offenders);
    }
}
