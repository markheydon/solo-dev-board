using SoloDevBoard.App.Planning;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="PlanningSettingsConstants"/>.</summary>
public sealed class PlanningSettingsConstantsTests
{
    [Fact]
    public void JavaScriptConstants_MatchPlanningSettingsConstants()
    {
        var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var repositoryRoot = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", "..", ".."));
        var constantsPath = Path.Combine(
            repositoryRoot,
            "src", "App", "SoloDevBoard.App", "wwwroot", "js", "planningSettingsConstants.js");
        var constantsSource = File.ReadAllText(constantsPath);

        Assert.Contains($"storageKey: '{PlanningSettingsConstants.StorageKey}'", constantsSource, StringComparison.Ordinal);
    }
}
