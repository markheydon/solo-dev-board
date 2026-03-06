---
name: Plan Next Issue
description: Selects next backlog item (or specific feature), validates scope, creates technical plan via breakdown-plan skill, and sets up GitHub issues with correct metadata. Invokes PM Orchestrator Agent.
agent: PM Orchestrator
---

# Plan Next Issue Workflow

**When to use:** When you're ready to plan the next piece of work from the backlog. This produces a technical specification and creates GitHub issues ready for implementation.

---

## Purpose

Execute the full planning workflow:
1. Select next item from backlog (or plan a specific item you specify)
2. Validate scope alignment
3. Create technical plan using `breakdown-plan` skill (Epic/Feature/Story decomposition)
4. Set up GitHub issues with correct labels, milestones, acceptance criteria
5. Define test strategy using `breakdown-test` skill

**Result:** Work is planned, issues created, and ready for the Delivery Agent to implement.

---

## Inputs Required

Provide **ONE** of:
- **Implicit selection:** "Plan the next item" (agent selects highest-priority unblocked item)
- **Explicit feature:** "Plan the Label Manager UI" (agent plans that specific feature)
- **Milestone filter:** "Plan next item for Phase 1" (agent filters by milestone)

---

## Workflow Steps

This prompt invokes the **PM Orchestrator Agent**, which executes:

### 1. Backlog Selection
- Read `plan/BACKLOG.md`
- Filter by:
  - Priority (critical > high > medium > low)
  - Dependencies (unblocked items only)
  - Milestone alignment (current phase from `plan/IMPLEMENTATION_PLAN.md`)
- Select next candidate or locate specified feature

### 2. Scope Validation
- Check `plan/SCOPE.md` to ensure item is in-scope
- Flag scope drift if detected
- Pause for user approval if scope clarification needed

### 3. Technical Planning
- Invoke `breakdown-plan` skill to decompose work
- Produce Epic > Feature > Story/Enabler > Test hierarchy
- Generate acceptance criteria for each work item
- Identify dependencies and blockers
- Estimate size (xs/s/m/l/xl)

### 4. ADR Check
- Determine if architectural decision required
- If yes, invoke `create-architectural-decision-record` skill
- Create ADR in `adr/` directory
- Update `adr/README.md` index

### 5. GitHub Issue Creation
- Use `github-issues` skill to create issues
- Apply labels per `plan/LABEL_STRATEGY.md`:
  - `type/` — feature, bug, chore, documentation, epic
  - `priority/` — critical, high, medium, low
  - `area/` — dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs
  - `size/` — xs, s, m, l, xl
  - `status/todo` for new items
- Assign to current milestone if applicable
- Note parent/child hierarchy and blocking relationships — GitHub CLI supports neither; list both in the **Manual Linking Required** section of the handoff for the user to set via the GitHub UI

### 6. Project Board Sync
- Use `github-project` skill (Lifecycle Event 1) to add each created issue to the **SoloDevBoard Roadmap** project (#8)
- Set Phase matching the issue's milestone (see Phase Assignment Rules in `github-project` skill)
- Set Priority matching the `priority/` label applied to each issue
- Set Status to "Todo" for all new issues
- Set Start Date and Target Date from the Roadmap Date Guidelines in `github-project` skill

### 7. Test Strategy
- Invoke `breakdown-test` skill
- Create test issues linked to feature issues
- Define quality gates and acceptance test scenarios

---

## Outputs Produced

### Artefacts Created
- **GitHub issues** with full metadata (title, description, labels, milestones, acceptance criteria)
- **Technical plan** embedded in epic/feature issue descriptions
- **Test issues** linked to feature work
- **ADR** if architectural decision required
- **Project board items** — all issues added to SoloDevBoard Roadmap with Phase/Priority/Status/dates

### Artefacts Updated
- **`plan/BACKLOG.md`** — item marked as planned/in-progress
- **`plan/SCOPE.md`** — updated if scope clarification needed
- **`adr/README.md`** — updated if new ADR created

### Planning Summary Delivered
```markdown
# Planning Complete: Label Manager UI

## Issues Created
- Epic #30: Label Management System
  - Feature #31: Label Manager UI (size/m, priority/high, area/labels)
  - Story #32: As a user, I can view all repository labels (size/s)
  - Story #33: As a user, I can filter labels by type (size/xs)
  - Story #34: As a user, I can create/edit/delete labels (size/m)
  - Test #35: Label Manager UI acceptance tests (size/s)
  - Test #36: Label CRUD operation tests (size/xs)

## Technical Plan Summary
- **Architecture:** Blazor Server page with FluentDataGrid component
- **Layers affected:** App (UI), Application (service), Infrastructure (GitHub API client)
- **Dependencies:** Requires GitHubService enhancement to support label CRUD
- **ADR created:** ADR-0007 (Fluent UI component selection)

## Test Strategy
- Unit tests: Label validation, filtering logic
- Integration tests: GitHub API label operations
- UI tests: Component rendering, user interactions

## Manual Linking Required
Set these relationships via the GitHub UI — `gh` CLI supports neither sub-issues nor blocking (see [cli/cli#11757](https://github.com/cli/cli/issues/11757), [cli/cli#10298](https://github.com/cli/cli/issues/10298)):

**Sub-issues** (open parent issue → Sub-issues → Add):

| Parent Issue | Child Issue(s) | Relationship |
|---|---|---|
| #30 Label Management System (Epic) | #31 Label Manager UI (Feature) | Epic → Feature |
| #31 Label Manager UI (Feature) | #32, #33, #34, #35, #36 | Feature → deliverables |

**Blocking** (open issue → Relationships → Mark as blocking):

| Blocking Issue | Blocked Issue | Type |
|---|---|---|
| _(none for this feature)_ | — | — |

## Next Action
✅ Ready for implementation — Use `execute-feature` prompt with issue #31
```

---

## Agents/Skills Invoked

- **PM Orchestrator Agent** — full planning orchestration
- **`breakdown-plan` skill** — technical decomposition
- **`breakdown-test` skill** — quality planning
- **`create-architectural-decision-record` skill** — ADR creation (if needed)
- **`github-issues` skill** — issue creation and metadata

---

## Follow-Up Prompts

After planning completes:
- **To implement:** Run `execute-feature.prompt.md` with the primary feature issue number
- **To review plan:** Ask "Show me the technical plan for issue #X"
- **To adjust scope:** Manually update `plan/SCOPE.md`, then re-run this prompt

---

## Example Invocations

### Example 1: Auto-select next item
**You say:**
```
Plan the next item
```

**Agent executes:**
- Reads backlog, selects "One-Click Migration UI" (highest priority, unblocked)
- Validates scope
- Creates epic #40 with 6 stories and 3 test issues
- Delivers planning summary

**Output:**
```
Planned "One-Click Migration UI" as epic #40. Created 9 issues (6 stories, 3 tests).
Ready for implementation. Use execute-feature prompt with issue #40.
```

---

### Example 2: Plan specific feature
**You say:**
```
Plan the Label Manager UI
```

**Agent executes:**
- Locates "Label Manager UI" in backlog
- Validates scope (in-scope)
- Creates technical plan (5 issues total)
- Creates ADR for UI component choice
- Delivers planning summary

**Output:**
```
Planned "Label Manager UI" as epic #30. Created ADR-0007 for Fluent UI selection.
5 issues created (3 stories, 2 tests). Ready for execute-feature prompt.
```

---

### Example 3: Milestone-filtered planning
**You say:**
```
Plan next item for Phase 1
```

**Agent executes:**
- Filters backlog by Phase 1 milestone
- Selects highest-priority Phase 1 item
- Creates plan and issues
- Delivers planning summary

**Output:**
```
Planned "Triage UI scaffolding" for Phase 1. Issue #25 created.
Phase 1 progress: 50% (5 of 10 issues planned). Ready for implementation.
```

---

## Scope Escalation

If scope ambiguity detected:
- Agent pauses and flags issue
- Provides recommendation (in-scope vs. out-of-scope)
- Waits for your decision before proceeding

**Example:**
```
⚠️ Scope Check Required:
"Real-time GitHub webhook integration" is not explicitly in plan/SCOPE.md.

Recommendation: Out of scope for initial release (Phase 1-2).
Suggest: Add to plan/BACKLOG.md under "Future Enhancements (Out of Scope)".

Proceed with planning? (yes/no)
```

---

## Mandatory Gates (Before Planning Complete)

Planning is NOT complete until:
- ✅ Scope validated in `plan/SCOPE.md`
- ✅ Technical plan created via `breakdown-plan`
- ✅ GitHub issues created with correct labels/milestones
- ✅ Test strategy defined via `breakdown-test`
- ✅ ADR created if architectural decision made
- ✅ Dependencies and acceptance criteria documented
- ✅ Backlog updated to reflect planning status

---

## Usage Tips

**Best for:**
- Starting fresh work from backlog
- Planning large features that need decomposition
- Creating structured issue hierarchy

**Not for:**
- Simple bug fixes (create issue directly)
- Urgent hotfixes (skip planning, fix immediately)
- Documentation-only changes (create issue manually)
