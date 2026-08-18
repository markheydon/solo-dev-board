/**
 * Shared GitHub HTTP helpers with retry on secondary rate limits.
 */

export const githubMaxAttempts = 6;

/**
 * @param {number} status
 * @param {string} bodyText
 * @returns {boolean}
 */
export function isTransientGitHubLimit(status, bodyText) {
    if (status === 429) {
        return true;
    }

    if (status !== 403) {
        return false;
    }

    return /secondary rate limit|rate limit/i.test(bodyText);
}

/**
 * @param {number} attempt 1-based attempt that just failed
 * @param {string | null} retryAfterHeader
 * @returns {number}
 */
export function delayMsForAttempt(attempt, retryAfterHeader) {
    const retryAfterSeconds = Number(retryAfterHeader);

    if (Number.isFinite(retryAfterSeconds) && retryAfterSeconds > 0) {
        return retryAfterSeconds * 1000;
    }

    return Math.min(32_000, 1000 * (2 ** attempt));
}

/**
 * @param {number} milliseconds
 * @returns {Promise<void>}
 */
export function sleep(milliseconds) {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

/**
 * @param {{ url: string, token: string, userAgent: string, method?: string, body?: string, apiVersion?: string }} options
 * @returns {Promise<unknown>}
 */
export async function githubJsonRequest(options) {
    const method = options.method ?? 'GET';

    for (let attempt = 1; attempt <= githubMaxAttempts; attempt += 1) {
        const response = await fetch(options.url, {
            method,
            headers: {
                Authorization: `Bearer ${options.token}`,
                Accept: 'application/vnd.github+json',
                'User-Agent': options.userAgent,
                ...(options.apiVersion ? { 'X-GitHub-Api-Version': options.apiVersion } : {}),
                ...(options.body ? { 'Content-Type': 'application/json' } : {}),
            },
            body: options.body,
        });

        if (response.status === 204) {
            return null;
        }

        const text = await response.text();

        if (response.ok) {
            return text ? JSON.parse(text) : null;
        }

        if (isTransientGitHubLimit(response.status, text) && attempt < githubMaxAttempts) {
            const delayMs = delayMsForAttempt(attempt, response.headers.get('retry-after'));
            console.warn(
                `GitHub rate limit on ${method} ${options.url} (HTTP ${response.status}); waiting ${delayMs}ms (attempt ${attempt}/${githubMaxAttempts}).`,
            );
            await sleep(delayMs);
            continue;
        }

        throw new Error(`REST request failed (${response.status}) for ${options.url}: ${text}`);
    }

    throw new Error(`REST request failed for ${options.url} after ${githubMaxAttempts} attempts.`);
}
