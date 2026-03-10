namespace SoloDevBoard.Application.Services;

/// <summary>Provides application version metadata from a single source of truth.</summary>
public interface IAppVersionService
{
    /// <summary>Gets the current application version.</summary>
    string Version { get; }

    /// <summary>Gets the user-agent value for outbound HTTP requests.</summary>
    string UserAgent { get; }
}