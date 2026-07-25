(function () {
    var config = window.soloDevBoardThemePreference;
    if (!config) {
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
    var background = isDark ? config.darkBackground : config.lightBackground;

    document.documentElement.style.colorScheme = isDark ? 'dark' : 'light';
    document.documentElement.style.backgroundColor = background;
    document.addEventListener('DOMContentLoaded', function () {
        document.body.style.backgroundColor = background;
    });
})();
