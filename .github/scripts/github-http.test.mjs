import test from 'node:test';
import assert from 'node:assert/strict';
import {
    delayMsForAttempt,
    extractGitHubErrorMessage,
    githubJsonRequest,
    isTransientGitHubLimit,
} from './github-http.mjs';

test('isTransientGitHubLimit_SecondaryRateLimit403_IsTransient', () => {
    const result = isTransientGitHubLimit(403, '{"message":"You have exceeded a secondary rate limit."}');

    assert.equal(result, true);
});

test('isTransientGitHubLimit_TooManyRequests_IsTransient', () => {
    const result = isTransientGitHubLimit(429, '{"message":"API rate limit exceeded"}');

    assert.equal(result, true);
});

test('isTransientGitHubLimit_ForbiddenWithoutRateLimit_IsNotTransient', () => {
    const body = JSON.stringify({
        message: 'Resource not accessible by integration',
        documentation_url: 'https://docs.github.com/rest/overview/rate-limits-for-the-rest-api',
    });
    const result = isTransientGitHubLimit(403, body);

    assert.equal(result, false);
});

test('extractGitHubErrorMessage_InvalidJson_ReturnsBodyText', () => {
    const result = extractGitHubErrorMessage('plain text error');

    assert.equal(result, 'plain text error');
});

test('delayMsForAttempt_RetryAfterHeader_UsesSeconds', () => {
    const result = delayMsForAttempt(1, '7');

    assert.equal(result, 7000);
});

test('delayMsForAttempt_MissingHeader_UsesExponentialBackoff', () => {
    const result = delayMsForAttempt(3, null);

    assert.equal(result, 8000);
});

test('githubJsonRequest_SecondaryRateLimitThenSuccess_Retries', async () => {
    const originalFetch = globalThis.fetch;
    let calls = 0;

    globalThis.fetch = async () => {
        calls += 1;

        if (calls === 1) {
            return new Response(JSON.stringify({ message: 'You have exceeded a secondary rate limit.' }), {
                status: 403,
                headers: { 'retry-after': '0' },
            });
        }

        return new Response(JSON.stringify({ ok: true }), { status: 200 });
    };

    try {
        const result = await githubJsonRequest({
            url: 'https://api.github.com/test',
            token: 'test-token',
            userAgent: 'solo-dev-board-test',
        });

        assert.deepEqual(result, { ok: true });
        assert.equal(calls, 2);
    } finally {
        globalThis.fetch = originalFetch;
    }
});
