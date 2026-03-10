---
name: Daily Start
description: Morning startup workflow for PM — reviews backlog, checks active work, identifies blockers, and recommends next action.
agent: PM Orchestrator
---

# Daily Start Workflow

**When to use:** At the start of each working session to orient yourself and plan the day's work.

---

## Purpose

Get a concise status update and clear recommendation for what to work on next. This prompt reads your planning artefacts, checks active GitHub issues, and identifies any blockers or scope drift. It may also identify the next batch of stories, enablers, or tests that should be placed in the board's **Up Next** queue, but it does not change the board unless the user explicitly asks for that follow-up step.

---

## Inputs Required

None — this is a zero-input startup prompt.

---

## Workflow Steps

The agent will execute the following sequence:

### 1. Review Active Work
- Check GitHub for open issues with `status/in-progress` or `status/in-review`
- List active pull requests awaiting review or merge
- Flag stale work (in-progress >3 days with no updates)

### 2. Check Backlog Health
- Read `plan/BACKLOG.md` and count items by priority:
  - Critical (immediate action needed)
  - High (next up)
  - Medium (planned)
  - Low (backlog)
- Flag any items missing acceptance criteria or labels
- Identify unblocked items ready for planning

### 3. Validate Scope Alignment
- Read `plan/SCOPE.md`
- Check if any active work is out-of-scope
- Flag scope drift if detected

### 4. Check Milestone Progress
- Read `plan/IMPLEMENTATION_PLAN.md` for current phase
- Count issues completed vs. remaining in current milestone
- Estimate phase completion percentage

### 5. Identify Blockers
- List issues with `status/blocked` label
- List issues with unresolved dependencies
- Flag any PRs awaiting your approval

---

## Outputs Produced

The agent delivers a **Daily Startup Summary** with:

### Active Work Section
```
📋 Active Work:
- Issue #15 (status/in-progress): Label Manager UI — in progress 2 days
- PR #25 (status/in-review): Triage UI scaffolding — awaiting your approval
```

### Backlog Health Section
```
📊 Backlog Health:
- Critical: 0
- High: 3 (2 ready for planning, 1 blocked)
- Medium: 8
- Low: 12
Total: 23 items | Phase 1 progress: 40% (4/10 complete)
```

### Blockers Section
```
⚠️ Blockers:
- Issue #18 blocked on external API access (waiting on credentials)
- PR #25 awaiting your approval (ready to merge)
```

### Recommended Next Action
```
✅ Recommended Next Action:
1. Approve and merge PR #25 (Review Agent can close issue #12)
2. Then: Plan next high-priority item "One-Click Migration UI" (use plan-next-issue prompt)
```

### Optional Up Next Queue
```
🧭 Up Next Queue (optional follow-up, no board change unless requested):
1. Issue #38 — Focus Order 1
2. Issue #34 — Focus Order 2
3. Issue #32 — Focus Order 3
```

---

## Agents/Skills Invoked

- **Direct file reads:** `plan/BACKLOG.md`, `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md`
- **GitHub queries:** Active issues, open PRs (via `github-issues` skill if available)
- **No modifications by default:** This is a read-only status check unless the user explicitly asks the agent to populate **Up Next** after reviewing the recommendation.

---

## Follow-Up Prompts

Based on the recommended action, use:
- **If work in review:** Run `review-and-close.prompt.md` to finish pending PRs
- **If backlog ready:** Run `plan-next-issue.prompt.md` to select and plan next item
- **If you want a visible daily queue:** Ask the agent to move the recommended stories, enablers, or tests into **Up Next** and set **Focus Order**.
- **If blockers present:** Resolve blockers manually, then re-run `daily-start.prompt.md`

---

## Example Output

```markdown
# Daily Startup Summary — March 5, 2026

## 📋 Active Work
- **Issue #15** (status/in-progress): Label Manager UI — 2 days in progress
  - Last update: Yesterday 4:30 PM
  - Next: Complete unit tests and update docs
  
- **PR #25** (status/in-review): Triage UI scaffolding
  - Ready to merge (all checks passed)
  - Awaiting your approval

## 📊 Backlog Health
- **Phase 1 Progress:** 40% complete (4 of 10 issues closed)
- **Priority breakdown:**
  - Critical: 0
  - High: 3 items (2 ready for planning)
  - Medium: 8 items
  - Low: 12 items
- **Total backlog:** 23 items

## ⚠️ Blockers
- None

## 🎯 Scope Check
- ✅ All active work is in-scope
- No scope drift detected

## ✅ Recommended Next Action
1. **Approve and merge PR #25** → Use Review Agent to close issue #12
2. **Plan next item:** "One-Click Migration UI" (high priority, unblocked)
   - Command: Run `plan-next-issue` prompt with "One-Click Migration UI"

---

**Ready to start?** Choose an action above or ask "What should I work on?"
```

---

## Usage Example

**You say:**
```
Run the daily start workflow
```

**Agent responds with:**
- Daily Startup Summary (as shown above)
- Clear next action recommendation
- Links to follow-up prompts
