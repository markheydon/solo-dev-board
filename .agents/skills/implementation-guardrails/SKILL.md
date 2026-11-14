---
name: implementation-guardrails
description: >-
  Guardrails while implementing .NET code: read architecture constraints first,
  fix root causes not symptoms, pass the proportionality gate before new layers,
  and run AGENTS.md validation. Use for features, refactors, EF migrations,
  analyzer or build fixes, and non-trivial agent-authored code. Not for typos-only
  edits, full-repo audits, or pre-merge review (use code-review).
---

# Implementation guardrails

Apply **while building**. A green build is not proof the change is sound.

Complements [code-review](../code-review/SKILL.md) (pre-merge review).

## Instructions

1. **Read constraints** - `AGENTS.md`, `CONVENTIONS.md`, `GOALS.md`, `SCOPE.md`, `adr/`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, CI workflows; Spec Kit `specs/` or constitution when present.
2. **Place work correctly** - confirm ownership, dependency direction, and the smallest home for the change before adding surface area.
3. **Proportionality gate** - before new projects, layers, or interfaces, answer the seven questions in [references/implementation-depth.md](references/implementation-depth.md). See red flags in [references/checklists.md](references/checklists.md).
4. **Root cause** - no forbidden suppressions or test weakening; see [references/implementation-depth.md](references/implementation-depth.md) and [references/checklists.md#diagnostic-integrity](references/checklists.md#diagnostic-integrity).
5. **Align with the repo** - nullable, `CancellationToken`, central package management, no new packages without explicit user approval.
6. **Watch agent-shaped defects** - list in [references/implementation-depth.md](references/implementation-depth.md).
7. **EF / tests / security** - follow [references/implementation-depth.md](references/implementation-depth.md) and [references/checklists.md](references/checklists.md) when those areas change.
8. **Finish** - run format, build `-warnaserror`, and tests from `AGENTS.md`. Escalate migration history, ADR challenges, and security risk to the user.

| Situation | Apply skill? |
| --- | --- |
| Feature, bug fix, refactor across layers | Yes |
| CS / analyzer / build / test fixes | Yes |
| New project, interface, package | Yes |
| EF entities, migrations | Yes |
| Typos or comments only | No (unless build/CI files) |
| Full repository audit | No |
| Pre-merge PR review | No - use `code-review` |

## Resources

| Path | Purpose |
| --- | --- |
| [references/checklists.md](references/checklists.md) | Proportionality red flags, diagnostics, EF tree, pre-PR self-review |
| [references/implementation-depth.md](references/implementation-depth.md) | Forbidden fixes, agent defects, EF and preserve rules |
