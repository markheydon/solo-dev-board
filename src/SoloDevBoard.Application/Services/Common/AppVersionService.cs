using System.Reflection;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Resolves application version metadata from assembly attributes.</summary>
public sealed class AppVersionService : IAppVersionService
{
    private const string ApplicationName = "SoloDevBoard";

    /// <summary>Initialises a new instance of the <see cref="AppVersionService"/> class.</summary>
    public AppVersionService()
    {
        var metadata = ResolveMetadata();
        Version = metadata.Version;
        BuildMetadata = metadata.BuildMetadata;
        BuiltAtDisplay = AppVersionBuiltAtFormatter.FormatDisplay(metadata.Version, metadata.BuildTimestampUtc);
    }

    /// <inheritdoc/>
    public string Version { get; }

    /// <inheritdoc/>
    public string BuildMetadata { get; }

    /// <inheritdoc/>
    public string BuiltAtDisplay { get; }

    /// <inheritdoc/>
    public string UserAgent => $"{ApplicationName}/{Version}";

    private static AppVersionMetadata ResolveMetadata()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return AppVersionMetadataParser.Parse(assembly);
    }
}
