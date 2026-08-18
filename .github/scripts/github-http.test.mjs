import test from 'node:test';
import assert from 'node:assert/strict';
import { delayMsForAttempt, isTransientGitHubLimit } from './github-http.mjs';

test('isTransientGitHubLimit_SecondaryRateLimit403_IsTransient', () => {
    const result = isTransientGitHubLimit(403, '{"message":"You have exceeded a secondary rate limit."}');

    assert.equal(result, true);
});

test('isTransientGitHubLimit_TooManyRequests_IsTransient', () => {
    const result = isTransientGitHubLimit(429, '{"message":"API rate limit exceeded"}');

    assert.equal(result, true);
});

test('isTransientGitHubLimit_ForbiddenWithoutRateLimit_IsNotTransient', () => {
    const result = isTransientGitHubLimit(403, '{"message":"Resource not accessible by integration"}');

    assert.equal(result, false);
});

test('delayMsForAttempt_RetryAfterHeader_UsesSeconds', () => {
    const result = delayMsForAttempt(1, '7');

    assert.equal(result, 7000);
});

test('delayMsForAttempt_MissingHeader_UsesExponentialBackoff', () => {
    const result = delayMsForAttempt(3, null);

    assert.equal(result, 8000);
});
