(function () {
    var config = window.soloDevBoardThemePreference;
    if (!config || typeof window.soloDevBoardApplyDocumentTheme !== 'function') {
        return;
    }

    var preference = 'system';

    try {
        preference = localStorage.getItem(config.storageKey) || 'system';
    } catch {
        preference = 'system';
    }

    var isDark = preference === 'dark'
        || (preference === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);

    window.soloDevBoardApplyDocumentTheme(isDark);

    if (!document.body) {
        document.addEventListener('DOMContentLoaded', function () {
            window.soloDevBoardApplyDocumentTheme(isDark);
        });
    }
})();
