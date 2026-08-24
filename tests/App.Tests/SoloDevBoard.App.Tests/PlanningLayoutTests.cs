using SoloDevBoard.App.Components.Features.Planning;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="PlanningLayout"/> path helpers.</summary>
public sealed class PlanningLayoutTests
{
    [Theory]
    [InlineData("https://localhost/planning/daily-focus", true)]
    [InlineData("https://localhost/planning/backlog", true)]
    [InlineData("https://localhost/planning/repos", true)]
    [InlineData("https://localhost/planning", true)]
    [InlineData("https://localhost/", false)]
    [InlineData("https://localhost/audit-dashboard", false)]
    public void IsPlanningPath_Uri_ExpectedOutcome(string uri, bool expected) =>
        Assert.Equal(expected, PlanningLayout.IsPlanningPath(uri));
}
