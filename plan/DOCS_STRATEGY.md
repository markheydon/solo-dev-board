# SoloDevBoard — Documentation Strategy

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file for guidance on documentation responsibilities. -->

This document defines the conventions and processes for maintaining SoloDevBoard's documentation.

---

## Documentation Structure

```
docs/
├── _config.yml              # Jekyll / GitHub Pages config
├── index.md                 # Project overview and quick links
├── getting-started.md       # Prerequisites, local setup, configuration
└── user-guide/
    ├── audit-dashboard.md
    ├── one-click-migration.md
    ├── label-manager.md
    ├── board-rules-visualiser.md
    ├── triage-ui.md
    └── workflow-templates.md

plan/
├── SCOPE.md
├── IMPLEMENTATION_PLAN.md
├── BACKLOG.md
├── RELEASE_PLAN.md
├── LABEL_STRATEGY.md
├── PROJECT_MANAGEMENT.md
├── PROJECT_BOARD_DESIGN.md
└── DOCS_STRATEGY.md         # This file

adr/
├── README.md
├── 0001-blazor-server.md
└── ...
```

---

## Conventions for Documenting New Features

When a new feature is implemented or reaches a stable state:

1. **User Guide Stub → Full Doc:** Update the stub in `docs/user-guide/<feature>.md`. Remove the "Under Development" notice. Write the **Overview**, **How to Use**, and **Configuration** sections with accurate, tested information.
2. **Index Page:** Update `docs/index.md` to ensure the feature appears in the Key Features table and the Quick Links section.
3. **Getting Started:** If the feature introduces new configuration (environment variables, appsettings keys), update `docs/getting-started.md`.
4. **ADRs:** If an architectural decision was made in the course of implementing the feature, create a new ADR in `adr/` and add it to `adr/README.md`.
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

## Cross-Linking ADRs and Planning Docs

- Each ADR in `adr/` should be referenced from `adr/README.md`.
- ADRs that affect a feature should be referenced in the relevant `docs/user-guide/<feature>.md` file.
- Planning docs that reference an ADR should use a relative link: `[ADR-0001](../adr/0001-blazor-server.md)`.
- When a decision documented in an ADR is superseded, update the ADR's **Status** to `Superseded by ADR-XXXX` and create a new ADR.

---

## Doc Writer Agent Instructions

<!-- Inspired by GitHub Awesome Copilot patterns -->

When Copilot or another AI agent is asked to write or update documentation:

1. **UK English required.** All documentation must use UK English spelling. Run a spell check if possible.
2. **Accuracy over completeness.** Do not document features that are not yet implemented. Use "Coming Soon" or "Under Development" notices for stubs.
3. **Sync with code.** When updating docs, verify that the documented behaviour matches the current implementation.
4. **Link generously.** Cross-reference related docs, ADRs, and planning files. Use relative links.
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
> | New environment variable | Update `docs/getting-started.md` configuration table and `infra/README.md` |
> | New architectural decision | Create `adr/XXXX-<title>.md`; add entry to `adr/README.md` |
> | Scope change | Update `plan/SCOPE.md`; update `docs/index.md` if feature list changes |
> | New release | Update `plan/RELEASE_PLAN.md`; draft CHANGELOG entry |
> | New label | Update `plan/LABEL_STRATEGY.md` |
> | New board column or rule | Update `plan/PROJECT_BOARD_DESIGN.md` |
>
> Documentation changes should be included in the **same PR** as the code change where possible. A PR that adds a feature without updating the relevant docs is considered incomplete.
