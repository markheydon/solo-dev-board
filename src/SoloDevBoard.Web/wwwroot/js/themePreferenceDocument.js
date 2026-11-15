// Shared document-level theme styling for flash and runtime preference changes.
// Must stay in sync with ThemePreferenceConstants.cs palette backgrounds.
(function () {
    /**
     * Applies colour-scheme and page background for the resolved theme.
     * @param {boolean} isDark
     */
    function applyDocumentTheme(isDark) {
        var config = window.soloDevBoardThemePreference;
        if (!config) {
            return;
        }

        var background = isDark ? config.darkBackground : config.lightBackground;
        document.documentElement.style.colorScheme = isDark ? 'dark' : 'light';
        document.documentElement.style.backgroundColor = background;

        if (document.body) {
            document.body.style.backgroundColor = background;
        }
    }

    window.soloDevBoardApplyDocumentTheme = applyDocumentTheme;
})();
