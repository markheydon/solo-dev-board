/**
 * Shared GitHub HTTP helpers with retry on secondary rate limits.
 */

export const githubMaxAttempts = 6;

/**
 * @param {string} bodyText
 * @returns {string}
 */
export function extractGitHubErrorMessage(bodyText) {
    try {
        const payload = JSON.parse(bodyText);

        if (typeof payload.message === 'string') {
            return payload.message;
        }
    } catch {
        // Response body is not JSON.
    }

    return bodyText;
}

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

    const message = extractGitHubErrorMessage(bodyText);

    return /secondary rate limit/i.test(message) || /rate limit exceeded/i.test(message);
}

/**
 * @param {string} errorText
 * @returns {boolean}
 */
export function isTransientGitHubGraphQlError(errorText) {
    return /RATE_LIMITED|rate limit/i.test(errorText);
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
 * @param {{ status: number, errorText: string, retryAfter: string | null, graphqlRateLimited?: boolean }} outcome
 * @param {number} attempt
 * @returns {boolean}
 */
export function shouldRetryGitHubRequest(outcome, attempt) {
    if (attempt >= githubMaxAttempts) {
        return false;
    }

    if (outcome.graphqlRateLimited) {
        return true;
    }

    return isTransientGitHubLimit(outcome.status, outcome.errorText);
}

/**
 * @param {(attempt: number) => Promise<{ success: true, value: unknown } | { success: false, status: number, errorText: string, retryAfter: string | null, graphqlRateLimited?: boolean, error: Error }>} executeOnce
 * @param {string} label
 * @returns {Promise<unknown>}
 */
export async function withGitHubRetry(executeOnce, label) {
    for (let attempt = 1; attempt <= githubMaxAttempts; attempt += 1) {
        const outcome = await executeOnce(attempt);

        if (outcome.success) {
            return outcome.value;
        }

        if (shouldRetryGitHubRequest(outcome, attempt)) {
            const delayMs = delayMsForAttempt(attempt, outcome.retryAfter);
            console.warn(
                `GitHub rate limit on ${label}; waiting ${delayMs}ms (attempt ${attempt}/${githubMaxAttempts}).`,
            );
            await sleep(delayMs);
            continue;
        }

        throw outcome.error;
    }

    throw new Error(`GitHub request failed for ${label} after ${githubMaxAttempts} attempts.`);
}

/**
 * @param {{ url: string, token: string, userAgent: string, method?: string, body?: string, apiVersion?: string }} options
 * @returns {Promise<unknown>}
 */
export async function githubJsonRequest(options) {
    const method = options.method ?? 'GET';

    return withGitHubRetry(async () => {
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
            return { success: true, value: null };
        }

        const text = await response.text();

        if (response.ok) {
            return { success: true, value: text ? JSON.parse(text) : null };
        }

        return {
            success: false,
            status: response.status,
            errorText: text,
            retryAfter: response.headers.get('retry-after'),
            error: new Error(`REST request failed (${response.status}) for ${options.url}: ${text}`),
        };
    }, `${method} ${options.url}`);
}
