namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Stores the GitHub login resolved from a PAT when <see cref="GitHubAuthOptions.OwnerLogin" /> is not configured.</summary>
public sealed class ResolvedPatOwnerLogin
{
    /// <summary>Gets or sets the resolved owner login.</summary>
    public string? Value { get; set; }
}
