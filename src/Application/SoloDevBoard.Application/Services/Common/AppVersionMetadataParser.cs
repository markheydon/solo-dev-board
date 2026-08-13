using System.Reflection;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Parses version metadata from assembly attributes.</summary>
internal static class AppVersionMetadataParser
{
    /// <summary>Parses version and build metadata from the supplied assembly.</summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>Parsed version metadata.</returns>
    public static AppVersionMetadata Parse(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return ParseInformationalVersion(informationalVersion);
        }

        var assemblyVersion = assembly.GetName().Version?.ToString();
        return string.IsNullOrWhiteSpace(assemblyVersion)
            ? AppVersionMetadata.Empty
            : new AppVersionMetadata(assemblyVersion, string.Empty);
    }

    private static AppVersionMetadata ParseInformationalVersion(string informationalVersion)
    {
        var metadataSeparatorIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (metadataSeparatorIndex < 0)
        {
            return new AppVersionMetadata(informationalVersion, string.Empty);
        }

        var version = informationalVersion[..metadataSeparatorIndex];
        var buildMetadata = informationalVersion[(metadataSeparatorIndex + 1)..];
        return new AppVersionMetadata(version, buildMetadata);
    }
}
