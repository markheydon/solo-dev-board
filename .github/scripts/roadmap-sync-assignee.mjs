/** Project #8 statuses that should assign the maintainer to the linked issue. */
export const issueAssignmentStatuses = new Set(['Up Next', 'In Progress']);

/**
 * Returns whether the maintainer should be assigned to an issue for the given roadmap status.
 *
 * @param {string | null | undefined} roadmapStatusName
 * @returns {boolean}
 */
export function shouldAssignMaintainerToIssue(roadmapStatusName) {
    return issueAssignmentStatuses.has(roadmapStatusName ?? '');
}

/**
 * Resolves whether Roadmap Sync should assign, unassign, or leave the maintainer unchanged.
 *
 * @param {readonly string[]} currentAssigneeLogins
 * @param {string | null | undefined} roadmapStatusName
 * @param {string} maintainerLogin
 * @returns {'assign' | 'unassign' | 'none'}
 */
export function resolveMaintainerAssignmentAction(currentAssigneeLogins, roadmapStatusName, maintainerLogin) {
    const shouldAssign = shouldAssignMaintainerToIssue(roadmapStatusName);
    const isAssigned = currentAssigneeLogins.includes(maintainerLogin);

    if (shouldAssign && !isAssigned) {
        return 'assign';
    }

    if (!shouldAssign && isAssigned) {
        return 'unassign';
    }

    return 'none';
}
