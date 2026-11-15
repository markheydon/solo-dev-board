namespace SoloDevBoard.Application.Services.Common;

/// <summary>Provides application version metadata from a single source of truth.</summary>
public interface IAppVersionService
{
    /// <summary>Gets the current application version.</summary>
    string Version { get; }

    /// <summary>Gets build metadata from the assembly informational version, such as a commit SHA.</summary>
    string BuildMetadata { get; }

    /// <summary>
    /// Gets a UK-localised build timestamp for pre-release versions, or an empty string for production releases.
    /// </summary>
    string BuiltAtDisplay { get; }

    /// <summary>Gets the user-agent value for outbound HTTP requests.</summary>
    string UserAgent { get; }
}
