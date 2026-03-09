---
name: Weekly PM Review
description: Weekly governance ritual — assesses milestone health, scope drift, release confidence, backlog hygiene, and identifies blockers. Produces executive summary and next week's priorities.
agent: PM Orchestrator
---

# Weekly PM Review Workflow

**When to use:** End of week (or start of new week) to assess overall project health, validate governance, and plan priorities for the coming week.

---

## Purpose

Execute a comprehensive weekly review covering:
1. Milestone progress and completion estimates
2. Scope drift detection and backlog hygiene
3. Release readiness and confidence assessment
4. Quality metrics (test coverage, doc completeness)
5. Blocker identification and resolution planning
6. Next week's priority recommendations

**Result:** Executive summary report with health metrics, blockers, and clear priorities for next week.

---

## Inputs Required

None — this is a comprehensive read-only assessment across all planning artefacts and GitHub state.

---

## Workflow Steps

### 1. Milestone Health Check
- Read `plan/IMPLEMENTATION_PLAN.md` for current phase
- Query GitHub for milestone progress:
  - Total issues in milestone
  - Issues closed vs. remaining
  - Completion percentage
  - Estimated completion date (based on velocity)
- Flag milestones at risk (e.g., <50% complete with <2 weeks remaining)

### 2. Scope Validation
- Read `plan/SCOPE.md`
- Compare active work and backlog items against scope boundaries
- Flag scope drift (items not in defined scope)
- Identify scope creep (new items added without explicit approval)
- Recommend scope clarifications or removals

### 3. Backlog Hygiene Assessment
- Read `plan/BACKLOG.md`
- Count items by status:
  - Todo (not started)
  - In Progress (active work)
  - Blocked (awaiting external dependencies)
  - Done (completed)
- Flag issues missing:
  - Acceptance criteria
  - Labels (`type/`, `priority/`, `area/`, `size/`)
  - Milestones
  - Size estimates
- Identify stale items (in-progress >5 days with no updates)

### 4. Release Confidence Check
- Read `plan/RELEASE_PLAN.md`
- Assess readiness for next release:
  - MVP features complete (%)
  - Showstopper bugs remaining
  - Documentation completeness
  - ADR coverage for architectural decisions
  - Infrastructure readiness (Bicep templates, deployments)
- Provide release confidence score: High / Medium / Low

### 5. Quality Metrics
- Test coverage trends:
  - New tests added this week
  - Test pass rate
  - Test project alignment with source structure
- Documentation completeness:
  - User guide pages vs. user-facing features
  - ADRs vs. architectural decisions
  - `docs/index.md` quick links up to date
- Code quality:
  - Compile errors/warnings trends
  - UK English compliance

### 6. Blocker Analysis
- Query GitHub for `status/blocked` issues
- Identify dependency chains (blocked-by relationships)
- Categorise blockers:
  - External (APIs, credentials, third-party services)
  - Internal (architectural decisions, technical debt)
  - Process (waiting for review, waiting for merge)
- Recommend resolution actions

### 7. Velocity Calculation
- Count issues closed this week by size:
  - xs/s/m/l/xl → story points
- Calculate weekly velocity (average story points per week)
- Project completion dates for active milestones
- Flag velocity drops (e.g., <50% of previous week average)

### 8. Priority Recommendations
- Based on milestone health, blockers, and velocity:
  - Recommend top 3 priorities for next week
  - Suggest backlog grooming needs (missing metadata, stale items)
  - Highlight critical path items (dependencies blocking other work)

---

## Outputs Produced

### Weekly Executive Summary
```markdown
# Weekly PM Review — Week of March 1, 2026

## 📊 Milestone Health

### Phase 1: Core Infrastructure
- **Progress:** 60% complete (6 of 10 issues closed)
- **Velocity:** 12 story points this week (on track)
- **Estimated Completion:** March 15, 2026 ✅
- **Status:** On track

### Phase 2: MVP Features
- **Progress:** 20% complete (3 of 15 issues closed)
- **Velocity:** 8 story points this week
- **Estimated Completion:** April 10, 2026
- **Status:** On track

## 🎯 Scope Health
- ✅ **No scope drift detected**
- All active work aligns with `plan/SCOPE.md`
- Backlog items: 23 in-scope, 0 out-of-scope

## 📋 Backlog Hygiene
- **Total Items:** 23
  - Todo: 15
  - In Progress: 3
  - Blocked: 1
  - Done: 4 (this week)
- **Issues Requiring Attention:**
  - ⚠️ Issue #18 missing acceptance criteria
  - ⚠️ Issue #22 missing size estimate
  - ⚠️ Issue #15 stale (in-progress 6 days, no updates)

## 🚀 Release Confidence: HIGH
- **MVP Completion:** 65% (13 of 20 features)
- **Showstopper Bugs:** 0
- **Documentation:** 90% complete (18 of 20 features documented)
- **ADR Coverage:** 100% (7 architectural decisions, 7 ADRs)
- **Infrastructure:** Ready (Bicep templates validated)
- **Estimated Release:** March 30, 2026

## 🧪 Quality Metrics
- **Tests Added This Week:** 42 (8 UI, 24 service, 10 integration)
- **Test Pass Rate:** 100% ✅
- **Compile Errors:** 0 ✅
- **UK English Compliance:** 100% ✅

## 🚧 Blockers (1 active)
1. **Issue #18** — Blocked on GitHub Personal Access Token with `workflow` scope
   - Category: External (waiting for credentials)
   - Impact: Blocks workflow template feature (high priority)
   - Recommendation: Escalate to user for PAT generation

## 📈 Velocity Trends
- **This Week:** 12 story points (6 issues closed)
- **Last Week:** 10 story points (5 issues closed)
- **4-Week Average:** 11 story points/week
- **Status:** Velocity stable ✅

## 🎯 Top 3 Priorities for Next Week
1. **Resolve blocker: Issue #18** — Generate PAT to unblock workflow template feature
2. **Complete Phase 1:** Close remaining 4 issues (Label Manager, Triage UI, Audit Dashboard, Board Rules)
3. **Backlog grooming:** Add acceptance criteria to issue #18, size estimate to #22

## 🔧 Recommended Actions
- [ ] Generate GitHub PAT with `workflow` scope (unblock #18)
- [ ] Review and merge PR #50 (Label Manager UI)
- [ ] Update acceptance criteria for issue #18
- [ ] Run `plan-next-issue` to plan remaining Phase 1 items
- [ ] Schedule release planning for March 30 target

---

**Overall Status:** 🟢 On Track
- Milestones healthy, velocity stable, release confidence high
- 1 blocker (external, low impact if resolved this week)
- Backlog hygiene good (minor metadata gaps)
```

---

## Agents/Skills Invoked

- **Direct file reads:** `plan/BACKLOG.md`, `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md`, `plan/RELEASE_PLAN.md`, `adr/README.md`
- **GitHub queries:** Milestone progress, issue states, PR status (via `github-issues` skill if available)
- **Quality checks:** `get_errors` for compile status
- **File save:** Creates `plan/weekly-updates/YYYY-MM-DD.md` with the full review artefact
- **No other modifications:** The review is otherwise read-only

---

### Project Board Status Update

At the end of every review, produce a **Project Board Status Update** block for the user to paste manually into the [SoloDevBoard Roadmap project](https://github.com/users/markheydon/projects/8) using **Add Status Update**:

```markdown
## Weekly Status — [Date]

**Phase X (vX.X.X):** [progress summary]

### Delivered this week
- [Bullet items of closed issues / ADRs / decisions]

### Next week's focus
1. [Priority 1]
2. [Priority 2]
3. [Priority 3]

### Health indicators
| Metric | Value |
|---|---|
| Tests | X/X passing |
| Compile errors | X |
| Open PRs | X |
| Active blockers | X |
```

Also provide the **manual values for the dialog**:
- **Status:** On track / At risk / Off track (based on milestone health)
- **Start date:** Date the current active milestone began (first issue in milestone created)
- **Target date:** Estimated milestone completion date (velocity-based when data exists; otherwise a reasoned estimate noted as tentative)

---

### Project Board Metadata Refresh

At the start of the **first review of a new phase** (or when the project description/README are stale), produce the following `gh` commands for the user to run once:

```powershell
# Update short description
gh project edit 8 --owner markheydon --description "[concise one-liner]"

# Update README (reflect current phase and resource links)
gh project edit 8 --owner markheydon --readme "[markdown content with escaped newlines]"
```

Only recommend running the metadata update when the content would materially change (e.g. a new phase begins, the description is placeholder text, or key resources links are missing).

---

### Save Weekly Update File

At the end of every review, save the complete artefact to:

```
plan/weekly-updates/YYYY-MM-DD.md
```

The file must contain, in order:
1. Header with date and overall status.
2. **Project Board Status Update** block (copy-paste markdown + manual dialog values).
3. **Project Board Metadata** section (gh commands, only if update recommended).
4. Full executive summary sections.

---

## Follow-Up Actions

Based on review results:
- **If blockers present:** Resolve blockers manually, then re-run weekly review
- **If backlog hygiene issues:** Run grooming session (update metadata, close stale items)
- **If milestone at risk:** Re-prioritise backlog, adjust scope or timeline
- **If release confidence low:** Run gap analysis, add missing features/docs to backlog
- **For next week planning:** Use top 3 priorities as input to `plan-next-issue` prompt

---

## Example Invocations

**You say:**
```
Run the weekly PM review
```

**Agent executes:**
- Reads all planning artefacts
- Queries GitHub for milestone/issue/PR state
- Calculates velocity and completion estimates
- Assesses release confidence
- Identifies blockers and hygiene issues
- Delivers weekly executive summary (as shown above)

**Output:**
```
Weekly PM Review complete. Overall status: 🟢 On Track.
Phase 1: 60% complete, on track for March 15.
1 blocker (external, PAT required). Top priority: resolve blocker for issue #18.
```

---

## Scheduling Recommendation

Run this workflow:
- **Weekly:** Friday end-of-day or Monday start-of-week
- **Pre-release:** 1 week before planned release date
- **On-demand:** After major milestone completion or scope change

---

## Integration with Other Workflows

**Weekly PM Review feeds into:**
- **Daily Start** — provides context for prioritisation
- **Plan Next Issue** — uses top priorities to guide selection
- **Release Planning** — uses confidence score and gap analysis

**Weekly PM Review reads from:**
- All daily workflows (planning, execution, review outcomes)
- GitHub state (issues, PRs, milestones)
- Planning artefacts (backlog, scope, implementation plan, release plan)

---

## Governance Validation

The weekly review validates compliance with:
- **`.github/copilot-instructions.md`** — mandatory gates enforced
- **`plan/LABEL_STRATEGY.md`** — label taxonomy applied
- **`plan/PROJECT_MANAGEMENT.md`** — issue workflow rules followed
- **`plan/DOCS_STRATEGY.md`** — documentation completeness
- **`plan/RELEASE_PLAN.md`** — release criteria met

---

## Usage Tips

**Best for:**
- Weekly project health assessment
- Release readiness validation
- Identifying trends (velocity, quality, blockers)
- Executive reporting (status to stakeholders)

**Not for:**
- Daily decision-making (use `daily-start` prompt)
- Tactical issue planning (use `plan-next-issue` prompt)
- Code review (use `review-and-close` prompt)

---

## Output Formats

You can request specific formats:
- **Standard:** Full executive summary (default, shown above)
- **Brief:** One-paragraph status with top 3 priorities only
- **Detailed:** Include issue-by-issue breakdown and test metrics
- **Release-focused:** Emphasise release confidence and gap analysis
