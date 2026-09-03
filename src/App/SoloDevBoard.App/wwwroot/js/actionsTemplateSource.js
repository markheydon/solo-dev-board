const { storageKey } = window.soloDevBoardActionsTemplateSource;

/**
 * Returns the last-used custom template source repository.
 * @returns {string | null}
 */
export function getLastUsedSource() {
    try {
        return localStorage.getItem(storageKey);
    } catch {
        // Ignore storage access failures.
        return null;
    }
}

/**
 * Persists the last-used custom template source repository.
 * @param {string} repositoryFullName
 */
export function setLastUsedSource(repositoryFullName) {
    try {
        localStorage.setItem(storageKey, repositoryFullName);
    } catch {
        // Ignore storage access failures.
    }
}
