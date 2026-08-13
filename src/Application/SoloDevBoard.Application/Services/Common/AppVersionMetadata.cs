namespace SoloDevBoard.Application.Services.Common;

/// <summary>Parsed application version metadata from assembly attributes.</summary>
internal readonly record struct AppVersionMetadata(string Version, string BuildMetadata)
{
    /// <summary>Gets empty metadata when no version attributes are available.</summary>
    public static AppVersionMetadata Empty { get; } = new("unknown", string.Empty);
}
