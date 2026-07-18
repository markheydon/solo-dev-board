# SoloDevBoard — Documentation Strategy

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file for guidance on documentation responsibilities. -->

This document defines the conventions and processes for maintaining SoloDevBoard's documentation.

---

## Documentation layers

| Layer | Location | Audience |
|-------|----------|----------|
| **End-user docs** | `docs/` | App users (GitHub Pages) |
| **Maintainer / PM** | `plan/` | Scope, decisions, runbooks, wireframes |
| **Constitution** | `AGENTS.md`, `.github/instructions/` | AI agents and contributors (always-on rules) |
| **ADR archive** | `adr/archive/` | Read-only historical records |

Do not add new files under `adr/` except via archive migration. Record new decisions per [`repo-decision-log`](../.agents/skills/repo-decision-log/SKILL.md).

---

## Documentation Structure

```
docs/
├── _config.yml              # Jekyll / GitHub Pages config
├── index.md                 # Project overview and quick links
├── getting-started.md       # Prerequisites, local setup, configuration
└── user-guide/
    └── ...

plan/
├── SCOPE.md
├── DECISIONS.md             # Active decision log (repo memory)
├── IMPLEMENTATION_PLAN.md
├── BACKLOG.md
├── RELEASE_PLAN.md
├── LABEL_STRATEGY.md
├── PROJECT_MANAGEMENT.md
├── PROJECT_BOARD_DESIGN.md
├── SPEC_KIT_MIGRATION.md    # Parked — future Spec Kit adoption
└── DOCS_STRATEGY.md         # This file

adr/
├── README.md                # Redirect to plan/DECISIONS.md
└── archive/                 # Read-only legacy ADR files
```

---

## Conventions for Documenting New Features

When a new feature is implemented or reaches a stable state:

1. **User Guide Stub → Full Doc:** Update the stub in `docs/user-guide/<feature>.md`. Remove the "Under Development" notice. Write the **Overview**, **How to Use**, and **Configuration** sections with accurate, tested information.
2. **Index Page:** Update `docs/index.md` to ensure the feature appears in the Key Features table and the Quick Links section.
3. **Getting Started:** If the feature introduces new configuration (environment variables, appsettings keys), update `docs/getting-started.md`.
4. **Decisions:** If an architectural decision was made, follow `repo-decision-log` — update `plan/DECISIONS.md` and/or constitution (`AGENTS.md`, instructions).
5. **Backlog:** Tick off the corresponding user stories in `plan/BACKLOG.md`.

---

## Changelog Conventions

SoloDevBoard follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions. A `CHANGELOG.md` will be maintained at the root of the repository starting from v0.1.0.

Changelog entries are grouped by:
- `Added` — new features
- `Changed` — changes to existing features
- `Deprecated` — features to be removed in a future release
- `Removed` — features that have been removed
- `Fixed` — bug fixes
- `Security` — security-related changes

---

## Cross-linking decisions and planning docs

- Active decisions live in [`plan/DECISIONS.md`](DECISIONS.md).
- Cross-cutting rules live in [`AGENTS.md`](../AGENTS.md) and [`.github/instructions/`](../.github/instructions/).
- Legacy full ADR text is in [`adr/archive/`](../adr/archive/) — link via `Legacy:` in decision log entries.
- Feature docs should reference `plan/DECISIONS.md` (DEC-NNN) rather than archived ADR paths.
- When a decision is superseded, update its **Status** in `plan/DECISIONS.md` and add a row to the **Superseded legacy** table.

---

## Doc Writer Agent Instructions

When Copilot or another AI agent is asked to write or update documentation:

1. **UK English required.** All documentation must use UK English spelling. Run a spell check if possible.
2. **Accuracy over completeness.** Do not document features that are not yet implemented. Use "Coming Soon" or "Under Development" notices for stubs.
3. **Sync with code.** When updating docs, verify that the documented behaviour matches the current implementation.
4. **Link generously.** Cross-reference related docs, decisions, and planning files. Use relative links.
5. **Heading hierarchy.** Use H1 for the page title, H2 for major sections, H3 for subsections. Do not skip levels.
6. **Code blocks.** All code, commands, and configuration snippets must be in fenced code blocks with the appropriate language identifier.
7. **Tables for structured data.** Prefer Markdown tables over bullet lists for structured comparisons (e.g. configuration options, label taxonomy).
8. **Update `index.md` last.** After writing a user guide page, check whether `docs/index.md` needs to be updated to reflect the new content.

---

## AI Collaborator Instructions

> When code changes are made, the following documentation updates are required:
>
> | Code Change | Documentation Action |
> |-------------|---------------------|
> | New feature implemented | Update `docs/user-guide/<feature>.md` to full content; remove "Under Development" notice |
> | New environment variable | Update `docs/getting-started.md` configuration table and `docs/deployment.md` |
> | New architectural decision | Follow `repo-decision-log`; update `plan/DECISIONS.md` and constitution if cross-cutting |
> | Scope change | Update `plan/SCOPE.md`; update `docs/index.md` if feature list changes |
> | New release | Update `plan/RELEASE_PLAN.md`; draft CHANGELOG entry |
> | New label | Update `plan/LABEL_STRATEGY.md` |
> | New board column or rule | Update `plan/PROJECT_BOARD_DESIGN.md` |
>
> Documentation changes should be included in the **same PR** as the code change where possible. A PR that adds a feature without updating the relevant docs is considered incomplete.
