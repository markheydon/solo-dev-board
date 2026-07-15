# Issue Templates

**Purpose:** Markdown templates for AI agents creating issues via MCP tools. These mirror the structure of YAML issue forms in `.github/ISSUE_TEMPLATE/` which are used by humans creating issues via GitHub UI.

**Canonical Source:** YAML templates (`.github/ISSUE_TEMPLATE/*.yml`) are the authoritative structure. When YAML templates change, update these markdown templates to match.

**Sync Requirements:** See [TEMPLATE_SYNC.md](TEMPLATE_SYNC.md) for governance and synchronisation rules.

---

## Bug Report Template

**Mirrors:** `.github/ISSUE_TEMPLATE/bug.yml`

**Labels to apply:** `type/bug`, `priority/high` (default), optionally adjust priority based on severity

```markdown
## Description
[Clear and concise description of the bug]

## Steps to Reproduce
1. [First step]
2. [Second step]
3. [And so on...]

## Expected Behaviour
[What did you expect to happen?]

## Actual Behaviour
[What actually happened? Include any error messages.]

## Environment
- **OS:** [e.g. Windows 11, Ubuntu 24.04]
- **.NET version:** [e.g. 10.0.x]
- **Browser (if applicable):** [e.g. Chrome 125]
- **SoloDevBoard version / commit:** [e.g. v0.1.0 or commit SHA]
- **Deployment:** [e.g. local, Azure App Service]

## Relevant Logs or Screenshots
[Paste any relevant log output or attach screenshots]
```

**Note:** All text must be written in **UK English** (behaviour, colour, organise, etc.).

---

## Feature / User Story Template

**Mirrors:** `.github/ISSUE_TEMPLATE/feature.yml`

**Labels to apply:** `type/story`, `priority/medium` (default), optionally add `area/` label (dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs)

> Use `type/feature` for Feature-tier issues grouping multiple stories. Use `type/enabler` for technical prerequisite issues. Use `type/test` for test coverage issues.

```markdown
## Description
[Clear and concise description of the feature]

## User Story
**As a** solo developer,
**I want** [goal],
**So that** [benefit].

## Acceptance Criteria
- [ ] Given ... when ... then ...
- [ ] The UI displays ...
- [ ] The API returns ...
- [ ] [Add more criteria as needed]

## Related Epic / Milestone
[Which epic or milestone does this belong to? e.g. Phase 2 — Label Manager]

## Additional Context
[Any other context, screenshots, or links relevant to this feature request]
```

**Note:** All text must be written in **UK English** (behaviour, colour, organise, etc.).

---

## Chore / Technical Debt Template

**Mirrors:** `.github/ISSUE_TEMPLATE/chore.yml`

**Labels to apply:** `type/chore`, `priority/low` (default), optionally add `area/` label

```markdown
## Description
[Clear description of the chore or technical debt item]

## Motivation / Context
[Why is this work needed? What problem does it solve or what risk does it mitigate?]

Examples:
- This dependency is outdated and has known security advisories.
- This module has grown beyond its original scope and should be split.
- This code is difficult to test and needs refactoring.

## Definition of Done
- [ ] [Completion criterion 1]
- [ ] [Completion criterion 2]
- [ ] [Add more as needed]

## Area
[Which area of the codebase does this relate to? dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs]
```

**Note:** All text must be written in **UK English** (behaviour, colour, organise, etc.).

---

## Minimal Template

For very simple issues or quick tasks:

```markdown
## Description
[What needs to be done and why]

## Tasks
- [ ] [Task 1]
- [ ] [Task 2]
- [ ] [Task 3]
```

**Note:** Even simple issues should have at least `type/` and `priority/` labels applied.
