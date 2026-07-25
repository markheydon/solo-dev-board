# MudBlazor Theming

MudBlazor uses a `MudTheme` object passed to `MudThemeProvider`.

---

## Basic Custom Theme

```razor
@* MainLayout.razor *@
<MudThemeProvider Theme="_theme" />

@code {
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976D2",
            PrimaryDarken = "#115293",
            PrimaryLighten = "#4791DB",
            Secondary = "#616161",
            AppbarBackground = "#1976D2",
            Background = "#F5F5F5",
            Surface = Colors.Shades.White,
            DrawerBackground = Colors.Shades.White,
            DrawerText = "rgba(0,0,0,0.87)",
            Success = "#388E3C",
            Error = "#D32F2F",
            Warning = "#F57C00",
            Info = "#1976D2"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90CAF9",
            AppbarBackground = "#1E1E2E"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"]
            }
        }
    };
}
```

---

## Dark Mode Toggle

```razor
<MudThemeProvider @bind-IsDarkMode="_isDark" Theme="_theme" />

@code {
    private bool _isDark;
}
```

---

## SoloDevBoard Three-Way Theme Preference

SoloDevBoard uses `AppThemeProvider` with `IThemePreferenceService` for **Automatic**, **Light**, and **Dark** modes.

- **Automatic (default):** `ObserveSystemDarkModeChange="true"` and no fixed `IsDarkMode` binding — MudBlazor follows `prefers-color-scheme`.
- **Light / Dark:** `ObserveSystemDarkModeChange="false"` with an explicit `IsDarkMode` value.
- Preference persists in `localStorage` via `themePreferenceConstants.js` (key: `solo-dev-board.theme-preference`, shared with `ThemePreferenceConstants.StorageKey` in C#).
- The shell app-bar button cycles Automatic → Light → Dark.

```razor
@* AppThemeProvider.razor *@
@if (ThemePreferenceService.ObserveSystemPreference)
{
    <MudThemeProvider Theme="@Theme" ObserveSystemDarkModeChange="true" />
}
else
{
    <MudThemeProvider Theme="@Theme"
                      ObserveSystemDarkModeChange="false"
                      IsDarkMode="@ThemePreferenceService.EffectiveIsDarkMode" />
}
```

`App.razor` loads `themePreferenceConstants.js` and `themePreferenceFlash.js` in the document head to set `color-scheme` and background before the Blazor circuit connects, reducing theme flash on load. Background colours are kept in sync with `SoloDevBoardTheme` via `ThemePreferenceConstants`.

---

## Using Theme Colours in Components

```razor
<MudButton Color="Color.Primary">Primary</MudButton>
<MudButton Color="Color.Secondary">Secondary</MudButton>
<MudButton Color="Color.Error">Delete</MudButton>
<MudButton Color="Color.Success">Confirm</MudButton>
```

---

Before reaching for CSS variables, prefer built-in component options such as `Color`, `Variant`, `Typo`, `Elevation`, `Square`, `Rounded`, `Dense`, and layout primitives plus utility classes.

---

## CSS Custom Properties

MudBlazor exposes theme colours as CSS custom properties:
- `var(--mud-palette-primary)`
- `var(--mud-palette-primary-text)`
- `var(--mud-palette-surface)`
- `var(--mud-palette-background)`

Use these in `.razor.css` files only for unavoidable custom styling that still needs to stay in sync with the theme.
