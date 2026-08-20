import test from 'node:test';
import assert from 'node:assert/strict';
import {
    issueAssignmentStatuses,
    resolveMaintainerAssignmentAction,
    shouldAssignMaintainerToIssue,
} from './roadmap-sync-assignee.mjs';

test('shouldAssignMaintainerToIssue_UpNextAndInProgress_ReturnsTrue', () => {
    assert.equal(shouldAssignMaintainerToIssue('Up Next'), true);
    assert.equal(shouldAssignMaintainerToIssue('In Progress'), true);
});

test('shouldAssignMaintainerToIssue_OtherStatuses_ReturnsFalse', () => {
    assert.equal(shouldAssignMaintainerToIssue('Todo'), false);
    assert.equal(shouldAssignMaintainerToIssue('Blocked'), false);
    assert.equal(shouldAssignMaintainerToIssue('Done'), false);
});

test('resolveMaintainerAssignmentAction_UpNextWithoutAssignee_ReturnsAssign', () => {
    const result = resolveMaintainerAssignmentAction([], 'Up Next', 'markheydon');

    assert.equal(result, 'assign');
});

test('resolveMaintainerAssignmentAction_TodoWithAssignee_ReturnsUnassign', () => {
    const result = resolveMaintainerAssignmentAction(['markheydon'], 'Todo', 'markheydon');

    assert.equal(result, 'unassign');
});

test('resolveMaintainerAssignmentAction_InProgressWithAssignee_ReturnsNone', () => {
    const result = resolveMaintainerAssignmentAction(['markheydon'], 'In Progress', 'markheydon');

    assert.equal(result, 'none');
});

test('issueAssignmentStatuses_ContainsExpectedStatuses', () => {
    assert.deepEqual([...issueAssignmentStatuses].sort(), ['In Progress', 'Up Next']);
});
