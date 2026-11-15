namespace SoloDevBoard.Application.Identity;

/// <summary>
/// Thrown when GitHub rejects the configured personal access token and the operator must
/// update PAT configuration before feature work can continue.
/// </summary>
public sealed class GitHubPatConnectivityRequiredException : Exception
{
    /// <summary>Initialises a new instance of the <see cref="GitHubPatConnectivityRequiredException"/> class.</summary>
    public GitHubPatConnectivityRequiredException()
    {
    }

    /// <summary>Initialises a new instance of the <see cref="GitHubPatConnectivityRequiredException"/> class with a message.</summary>
    /// <param name="message">The exception message.</param>
    public GitHubPatConnectivityRequiredException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance of the <see cref="GitHubPatConnectivityRequiredException"/> class with a message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GitHubPatConnectivityRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
