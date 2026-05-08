# ADR-0017: Use a GitHub Actions Bridge for SoloDevBoard Roadmap Project Sync

**Date:** 2026-05-08
**Status:** Accepted

## Context

SoloDevBoard uses a **user-owned** GitHub Projects v2 board (`markheydon` Project #8) as the canonical roadmap view. In practice, some agent runtimes can inspect repository contents and modify files but cannot mutate that user-owned Project directly because the available runtime token is repository-scoped rather than user-scoped.

This created an operational gap:

- issue and pull request lifecycle work could still happen,
- the roadmap board could drift out of sync,
- historical Start Date and Target Date gaps accumulated, and
- a direct local remediation path was not consistently available to the agent.

The project needs a durable automation bridge that runs inside GitHub, close to the repository events, and can keep the roadmap board aligned even when the interactive agent session cannot call the Projects API directly.

## Decision

Use a dedicated **GitHub Actions bridge workflow** to reconcile the SoloDevBoard Roadmap board from repository lifecycle events and scheduled audits.

The bridge consists of:

- `.github/workflows/roadmap-sync.yml` — the workflow entry point.
- `.github/scripts/roadmap-sync.mjs` — the reconciliation logic.

The workflow:

- runs on issue lifecycle events relevant to roadmap state,
- runs on selected pull request events to remove stray pull request cards,
- runs on a daily schedule as a hygiene backstop, and
- supports manual dispatch for operator-initiated repair runs.

Authentication uses a **dedicated classic Personal Access Token** stored as the repository secret `ROADMAP_PROJECT_TOKEN`, because user-owned Projects v2 mutations require user-scoped project access that `GITHUB_TOKEN` cannot reliably provide in this scenario.

The bridge is responsible for:

- adding missing issues to the roadmap board,
- synchronising Status, Phase, and Priority fields,
- setting or repairing Start Date and Target Date values,
- rolling up parent Feature / Epic dates from child items, and
- removing standalone pull request cards that do not belong on the roadmap board.

## Rationale

Running the bridge in GitHub Actions keeps the automation close to the authoritative issue and pull request events, rather than depending on whichever credentials happen to exist in a local agent session.

Using a separate bridge also improves reliability:

- repository events can continue to drive roadmap updates automatically,
- scheduled audits can correct historical drift,
- the project remains manageable even when direct local Projects access is unavailable, and
- the automation logic is versioned with the repository and reviewable in pull requests.

The dedicated project token is a deliberate compromise. It introduces one extra secret to manage, but it is currently the most reliable way to mutate a **user-owned** Projects v2 board from GitHub-hosted automation.

## Consequences

### Positive

- Roadmap synchronisation no longer depends on direct Projects access from an interactive agent session.
- Missing dates, field drift, and stray pull request cards can be repaired automatically.
- Future issue and pull request lifecycle work should keep the roadmap accurate by default.
- The bridge logic is explicit, reviewable, and easy to evolve with the roadmap rules.

### Negative

- The repository now depends on a manually provisioned `ROADMAP_PROJECT_TOKEN` secret.
- A classic PAT is required for compatibility with the user-owned Projects v2 API surface.
- Workflow failures can now block roadmap hygiene until the token or workflow is repaired.

### Operational follow-up

- The secret must be created and rotated by the repository owner.
- Weekly PM review should continue to inspect roadmap hygiene and workflow health.
- If the roadmap board is ever moved to an organisation-owned Project with compatible automation permissions, this decision should be revisited.
