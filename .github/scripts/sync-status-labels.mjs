const apiBaseUrl = 'https://api.github.com';
const owner = 'markheydon';
const repo = 'solo-dev-board';

const statusLabels = [
    'status/todo',
    'status/in-progress',
    'status/blocked',
    'status/in-review',
    'status/done',
];

const statusPrecedence = [
    'status/in-review',
    'status/in-progress',
    'status/blocked',
    'status/todo',
];

const applyChanges = process.argv.includes('--apply');
const dryRun = !applyChanges;

const token =
    process.env.ROADMAP_PROJECT_TOKEN ??
    process.env.GH_TOKEN ??
    process.env.GITHUB_TOKEN ??
    null;

if (!token) {
    throw new Error('A GitHub token is required. Set ROADMAP_PROJECT_TOKEN, GH_TOKEN, or GITHUB_TOKEN.');
}

await main();

async function main() {
    const [issues, pullRequests, openLinkedIssueNumbers] = await Promise.all([
        fetchIssuesAsync(),
        fetchPullRequestsAsync(),
        fetchOpenLinkedIssueNumbersAsync(),
    ]);

    const items = [
        ...issues.map(issue => ({ kind: 'issue', item: issue })),
        ...pullRequests.map(pullRequest => ({ kind: 'pr', item: pullRequest })),
    ];

    const repairs = [];

    for (const { kind, item } of items) {
        const plan = planRepair(kind, item, openLinkedIssueNumbers);

        if (!plan) {
            continue;
        }

        repairs.push({ kind, item, plan });
    }

    printReport(repairs);

    if (repairs.length === 0) {
        console.log('No status label repairs required.');
        return;
    }

    if (dryRun) {
        console.log(`Dry run complete. ${repairs.length} item(s) would be updated. Re-run with --apply to mutate labels.`);
        process.exitCode = 1;
        return;
    }

    for (const repair of repairs) {
        if (repair.plan.reportOnly) {
            continue;
        }

        await applyRepairAsync(repair);
    }

    console.log(`Applied status label repairs to ${repairs.filter(repair => !repair.plan.reportOnly).length} item(s).`);

    const [refreshedIssues, refreshedPullRequests, refreshedOpenLinkedIssueNumbers] = await Promise.all([
        fetchIssuesAsync(),
        fetchPullRequestsAsync(),
        fetchOpenLinkedIssueNumbersAsync(),
    ]);

    const refreshedItems = [
        ...refreshedIssues.map(issue => ({ kind: 'issue', item: issue })),
        ...refreshedPullRequests.map(pullRequest => ({ kind: 'pr', item: pullRequest })),
    ];

    const remainingRepairs = refreshedItems
        .map(({ kind, item }) => {
            const plan = planRepair(kind, item, refreshedOpenLinkedIssueNumbers);
            return plan ? { kind, item, plan } : null;
        })
        .filter(Boolean)
        .filter(repair => !repair.plan.reportOnly);

    if (remainingRepairs.length > 0) {
        console.error(`Status label sync finished with ${remainingRepairs.length} remaining repair(s).`);
        process.exitCode = 1;
    }
}

function planRepair(kind, item, openLinkedIssueNumbers) {
    const currentStatusLabels = getCurrentStatusLabels(item);
    const isClosed = item.state === 'closed';
    const isDuplicate = kind === 'issue' && item.state_reason === 'duplicate';

    if (isClosed) {
        if (isDuplicate) {
            if (currentStatusLabels.length === 0) {
                return null;
            }

            return {
                reportOnly: false,
                remove: [...currentStatusLabels],
                add: [],
                action: 'remove all status/* (closed duplicate)',
            };
        }

        const extras = currentStatusLabels.filter(label => label !== 'status/done');
        const missingDone = !currentStatusLabels.includes('status/done');

        if (extras.length === 0 && !missingDone) {
            return null;
        }

        return {
            reportOnly: false,
            remove: extras,
            add: missingDone ? ['status/done'] : [],
            action: missingDone ? 'set status/done only' : 'remove stale status/* labels; keep status/done',
        };
    }

    if (currentStatusLabels.length === 0) {
        return {
            reportOnly: true,
            remove: [],
            add: [],
            action: 'report missing status/* label (manual triage required)',
        };
    }

    if (currentStatusLabels.includes('status/done')) {
        const replacement = kind === 'pr' || openLinkedIssueNumbers.has(item.number)
            ? 'status/in-review'
            : 'status/todo';

        return {
            reportOnly: false,
            remove: currentStatusLabels.filter(label => label !== replacement),
            add: currentStatusLabels.includes(replacement) ? [] : [replacement],
            action: `replace status/done with ${replacement}`,
        };
    }

    if (currentStatusLabels.length > 1) {
        const keeper = pickHighestPrecedenceLabel(currentStatusLabels);

        return {
            reportOnly: false,
            remove: currentStatusLabels.filter(label => label !== keeper),
            add: [],
            action: `keep ${keeper}; remove duplicate status/* labels`,
        };
    }

    return null;
}

function pickHighestPrecedenceLabel(labels) {
    for (const label of statusPrecedence) {
        if (labels.includes(label)) {
            return label;
        }
    }

    return labels[0];
}

function getCurrentStatusLabels(item) {
    const labelNames = (item.labels ?? []).map(label => label.name);
    return labelNames.filter(label => statusLabels.includes(label));
}

function printReport(repairs) {
    if (repairs.length === 0) {
        return;
    }

    console.log('');
    console.log('number\ttype\tstate\tcurrent status/*\taction');
    console.log('------\t----\t-----\t----------------\t------');

    for (const repair of repairs) {
        const current = getCurrentStatusLabels(repair.item);
        const currentText = current.length > 0 ? current.join(', ') : '(none)';

        console.log(
            `${repair.item.number}\t${repair.kind}\t${repair.item.state}\t${currentText}\t${repair.plan.action}`,
        );
    }

    console.log('');
}

async function applyRepairAsync({ kind, item, plan }) {
    for (const label of plan.remove) {
        await restAsync(
            `/repos/${owner}/${repo}/issues/${item.number}/labels/${encodeURIComponent(label)}`,
            { method: 'DELETE' },
        );
    }

    if (plan.add.length > 0) {
        await restAsync(`/repos/${owner}/${repo}/issues/${item.number}/labels`, {
            method: 'POST',
            body: JSON.stringify({ labels: plan.add }),
        });
    }

    console.log(`Updated ${kind} #${item.number}: ${plan.action}`);
}

async function fetchIssuesAsync() {
    const issues = [];
    let page = 1;

    while (true) {
        const pageIssues = await restAsync(`/repos/${owner}/${repo}/issues?state=all&per_page=100&page=${page}`);
        const issuesOnly = pageIssues.filter(issue => !issue.pull_request);

        issues.push(...issuesOnly);

        if (pageIssues.length < 100) {
            break;
        }

        page += 1;
    }

    return issues;
}

async function fetchPullRequestsAsync() {
    const pullRequests = [];
    let page = 1;

    while (true) {
        const pagePullRequests = await restAsync(`/repos/${owner}/${repo}/pulls?state=all&per_page=100&page=${page}`);
        const issueBackedPullRequests = await Promise.all(
            pagePullRequests.map(async pullRequest => {
                const issue = await restAsync(`/repos/${owner}/${repo}/issues/${pullRequest.number}`);
                return {
                    number: pullRequest.number,
                    state: pullRequest.state,
                    state_reason: null,
                    labels: issue.labels ?? [],
                };
            }),
        );

        pullRequests.push(...issueBackedPullRequests);

        if (pagePullRequests.length < 100) {
            break;
        }

        page += 1;
    }

    return pullRequests;
}

async function fetchOpenLinkedIssueNumbersAsync() {
    const linkedIssueNumbers = new Set();
    let page = 1;

    while (true) {
        const pagePullRequests = await restAsync(`/repos/${owner}/${repo}/pulls?state=open&per_page=100&page=${page}`);

        for (const pullRequest of pagePullRequests) {
            const timeline = await restAsync(
                `/repos/${owner}/${repo}/issues/${pullRequest.number}/timeline?per_page=100`,
            );

            for (const event of timeline) {
                if (event.event === 'cross-referenced' && event.source?.issue?.number) {
                    linkedIssueNumbers.add(event.source.issue.number);
                }
            }

            const bodyMatches = pullRequest.body?.matchAll(/(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+#(\d+)/gi) ?? [];

            for (const match of bodyMatches) {
                linkedIssueNumbers.add(Number.parseInt(match[1], 10));
            }
        }

        if (pagePullRequests.length < 100) {
            break;
        }

        page += 1;
    }

    return linkedIssueNumbers;
}

async function restAsync(path, options = {}) {
    const response = await fetch(`${apiBaseUrl}${path}`, {
        method: options.method ?? 'GET',
        headers: {
            Authorization: `Bearer ${token}`,
            Accept: 'application/vnd.github+json',
            'User-Agent': 'solo-dev-board-sync-status-labels',
            'X-GitHub-Api-Version': '2022-11-28',
            ...(options.body ? { 'Content-Type': 'application/json' } : {}),
        },
        body: options.body,
    });

    if (response.status === 204) {
        return null;
    }

    if (!response.ok) {
        throw new Error(`REST request failed (${response.status}) for ${path}: ${await response.text()}`);
    }

    return response.json();
}
