---
name: Review and Close
description: Creates PR, validates quality gates, verifies documentation sync, runs tests, and closes issues after approval. Invokes Review Agent.
agent: Review Agent
---

# Review and Close Workflow

**When to use:** After implementation completes (via `execute-feature` prompt), use this to validate quality, create PR, and close the issue.

---

## Purpose

Execute the full review and closure workflow:
1. Validate all quality gates (code, tests, docs)
2. Create pull request with proper metadata
3. Update issue labels to `status/in-review`
4. After PR merge: close issue with `status/done`
5. Synchronise backlog and suggest next work

**Result:** PR created, quality validated, issue closed (post-merge), ready for next work item.

---

## Inputs Required

Provide **ONE** of:
- **Issue number:** "Review issue #31"
- **Feature name:** "Review Label Manager implementation"
- **Branch name:** "Review branch feature/label-manager"

---

## Workflow Steps

This prompt invokes the **Review Agent**, which executes:

### 1. Quality Gate Validation

#### Code Quality
- Run `get_errors` to check for compile errors/warnings
- Verify no new errors introduced
- Confirm layered architecture rules followed
- Check UK English used throughout (no US spellings)

#### Test Coverage
- Verify tests exist for new/changed code
- Confirm test naming follows `MethodUnderTest_Scenario_ExpectedOutcome`
- Validate tests pass locally (suggest: `dotnet test`)
- Check test projects mirror source project structure

#### Documentation Sync
- Verify user-facing features have docs in `docs/user-guide/`
- Confirm `docs/index.md` quick links updated if new page added
- Check ADR created if architectural decision made
- Validate `adr/README.md` updated if new ADR added

#### Backlog Sync
- Confirm `plan/BACKLOG.md` reflects completion status
- Verify `plan/SCOPE.md` updated if scope changed
- Check `plan/IMPLEMENTATION_PLAN.md` phase alignment

### 2. PR Creation
- **Before raising the PR:** Update `plan/BACKLOG.md` to mark the item as complete and commit that change to the feature branch so it is included in the PR merge. This prevents an uncommitted planning artefact after merge.
- Create pull request with:
  - Title following convention: `[type/label] Brief description`
  - PR body linking issues: `Closes #X`, `Relates to #Y`
  - **Assignee:** always assign to `markheydon` (the sole developer on this project)
  - **Labels:** copy `type/`, `priority/`, `area/`, and `size/` labels from the linked issue; add `status/in-review`; do **not** carry over `status/in-progress` or `status/todo`
  - Milestone assignment if applicable (same milestone as linked issue)
- Use `.github/pull_request_template.md` if present
- Link PR to GitHub project board if tracking enabled

### 3. Release Readiness Check
- Verify `plan/RELEASE_PLAN.md` updated if work affects next release
- Check changelog implications (suggest user adds entry)
- Flag breaking changes or migration requirements
- Confirm infrastructure changes have Bicep updates if applicable

### 4. Issue Label Update
- Update issue labels:
  - Remove `status/in-progress`
  - Add `status/in-review`
- Add PR link in issue comments
- Notify user of review completion

### 5. Post-Merge Closure
*(After you approve and merge PR)*
- Update issue labels:
  - Remove `status/in-review`
  - Add `status/done`
- Close issue with comment linking PR
- **Project board update:** Use `github-project` skill (Lifecycle Event 3) to set project Status → "Done" and **overwrite Target Date with today's actual completion date** (not the original planned date)
- Suggest next backlog item for PM Orchestrator

---

## Outputs Produced

### Artefacts Created
- **Pull request** with full metadata (title, description, labels, milestone, linked issues)
- **Review comment** summarizing quality gate validation results

### Artefacts Updated (Post-PR Creation)
- **Issue labels** — `status/in-progress` → `status/in-review`
- **Issue comments** — PR link added

### Artefacts Updated (Post-Merge)
- **Issue status** — `status/in-review` → `status/done`, issue closed
- **Project board** — Status set to "Done", Target Date overwritten with actual completion date via `github-project` skill
- **`plan/RELEASE_PLAN.md`** — updated if release impact (flagged for user)

### Review Summary Delivered
```markdown
# Review Complete: Label Manager UI (Issue #31)

## Quality Gates: ✅ ALL PASSED

### Code Quality
- ✅ Zero compile errors/warnings
- ✅ UK English verified (colour, organise, behaviour)
- ✅ Layered architecture followed (Domain/Application/Infrastructure/App)
- ✅ No business logic in Razor components

### Test Coverage
- ✅ 26 tests added (8 UI, 12 service, 6 integration)
- ✅ Test naming follows convention
- ✅ All tests passing (ran `dotnet test`)
- ✅ Test projects mirror source structure

### Documentation
- ✅ User guide created: docs/user-guide/label-manager.md
- ✅ Index updated: docs/index.md quick links
- ✅ ADR created: adr/0007-fluent-ui-datagrid.md
- ✅ ADR index updated: adr/README.md

### Backlog Sync
- ✅ plan/BACKLOG.md updated (Label Manager → complete)
- ✅ No scope changes (plan/SCOPE.md unchanged)
- ✅ Phase 1 alignment verified

## Pull Request Created
- **PR #50**: [feature] Label Manager UI with FluentDataGrid
- **Link**: https://github.com/markheydon/solo-dev-board/pull/50
- **Closes**: #31
- **Labels**: type/story, priority/high, area/labels, size/m
- **Milestone**: Phase 1

## Release Impact
- ✅ No breaking changes
- ✅ No migration required
- ℹ️ Suggest: Add changelog entry for "Label Manager UI" in next release notes

## Next Actions
1. **Approve and merge PR #50**
2. **After merge:** Agent will close issue #31 and mark backlog complete
3. **Then:** Run `daily-start` or `plan-next-issue` to select next work

---

**Status:** Issue #31 moved to `status/in-review`. Ready for your PR approval.
```

---

## Agents/Skills Invoked

- **Review Agent** — full quality validation and PR orchestration
- **`get_errors` tool** — compile error check
- **`github-issues` skill** — issue updates and closure
- **`gh-cli` skill** — PR creation and label management (if bulk operations needed)

---

## Follow-Up Prompts

After PR created:
- **You approve and merge PR** → Agent automatically closes issue and updates backlog
- **Then run:** `daily-start.prompt.md` to get next recommendation
- **Or run:** `plan-next-issue.prompt.md` to immediately plan next work

---

## Example Invocations

### Example 1: Standard review and PR creation
**You say:**
```
Review issue #31
```

**Agent executes:**
- Validates quality gates (all pass)
- Creates PR #50
- Updates issue #31 labels to `status/in-review`
- Delivers review summary

**Output:**
```
Created PR #50 for Label Manager UI (issue #31). All quality gates passed.
Ready for your approval and merge.
```

*(You approve and merge PR #50)*

**Agent auto-executes:**
- Closes issue #31 with `status/done`
- Updates `plan/BACKLOG.md` marking complete
- Suggests next item: "One-Click Migration UI (issue #40)"

---

### Example 2: Review with quality gate failure
**You say:**
```
Review issue #42
```

**Agent executes:**
- Validates quality gates
- **FAILS:** Missing user guide documentation

**Output:**
```
⚠️ Quality Gate Failed: Documentation missing

Issue #42 is a user-facing feature but docs/user-guide/triage-ui.md was not created.

Required action:
1. Escalating to Delivery Agent for doc update
2. After docs added, re-run review-and-close prompt

PR NOT created. Fix required first.
```

---

### Example 3: Review with release impact
**You say:**
```
Review issue #50
```

**Agent executes:**
- Validates quality gates (all pass)
- Detects breaking change in API
- Creates PR #60
- Flags release plan update needed

**Output:**
```
Created PR #60 for GitHub API refactoring (issue #50).

⚠️ Breaking Change Detected:
- GitHubService.GetIssues() signature changed (renamed to GetIssuesAsync)

Required before merge:
1. Update plan/RELEASE_PLAN.md with migration notes
2. Add changelog entry for breaking change

PR created but flagged for release impact review.
```

---

## Quality Gate Checklist

The Review Agent validates this checklist automatically:

### Pull Request Metadata
- [ ] PR assigned to `markheydon`
- [ ] PR labels set: `type/`, `priority/`, `area/`, `size/` from issue + `status/in-review`
- [ ] PR milestone matches linked issue milestone

### Code Quality
- [ ] `get_errors` shows zero errors/warnings
- [ ] UK English verified (no "behavior", "color", "organize")
- [ ] Layered architecture followed (Domain/Application/Infrastructure/App)
- [ ] No business logic in Razor components
- [ ] Constructor injection used throughout

### Testing
- [ ] xUnit tests added for new/changed code
- [ ] Test naming follows `MethodUnderTest_Scenario_ExpectedOutcome`
- [ ] Tests pass locally (`dotnet test`)
- [ ] Test projects mirror source structure
- [ ] Moq for mocking, xUnit built-in `Assert.*` for assertions (FluentAssertions is prohibited per ADR-0008)

### Documentation
- [ ] User-facing features have `docs/user-guide/*.md`
- [ ] `docs/index.md` quick links updated if new page added
- [ ] ADR created if architectural decision made
- [ ] `adr/README.md` updated if new ADR added
- [ ] All public members have XML doc comments

### Backlog Sync
- [ ] `plan/BACKLOG.md` reflects completion status
- [ ] `plan/SCOPE.md` updated if scope changed
- [ ] `plan/IMPLEMENTATION_PLAN.md` phase alignment verified

### Release Impact
- [ ] `plan/RELEASE_PLAN.md` updated if work affects release
- [ ] Breaking changes flagged
- [ ] Migration requirements documented
- [ ] Infrastructure changes have Bicep updates if needed

---

## Escalation Paths

**Escalate to Delivery Agent if:**
- Quality gate failures require code changes
- Tests failing
- Documentation missing
- Compile errors present

**Escalate to PM Orchestrator if:**
- Scope drift discovered during review
- Follow-up stories needed for backlog
- Issue acceptance criteria were incomplete

**Escalate to User if:**
- Breaking changes or migration required
- Release plan impact significant
- Architectural decision made during implementation not documented

---

## Post-Merge Automation

After you merge the PR, the Review Agent can automatically:
1. Close issue with `status/done` label
2. Update `plan/BACKLOG.md` marking complete
3. Check for next backlog item
4. Suggest next action (daily-start or plan-next-issue)

**To trigger post-merge automation:**
```
Close issue #31 after PR #50 merged
```

---

## Usage Tips

**Best for:**
- Completed implementations ready for PR
- Quality validation before code review
- Ensuring all completion gates met

**Not for:**
- Work-in-progress code (finish implementation first)
- Unplanned code changes (run `plan-next-issue` first)
- Hotfixes requiring immediate merge (create PR manually, skip gates)
