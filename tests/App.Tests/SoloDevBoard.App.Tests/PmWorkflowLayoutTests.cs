using SoloDevBoard.App.Components.Features.PmWorkflow;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="PmWorkflowLayout"/> path helpers.</summary>
public sealed class PmWorkflowLayoutTests
{
    [Theory]
    [InlineData("https://localhost/pm-workflow/daily-focus", true)]
    [InlineData("https://localhost/pm-workflow/repos", true)]
    [InlineData("https://localhost/pm-workflow", true)]
    [InlineData("https://localhost/", false)]
    [InlineData("https://localhost/audit-dashboard", false)]
    public void IsPmWorkflowPath_Uri_ExpectedOutcome(string uri, bool expected) =>
        Assert.Equal(expected, PmWorkflowLayout.IsPmWorkflowPath(uri));
}
