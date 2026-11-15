namespace SoloDevBoard.Application.Identity;

/// <summary>
/// Thrown when hosted sign-in credentials are missing, expired, or rejected by GitHub,
/// indicating that the user must sign in again.
/// </summary>
public sealed class HostedAuthenticationRequiredException : Exception
{
    /// <summary>Initialises a new instance of the <see cref="HostedAuthenticationRequiredException"/> class.</summary>
    public HostedAuthenticationRequiredException()
        : base("Hosted GitHub authentication is required. Sign in again to continue.")
    {
    }

    /// <summary>Initialises a new instance of the <see cref="HostedAuthenticationRequiredException"/> class with a message.</summary>
    /// <param name="message">The exception message.</param>
    public HostedAuthenticationRequiredException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance of the <see cref="HostedAuthenticationRequiredException"/> class with a message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public HostedAuthenticationRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
