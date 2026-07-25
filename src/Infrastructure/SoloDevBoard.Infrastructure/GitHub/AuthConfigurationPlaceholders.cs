namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Sentinel values used for inactive Aspire AppHost parameters and optional config.</summary>
public static class AuthConfigurationPlaceholders
{
    /// <summary>Legacy placeholder; prefer <see cref="NotUsed" /> for inactive mode parameters.</summary>
    public const string Disabled = "__disabled__";

    /// <summary>Marks an Aspire parameter as not in use for the current authentication mode.</summary>
    public const string NotUsed = "-";

    /// <summary>CI placeholder personal access token used by Playwright end-to-end tests.</summary>
    public const string CiE2ePlaceholder = "ci-e2e-placeholder";

    /// <summary>Local placeholder personal access token used by Playwright end-to-end tests.</summary>
    public const string LocalE2ePlaceholder = "local-e2e-placeholder";

    /// <summary>Returns <see langword="true" /> when <paramref name="value" /> is a real configuration value.</summary>
    public static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value.Trim(), Disabled, StringComparison.Ordinal)
        && !string.Equals(value.Trim(), NotUsed, StringComparison.Ordinal);

    /// <summary>Returns <see langword="true" /> when <paramref name="value" /> is a known E2E placeholder token.</summary>
    public static bool IsE2ePlaceholder(string? value) =>
        string.Equals(value?.Trim(), CiE2ePlaceholder, StringComparison.Ordinal)
        || string.Equals(value?.Trim(), LocalE2ePlaceholder, StringComparison.Ordinal);

    /// <summary>Returns <see langword="true" /> when a real GitHub PAT connectivity probe should run.</summary>
    public static bool RequiresPatConnectivityProbe(string? personalAccessToken) =>
        IsConfigured(personalAccessToken) && !IsE2ePlaceholder(personalAccessToken);
}
