using SoloDevBoard.App.PmWorkflow;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="PmSettingsConstants"/>.</summary>
public sealed class PmSettingsConstantsTests
{
    [Fact]
    public void JavaScriptConstants_MatchPmSettingsConstants()
    {
        var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var repositoryRoot = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", "..", ".."));
        var constantsPath = Path.Combine(
            repositoryRoot,
            "src", "App", "SoloDevBoard.App", "wwwroot", "js", "pmSettingsConstants.js");
        var constantsSource = File.ReadAllText(constantsPath);

        Assert.Contains($"storageKey: '{PmSettingsConstants.StorageKey}'", constantsSource, StringComparison.Ordinal);
    }
}
