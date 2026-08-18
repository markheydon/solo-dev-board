const { storageKey } = window.soloDevBoardPmSettings;

/**
 * Returns the stored PM settings JSON payload.
 * @returns {string | null}
 */
export function getSettingsJson() {
    try {
        return localStorage.getItem(storageKey);
    } catch {
        // Ignore storage access failures.
        return null;
    }
}

/**
 * Persists the PM settings JSON payload.
 * @param {string} json
 */
export function setSettingsJson(json) {
    try {
        localStorage.setItem(storageKey, json);
    } catch {
        // Ignore storage access failures.
    }
}
