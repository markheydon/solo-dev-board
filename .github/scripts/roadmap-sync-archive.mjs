/**
 * Archive eligibility for SoloDevBoard Roadmap (Project #8).
 * Uses the issue close date, not the project-card updated timestamp.
 */

export const ARCHIVE_AFTER_CALENDAR_DAYS = 14;

/**
 * Returns true when a closed, non-duplicate issue has been closed long enough
 * to hide from the live Roadmap and Story Board views.
 * @param {{ state?: string | null, state_reason?: string | null, closed_at?: string | null, closedAt?: string | null }} issue
 * @param {string} runDate ISO date `YYYY-MM-DD`.
 * @returns {boolean}
 */
export function isIssueClosedLongEnoughToArchive(issue, runDate) {
    const state = (issue?.state ?? '').toString().toLowerCase();

    if (state !== 'closed') {
        return false;
    }

    if ((issue?.state_reason ?? '').toString().toLowerCase() === 'duplicate') {
        return false;
    }

    const closedAt = issue?.closed_at ?? issue?.closedAt ?? null;

    if (!closedAt) {
        return false;
    }

    const closedDate = closedAt.slice(0, 10);
    const eligibleFrom = addUtcCalendarDays(closedDate, ARCHIVE_AFTER_CALENDAR_DAYS);
    return eligibleFrom <= runDate;
}

/**
 * @param {string} startDate
 * @param {number} calendarDays
 * @returns {string}
 */
export function addUtcCalendarDays(startDate, calendarDays) {
    const date = new Date(`${startDate}T00:00:00.000Z`);
    date.setUTCDate(date.getUTCDate() + calendarDays);
    return date.toISOString().slice(0, 10);
}
