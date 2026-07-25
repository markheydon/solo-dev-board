const { storageKey } = window.soloDevBoardThemePreference;

/**
 * Returns the stored theme preference, defaulting to automatic mode.
 * @returns {'system' | 'light' | 'dark'}
 */
export function getPreference() {
    try {
        const value = localStorage.getItem(storageKey);
        if (value === 'light' || value === 'dark' || value === 'system') {
            return value;
        }
    } catch {
        // Ignore storage access failures and fall back to automatic mode.
    }

    return 'system';
}

/**
 * Persists the theme preference.
 * @param {'system' | 'light' | 'dark'} preference
 */
export function setPreference(preference) {
    try {
        localStorage.setItem(storageKey, preference);
    } catch {
        // Ignore storage access failures.
    }
}

/**
 * Returns whether the operating system currently prefers dark mode.
 * @returns {boolean}
 */
export function getSystemIsDarkMode() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}
