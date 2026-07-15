# GitHub Template Synchronisation Guide

**Purpose:** Governance document defining synchronisation requirements between YAML issue forms (for humans) and markdown templates (for AI agents).

---

## Why Two Template Formats Exist

SoloDevBoard maintains templates in **two formats** for **different consumers**:

### YAML Issue Forms (`.github/ISSUE_TEMPLATE/*.yml`)
- **Consumer:** Human users creating issues via GitHub UI
- **Format:** GitHub issue forms with dropdowns, validation, structured fields
- **Location:** `.github/ISSUE_TEMPLATE/`
- **Files:** `bug.yml`, `feature.yml`, `chore.yml`
- **Advantage:** Rich UI, required field enforcement, dropdown menus for labels

### Markdown Templates (`.github/skills/github-issues/references/templates.md`)
- **Consumer:** AI agents creating issues via MCP tools (`mcp__github__create_issue`)
- **Format:** Markdown (API-compatible text format)
- **Location:** `.github/skills/github-issues/references/templates.md`
- **Templates:** Bug Report, Feature/User Story, Chore/Technical Debt
- **Advantage:** Programmatic creation via GitHub API

**Architecture Decision:** Both formats must coexist because GitHub UI requires YAML and GitHub API requires markdown. They serve different technical consumers but represent the same logical issue types.

---

## Canonical Source of Truth

**YAML templates are canonical.** When structure or fields change:

1. **Update YAML first** — edit `.github/ISSUE_TEMPLATE/*.yml`
2. **Then sync markdown** — update `templates.md` to mirror YAML structure
3. **Test both paths** — create issue manually (YAML) and via AI (markdown), compare results

**Rationale:** YAML templates are visible to all contributors via GitHub UI and represent the "official" issue structure. Markdown templates are an implementation detail for AI automation.

---

## Synchronisation Requirements

### Mandatory Alignment

The following **must** stay synchronised across formats:

#### 1. Field Names and Structure
- YAML field IDs → markdown section headings
- Example: YAML `steps-to-reproduce` → markdown `## Steps to Reproduce`
- Order of sections should match where possible

#### 2. Required vs. Optional Fields
- YAML `validations: required: true` → markdown section marked as essential
- YAML `validations: required: false` → markdown section marked as optional or omitted
- When AI creates issues, required fields from YAML should be included in markdown

#### 3. Label Application
- YAML `labels:` → markdown template meta notes specify labels to apply
- Must align with `plan/LABEL_STRATEGY.md` taxonomy:
  - `type/` — bug, feature, chore, documentation, epic
  - `priority/` — critical, high, medium, low
  - `area/` — dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs
  - `size/` — xs, s, m, l, xl (typically added by PM during planning)
  - `status/` — todo, in-progress, blocked, in-review, done (lifecycle state)

#### 4. UK English Enforcement
- Both YAML and markdown templates must specify: "All text should be written in **UK English**"
- Consistent spelling: behaviour, colour, organise, licence (noun), analyse, prioritise, centre, favour

#### 5. Default Values
- YAML dropdown defaults → markdown template guidance for AI
- Example: Bug reports default to `priority/high`, features to `priority/medium`, chores to `priority/low`

---

## Current Template Mappings

| YAML Template | Markdown Template | Key Fields | Default Labels |
|---------------|-------------------|------------|----------------|
| `bug.yml` | Bug Report | Description, Steps to Reproduce, Expected Behaviour, Actual Behaviour, Environment, Logs/Screenshots | `type/bug`, `priority/high` |
| `feature.yml` | Feature/User Story | Description, User Story, Acceptance Criteria, Related Epic/Milestone, Area | `type/story` (user story), `type/feature` (feature grouping), `priority/medium`, optionally `area/*` |
| `chore.yml` | Chore/Technical Debt | Description, Motivation/Context, Definition of Done, Area | `type/chore`, `priority/low`, optionally `area/*` |

---

## Synchronisation Procedure

### When YAML Template Changes

1. **Identify change scope:**
   - New field added? → Add to markdown template
   - Field renamed? → Update markdown section heading
   - Validation changed? → Update markdown guidance
   - Label changed? → Update markdown meta notes

2. **Update markdown template:**
   - Edit `.github/skills/github-issues/references/templates.md`
   - Mirror YAML field structure in markdown sections
   - Update label application notes
   - Preserve UK English enforcement note

3. **Update AI references:**
   - Check if `.github/agents/pm-orchestrator.agent.md` needs updates
   - Check if `.github/skills/github-issues/SKILL.md` needs updates

4. **Test synchronisation:**
   - Create issue manually via GitHub UI (uses YAML)
   - Create issue via AI agent (uses markdown)
   - Compare field structure and labels applied

### When Markdown Template Changes

Generally, **avoid** changing markdown templates first. If AI behaviour suggests a template improvement:

1. **Assess if change should apply to YAML** (usually yes)
2. **Update YAML template first** following above procedure
3. **Then sync markdown**

Exception: AI-specific guidance (like "when creating via API, use this pattern") can be added to markdown without YAML change.

---

## Validation Checklist

Use this checklist when synchronising templates:

- [ ] YAML and markdown templates have matching section names (allowing for format differences)
- [ ] Required fields in YAML are included in markdown
- [ ] Optional fields in YAML are noted as optional in markdown
- [ ] Label application in markdown matches YAML `labels:` and dropdown options
- [ ] UK English enforcement note present in both formats
- [ ] Default priority values match (`bug` → high, `feature` → medium, `chore` → low)
- [ ] Area dropdown options in YAML match `plan/LABEL_STRATEGY.md` taxonomy
- [ ] Tested: manual issue creation (YAML) produces same structure as AI creation (markdown)

---

## Special Cases

### Dropdown Fields in YAML

YAML templates have `dropdown` fields for priority and area. Markdown templates cannot replicate dropdowns but should:
- Document available options in meta notes
- Specify default selection
- Link to `plan/LABEL_STRATEGY.md` for label taxonomy

Example:
```markdown
**Labels to apply:** `type/story`, `priority/medium` (default), optionally add `area/` label (dashboard, migration, labels, board-rules, triage, workflows, infrastructure, docs)
```

### User Story Template Pattern

YAML `feature.yml` includes a pre-filled user story template:
```yaml
**As a** solo developer,
**I want** [goal],
**So that** [benefit].
```

Markdown template should include identical template pattern so AI-created issues have consistent structure.

### Environment Field Complexity

YAML `bug.yml` has a pre-filled multi-line `value:` for environment details. Markdown template should match this structure exactly.

---

## Escalation

If you discover template drift or synchronisation issues:

1. **Document the drift:** Note which fields/labels are out of sync
2. **Determine canonical version:** Check YAML template (it's authoritative)
3. **Update markdown to match YAML**
4. **File issue** (if significant): Use `type/chore`, `area/docs`, `priority/medium` to track template maintenance
5. **Consider automation:** If drift is frequent, consider adding a CI check to validate synchronisation

---

## References

- **YAML templates:** `.github/ISSUE_TEMPLATE/bug.yml`, `feature.yml`, `chore.yml`
- **Markdown templates:** `.github/skills/github-issues/references/templates.md`
- **Label taxonomy:** `plan/LABEL_STRATEGY.md`
- **AI issue creation:** `.github/skills/github-issues/SKILL.md`
- **PM planning workflow:** `.github/agents/pm-orchestrator.agent.md`

---

**Last Synchronised:** March 5, 2026  
**Maintainer:** Product Manager  
**Review Frequency:** After any YAML template change, or quarterly during backlog grooming
