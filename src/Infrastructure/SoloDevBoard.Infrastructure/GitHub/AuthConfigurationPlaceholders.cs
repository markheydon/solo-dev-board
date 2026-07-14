namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Sentinel values used for inactive Aspire AppHost parameters and optional config.</summary>
public static class AuthConfigurationPlaceholders
{
    /// <summary>Legacy placeholder; prefer <see cref="NotUsed" /> for inactive mode parameters.</summary>
    public const string Disabled = "__disabled__";

    /// <summary>Marks an Aspire parameter as not in use for the current authentication mode.</summary>
    public const string NotUsed = "-";

    /// <summary>Returns <see langword="true" /> when <paramref name="value" /> is a real configuration value.</summary>
    public static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value.Trim(), Disabled, StringComparison.Ordinal)
        && !string.Equals(value.Trim(), NotUsed, StringComparison.Ordinal);
}
