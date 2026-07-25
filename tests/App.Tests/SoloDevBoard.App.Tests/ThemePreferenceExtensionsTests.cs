using SoloDevBoard.App.Theming;

namespace SoloDevBoard.App.Tests;

/// <summary>Tests for <see cref="ThemePreferenceExtensions"/>.</summary>
public sealed class ThemePreferenceExtensionsTests
{
    [Theory]
    [InlineData(ThemePreference.System, ThemePreference.Light)]
    [InlineData(ThemePreference.Light, ThemePreference.Dark)]
    [InlineData(ThemePreference.Dark, ThemePreference.System)]
    public void GetNext_CyclesThroughAutomaticLightAndDark(ThemePreference current, ThemePreference expected)
    {
        Assert.Equal(expected, current.GetNext());
    }

    [Theory]
    [InlineData(ThemePreference.System, "Theme: automatic (follow system). Activate light mode.")]
    [InlineData(ThemePreference.Light, "Theme: light. Activate dark mode.")]
    [InlineData(ThemePreference.Dark, "Theme: dark. Activate automatic mode.")]
    public void GetButtonAriaLabel_ReturnsAccessibleNameForEachPreference(ThemePreference preference, string expectedLabel)
    {
        Assert.Equal(expectedLabel, preference.GetButtonAriaLabel());
    }

    [Theory]
    [InlineData("system", ThemePreference.System)]
    [InlineData("light", ThemePreference.Light)]
    [InlineData("dark", ThemePreference.Dark)]
    [InlineData(null, ThemePreference.System)]
    [InlineData("invalid", ThemePreference.System)]
    public void ParsePreference_ReturnsExpectedPreference(string? value, ThemePreference expected)
    {
        Assert.Equal(expected, ThemePreferenceExtensions.ParsePreference(value));
    }

    [Theory]
    [InlineData(ThemePreference.System, "system")]
    [InlineData(ThemePreference.Light, "light")]
    [InlineData(ThemePreference.Dark, "dark")]
    public void ToStorageValue_ReturnsLowercasePreferenceName(ThemePreference preference, string expected)
    {
        Assert.Equal(expected, preference.ToStorageValue());
    }
}
