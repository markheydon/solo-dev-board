# Product Manager Runbook

**Purpose:** Your single source of truth for managing SoloDevBoard development using AI agents and prompts.

This runbook orchestrates [`.agents/contracts/`](../.agents/contracts/) and [`.agents/workflows/`](../.agents/workflows/) to ensure consistent, high-quality delivery from backlog to release.

---

## Quick Reference Card

| **When you want to...**                  | **How to invoke**                                          | **What it produces**                          |
|------------------------------------------|------------------------------------------------------------|-----------------------------------------------|
| Start a working session                  | "Run the daily start workflow" or `/daily-start`           | Status summary + next action recommendation   |
| Plan the next feature                    | "Plan the next item" or `/plan-next-issue`                 | GitHub issues with full metadata + tech spec  |
| Deliver planned work (happy path)        | "Deliver issue #N" or `/deliver-issue`                     | Preflight (when required) + code + verify + PR |
| Implement planned work (split)           | "Implement issue #N" or `/implement-issue`                 | Preflight + code + tests + docs + decision log (if needed) |
| Preflight before implementation          | "Preflight issue #N" or `/preflight-issue`                  | Context, touch map, approach sketch (no code)              |
| Verify and create PR                     | "Verify issue #N", "Create PR for issue #N", or `/verify-and-create-pr` | PR + quality validation + issue closure       |
| Code review (independent session)        | "Code review PR #N" or `/code-review`                      | GitHub review with resolvable threads         |
| Address PR review comments               | "Address PR review comments on PR #N" or `/address-pr-review-comments` | PR fixes + thread replies + resolved comments |
| Progress review (since last update)      | "Run the PM progress review" or `/pm-progress-review`        | Executive summary + next-session priorities   |
| End-to-end feature delivery              | `.agents/skills/repo-pm-feature-workflow/SKILL.md`         | Full workflow from backlog to closure         |

---

## Working Session Rhythm

### Session Start (5–10 minutes)

**Goal:** Get oriented at the start of a working session, identify priorities, clear blockers.

#### Step 1: Run Daily Start Prompt
```
Run the daily start workflow
```

**What it does:**
- Lists active work (in-progress issues, PRs awaiting review)
- Shows backlog health (priority breakdown, ready items)
- Flags blockers (dependencies, stale work)
- Recommends your next action
- Identifies the next story-level batch that can be placed in **Up Next** if you request board updates.

**What you produce:** Decision on what to work on in this session.

**Queue rule:** Daily start is read-only by default. If you want the recommendation reflected on the board, explicitly ask the agent to move the chosen stories, enablers, or tests into **Up Next** and set **Focus Order**.

**Example output:**
```
📋 Active Work: PR #50 ready to merge
📊 Backlog: 3 high-priority items ready for planning
✅ Recommended: Merge PR #50, then plan "One-Click Migration UI"
```

**Next action:** Follow the recommendation (see below for execution patterns).

#### Step 2: Populate Up Next Queue (optional, 1-2 minutes)

**Goal:** Turn the recommendation into a visible execution queue on the Story Board.

**Run:**
```
Populate Up Next for this session
```

**What it does:**
- Moves the selected stories, enablers, or tests from **Todo** to **Up Next** on the GitHub Project board.
- Sets the **Focus Order** number field so the Story Board reflects the recommended execution sequence.
- Leaves Features and Epics unchanged.

**Operating rules:**
- Use **Up Next** only for the short-horizon batch you intend to work through next.
- Use **Focus Order** only on stories, enablers, and tests in that batch.
- Leave **Focus Order** blank on all other items.
- Keep Story Board sorted by **Focus Order** ascending; do not sort Feature Board or Epic Board by **Focus Order**.

---

### Execution (throughout the session)

**Goal:** Move work from backlog → planned → implemented → reviewed → closed.

You'll follow this **4-stage workflow** for each feature:

#### Stage 1: Planning (30-60 minutes per feature)
**Trigger:** Session start recommends planning, or you decide to plan a specific item.

**Run:**
```
Plan the next item
```
or
```
Plan the [feature name]
```

**What it does:**
- Invokes **PM Orchestrator Agent**
- Selects from backlog (or uses your specified feature)
- Validates scope alignment
- Creates technical plan via `breakdown-plan` skill
- Sets up GitHub issues with labels, milestones, acceptance criteria
- Defines test strategy via `breakdown-test` skill

**What you produce:**
- GitHub issues ready for implementation (with `status/todo` label)
- Technical spec in issue descriptions
- Test issues linked to features

**Gates before proceeding:**
- ✅ Scope validated (in `plan/SCOPE.md`)
- ✅ Acceptance criteria clear
- ✅ Labels/milestones applied
- ✅ No ambiguity or blockers

**Next action:** Move to Stage 2 (Implementation).

---

#### Stage 2: Implementation (2-8 hours per feature, varies by size)
**Trigger:** Planning complete, issues have `status/todo` label.

**Run (happy path — recommended):**
```
Deliver issue #[number]
```

**Run (Cursor happy path):**
```
/deliver-issue [number]
```

This orchestrates preflight (when required), implementation, and verify/PR creation in one session. Use split commands below when you want to pause between stages.

**Run (preflight only — optional, recommended for `size/l+` or enablers):**
```
Preflight issue #[number]
```

**Run (Cursor preflight):**
```
/preflight-issue [number]
```

**Run (prompt or natural language):**
```
Implement issue #[number]
```

**Run (Cursor):**
```
/implement-issue [number]
```

**What it does:**
- Invokes the **Delivery** contract ([`.agents/contracts/delivery.md`](../.agents/contracts/delivery.md))
- Runs **implementation preflight** before coding (load context, codebase discovery, touch map, proceed gate)
- Writes code following layered architecture (Domain/Application/Infrastructure/App)
- Creates xUnit v3 tests (NSubstitute, `Assert.*`, correct naming)
- Updates user-facing docs in `website/content/docs/` if needed
- Records architectural decisions via `repo-decision-log` / `plan/DECISIONS.md` when needed (do not create new `adr/` files)
- Ensures UK English throughout

**Board expectation before coding starts:**
- The issue may already be in **Up Next** if it was selected during session start.
- Starting work moves the issue from **Up Next** or **Todo** into **In Progress**.
- Starting work also stamps **Start Date** and the current size-based **Target Date** forecast on the roadmap item.
- Untouched sibling items stay blank until they actually start.

**Note:** The `/implement-issue` command runs preflight, sets `status/in-progress` on the issue, then code, tests, and docs. Roadmap Sync updates Project #8 board fields from the label. Do not call `gh project` commands during implementation.

**What you produce:**
- Preflight summary (context loaded, touch map, approach) before coding begins
- Source code in `src/` (compiles, follows conventions)
- Test code in `tests/` (passes, full coverage)
- Documentation updates (user guides, decision log entries, XML comments)

**Gates before proceeding:**
- ✅ Implementation preflight completed and proceed gate satisfied
- ✅ All acceptance criteria met
- ✅ Code compiles, zero errors/warnings
- ✅ Tests pass locally (`dotnet test`)
- ✅ Docs updated (if user-facing feature)
- ✅ Decision log updated (if architectural decision)
- ✅ UK English verified

**Next action:** If you used `/deliver-issue`, verify and PR creation run in the same session. Otherwise move to Stage 3 (Verify). After the PR exists, open a **new** agent session for `/code-review`.

---

#### Stage 3: Verify & PR Creation (15-30 minutes per feature)
**Trigger:** Implementation complete, ready for quality check and PR. Skipped when you used `/deliver-issue` (verify runs at the end of that workflow).

**Run:**
```
Verify issue #[number]
```

**What it does:**
- Invokes **Verify Agent**
- Validates all quality gates (build, clean rebuild tests, direct package versions on changed projects, docs)
- Creates pull request with metadata from [`PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md)
- Updates issue labels to `status/in-review`
- Provides verify summary and hands off to `/code-review` in a new session

**What you produce:**
- Pull request linked to issue
- Quality validation report
- Issue ready for independent code review

**Gates before proceeding:**
- ✅ All quality gates passed
- ✅ PR created and linked to issue
- ✅ Clean rebuild tests pass (`dotnet clean && dotnet test`)
- ✅ Direct packages added or bumped on this branch are current
- ✅ Documentation complete
- ✅ Backlog synchronised
- ✅ Roadmap item still has a valid Start Date before merge

**Next action:** Open a **new** agent session and run `/code-review` on the PR. You approve and merge manually after manual testing.

---

#### Stage 3b: Independent Code Review (15-30 minutes per feature)
**Trigger:** Pull request exists and verify has completed.

**Run (new agent session — do not reuse the implement/verify session):**
```
/code-review [PR number]
```

**What it does:**
- Invokes **Code Review Agent** ([`.agents/contracts/code-review.md`](../.agents/contracts/code-review.md))
- Audits the PR against architecture, testing, documentation, and UK English conventions
- Submits a **GitHub Pull Request Review** with resolvable inline threads (not a lone issue comment)

**What you produce:**
- GitHub review with severities and merge recommendation
- Resolvable review threads for any findings (same shape as Copilot or human reviews)

**Gates before proceeding:**
- ✅ Review submitted on GitHub (not chat only)
- ✅ Actionable findings appear as resolvable review threads

**Next action:** If unresolved review threads remain, run `/address-pr-review-comments`. Then manual product test and merge.

**Optional:** A Cursor Automation on pull request opened (ignore drafts) can run the same `/code-review` prompt in the background. Configure in the Cursor Automations dashboard — not in this repository.

---

#### Stage 4: Closure (5 minutes per feature)
**Trigger:** PR approved and merged to main.

**Run:**
```
Close issue #[number] after PR #[number] merged
```

**What it does:**
- Verify Agent updates issue labels to `status/done`
- Closes issue with comment linking PR
- Updates the roadmap item to **Done** and overwrites **Target Date** with the actual completion date
- Updates the GitHub Issue and Project #8 when work is complete
- Suggests next backlog item

**What you produce:**
- Issue closed and archived
- Backlog up to date
- Ready for next work item

**Gates before repeating cycle:**
- ✅ Issue closed with `status/done`
- ✅ Backlog updated
- ✅ Roadmap item dates and status aligned with the merged outcome
- ✅ No follow-up items (or new issues created if needed)

**Next action:** Return to Stage 1 (Planning) for next feature.

---

### PR Review Comment Loop (5-20 minutes per round)

**Goal:** Keep an open pull request moving after coding review feedback arrives, without losing thread history. Merge is blocked while **unresolved review conversations** remain.

**Trigger:** A reviewer (Copilot, human, `/code-review` agent, or bot) leaves coding review comments or requested changes on an open pull request.

**Run:**
```
Address PR review comments on PR #[number]
```

**What it does:**
- Invokes **Delivery Agent** on the existing pull request branch ([`.agents/contracts/delivery.md`](../.agents/contracts/delivery.md) §10).
- Fetches every unresolved review thread on the PR (not only one reviewer).
- Implements in-scope findings; replies on each thread; resolves conversations.
- Invalid or out-of-scope threads: reply with the reason, then resolve so merge is not blocked.
- Threads needing your decision: reply and leave unresolved; stop with a short list.
- Posts one final summary issue comment when all addressed threads are handled.
- No-ops when no unresolved review threads exist.

**What you produce:**
- Updated branch contents on the existing PR.
- Thread-by-thread reviewer feedback responses.
- A clean pull request with resolved review conversations.

**Gates before returning to review:**
- ✅ Requested changes implemented (or declined threads resolved with explanation).
- ✅ Relevant tests rerun.
- ✅ Each addressed review thread has a reply.
- ✅ Each addressed review thread is resolved.
- ✅ Final PR summary comment posted (informational issue comment — not a review thread).

**Note:** Top-level issue comments are not review threads and cannot be resolved. Actionable work must live on GitHub review conversations.

---

### End-of-Session Wrap-Up (5 minutes)

**Goal:** Leave work in a clean state before you step away.

#### Step 1: Quick Status Check
**Run:**
```
Run the daily start workflow
```
*(Same workflow as session start — now you see end-of-session status.)*

**What to check:**
- Any PRs still awaiting your approval? (Approve/merge before stepping away if possible)
- Any work still `status/in-progress`? (Leave a note on what's next)
- Any new blockers? (Document in issue comments)
- Any stale **Up Next** items? (Either keep them queued for the next session, move to **Ice Box** with `status/ice-box`, mark **Blocked** with `status/blocked`, or clear their **Focus Order** if they no longer belong in the active batch.)
- Any roadmap drift? (Active or done items missing dates, stray PR cards, or planned issues missing from the board.)

#### Step 2: Update Planning Artefacts (if needed)
- **Scope changed?** Update `plan/SCOPE.md`
- **New items discovered?** Create or update a GitHub Issue and sync Project #8
- **Release impact?** Update `plan/RELEASE_PLAN.md`

#### Step 3: Commit and Push
Ensure this session's work is committed and pushed to your branch.

---

## Progress Review Rhythm

### Progress Review (30–45 minutes when you return to the project)

**Goal:** Assess project health since the last review, validate governance, and set priorities for your next working session(s).

#### Run PM Progress Review Prompt
```
Run the PM progress review
```

**What it does:**
- Establishes the review window from the newest file in `plan/weekly-updates/` (not a fixed seven-day cadence).
- Calculates milestone progress and completion estimates.
- Validates scope (no drift detected).
- Checks backlog hygiene (missing metadata, stale items).
- Assesses release confidence (MVP completion, docs, decision log).
- Identifies blockers and delivery since the last review.
- Recommends top 3 priorities for the next working session(s).
- Writes `plan/weekly-updates/YYYY-MM-DD.md`.

**What you produce:**
- Progress executive summary (status report).
- Top 3 priorities for the next session(s).
- Backlog grooming to-do list (metadata gaps, stale items).
- Release confidence assessment.

**Actions after review:**
- Resolve flagged blockers.
- Update backlog metadata (acceptance criteria, size estimates).
- Adjust priorities if milestones at risk.
- Update `plan/RELEASE_PLAN.md` if release confidence low.

**Next action:** Use top 3 priorities to guide your next `plan-next-issue` selections.

---

## Workflow Decision Tree

Use this decision tree when you're unsure what to do next:

```
START
  │
  ├─ Start of a working session?
  │   └─> Run daily start workflow → Follow recommendation
  │
  ├─ Have planned issue ready for coding?
  │   ├─> Happy path → `/deliver-issue` or "Deliver issue #N"
  │   ├─> Large or enabler? → `/preflight-issue` first (optional), then deliver or implement
  │   └─> Split workflow → `/implement-issue` or "Implement issue #N"
  │
  ├─ Have implemented code ready for review (split workflow)?
  │   └─> `/verify-and-create-pr` or "Verify issue #N"
  │
  ├─ Have a PR and need independent conventions review?
  │   └─> New session: `/code-review` on PR #N
  │
  ├─ Has an open PR received coding review comments?
  │   └─> `/address-pr-review-comments` or "Address PR review comments on PR #N"
  │
  ├─ Have merged PR ready for closure?
  │   └─> Run "Close issue #X after PR #Y merged"
  │
  ├─ Need to plan next feature?
  │   └─> Run plan-next-issue workflow (auto-select or specify)
  │
  ├─ Time for a progress review?
  │   └─> Run PM progress review → Review and plan next session(s)
  │
  ├─ Stuck / Blocked / Unsure?
  │   └─> Run daily start workflow → Get recommendation
  │
  └─ Want end-to-end automation?
      └─> Use repo-pm-feature-workflow skill (plans + implements + reviews), or `/deliver-issue` for delivery-only happy path
```

---

## Agent Responsibilities (Who Does What)

### PM Orchestrator ([`.agents/contracts/pm-orchestrator.md`](../.agents/contracts/pm-orchestrator.md))
**Trigger:** "Plan the next item", "What's next?", "Plan feature X", "Run the PM progress review"

**Responsibilities:**
- Selects from backlog (priority, dependencies, milestone)
- Validates scope alignment
- Creates technical plan (Epic/Feature/Story breakdown)
- Sets up GitHub issues with labels/milestones
- Defines test strategy
- Hands off to Delivery Agent when ready

**Boundaries:**
- ❌ Does NOT write code (planning and read-only progress review only)
- ❌ Does NOT close issues (Verify Agent's job)
- ❌ Does NOT override your scope decisions (flags and asks)

---

### Delivery ([`.agents/contracts/delivery.md`](../.agents/contracts/delivery.md))
**Trigger:** "Implement issue #X", "Preflight issue #X", "Deliver issue #X", "Address PR review comments on PR #N", "Build feature X"

**Responsibilities:**
- Runs implementation preflight (context, codebase discovery, touch map, proceed gate)
- Sets `status/in-progress` on the implementing issue (Roadmap Sync updates Project #8)
- Implements code (Domain/Application/Infrastructure/App layers)
- Creates xUnit v3 tests (NSubstitute, `Assert.*`)
- Updates user-facing docs
- Records architectural decisions via `repo-decision-log` / `plan/DECISIONS.md`
- Ensures UK English throughout
- Hands off to Verify Agent when complete
- Addresses PR review comment threads (implement, reply, resolve) on existing pull requests

**Boundaries:**
- ❌ Does NOT start without clear acceptance criteria (escalates to PM Orchestrator)
- ❌ Does NOT create pull requests (Verify Agent's job)
- ❌ Does NOT close issues (Verify Agent's job)
- ❌ Does NOT change scope without your approval

---

### Verify ([`.agents/contracts/verify.md`](../.agents/contracts/verify.md))
**Trigger:** "Verify issue #X", "Create PR for issue #X"

**Responsibilities:**
- Validates quality gates (build, clean rebuild tests, direct package hygiene, docs, backlog sync)
- Creates pull request with metadata from [`PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md)
- Updates issue labels (`status/in-review` → `status/done`)
- Closes issues post-merge
- Hands off to independent `/code-review` in a new session
- Suggests next backlog item

**Boundaries:**
- ❌ Does NOT approve PRs (you approve and merge)
- ❌ Does NOT modify code (escalates to Delivery Agent if fixes needed)
- ❌ Does NOT close issues with failing tests

---

## Workflow Library Reference

Canonical workflow definitions live in [`.agents/workflows/`](../.agents/workflows/). Copilot prompts and Cursor commands are thin mirrors — edit the workflow file only.

| Workflow | Canonical file | Cursor command |
|----------|----------------|----------------|
| Daily start | [`daily-start.md`](../.agents/workflows/daily-start.md) | `/daily-start` |
| Plan next issue | [`plan-next-issue.md`](../.agents/workflows/plan-next-issue.md) | `/plan-next-issue` |
| Deliver issue | [`deliver-issue.md`](../.agents/workflows/deliver-issue.md) | `/deliver-issue` |
| Preflight issue | [`preflight-issue.md`](../.agents/workflows/preflight-issue.md) | `/preflight-issue` |
| Implement issue | [`implement-issue.md`](../.agents/workflows/implement-issue.md) | `/implement-issue` |
| Verify and create PR | [`verify-and-create-pr.md`](../.agents/workflows/verify-and-create-pr.md) | `/verify-and-create-pr` |
| Address PR review comments | [`address-pr-review-comments.md`](../.agents/workflows/address-pr-review-comments.md) | `/address-pr-review-comments` |
| PM progress review | [`pm-progress-review.md`](../.agents/workflows/pm-progress-review.md) | `/pm-progress-review` |
| Code review | [`code-review.md`](../.agents/workflows/code-review.md) | `/code-review` |
| Docs update | [`docs-update.md`](../.agents/workflows/docs-update.md) | `/docs-update` |

See the session and progress-review sections above for orchestration rhythm and quality gates.

---

## Mandatory Completion Gates (Enforced by Agents)

These gates are defined in [`AGENTS.md`](../AGENTS.md) and enforced by role contracts:

### Before Coding (PM Orchestrator enforces)
- ✅ Backlog item selected and scope validated
- ✅ Acceptance criteria clear
- ✅ GitHub issues created with labels/milestones
- ✅ Test strategy defined

### Before Coding (Delivery Agent enforces)
- ✅ Implementation preflight completed (context loaded, touch map produced, proceed gate satisfied)
- ✅ Issue label set to `status/in-progress` (Roadmap Sync updates the board)
- ✅ Linked wireframe read for page-producing UI work

### Before PR Creation (Verify Agent enforces)
- ✅ All acceptance criteria met (Delivery handoff)
- ✅ Code compiles, zero errors/warnings
- ✅ Clean rebuild tests pass (`dotnet clean && dotnet test`)
- ✅ Direct packages added or bumped on this branch are current (`dotnet list package --outdated`)
- ✅ Documentation updated (if user-facing)
- ✅ Decision log updated via `repo-decision-log` (if architectural decision)
- ✅ UK English verified
- ✅ Roadmap item moved to **In Progress** with Start Date and Target Date recorded

### Before Issue Closure (Verify Agent enforces)
- ✅ PR created and approved
- ✅ All quality gates passed
- ✅ Backlog synchronised
- ✅ Roadmap item moved to **Done** with Start Date present and Target Date overwritten to the actual completion date
- ✅ No follow-up blockers

**If any gate fails:** Agent escalates to you for resolution, workflow pauses.

---

## Artefacts Updated by Workflow

| **Artefact**                          | **Updated by**           | **When**                              |
|---------------------------------------|--------------------------|---------------------------------------|
| GitHub Issues / Project #8          | PM Orchestrator, Review  | Planning (create/update), Closure (close issue) |
| `plan/SCOPE.md`                       | PM Orchestrator, Delivery| Scope clarification needed            |
| `plan/IMPLEMENTATION_PLAN.md`         | (Manual by you)          | Phase transitions, major milestones   |
| `plan/RELEASE_PLAN.md`                | Verify Agent             | Breaking changes, release impact      |
| GitHub issues                         | PM Orchestrator, Review  | Planning (create), Review (close)     |
| `src/` (source code)                  | Delivery Agent           | Implementation                        |
| `tests/` (test code)                  | Delivery Agent           | Implementation                        |
| `website/content/docs/` (user guides)        | Delivery Agent           | User-facing features                  |
| `website/content/_index.md` (landing)         | Delivery Agent           | New doc pages added                   |
| `adr/` (archived decisions)           | —                        | Historical reference only             |
| `plan/DECISIONS.md`                   | Delivery Agent           | New decision recorded via `repo-decision-log` |

---

## Escalation Paths

### When Planning Stalls
**Symptom:** Scope ambiguity, missing requirements, no clear acceptance criteria  
**Action:**
1. Pause workflow
2. Update `plan/SCOPE.md` or create/update the corresponding GitHub Issue
3. Re-run plan-next-issue workflow with clarified scope

---

### When Implementation Stalls
**Symptom:** Scope change discovered, architectural decision needed, technical blocker  
**Action:**
1. Delivery Agent flags issue and pauses
2. **If scope change:** Update `plan/SCOPE.md`, re-run PM Orchestrator
3. **If architectural decision:** Record via `repo-decision-log` in `plan/DECISIONS.md`, resume
4. **If technical blocker:** Document in issue comments, add `status/blocked` label, escalate to you
5. **If shelved for later (not blocked):** Document in issue comments, apply `status/ice-box` label (removes from active queue; Roadmap Sync moves the card to **Ice Box**)

---

### When Quality Gates Fail
**Symptom:** Tests failing, docs missing, compile errors, UK English violations  
**Action:**
1. Verify Agent flags failure
2. Escalates to Delivery Agent for fixes
3. After fixes, re-run verify-and-create-pr workflow

---

### When Velocity Drops
**Symptom:** Progress review shows milestone at risk (<50% complete with deadline approaching).  
**Action:**
1. Check for blockers (external dependencies, waiting on review)
2. Check for scope creep (too many in-progress items)
3. Adjust backlog priorities (defer lower-priority work)
4. Simplify features (split large stories into smaller ones)

---

## Integration with Existing Governance

This runbook orchestrates (does NOT duplicate) existing policy:

| **Policy Source**                        | **What It Defines**                     | **How Runbook Uses It**                        |
|------------------------------------------|-----------------------------------------|------------------------------------------------|
| [`AGENTS.md`](../AGENTS.md)        | Mandatory workflow gates, skill matrix  | Contracts enforce gates; runbook references them  |
| `plan/LABEL_STRATEGY.md`                 | Label taxonomy                          | PM Orchestrator applies labels per taxonomy    |
| `plan/PROJECT_MANAGEMENT.md`             | Issue workflow rules                    | Agents follow issue state transitions          |
| `plan/SCOPE.md`                          | In-scope vs. out-of-scope features      | PM Orchestrator validates before planning      |
| GitHub Issues / Project #8               | Prioritised work items                  | PM Orchestrator selects from Issues / Project #8 |
| `plan/IMPLEMENTATION_PLAN.md`            | Phase/milestone definitions             | Agents align work to current phase             |
| `plan/RELEASE_PLAN.md`                   | Release criteria and dates              | Verify Agent checks release impact             |
| `.github/instructions/*.md`              | .NET, Blazor, GitHub Actions standards  | Delivery Agent follows coding standards        |
| [`.agents/skills/*/SKILL.md`](../.agents/skills/)              | Workflow procedures (breakdown, test)   | Contracts invoke skills at correct stages         |

**Design principle:** Runbook is a **lightweight orchestration layer** — it tells you what to run and when, but delegates policy enforcement to existing governance files and AI agents.

---

## Advanced Usage Patterns

### Pattern 1: Delivery Happy Path
**Scenario:** You have a planned issue and want to implement and open a PR in one session.

**Command:**
```
/deliver-issue [number]
```

**What happens:**
1. Preflight (pauses for `size/m+` and enablers unless already completed in this session)
2. Implementation (Delivery Agent)
3. Verify and PR creation (Verify Agent)
4. Handoff to a **new** session for `/code-review`

**Use when:** You have uninterrupted time and want the default delivery loop without chaining slash commands manually.

---

### Pattern 2: End-to-End Automation
**Scenario:** You want to plan and implement a feature in one go.

**Command:**
```
Take the next backlog item and run the full PM feature workflow
```

**What happens:** Invokes `repo-pm-feature-workflow` skill, which:
1. Plans (PM Orchestrator)
2. Implements (Delivery Agent)
3. Verifies (Verify Agent)
4. Creates PR (you approve/merge manually)

**Use when:** You have uninterrupted time and trust the workflow to handle complexity.

---

### Pattern 3: Milestone Sprint
**Scenario:** Focus on completing a specific milestone (e.g., Phase 1).

**Session start:**
```
Plan next item for Phase 1
```

**Throughout the session:** Implement and review milestone issues only.

**After a focused delivery block:** Run progress review, check milestone completion percentage.

---

### Pattern 4: Hotfix Mode
**Scenario:** Critical bug needs immediate fix, bypass planning workflow.

**Steps:**
1. Create issue manually (type/bug, priority/critical)
2. Run `/deliver-issue` or `/implement-issue` (Delivery contract implements)
3. If split workflow: run `/verify-and-create-pr` (create PR)
4. New session: `/code-review` on the PR
5. `/address-pr-review-comments` if review threads remain
6. Approve and merge immediately
7. Run `Close issue #X after PR #Y merged`

**Skip:** Planning workflow (breakdown-plan), test strategy (if time-critical).

---

## Troubleshooting

### "Agent says issue not ready for implementation"
**Cause:** Missing acceptance criteria, labels, or scope validation.  
**Fix:** Re-run plan-next-issue workflow to complete planning.

---

### "Quality gate failed: documentation missing"
**Cause:** User-facing feature without `website/content/docs/*.md`.  
**Fix:** Delivery Agent escalates; add docs manually or re-run `implement-issue` with explicit doc request.

---

### "Scope drift detected during review"
**Cause:** Implementation added features not in `plan/SCOPE.md`.  
**Fix:** Update `plan/SCOPE.md`, get approval, re-run verify.

---

### "Progress review shows milestone at risk"
**Cause:** <50% complete with deadline approaching.  
**Fix:** Re-prioritise backlog (defer lower-priority items), or extend milestone deadline in `plan/IMPLEMENTATION_PLAN.md`.

---

## Checklist: "Am I Using This Correctly?"

- [ ] I run `daily-start` at the start of each working session to get oriented
- [ ] I use `plan-next-issue` to create technical specs before coding
- [ ] I use `/deliver-issue` for the default happy path, or split `/implement-issue` and `/verify-and-create-pr` when I want to pause between stages
- [ ] `implement-issue` runs preflight before coding; I use `/preflight-issue` first for large items or enablers when I want to review the approach
- [ ] I run `/code-review` in a **new** agent session after the PR exists (not in the same chat as implement/verify)
- [ ] I use `/address-pr-review-comments` to implement findings and resolve review threads before merge
- [ ] I run `pm-progress-review` when I return to the project after a gap or need a governance checkpoint
- [ ] All my GitHub issues have labels per `plan/LABEL_STRATEGY.md`
- [ ] All user-facing features have docs in `website/content/docs/`
- [ ] All architectural decisions are recorded in `plan/DECISIONS.md` via `repo-decision-log`
- [ ] I create or update GitHub Issues when new work is discovered
- [ ] I update `plan/SCOPE.md` when scope changes
- [ ] I approve and merge PRs manually (agents don't auto-merge)

---

## Quick Command Reference

**Session start:**
```
Run the daily start workflow
```

**Planning:**
```
Plan the next item
Plan the [feature name]
Plan next item for [milestone]
```

**Delivery (happy path):**
```
Deliver issue #[number]
```

**Implementation (split):**
```
Preflight issue #[number]
Implement issue #[number]
Build [feature name]
```

**Verify:**
```
Verify issue #[number]
Create PR for [feature name]
```

**Code review (new session):**
```
Code review PR #[number]
```

**Address review threads:**
```
Address PR review comments on PR #[number]
```

**Closure:**
```
Close issue #[number] after PR #[number] merged
```

**Progress review:**
```
Run the PM progress review
```

**End-to-End:**
```
Take the next backlog item and run the full PM feature workflow
```

---

## This Runbook in Context

**This runbook is one part of your PM operating system:**

1. **Planning artefacts** (`plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md`, GitHub Issues) — define what to build
2. **Governance** ([`AGENTS.md`](../AGENTS.md), `plan/LABEL_STRATEGY.md`, `plan/PROJECT_MANAGEMENT.md`) — define how to build it
3. **Role contracts** ([`.agents/contracts/`](../.agents/contracts/)) — execution contracts (who does what)
4. **Workflow entry points** ([`.agents/workflows/`](../.agents/workflows/)) — canonical workflow stubs (when to do it)
5. **This runbook** — orchestration guide (session rhythm and progress reviews)

**Use this runbook as your operating reference.** It tells you which workflow to run at each stage, what each produces, and what gates must pass before moving forward.

---

**Last Updated:** March 5, 2026 (delivery loop: `/deliver-issue`, verify hygiene, review threads)  
**Version:** 1.0  
**Maintained by:** Product Manager (you)
