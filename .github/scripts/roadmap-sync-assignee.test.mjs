import test from 'node:test';
import assert from 'node:assert/strict';
import {
    issueAssignmentStatuses,
    resolveMaintainerAssignmentAction,
    shouldAssignMaintainerToIssue,
} from './roadmap-sync-assignee.mjs';

const maintainer = 'markheydon';

test('issueAssignmentStatuses_ContainsOnlyUpNextAndInProgress', () => {
    assert.deepEqual([...issueAssignmentStatuses].sort(), ['In Progress', 'Up Next']);
});

test('shouldAssignMaintainerToIssue_UpNext_ReturnsTrue', () => {
    assert.equal(shouldAssignMaintainerToIssue('Up Next'), true);
});

test('shouldAssignMaintainerToIssue_InProgress_ReturnsTrue', () => {
    assert.equal(shouldAssignMaintainerToIssue('In Progress'), true);
});

test('shouldAssignMaintainerToIssue_Todo_ReturnsFalse', () => {
    assert.equal(shouldAssignMaintainerToIssue('Todo'), false);
});

test('shouldAssignMaintainerToIssue_IceBox_ReturnsFalse', () => {
    assert.equal(shouldAssignMaintainerToIssue('Ice Box'), false);
});

test('shouldAssignMaintainerToIssue_Blocked_ReturnsFalse', () => {
    assert.equal(shouldAssignMaintainerToIssue('Blocked'), false);
});

test('shouldAssignMaintainerToIssue_Done_ReturnsFalse', () => {
    assert.equal(shouldAssignMaintainerToIssue('Done'), false);
});

test('resolveMaintainerAssignmentAction_UpNextWithoutAssignee_ReturnsAssign', () => {
    const action = resolveMaintainerAssignmentAction([], 'Up Next', maintainer);

    assert.equal(action, 'assign');
});

test('resolveMaintainerAssignmentAction_InProgressWithAssignee_ReturnsNone', () => {
    const action = resolveMaintainerAssignmentAction([maintainer], 'In Progress', maintainer);

    assert.equal(action, 'none');
});

test('resolveMaintainerAssignmentAction_TodoWithAssignee_ReturnsUnassign', () => {
    const action = resolveMaintainerAssignmentAction([maintainer], 'Todo', maintainer);

    assert.equal(action, 'unassign');
});

test('resolveMaintainerAssignmentAction_IceBoxWithAssignee_ReturnsUnassign', () => {
    const action = resolveMaintainerAssignmentAction([maintainer], 'Ice Box', maintainer);

    assert.equal(action, 'unassign');
});

test('resolveMaintainerAssignmentAction_TodoWithoutAssignee_ReturnsNone', () => {
    const action = resolveMaintainerAssignmentAction([], 'Todo', maintainer);

    assert.equal(action, 'none');
});
