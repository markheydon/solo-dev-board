namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Configuration options for hosted admission control.
/// </summary>
public sealed class HostedAdmissionControlOptions
{
    public const string SectionName = "HostedAdmissionControl";

    /// <summary>
    /// Gets or sets a value indicating whether hosted admission control enforcement is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to enforce allow-list admission control for hosted requests; otherwise, <see langword="false" />.
    /// The default is <see langword="true" />.
    /// </value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the explicitly allowed GitHub user logins for hosted admission.
    /// </summary>
    /// <value>The operator-managed user allow-list.</value>
    public string[] AllowedUserLogins { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly allowed GitHub organisation logins for hosted admission.
    /// </summary>
    /// <value>The operator-managed organisation allow-list.</value>
    public string[] AllowedOrganisationLogins { get; set; } = [];

    /// <summary>
    /// Gets or sets the claim type used to read hosted GitHub organisation logins.
    /// </summary>
    /// <value>The hosted authentication claim type for organisation logins.</value>
    public string HostedOrganisationLoginsClaimType { get; set; } = HostedAuthClaimTypes.OrganisationLogins;
}