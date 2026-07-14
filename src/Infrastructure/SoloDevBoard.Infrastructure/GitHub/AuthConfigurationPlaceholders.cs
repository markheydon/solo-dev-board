namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Sentinel values used for inactive Aspire AppHost parameters and optional config.</summary>
public static class AuthConfigurationPlaceholders
{
    /// <summary>Placeholder indicating a configuration value is intentionally unset.</summary>
    public const string Disabled = "__disabled__";

    /// <summary>Returns <see langword="true" /> when <paramref name="value" /> is a usable configuration value.</summary>
    public static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value.Trim(), Disabled, StringComparison.Ordinal);
}
