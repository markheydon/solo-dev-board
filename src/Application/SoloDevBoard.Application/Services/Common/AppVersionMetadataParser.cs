using System.Globalization;
using System.Reflection;

namespace SoloDevBoard.Application.Services.Common;

/// <summary>Parses version metadata from assembly attributes.</summary>
internal static class AppVersionMetadataParser
{
    private const string BuildTimestampMetadataKey = "BuildTimestampUtc";

    /// <summary>Parses version and build metadata from the supplied assembly.</summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>Parsed version metadata.</returns>
    public static AppVersionMetadata Parse(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var buildTimestampUtc = TryParseBuildTimestampUtc(assembly);

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var (version, buildMetadata) = SplitInformationalVersion(informationalVersion);
            return new AppVersionMetadata(version, buildMetadata, buildTimestampUtc);
        }

        var assemblyVersion = assembly.GetName().Version?.ToString();
        return string.IsNullOrWhiteSpace(assemblyVersion)
            ? AppVersionMetadata.Empty
            : new AppVersionMetadata(assemblyVersion, string.Empty, buildTimestampUtc);
    }

    private static (string Version, string BuildMetadata) SplitInformationalVersion(string informationalVersion)
    {
        var metadataSeparatorIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (metadataSeparatorIndex < 0)
        {
            return (informationalVersion, string.Empty);
        }

        var version = informationalVersion[..metadataSeparatorIndex];
        var buildMetadata = informationalVersion[(metadataSeparatorIndex + 1)..];
        return (version, buildMetadata);
    }

    private static DateTimeOffset? TryParseBuildTimestampUtc(Assembly assembly)
    {
        var buildTimestampValue = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(
                attribute.Key,
                BuildTimestampMetadataKey,
                StringComparison.Ordinal))?
            .Value;

        if (string.IsNullOrWhiteSpace(buildTimestampValue))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            buildTimestampValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var buildTimestampUtc)
            ? buildTimestampUtc
            : null;
    }
}
