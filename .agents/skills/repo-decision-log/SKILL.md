---
name: repo-decision-log
description: Route and record architectural and technical decisions for SoloDevBoard using the repo memory model (constitution, decision log, feature issues).
---

# Repo Decision Log (SoloDevBoard)

Use this skill when a change introduces or changes a **technical or architectural decision**. Do not create new ADR files in `adr/`.

## Decision routing

| Situation | Where to record | Example |
|-----------|-----------------|--------|
| Permanent cross-cutting rule every agent must follow | **Constitution** — [`AGENTS.md`](../../AGENTS.md) and/or [`.github/instructions/`](../../.github/instructions/) | DTO boundary, banned libraries, deploy model |
| Significant decision not fully captured in constitution | **Decision log** — [`plan/DECISIONS.md`](../../plan/DECISIONS.md) | API strategy, auth phasing, migration scope |
| Feature-scoped choice | **GitHub issue** + wireframe or plan note | Preview UX, single-endpoint behaviour |
| Historical full prose | **Do not add** — [`adr/archive/`](../../adr/archive/) is read-only | — |

When in doubt: promote to constitution only if violating the rule would break builds, security, or layer boundaries on every future change.

## Decision log entry template

Append to [`plan/DECISIONS.md`](../../plan/DECISIONS.md) under **Active decisions**:

```markdown
### DEC-NNN: Short title

**Status:** Active  
**Date:** YYYY-MM-DD  
**Legacy:** _(omit for new decisions; link ADR archive only when migrating)_  
**Constitution:** _(link if also reflected in AGENTS.md or instructions)_  
**Summary:** One paragraph — rule, rationale, what to reject.
```

Increment `DEC-NNN` from the highest existing number. Mark superseded entries **Status: Superseded** and add a row to the **Superseded legacy** table if replacing an older DEC.

## Constitution updates

When updating constitution:

1. Edit [`AGENTS.md`](../../AGENTS.md) and/or the relevant [`.github/instructions/*.md`](../../.github/instructions/) file.
2. Add or update the matching entry in `plan/DECISIONS.md` with a **Constitution** link.
3. Update [`plan/DOCS_STRATEGY.md`](../../plan/DOCS_STRATEGY.md) only if documentation process changes.

## Do not

- Create new files under `adr/` or `adr/archive/`.
- Duplicate full decision prose in multiple places — constitution holds rules; `DECISIONS.md` holds summaries.
- Use enterprise ADR ceremony (options tables, long consequence sections) unless the user explicitly requests it.

## Future: Spec Kit

When [`plan/SPEC_KIT_MIGRATION.md`](../../plan/SPEC_KIT_MIGRATION.md) is executed, feature decisions will move into Spec Kit specs and constitution. Until then, follow this skill.
