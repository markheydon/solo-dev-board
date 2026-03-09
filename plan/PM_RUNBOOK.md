# Product Manager Daily Runbook

**Purpose:** Your single source of truth for managing SoloDevBoard development using AI agents and prompts.

This runbook orchestrates the `.github/agents/` and `.github/prompts/` workflows to ensure consistent, high-quality delivery from backlog to release.

---

## Quick Reference Card

| **When you want to...**                  | **Run this prompt/agent**                                  | **What it produces**                          |
|------------------------------------------|------------------------------------------------------------|-----------------------------------------------|
| Start your day                           | `.github/prompts/daily-start.prompt.md`                    | Status summary + next action recommendation   |
| Plan the next feature                    | `.github/prompts/plan-next-issue.prompt.md`                | GitHub issues with full metadata + tech spec  |
| Implement planned work                   | `.github/prompts/execute-feature.prompt.md`                | Code + tests + docs + ADR (if needed)         |
| Review and create PR                     | `.github/prompts/review-and-close.prompt.md`               | PR + quality validation + issue closure       |
| Weekly health check                      | `.github/prompts/weekly-pm-review.prompt.md`               | Executive summary + priorities for next week  |
| End-to-end feature delivery              | `.github/skills/pm-feature-workflow/SKILL.md`              | Full workflow from backlog to closure         |

---

## Daily Operating Rhythm

### Morning Ritual (5-10 minutes)

**Goal:** Get oriented, identify priorities, clear blockers.

#### Step 1: Run Daily Start Prompt
```
Run the daily start workflow
```

**What it does:**
- Lists active work (in-progress issues, PRs awaiting review)
- Shows backlog health (priority breakdown, ready items)
- Flags blockers (dependencies, stale work)
- Recommends your next action

**What you produce:** Decision on what to work on today.

**Example output:**
```
📋 Active Work: PR #50 ready to merge
📊 Backlog: 3 high-priority items ready for planning
✅ Recommended: Merge PR #50, then plan "One-Click Migration UI"
```

**Next action:** Follow the recommendation (see below for execution patterns).

---

### Execution Ritual (throughout the day)

**Goal:** Move work from backlog → planned → implemented → reviewed → closed.

You'll follow this **4-stage workflow** for each feature:

#### Stage 1: Planning (30-60 minutes per feature)
**Trigger:** Daily start recommends planning, or you decide to plan specific item.

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

**Run:**
```
Implement issue #[number]
```

**What it does:**
- Invokes **Delivery Agent**
- Writes code following layered architecture (Domain/Application/Infrastructure/App)
- Creates xUnit tests (Moq, `Assert.*`, correct naming)
- Updates user-facing docs in `docs/user-guide/` if needed
- Creates ADR in `adr/` if architectural decision made
- Ensures UK English throughout

**What you produce:**
- Source code in `src/` (compiles, follows conventions)
- Test code in `tests/` (passes, full coverage)
- Documentation updates (user guides, ADRs, XML comments)

**Gates before proceeding:**
- ✅ All acceptance criteria met
- ✅ Code compiles, zero errors/warnings
- ✅ Tests pass locally (`dotnet test`)
- ✅ Docs updated (if user-facing feature)
- ✅ ADR created (if architectural decision)
- ✅ UK English verified

**Next action:** Move to Stage 3 (Review).

---

#### Stage 3: Review & PR Creation (15-30 minutes per feature)
**Trigger:** Implementation complete, ready for quality check and PR.

**Run:**
```
Review issue #[number]
```

**What it does:**
- Invokes **Review Agent**
- Validates all quality gates (code, tests, docs)
- Creates pull request with proper metadata
- Updates issue labels to `status/in-review`
- Provides review summary

**What you produce:**
- Pull request linked to issue
- Quality validation report
- Issue ready for your PR approval

**Gates before proceeding:**
- ✅ All quality gates passed
- ✅ PR created and linked to issue
- ✅ No compile errors, tests pass
- ✅ Documentation complete
- ✅ Backlog synchronised

**Next action:** **You** approve and merge PR (manual step).

---

#### Stage 4: Closure (5 minutes per feature)
**Trigger:** PR approved and merged to main.

**Run:**
```
Close issue #[number] after PR #[number] merged
```

**What it does:**
- Review Agent updates issue labels to `status/done`
- Closes issue with comment linking PR
- Updates `plan/BACKLOG.md` marking complete
- Suggests next backlog item

**What you produce:**
- Issue closed and archived
- Backlog up to date
- Ready for next work item

**Gates before repeating cycle:**
- ✅ Issue closed with `status/done`
- ✅ Backlog updated
- ✅ No follow-up items (or new issues created if needed)

**Next action:** Return to Stage 1 (Planning) for next feature.

---

### End-of-Day Ritual (5 minutes)

**Goal:** Leave work in a clean state, capture notes for tomorrow.

#### Step 1: Quick Status Check
**Run:**
```
Run the daily start workflow
```
*(Yes, same as morning — but now you see end-of-day status)*

**What to check:**
- Any PRs still awaiting your approval? (Approve/merge before end of day if possible)
- Any work still `status/in-progress`? (Leave note on what's next)
- Any new blockers? (Document in issue comments)

#### Step 2: Update Planning Artefacts (if needed)
- **Scope changed?** Update `plan/SCOPE.md`
- **New items discovered?** Add to `plan/BACKLOG.md`
- **Release impact?** Update `plan/RELEASE_PLAN.md`

#### Step 3: Commit and Push
Ensure all today's work is committed and pushed to your branch.

---

## Weekly Operating Rhythm

### End-of-Week Review (30-45 minutes, typically Friday or Monday)

**Goal:** Assess project health, validate governance, plan next week's priorities.

#### Run Weekly PM Review Prompt
```
Run the weekly PM review
```

**What it does:**
- Calculates milestone progress and completion estimates
- Validates scope (no drift detected)
- Checks backlog hygiene (missing metadata, stale items)
- Assesses release confidence (MVP completion, docs, ADRs)
- Identifies blockers and velocity trends
- Recommends top 3 priorities for next week

**What you produce:**
- Weekly executive summary (status report)
- Top 3 priorities for coming week
- Backlog grooming to-do list (metadata gaps, stale items)
- Release confidence assessment

**Actions after review:**
- Resolve flagged blockers
- Update backlog metadata (acceptance criteria, size estimates)
- Adjust priorities if milestones at risk
- Update `plan/RELEASE_PLAN.md` if release confidence low

**Next action:** Use top 3 priorities to guide next week's `plan-next-issue` selections.

---

## Workflow Decision Tree

Use this decision tree when you're unsure what to do next:

```
START
  │
  ├─ Morning / Start of Session?
  │   └─> Run daily-start.prompt.md → Follow recommendation
  │
  ├─ Have planned issue ready for coding?
  │   └─> Run execute-feature.prompt.md with issue number
  │
  ├─ Have implemented code ready for review?
  │   └─> Run review-and-close.prompt.md with issue number
  │
  ├─ Have merged PR ready for closure?
  │   └─> Run "Close issue #X after PR #Y merged"
  │
  ├─ Need to plan next feature?
  │   └─> Run plan-next-issue.prompt.md (auto-select or specify)
  │
  ├─ End of week?
  │   └─> Run weekly-pm-review.prompt.md → Review and plan next week
  │
  ├─ Stuck / Blocked / Unsure?
  │   └─> Run daily-start.prompt.md → Get recommendation
  │
  └─ Want end-to-end automation?
      └─> Use pm-feature-workflow skill (plans + implements + reviews)
```

---

## Agent Responsibilities (Who Does What)

### PM Orchestrator Agent (`.github/agents/pm-orchestrator.agent.md`)
**Trigger:** "Plan the next item", "What's next?", "Plan feature X"

**Responsibilities:**
- Selects from backlog (priority, dependencies, milestone)
- Validates scope alignment
- Creates technical plan (Epic/Feature/Story breakdown)
- Sets up GitHub issues with labels/milestones
- Defines test strategy
- Hands off to Delivery Agent when ready

**Boundaries:**
- ❌ Does NOT write code (planning only)
- ❌ Does NOT close issues (Review Agent's job)
- ❌ Does NOT override your scope decisions (flags and asks)

---

### Delivery Agent (`.github/agents/delivery.agent.md`)
**Trigger:** "Implement issue #X", "Build feature X"

**Responsibilities:**
- Implements code (Domain/Application/Infrastructure/App layers)
- Creates xUnit tests (Moq, `Assert.*`)
- Updates user-facing docs
- Creates ADRs for architectural decisions
- Ensures UK English throughout
- Hands off to Review Agent when complete

**Boundaries:**
- ❌ Does NOT start without clear acceptance criteria (escalates to PM Orchestrator)
- ❌ Does NOT close issues (Review Agent's job)
- ❌ Does NOT change scope without your approval

---

### Review Agent (`.github/agents/review.agent.md`)
**Trigger:** "Review issue #X", "Create PR for X"

**Responsibilities:**
- Validates quality gates (code, tests, docs, backlog sync)
- Creates pull request with metadata
- Updates issue labels (`status/in-review` → `status/done`)
- Closes issues post-merge
- Suggests next backlog item

**Boundaries:**
- ❌ Does NOT approve PRs (you approve and merge)
- ❌ Does NOT modify code (escalates to Delivery Agent if fixes needed)
- ❌ Does NOT close issues with failing tests

---

## Prompt Library Reference

### 1. Daily Start (`.github/prompts/daily-start.prompt.md`)
**When:** Morning / start of session  
**Input:** None  
**Output:** Status summary + next action recommendation  
**Duration:** Auto (read-only, fast)

---

### 2. Plan Next Issue (`.github/prompts/plan-next-issue.prompt.md`)
**When:** Ready to plan next feature  
**Input:** None (auto-select) OR "feature name" (explicit)  
**Output:** GitHub issues + technical spec + test strategy  
**Duration:** 5-15 minutes (depends on feature complexity)  
**Invokes:** PM Orchestrator Agent, breakdown-plan skill, breakdown-test skill

**Two-level planning:** This prompt performs **both**:
1. **PM Planning:** Selects item from backlog, validates scope
2. **Technical Planning:** Invokes `breakdown-plan` to create Epic/Feature/Story spec

---

### 3. Execute Feature (`.github/prompts/execute-feature.prompt.md`)
**When:** After planning, ready to code  
**Input:** Issue number OR feature name  
**Output:** Code + tests + docs + ADR (if needed)  
**Duration:** 30 minutes to 8 hours (depends on size: xs → xl)  
**Invokes:** Delivery Agent, dotnet-best-practices, mudblazor, csharp-xunit, csharp-docs, create-architectural-decision-record

---

### 4. Review and Close (`.github/prompts/review-and-close.prompt.md`)
**When:** After implementation, ready for PR  
**Input:** Issue number OR feature name  
**Output:** PR + quality validation + issue closure (post-merge)  
**Duration:** 5-15 minutes  
**Invokes:** Review Agent, get_errors, github-issues

**Two-part execution:**
1. **Pre-merge:** Creates PR, validates gates, updates labels to `status/in-review`
2. **Post-merge:** (After you approve/merge) Closes issue with `status/done`

---

### 5. Weekly PM Review (`.github/prompts/weekly-pm-review.prompt.md`)
**When:** End of week / start of new week  
**Input:** None  
**Output:** Executive summary + top 3 priorities + blocker analysis  
**Duration:** Auto (read-only, comprehensive)

---

## Mandatory Completion Gates (Enforced by Agents)

These gates are defined in `.github/copilot-instructions.md` and enforced by agents:

### Before Coding (PM Orchestrator enforces)
- ✅ Backlog item selected and scope validated
- ✅ Acceptance criteria clear
- ✅ GitHub issues created with labels/milestones
- ✅ Test strategy defined

### Before PR Creation (Delivery Agent enforces)
- ✅ All acceptance criteria met
- ✅ Code compiles, zero errors/warnings
- ✅ Tests pass locally
- ✅ Documentation updated (if user-facing)
- ✅ ADR created (if architectural decision)
- ✅ UK English verified

### Before Issue Closure (Review Agent enforces)
- ✅ PR created and approved
- ✅ All quality gates passed
- ✅ Backlog synchronised
- ✅ No follow-up blockers

**If any gate fails:** Agent escalates to you for resolution, workflow pauses.

---

## Artefacts Updated by Workflow

| **Artefact**                          | **Updated by**           | **When**                              |
|---------------------------------------|--------------------------|---------------------------------------|
| `plan/BACKLOG.md`                     | PM Orchestrator, Review  | Planning (in-progress), Closure (done)|
| `plan/SCOPE.md`                       | PM Orchestrator, Delivery| Scope clarification needed            |
| `plan/IMPLEMENTATION_PLAN.md`         | (Manual by you)          | Phase transitions, major milestones   |
| `plan/RELEASE_PLAN.md`                | Review Agent             | Breaking changes, release impact      |
| GitHub issues                         | PM Orchestrator, Review  | Planning (create), Review (close)     |
| `src/` (source code)                  | Delivery Agent           | Implementation                        |
| `tests/` (test code)                  | Delivery Agent           | Implementation                        |
| `docs/user-guide/` (user docs)        | Delivery Agent           | User-facing features                  |
| `docs/index.md` (quick links)         | Delivery Agent           | New doc pages added                   |
| `adr/` (architectural decisions)      | Delivery Agent           | Architectural decisions               |
| `adr/README.md` (ADR index)           | Delivery Agent           | New ADR created                       |

---

## Escalation Paths

### When Planning Stalls
**Symptom:** Scope ambiguity, missing requirements, no clear acceptance criteria  
**Action:**
1. Pause workflow
2. Update `plan/SCOPE.md` or `plan/BACKLOG.md` manually
3. Re-run `plan-next-issue.prompt.md` with clarified scope

---

### When Implementation Stalls
**Symptom:** Scope change discovered, architectural decision needed, technical blocker  
**Action:**
1. Delivery Agent flags issue and pauses
2. **If scope change:** Update `plan/SCOPE.md`, re-run PM Orchestrator
3. **If architectural decision:** Document decision, create ADR, resume
4. **If technical blocker:** Document in issue comments, add `status/blocked` label, escalate to you

---

### When Quality Gates Fail
**Symptom:** Tests failing, docs missing, compile errors, UK English violations  
**Action:**
1. Review Agent flags failure
2. Escalates to Delivery Agent for fixes
3. After fixes, re-run `review-and-close.prompt.md`

---

### When Velocity Drops
**Symptom:** Weekly review shows <50% of average velocity  
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
| `.github/copilot-instructions.md`        | Mandatory workflow gates, skill matrix  | Agents enforce gates; runbook references them  |
| `plan/LABEL_STRATEGY.md`                 | Label taxonomy                          | PM Orchestrator applies labels per taxonomy    |
| `plan/PROJECT_MANAGEMENT.md`             | Issue workflow rules                    | Agents follow issue state transitions          |
| `plan/SCOPE.md`                          | In-scope vs. out-of-scope features      | PM Orchestrator validates before planning      |
| `plan/BACKLOG.md`                        | Prioritised work items                  | PM Orchestrator selects from backlog           |
| `plan/IMPLEMENTATION_PLAN.md`            | Phase/milestone definitions             | Agents align work to current phase             |
| `plan/RELEASE_PLAN.md`                   | Release criteria and dates              | Review Agent checks release impact             |
| `.github/instructions/*.md`              | .NET, Blazor, GitHub Actions standards  | Delivery Agent follows coding standards        |
| `.github/skills/*.md`                    | Workflow procedures (breakdown, test)   | Agents invoke skills at correct stages         |

**Design principle:** Runbook is a **lightweight orchestration layer** — it tells you what to run and when, but delegates policy enforcement to existing governance files and AI agents.

---

## Advanced Usage Patterns

### Pattern 1: End-to-End Automation
**Scenario:** You want to plan and implement a feature in one go.

**Command:**
```
Take the next backlog item and run the full PM feature workflow
```

**What happens:** Invokes `pm-feature-workflow` skill, which:
1. Plans (PM Orchestrator)
2. Implements (Delivery Agent)
3. Reviews (Review Agent)
4. Creates PR (you approve/merge manually)

**Use when:** You have uninterrupted time and trust the workflow to handle complexity.

---

### Pattern 2: Milestone Sprint
**Scenario:** Focus on completing a specific milestone (e.g., Phase 1).

**Morning routine:**
```
Plan next item for Phase 1
```

**Throughout day:** Implement and review Phase 1 issues only.

**End of week:** Run weekly review, check Phase 1 completion percentage.

---

### Pattern 3: Hotfix Mode
**Scenario:** Critical bug needs immediate fix, bypass planning workflow.

**Steps:**
1. Create issue manually (type/bug, priority/critical)
2. Run `execute-feature.prompt.md` with issue number (Delivery Agent implements)
3. Run `review-and-close.prompt.md` (create PR)
4. Approve and merge immediately
5. Run `Close issue #X after PR #Y merged`

**Skip:** Planning workflow (breakdown-plan), test strategy (if time-critical).

---

## Troubleshooting

### "Agent says issue not ready for implementation"
**Cause:** Missing acceptance criteria, labels, or scope validation.  
**Fix:** Re-run `plan-next-issue.prompt.md` to complete planning.

---

### "Quality gate failed: documentation missing"
**Cause:** User-facing feature without `docs/user-guide/*.md`.  
**Fix:** Delivery Agent escalates; add docs manually or re-run `execute-feature.prompt.md` with explicit doc request.

---

### "Scope drift detected during review"
**Cause:** Implementation added features not in `plan/SCOPE.md`.  
**Fix:** Update `plan/SCOPE.md`, get approval, re-run review.

---

### "Weekly review shows milestone at risk"
**Cause:** <50% complete with <2 weeks remaining.  
**Fix:** Re-prioritise backlog (defer lower-priority items), or extend milestone deadline in `plan/IMPLEMENTATION_PLAN.md`.

---

## Checklist: "Am I Using This Correctly?"

- [ ] I run `daily-start` every morning to get oriented
- [ ] I use `plan-next-issue` to create technical specs before coding
- [ ] I use `execute-feature` only for planned issues with clear acceptance criteria
- [ ] I use `review-and-close` to validate quality before PR merge
- [ ] I run `weekly-pm-review` at least once per week
- [ ] All my GitHub issues have labels per `plan/LABEL_STRATEGY.md`
- [ ] All user-facing features have docs in `docs/user-guide/`
- [ ] All architectural decisions have ADRs in `adr/`
- [ ] I update `plan/BACKLOG.md` when new work discovered
- [ ] I update `plan/SCOPE.md` when scope changes
- [ ] I approve and merge PRs manually (agents don't auto-merge)

---

## Quick Command Reference

**Morning:**
```
Run the daily start workflow
```

**Planning:**
```
Plan the next item
Plan the [feature name]
Plan next item for [milestone]
```

**Implementation:**
```
Implement issue #[number]
Build [feature name]
```

**Review:**
```
Review issue #[number]
Create PR for [feature name]
```

**Closure:**
```
Close issue #[number] after PR #[number] merged
```

**Weekly:**
```
Run the weekly PM review
```

**End-to-End:**
```
Take the next backlog item and run the full PM feature workflow
```

---

## This Runbook in Context

**This runbook is one part of your PM operating system:**

1. **Planning artefacts** (`plan/BACKLOG.md`, `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md`) — define what to build
2. **Governance** (`.github/copilot-instructions.md`, `plan/LABEL_STRATEGY.md`, `plan/PROJECT_MANAGEMENT.md`) — define how to build it
3. **Agents** (`.github/agents/*.agent.md`) — execution contracts (who does what)
4. **Prompts** (`.github/prompts/*.prompt.md`) — reusable workflows (when to do it)
5. **This runbook** — orchestration guide (daily/weekly rhythm)

**Use this runbook as your daily reference.** It tells you what prompt to run at each stage, what each prompt produces, and what gates must pass before moving forward.

---

**Last Updated:** March 5, 2026  
**Version:** 1.0  
**Maintained by:** Product Manager (you)
