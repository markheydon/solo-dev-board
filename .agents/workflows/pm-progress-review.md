# PM Progress Review

**Contract:** [`.agents/contracts/pm-orchestrator.md`](../contracts/pm-orchestrator.md) — Progress review mode (read-only)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Progress Review Rhythm

## Purpose

Cadence-neutral governance review covering the period **since the last progress review** (or project inception if none exists). Do not assume a seven-day window.

## Easy-to-miss specifics

- **Read-only by default** — do not mutate the project board or issues unless the user requests follow-up actions.
- Assess milestone and issue health via `gh issue list` and Project #8, not `plan/BACKLOG.md` as a work queue.
- If the gap since the last review is long, summarise the idle period in one line. Do not invent filler reviews for months with no delivery.
- Forward-looking priorities are for the **next working session(s)**, not "next week".
- Velocity is **closed/merged since last review**, not weekly throughput.

## Procedure

### 1. Establish the review window

1. List files in [`plan/weekly-updates/`](../../plan/weekly-updates/) and identify the newest by date in the filename (`YYYY-MM-DD.md`).
2. Read that file to determine the prior review date and baseline context.
3. If no prior file exists, treat the window as "project inception to today".

### 2. Gather current state

Run read-only queries:

- `gh issue list` — open and recently closed issues (filter by state and updated date as needed).
- Project #8 (SoloDevBoard Roadmap) — milestone progress, board status, Up Next queue, blockers.
- `plan/SCOPE.md` and `plan/IMPLEMENTATION_PLAN.md` — scope and phase alignment.
- `plan/RELEASE_PLAN.md` — release confidence context.
- Open PRs: `gh pr list --state open`.

Optional quality check (if feasible): `dotnet test` summary for health indicators.

### 3. Assess health

Cover each area:

| Area | What to check |
|------|---------------|
| **Milestone health** | Closed/open counts and % complete per active milestone. |
| **Scope / governance** | Drift against `plan/SCOPE.md`; planning artefact freshness. |
| **Backlog hygiene** | Missing labels, acceptance criteria, size estimates, stale items. |
| **Release confidence** | Next planned release, blockers, docs/ADR completeness for ship. |
| **Blockers** | Explicit `status/blocked` issues and dependency chains. |
| **Ice Box** | Explicit `status/ice-box` issues (shelved backlog — separate from blockers). |
| **Board hygiene** | Missing dates, invalid date pairs, roadmap issues missing from board, stray PR cards — see [`plan/PROJECT_BOARD_DESIGN.md`](../../plan/PROJECT_BOARD_DESIGN.md). |
| **Delivery since last review** | Issues closed, PRs merged, notable commits in the window. |
| **Velocity** | Count closed/merged since last review; note if the sample is too small for forecasting. |

### 4. Produce recommendations

- **Overall status:** On track / at risk / blocked (one line).
- **Top 3 priorities** for the next working session(s).
- **Recommended actions** — grooming, scope updates, release plan changes, board metadata refresh.
- **Project board status block** — refresh [`plan/PROJECT_README.md`](../../plan/PROJECT_README.md) when the Project #8 info pane is stale, and include the paste-ready README in the review artefact.

### 5. Write the artefact

Save to `plan/weekly-updates/YYYY-MM-DD.md` (today's date, ISO format).

**Title:** `PM Progress Review — {D MMMM YYYY}` (UK English date).

**Structure** (follow the July 2026 template):

```markdown
# PM Progress Review — {date}

**Overall Status:** {On track | At risk | Blocked}.

{One-paragraph executive headline.}

---

## Project Board Status Update

**Manual dialog values:**
- Status: {On track | At risk | Blocked}.
- Start date: {project start or unchanged}.
- Target date: {tentative target or unchanged}.

\```markdown
## Progress Status — {date}

**{Active phase/milestone}:** {one-line status}.

### Delivered since the last review
- {bullet list}

### Next session focus
1. {priority 1}
2. {priority 2}
3. {priority 3}

### Health indicators
| Metric | Value |
|---|---|
| Tests | {pass/fail count or "not run"} |
| Compile errors | {0 or count} |
| Open PRs | {count} |
| Active blockers | {count and refs} |
\```

---

## Project Board Metadata

{Recommendations for Phase, milestone, or readme refresh — or "No structural change recommended."}

---

## Executive Summary

### Milestone Health
{table}

### Scope / Governance Health
{bullets}

### Backlog Hygiene
{bullets}

### Release Confidence
{bullets}

### Quality Metrics
{bullets}

### Delivery Activity Since The Last Review
{bullets}

### Blocker Analysis
{bullets}

### Velocity Trends
{bullets — count since last review, not weekly rate}

### Top 3 Priorities For The Next Session
{numbered list}

### Recommended Actions
{bullets}
```

### 6. Present to the user

Deliver a concise chat summary with:

1. Overall status and headline.
2. Key changes since last review.
3. Top 3 priorities for next session.
4. Path to the saved artefact.
5. Optional follow-up actions (board updates, grooming) — only execute if requested.

## Invocation

Natural language: "Run the PM progress review" or "Run a progress review since the last update"

Slash command: `/pm-progress-review`
