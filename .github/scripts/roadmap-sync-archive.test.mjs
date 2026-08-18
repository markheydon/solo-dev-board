import test from 'node:test';
import assert from 'node:assert/strict';
import { ARCHIVE_AFTER_CALENDAR_DAYS, isIssueClosedLongEnoughToArchive } from './roadmap-sync-archive.mjs';

const runDate = '2026-08-18';

test('isIssueClosedLongEnoughToArchive_OpenIssue_DoesNotArchive', () => {
    const result = isIssueClosedLongEnoughToArchive({ state: 'open', closed_at: null }, runDate);

    assert.equal(result, false);
});

test('isIssueClosedLongEnoughToArchive_DuplicateClosure_DoesNotArchive', () => {
    const result = isIssueClosedLongEnoughToArchive(
        { state: 'closed', state_reason: 'duplicate', closed_at: '2026-03-07T18:00:00Z' },
        runDate);

    assert.equal(result, false);
});

test('isIssueClosedLongEnoughToArchive_ClosedInsideWindow_DoesNotArchive', () => {
    const result = isIssueClosedLongEnoughToArchive(
        { state: 'closed', closed_at: '2026-08-17T23:23:03Z' },
        runDate);

    assert.equal(result, false);
});

test('isIssueClosedLongEnoughToArchive_ClosedExactlyFourteenDaysAgo_Archives', () => {
    const result = isIssueClosedLongEnoughToArchive(
        { state: 'CLOSED', closedAt: '2026-08-04T10:00:00Z' },
        runDate);

    assert.equal(result, true);
    assert.equal(ARCHIVE_AFTER_CALENDAR_DAYS, 14);
});

test('isIssueClosedLongEnoughToArchive_FoundationIssueClosedInMarch_Archives', () => {
    const result = isIssueClosedLongEnoughToArchive(
        { state: 'closed', closed_at: '2026-03-07T18:53:28Z' },
        runDate);

    assert.equal(result, true);
});
