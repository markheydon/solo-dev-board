using MudBlazor;

namespace SoloDevBoard.Web.Theming;

/// <summary>Shared Material icon names for shell and toolbar chrome.</summary>
public static class AppIcons
{
    /// <summary>Navigation drawer and app-bar chrome.</summary>
    public const string Menu = Icons.Material.Filled.Menu;

    /// <summary>Theme preference toggle (see <see cref="ThemePreferenceExtensions.GetButtonIcon"/>).</summary>
    public const string ThemeAutomatic = Icons.Material.Rounded.AutoMode;

    /// <summary>Theme preference toggle in light mode.</summary>
    public const string ThemeLight = Icons.Material.Outlined.LightMode;

    /// <summary>Theme preference toggle in dark mode.</summary>
    public const string ThemeDark = Icons.Material.Outlined.DarkMode;

    /// <summary>Filter and search fields in toolbars.</summary>
    public const string Search = Icons.Material.Filled.Search;

    /// <summary>Overflow menus on narrow viewports.</summary>
    public const string OverflowVertical = Icons.Material.Filled.MoreVert;

    /// <summary>Row-level overflow actions.</summary>
    public const string OverflowHorizontal = Icons.Material.Filled.MoreHoriz;

    /// <summary>Inline warning affordances.</summary>
    public const string Warning = Icons.Material.Filled.Warning;

    /// <summary>Warning severity in setup and summary panels.</summary>
    public const string WarningAmber = Icons.Material.Filled.WarningAmber;
}
