using System.Reflection;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Resolves application version metadata from assembly attributes.</summary>
public sealed class AppVersionService : IAppVersionService
{
    private const string ApplicationName = "SoloDevBoard";
    private readonly string _version;

    /// <summary>Initialises a new instance of the <see cref="AppVersionService"/> class.</summary>
    public AppVersionService()
    {
        _version = ResolveVersion();
    }

    /// <inheritdoc/>
    public string Version => _version;

    /// <inheritdoc/>
    public string UserAgent => $"{ApplicationName}/{Version}";

    private static string ResolveVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataSeparatorIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return metadataSeparatorIndex < 0
                ? informationalVersion
                : informationalVersion[..metadataSeparatorIndex];
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}