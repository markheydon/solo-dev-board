using Microsoft.AspNetCore.Components;
using SoloDevBoard.App.Authentication;
using SoloDevBoard.Application.Services.Common;

namespace SoloDevBoard.App.Components.Features.About.Pages;

/// <summary>Displays application and runtime version information.</summary>
public partial class About : ComponentBase
{
    private const string RepositoryAddress = "https://github.com/markheydon/solo-dev-board";
    private const string ProductName = "SoloDevBoard";

    private GitHubAuthenticationSummary? _authenticationSummary;

    /// <summary>Gets or sets the service that exposes application version metadata.</summary>
    [Inject]
    public IAppVersionService AppVersionService { get; set; } = default!;

    /// <summary>Gets or sets the GitHub authentication summary service.</summary>
    [Inject]
    public IGitHubAuthenticationSummaryService AuthenticationSummaryService { get; set; } = default!;

    private string ApplicationName => ProductName;

    private string Version => AppVersionService.Version;

    private string DotNetRuntimeVersion => Environment.Version.ToString();

    private string RepositoryUrl => RepositoryAddress;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _authenticationSummary = await AuthenticationSummaryService.GetSummaryAsync().ConfigureAwait(false);
    }

    private static string FormatGitHubLogin(string? login) =>
        string.IsNullOrWhiteSpace(login) ? "Not available" : $"@{login}";
}
