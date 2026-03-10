using Microsoft.AspNetCore.Components;
using SoloDevBoard.Application.Services;

namespace SoloDevBoard.App.Components.Pages;

/// <summary>Displays application and runtime version information.</summary>
public partial class About : ComponentBase
{
    private const string RepositoryAddress = "https://github.com/markheydon/solo-dev-board";
    private const string ProductName = "SoloDevBoard";

    /// <summary>Gets or sets the service that exposes application version metadata.</summary>
    [Inject]
    public IAppVersionService AppVersionService { get; set; } = default!;

    private string ApplicationName => ProductName;

    private string Version => AppVersionService.Version;

    private string DotNetRuntimeVersion => Environment.Version.ToString();

    private string RepositoryUrl => RepositoryAddress;
}