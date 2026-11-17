/// <summary>
/// Azure resource naming for Aspire deploy tiers (DEC-021).
/// </summary>
internal sealed class AppHostAzureNaming
{
    private readonly string _environmentName;

    /// <summary>Initialises naming for the current Aspire environment.</summary>
    /// <param name="environmentName">Aspire environment name (for example Staging or Production).</param>
    public AppHostAzureNaming(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        _environmentName = environmentName;
    }

    /// <summary>
    /// Returns a tier-suffixed Azure resource name for Staging; Production keeps the original name.
    /// </summary>
    /// <param name="resourceName">Logical resource name.</param>
    public string AzureName(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return string.Equals(_environmentName, "Staging", StringComparison.OrdinalIgnoreCase)
            ? $"{resourceName}-staging"
            : resourceName;
    }

    /// <summary>Storage account logical name (alphanumeric only; no hyphen suffix on Staging).</summary>
    public string AzureStorageResourceName() =>
        string.Equals(_environmentName, "Staging", StringComparison.OrdinalIgnoreCase)
            ? "storagestaging"
            : "storage";
}
