import { isIssueClosedLongEnoughToArchive } from './roadmap-sync-archive.mjs';
import { githubJsonRequest, isTransientGitHubGraphQlError, withGitHubRetry } from './github-http.mjs';

const apiBaseUrl = 'https://api.github.com';
const graphqlUrl = `${apiBaseUrl}/graphql`;
const owner = 'markheydon';
const repo = 'solo-dev-board';
const projectId = 'PVT_kwHOAJefG84BQ6bh';
const archivedStates = ['ARCHIVED', 'NOT_ARCHIVED'];

const fieldIds = {
    status: 'PVTSSF_lAHOAJefG84BQ6bhzg-5WGY',
    phase: 'PVTSSF_lAHOAJefG84BQ6bhzg-5WLw',
    priority: 'PVTSSF_lAHOAJefG84BQ6bhzg-5WMc',
    startDate: 'PVTF_lAHOAJefG84BQ6bhzg-5WQE',
    targetDate: 'PVTF_lAHOAJefG84BQ6bhzg-5WQw',
    focusOrder: 'PVTF_lAHOAJefG84BQ6bhzg_Lx34',
};

const statusOptions = {
    Todo: 'f75ad846',
    'Up Next': 'df9275ed',
    'In Progress': '47fc9ee4',
    Done: '98236657',
};

// Phase field is legacy (closed pre-1.0 releases). Post-1.0 milestones do not set Phase.
const legacyPhaseOptionsByMilestone = new Map([
    ['v0.1.0', '1fbac877'],
    ['v0.2.0', '0f90ba94'],
    ['v0.3.0', 'f3de38ba'],
    ['v0.4.0', 'f5bc6726'],
    ['v0.5.0', '495afaf1'],
    ['v1.0.0', 'dfa36cee'],
]);

const priorityOptionsByLabel = new Map([
    ['priority/critical', '8d63dbb3'],
    ['priority/high', 'e89555ab'],
    ['priority/medium', '90261711'],
    ['priority/low', '0f0afb94'],
]);

const roadmapStates = new Set(['Todo', 'Up Next', 'In Progress', 'Done']);
const token = process.env.ROADMAP_PROJECT_TOKEN;

if (!token) {
    throw new Error('ROADMAP_PROJECT_TOKEN is required.');
}

await main();

async function main() {
    const projectItems = await fetchProjectItemsAsync();
    await removeStrayPullRequestCardsAsync(projectItems.pullRequestItems);

    const repositoryIssues = await fetchRepositoryIssuesAsync();
    const timelineCache = new Map();

    for (const issue of repositoryIssues.issues) {
        await syncIssueAsync(issue, projectItems.issueItemsByContentId, timelineCache);
    }

    const refreshedProjectItems = await fetchProjectItemsAsync();
    await syncParentIssuesAsync(refreshedProjectItems.issueItemsByContentId, repositoryIssues.issueByNumber, timelineCache);

    console.log(`Roadmap sync complete for ${repositoryIssues.issues.length} issue(s).`);
}

function isProjectItemArchived(projectItem) {
    return projectItem?.isArchived === true;
}

async function fetchProjectItemsAsync() {
    const issueItemsByContentId = new Map();
    const pullRequestItems = [];
    let hasNextPage = true;
    let after = null;

    while (hasNextPage) {
        const response = await graphqlAsync(
            `
            query ProjectItems($projectId: ID!, $after: String, $archivedStates: [ProjectV2ItemArchivedState!]) {
              node(id: $projectId) {
                ... on ProjectV2 {
                  items(first: 100, after: $after, archivedStates: $archivedStates) {
                    pageInfo {
                      hasNextPage
                      endCursor
                    }
                    nodes {
                      id
                      isArchived
                      content {
                        __typename
                        ... on Issue {
                          id
                          number
                          state
                          createdAt
                          updatedAt
                          closedAt
                          milestone {
                            title
                          }
                          labels(first: 50) {
                            nodes {
                              name
                            }
                          }
                          trackedIssues(first: 50) {
                            nodes {
                              id
                              number
                              state
                              closedAt
                            }
                          }
                        }
                        ... on PullRequest {
                          id
                          number
                          url
                        }
                      }
                      status: fieldValueByName(name: "Status") {
                        ... on ProjectV2ItemFieldSingleSelectValue {
                          optionId
                          name
                        }
                      }
                      phase: fieldValueByName(name: "Phase") {
                        ... on ProjectV2ItemFieldSingleSelectValue {
                          optionId
                          name
                        }
                      }
                      priority: fieldValueByName(name: "Priority") {
                        ... on ProjectV2ItemFieldSingleSelectValue {
                          optionId
                          name
                        }
                      }
                      startDate: fieldValueByName(name: "Start Date") {
                        ... on ProjectV2ItemFieldDateValue {
                          date
                        }
                      }
                      targetDate: fieldValueByName(name: "Target Date") {
                        ... on ProjectV2ItemFieldDateValue {
                          date
                        }
                      }
                      focusOrder: fieldValueByName(name: "Focus Order") {
                        ... on ProjectV2ItemFieldNumberValue {
                          number
                        }
                      }
                    }
                  }
                }
              }
            }`,
            { projectId, after, archivedStates });

        const itemsConnection = response.node?.items;
        const nodes = itemsConnection?.nodes ?? [];

        for (const node of nodes) {
            if (!node?.content) {
                continue;
            }

            if (node.content.__typename === 'Issue') {
                issueItemsByContentId.set(node.content.id, node);
                continue;
            }

            if (node.content.__typename === 'PullRequest') {
                pullRequestItems.push(node);
            }
        }

        hasNextPage = itemsConnection?.pageInfo?.hasNextPage ?? false;
        after = itemsConnection?.pageInfo?.endCursor ?? null;
    }

    return { issueItemsByContentId, pullRequestItems };
}

async function fetchRepositoryIssuesAsync() {
    const issues = [];
    const issueByNumber = new Map();
    let page = 1;

    while (true) {
        const pageIssues = await restAsync(`/repos/${owner}/${repo}/issues?state=all&per_page=100&page=${page}`);
        const issuesOnly = pageIssues.filter(issue => !issue.pull_request);

        issues.push(...issuesOnly);
        for (const issue of issuesOnly) {
            issueByNumber.set(issue.number, issue);
        }

        if (pageIssues.length < 100) {
            break;
        }

        page += 1;
    }

    return { issues, issueByNumber };
}

async function syncIssueAsync(issue, issueItemsByContentId, timelineCache) {
    let projectItem = issueItemsByContentId.get(issue.node_id) ?? null;

    if (isClosedAsDuplicate(issue)) {
        if (projectItem) {
            await deleteProjectItemAsync(projectItem.id, `duplicate issue #${issue.number}`);
        }

        return;
    }

    const shouldArchive = isIssueClosedLongEnoughToArchive(issue, getRunDate());

    if (!projectItem) {
        if (shouldArchive) {
            return;
        }

        const projectItemId = await addIssueToProjectAsync(issue.node_id);
        projectItem = {
            id: projectItemId,
            isArchived: false,
            content: {
                __typename: 'Issue',
                id: issue.node_id,
                number: issue.number,
            },
            status: null,
            phase: null,
            priority: null,
            startDate: null,
            targetDate: null,
            focusOrder: null,
        };

        issueItemsByContentId.set(issue.node_id, projectItem);
        console.log(`Added issue #${issue.number} to the roadmap project.`);
    }

    if (isProjectItemArchived(projectItem)) {
        if (shouldArchive) {
            return;
        }

        await unarchiveProjectItemAsync(projectItem.id, `issue #${issue.number}`);
        projectItem.isArchived = false;
    }

    if (shouldArchive) {
        await archiveProjectItemAsync(projectItem.id, `issue #${issue.number}`);
        projectItem.isArchived = true;
        return;
    }

    const currentStatusName = projectItem.status?.name ?? null;
    const desiredStatusName = determineRoadmapStatus(issue, currentStatusName);
    const desiredPhaseOptionId = determinePhaseOptionId(issue);
    const desiredPriorityOptionId = determinePriorityOptionId(issue);
    const startDate = await determineStartDateAsync(issue, desiredStatusName, projectItem.startDate?.date ?? null, timelineCache);
    const targetDate = determineTargetDate(issue, desiredStatusName, startDate);

    await syncSingleSelectFieldAsync(projectItem.id, fieldIds.status, projectItem.status?.optionId ?? null, statusOptions[desiredStatusName], `issue #${issue.number} status`);
    await syncSingleSelectFieldAsync(projectItem.id, fieldIds.phase, projectItem.phase?.optionId ?? null, desiredPhaseOptionId, `issue #${issue.number} phase`);
    await syncSingleSelectFieldAsync(projectItem.id, fieldIds.priority, projectItem.priority?.optionId ?? null, desiredPriorityOptionId, `issue #${issue.number} priority`);

    if (desiredStatusName === 'Up Next') {
        await syncDateFieldAsync(projectItem.id, fieldIds.startDate, projectItem.startDate?.date ?? null, null, `issue #${issue.number} start date`);
        await syncDateFieldAsync(projectItem.id, fieldIds.targetDate, projectItem.targetDate?.date ?? null, null, `issue #${issue.number} target date`);
        return;
    }

    if (desiredStatusName === 'Todo') {
        await syncDateFieldAsync(projectItem.id, fieldIds.startDate, projectItem.startDate?.date ?? null, null, `issue #${issue.number} start date`);
        await syncDateFieldAsync(projectItem.id, fieldIds.targetDate, projectItem.targetDate?.date ?? null, null, `issue #${issue.number} target date`);
        await clearFieldAsync(projectItem.id, fieldIds.focusOrder, projectItem.focusOrder?.number ?? null, `issue #${issue.number} focus order`);
        return;
    }

    await syncDateFieldAsync(projectItem.id, fieldIds.startDate, projectItem.startDate?.date ?? null, startDate, `issue #${issue.number} start date`);
    await syncDateFieldAsync(projectItem.id, fieldIds.targetDate, projectItem.targetDate?.date ?? null, targetDate, `issue #${issue.number} target date`);
    await clearFieldAsync(projectItem.id, fieldIds.focusOrder, projectItem.focusOrder?.number ?? null, `issue #${issue.number} focus order`);
}

async function syncParentIssuesAsync(issueItemsByContentId, issueByNumber, timelineCache) {
    for (const projectItem of issueItemsByContentId.values()) {
        const issue = projectItem.content;

        if (issue?.__typename !== 'Issue') {
            continue;
        }

        if (isProjectItemArchived(projectItem)) {
            continue;
        }

        const trackedIssues = issue.trackedIssues?.nodes ?? [];

        if (trackedIssues.length === 0) {
            continue;
        }

        const childItems = trackedIssues
            .map(child => issueItemsByContentId.get(child.id))
            .filter(Boolean);

        if (childItems.length === 0) {
            continue;
        }

        const childStates = childItems.map(childItem => determineChildLifecycleState(childItem));
        const allChildrenDone = childStates.every(state => state === 'Done');
        const anyChildStarted = childStates.some(state => state === 'In Progress' || state === 'Done');
        const desiredStatusName = allChildrenDone ? 'Done' : anyChildStarted ? 'In Progress' : 'Todo';

        const parentIssue = issueByNumber.get(issue.number) ?? createRepositoryIssueFromProjectItem(issue);
        const desiredPhaseOptionId = determinePhaseOptionId(parentIssue);
        const desiredPriorityOptionId = determinePriorityOptionId(parentIssue);
        const childStartDates = await Promise.all(
            childItems.map(childItem => determineProjectItemStartDateAsync(childItem, issueByNumber, timelineCache))
        );
        const effectiveChildStartDates = childStartDates.filter(Boolean).sort();
        const effectiveChildTargetDates = childItems
            .map(childItem => childItem.targetDate?.date ?? childItem.content.closedAt?.slice(0, 10) ?? null)
            .filter(Boolean)
            .sort();

        const desiredStartDate = desiredStatusName === 'Todo' ? null : effectiveChildStartDates[0] ?? null;
        const desiredTargetDate =
            desiredStatusName === 'Todo'
                ? null
                : effectiveChildTargetDates.length > 0
                    ? effectiveChildTargetDates[effectiveChildTargetDates.length - 1]
                    : desiredStartDate;

        await syncSingleSelectFieldAsync(projectItem.id, fieldIds.status, projectItem.status?.optionId ?? null, statusOptions[desiredStatusName], `parent issue #${issue.number} status`);
        await syncSingleSelectFieldAsync(projectItem.id, fieldIds.phase, projectItem.phase?.optionId ?? null, desiredPhaseOptionId, `parent issue #${issue.number} phase`);
        await syncSingleSelectFieldAsync(projectItem.id, fieldIds.priority, projectItem.priority?.optionId ?? null, desiredPriorityOptionId, `parent issue #${issue.number} priority`);
        await syncDateFieldAsync(projectItem.id, fieldIds.startDate, projectItem.startDate?.date ?? null, desiredStartDate, `parent issue #${issue.number} start date`);
        await syncDateFieldAsync(projectItem.id, fieldIds.targetDate, projectItem.targetDate?.date ?? null, desiredTargetDate, `parent issue #${issue.number} target date`);
        await clearFieldAsync(projectItem.id, fieldIds.focusOrder, projectItem.focusOrder?.number ?? null, `parent issue #${issue.number} focus order`);
    }
}

function determineRoadmapStatus(issue, currentStatusName) {
    const labels = getLabelNames(issue);

    if (issue.state === 'closed' || labels.has('status/done')) {
        return 'Done';
    }

    if (labels.has('status/in-progress')) {
        return 'In Progress';
    }

    if (labels.has('status/in-review')) {
        return roadmapStates.has(currentStatusName) ? currentStatusName : 'In Progress';
    }

    if (currentStatusName === 'Up Next') {
        return 'Up Next';
    }

    return 'Todo';
}

function isClosedAsDuplicate(issue) {
    return issue.state === 'closed' && issue.state_reason === 'duplicate';
}

function determinePhaseOptionId(issue) {
    const milestoneTitle = issue.milestone?.title ?? null;

    if (!milestoneTitle) {
        return null;
    }

    if (legacyPhaseOptionsByMilestone.has(milestoneTitle)) {
        return legacyPhaseOptionsByMilestone.get(milestoneTitle);
    }

    for (const [prefix, optionId] of legacyPhaseOptionsByMilestone.entries()) {
        if (milestoneTitle.startsWith(`${prefix} `) || milestoneTitle.startsWith(`${prefix}—`) || milestoneTitle.startsWith(`${prefix} —`)) {
            return optionId;
        }
    }

    return null;
}

function determinePriorityOptionId(issue) {
    const labels = getLabelNames(issue);

    for (const [label, optionId] of priorityOptionsByLabel.entries()) {
        if (labels.has(label)) {
            return optionId;
        }
    }

    return null;
}

async function determineStartDateAsync(issue, desiredStatusName, currentStartDate, timelineCache) {
    const closedDate = issue.closed_at?.slice(0, 10) ?? null;

    if (currentStartDate) {
        if (desiredStatusName !== 'Done' || isDateBeforeOrEqualClosureDate(currentStartDate, closedDate)) {
            return currentStartDate;
        }
    }

    if (desiredStatusName === 'Todo' || desiredStatusName === 'Up Next') {
        return null;
    }

    const firstInProgressDate = await findFirstInProgressDateAsync(issue.number, timelineCache);

    if (firstInProgressDate) {
        if (isDateBeforeOrEqualClosureDate(firstInProgressDate, closedDate)) {
            return firstInProgressDate;
        }
    }

    if (desiredStatusName === 'Done' && closedDate) {
        return closedDate;
    }

    return getRunDate();
}

async function determineProjectItemStartDateAsync(projectItem, issueByNumber, timelineCache) {
    if (projectItem.startDate?.date) {
        return projectItem.startDate.date;
    }

    const issue = issueByNumber.get(projectItem.content.number) ?? createRepositoryIssueFromProjectItem(projectItem.content);
    return determineStartDateAsync(issue, determineRoadmapStatus(issue, projectItem.status?.name ?? null), null, timelineCache);
}

function determineTargetDate(issue, desiredStatusName, startDate) {
    if (desiredStatusName === 'Todo' || desiredStatusName === 'Up Next') {
        return null;
    }

    if (desiredStatusName === 'Done') {
        return issue.closed_at?.slice(0, 10) ?? startDate ?? getRunDate();
    }

    if (!startDate) {
        return null;
    }

    return addCalendarDays(startDate, determineCalendarDays(issue));
}

function determineCalendarDays(issue) {
    const labels = getLabelNames(issue);

    if (labels.has('size/xs') || labels.has('size/s')) {
        return 1;
    }

    if (labels.has('size/m')) {
        return 3;
    }

    if (labels.has('size/l')) {
        return 7;
    }

    if (labels.has('size/xl')) {
        return 14;
    }

    return 3;
}

function determineChildLifecycleState(projectItem) {
    if (projectItem.content.state === 'CLOSED') {
        return 'Done';
    }

    return determineRoadmapStatus(
        {
            state: projectItem.content.state === 'CLOSED' ? 'closed' : 'open',
            labels: projectItem.content.labels?.nodes ?? [],
        },
        projectItem.status?.name ?? null);
}

async function addIssueToProjectAsync(contentId) {
    const response = await graphqlAsync(
        `
        mutation AddProjectItem($projectId: ID!, $contentId: ID!) {
          addProjectV2ItemById(input: { projectId: $projectId, contentId: $contentId }) {
            item {
              id
            }
          }
        }`,
        { projectId, contentId });

    return response.addProjectV2ItemById.item.id;
}

async function archiveProjectItemAsync(itemId, label) {
    await graphqlAsync(
        `
        mutation ArchiveProjectItem($projectId: ID!, $itemId: ID!) {
          archiveProjectV2Item(input: { projectId: $projectId, itemId: $itemId }) {
            item {
              id
            }
          }
        }`,
        { projectId, itemId });

    console.log(`Archived ${label} on the roadmap project.`);
}

async function unarchiveProjectItemAsync(itemId, label) {
    await graphqlAsync(
        `
        mutation UnarchiveProjectItem($projectId: ID!, $itemId: ID!) {
          unarchiveProjectV2Item(input: { projectId: $projectId, itemId: $itemId }) {
            item {
              id
            }
          }
        }`,
        { projectId, itemId });

    console.log(`Unarchived ${label} on the roadmap project.`);
}

async function deleteProjectItemAsync(itemId, label) {
    await graphqlAsync(
        `
        mutation DeleteProjectItem($projectId: ID!, $itemId: ID!) {
          deleteProjectV2Item(input: { projectId: $projectId, itemId: $itemId }) {
            deletedItemId
          }
        }`,
        { projectId, itemId });

    console.log(`Removed ${label} from the roadmap project.`);
}

async function removeStrayPullRequestCardsAsync(pullRequestItems) {
    for (const projectItem of pullRequestItems) {
        await deleteProjectItemAsync(projectItem.id, `stray pull request card for #${projectItem.content.number}`);
    }
}

async function syncSingleSelectFieldAsync(itemId, fieldId, currentOptionId, desiredOptionId, label) {
    if (currentOptionId === desiredOptionId) {
        return;
    }

    if (!desiredOptionId) {
        await clearFieldAsync(itemId, fieldId, currentOptionId, label);
        return;
    }

    await graphqlAsync(
        `
        mutation SetSingleSelectField($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $projectId
              itemId: $itemId
              fieldId: $fieldId
              value: { singleSelectOptionId: $optionId }
            }
          ) {
            projectV2Item {
              id
            }
          }
        }`,
        { projectId, itemId, fieldId, optionId: desiredOptionId });

    console.log(`Updated ${label}.`);
}

async function syncDateFieldAsync(itemId, fieldId, currentDate, desiredDate, label) {
    if ((currentDate ?? null) === (desiredDate ?? null)) {
        return;
    }

    if (!desiredDate) {
        await clearFieldAsync(itemId, fieldId, currentDate, label);
        return;
    }

    await graphqlAsync(
        `
        mutation SetDateField($projectId: ID!, $itemId: ID!, $fieldId: ID!, $date: Date!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $projectId
              itemId: $itemId
              fieldId: $fieldId
              value: { date: $date }
            }
          ) {
            projectV2Item {
              id
            }
          }
        }`,
        { projectId, itemId, fieldId, date: desiredDate });

    console.log(`Updated ${label}.`);
}

async function clearFieldAsync(itemId, fieldId, currentValue, label) {
    if (currentValue === null || currentValue === undefined) {
        return;
    }

    await graphqlAsync(
        `
        mutation ClearField($projectId: ID!, $itemId: ID!, $fieldId: ID!) {
          clearProjectV2ItemFieldValue(
            input: {
              projectId: $projectId
              itemId: $itemId
              fieldId: $fieldId
            }
          ) {
            projectV2Item {
              id
            }
          }
        }`,
        { projectId, itemId, fieldId });

    console.log(`Cleared ${label}.`);
}

async function findFirstInProgressDateAsync(issueNumber, timelineCache) {
    if (timelineCache.has(issueNumber)) {
        return timelineCache.get(issueNumber);
    }

    const timeline = await fetchIssueTimelineAsync(issueNumber);
    const inProgressLabelEvent = timeline.reduce((earliest, event) => {
        if (event.event !== 'labeled' || event.label?.name !== 'status/in-progress') {
            return earliest;
        }

        if (!earliest || event.created_at < earliest.created_at) {
            return event;
        }

        return earliest;
    }, null);

    const firstInProgressDate = inProgressLabelEvent?.created_at?.slice(0, 10) ?? null;
    timelineCache.set(issueNumber, firstInProgressDate);
    return firstInProgressDate;
}

async function fetchIssueTimelineAsync(issueNumber) {
    const timeline = [];
    let page = 1;

    while (true) {
        const pageItems = await restAsync(`/repos/${owner}/${repo}/issues/${issueNumber}/timeline?per_page=100&page=${page}`);
        timeline.push(...pageItems);

        if (pageItems.length < 100) {
            break;
        }

        page += 1;
    }

    return timeline;
}

async function graphqlAsync(query, variables) {
    return withGitHubRetry(async () => {
        const response = await fetch(graphqlUrl, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json',
                Accept: 'application/vnd.github+json',
                'User-Agent': 'solo-dev-board-roadmap-sync',
            },
            body: JSON.stringify({ query, variables }),
        });

        const text = await response.text();
        let payload = {};

        try {
            payload = text ? JSON.parse(text) : {};
        } catch {
            payload = { message: text };
        }

        const hasErrors = Boolean(payload.errors && payload.errors.length > 0);
        const errorText = hasErrors ? JSON.stringify(payload.errors) : text;
        const graphqlRateLimited = hasErrors && isTransientGitHubGraphQlError(errorText);

        if (response.ok && !hasErrors) {
            return { success: true, value: payload.data };
        }

        return {
            success: false,
            status: response.status,
            errorText,
            retryAfter: response.headers.get('retry-after'),
            graphqlRateLimited,
            error: new Error(`GraphQL request failed: ${JSON.stringify(payload.errors ?? payload, null, 2)}`),
        };
    }, 'GraphQL');
}

async function restAsync(path) {
    return githubJsonRequest({
        url: `${apiBaseUrl}${path}`,
        token,
        userAgent: 'solo-dev-board-roadmap-sync',
        apiVersion: '2022-11-28',
    });
}

function getLabelNames(issue) {
    if (!issue.labels) {
        return new Set();
    }

    if (Array.isArray(issue.labels)) {
        return new Set(issue.labels.map(label => label.name));
    }

    return new Set((issue.labels.nodes ?? []).map(label => label.name));
}

function createRepositoryIssueFromProjectItem(issue) {
    return {
        number: issue.number,
        state: issue.state?.toLowerCase() === 'closed' ? 'closed' : 'open',
        // Project-item fallback data does not expose GitHub's closure reason, so duplicate-closure
        // detection must use the repository issue payload when that distinction matters.
        state_reason: null,
        closed_at: issue.closedAt ?? null,
        milestone: issue.milestone ?? null,
        labels: issue.labels?.nodes ?? [],
    };
}

function isDateBeforeOrEqualClosureDate(date, closedDate) {
    return !closedDate || date <= closedDate;
}

function addCalendarDays(startDate, calendarDays) {
    const date = new Date(`${startDate}T00:00:00.000Z`);
    date.setUTCDate(date.getUTCDate() + calendarDays);
    return date.toISOString().slice(0, 10);
}

function getRunDate() {
    return new Date().toISOString().slice(0, 10);
}
