# SoloDevBoard — Project Scope

<!-- AI Collaborator Instructions: When scope changes, update this file and create/close GitHub issues accordingly. Add a new entry to the changelog at the bottom of this file noting the date and nature of the scope change. -->

## Project Vision

SoloDevBoard is a **single pane of glass** for solo developers who maintain multiple GitHub repositories. The goal is to eliminate context-switching and reduce the friction of routine GitHub housekeeping tasks — triaging issues, managing labels, auditing project health, and deploying workflow configurations — by surfacing everything in one cohesive, AI-friendly application.

> **Motivating context:** SoloDevBoard was directly inspired by the AI-driven PM workflow built in the companion repository [markheydon/github-workflows](https://github.com/markheydon/github-workflows). That system uses VS Code Copilot agents and prompts to manage cross-repo workloads across two operating modes — a weekly **PM Mode** (scan all repos, triage issues and PRs, curate the project board) and a daily **Work Mode** (pick the next item from a pre-curated board). SoloDevBoard's long-term destination is to provide a proper visual interface for everything that system does today via text prompts and scripts: daily focus views, cross-repo backlog prioritisation, iteration planning, label strategy enforcement, workflow migration, and issue triage. Each intermediate phase delivers individual tooling features; Epic 7 (Cross-Repo PM Workflow, Phase 5) closes the loop by bringing the planning intelligence into the UI.

---

## In Scope

The following six features define the current scope of SoloDevBoard:

### 1. Audit Dashboard
A consolidated view of repository health: open issues, stale PRs, label consistency warnings, and GitHub Actions workflow statuses across all configured repositories.

### 2. One-Click Migration
Copy labels, milestones, and project board configurations from a source repository to one or more target repositories in a single, preview-first action.

### 3. Label Manager
Create, edit, recolour, delete, and synchronise GitHub labels across multiple repositories from a single interface, using a canonical label taxonomy as the source of truth.

### 4. Board Rules Visualiser
An interactive diagram showing the automation rules configured on GitHub project boards, making it easy to understand how issues flow between columns.

### 5. Triage UI
A focused, keyboard-friendly interface for triaging incoming GitHub issues one at a time, supporting quick labelling, milestone assignment, and duplicate closure.

### 6. Workflow Templates
Browse, customise, and apply GitHub Actions workflow templates across repositories, with tracking of which repositories have which templates applied.

### 7. Cross-Repo PM Workflow
A UI-based implementation of the two-mode PM operating system from [markheydon/github-workflows](https://github.com/markheydon/github-workflows): a Daily Focus view (board state, stalled items, top priorities), a cross-repository Backlog Review (prioritised work across all repos, neglected repo detection), and an Iteration Planning tool (capacity management, Up Next curation, milestone assignment). This epic represents the ultimate destination of SoloDevBoard.

---

## Out of Scope

The following are explicitly **not** in scope for the current version of SoloDevBoard:

- **Multi-user / team features** — SoloDevBoard is currently designed for a single developer. No multi-user authentication, authorisation, or user management is in scope for Phases 1–5. Multi-tenancy (allowing any developer to sign in with their own GitHub account) is a deferred goal, targeted for Phase 6 (v1.0.0) — see [ADR-0007](../adr/0007-multi-tenancy-authentication-phased-approach.md).
- **Non-GitHub providers** — GitLab, Bitbucket, Azure DevOps, and other platforms are not supported. GitHub.com is the only supported provider initially.
- **Mobile application** — SoloDevBoard is a web application. No native iOS or Android app is planned.
- **Real-time collaboration** — No shared sessions, shared boards, or live collaboration features.
- **Issue content editing** — SoloDevBoard manages metadata (labels, milestones, assignments) but does not provide a full issue editor.
- **Billing / GitHub marketplace features** — No integration with GitHub Marketplace or billing APIs.

---

## Assumptions

- **Single-user / solo developer:** The application is used by one person who is the owner or an admin of all managed repositories.
- **GitHub.com initially:** The application targets GitHub.com. GitHub Enterprise Server support may be considered in a future release.
- **.NET 10:** The application is built on .NET 10 and Blazor Server. No legacy .NET Framework support is required.
- **Modern browser:** Users are assumed to be using a current version of Chrome, Firefox, Edge, or Safari.
- **Internet connection:** The application requires a live connection to the GitHub API.

---

## Constraints

- **UK English:** All user-facing text, code comments, documentation, and commit messages must be written in UK English.
- **Open source:** The project is intended to be open source under the MIT Licence.
- **AI-driven development:** The project is developed with GitHub Copilot as an active collaborator. All planning documents are written to be machine-readable and actionable by AI agents.
- **Minimal dependencies:** Prefer the .NET ecosystem and well-established open source libraries. Avoid adding dependencies without an ADR.
- **UI component library:** MudBlazor is the sole UI component library for the Blazor front-end (see [ADR-0012](../adr/0012-switch-to-mudblazor-component-library.md)). Raw HTML form elements are not used where a MudBlazor equivalent exists.

---

## AI Collaborator Instructions

> When this project's scope changes — whether a feature is added, removed, or modified — follow these steps:
>
> 1. Update the **In Scope** or **Out of Scope** sections of this file to reflect the change.
> 2. Update `plan/IMPLEMENTATION_PLAN.md` if the phase breakdown is affected.
> 3. Update `plan/BACKLOG.md` to add or remove the relevant epics and user stories.
> 4. Create a GitHub Issue for the scope change using the `type/feature` or `type/chore` template.
> 5. If the change affects the architecture, create or update the relevant ADR in `adr/`.
> 6. If the change affects the user-facing docs, update or create the relevant file in `docs/user-guide/`.
> 7. Add a changelog entry at the bottom of this file.

---

## Scope Changelog

| Date | Change | Author |
|------|--------|--------|
| 2025-01-01 | Initial scope defined | Solo developer |
| 2026-03-06 | Multi-user / team features updated from permanently out of scope to deferred (Phase 5). `ICurrentUserContext` interface preparation added to Phase 2. See ADR-0007. | Solo developer |
| 2026-03-07 | Added Epic 7 (Cross-Repo PM Workflow) to In Scope. Updated Project Vision to document the motivating context from markheydon/github-workflows. Phase 5 added to IMPLEMENTATION_PLAN.md for this epic. | Solo developer |
| 2026-03-09 | Added constraint: MudBlazor is the sole UI component library (ADR-0012). Fluent UI Blazor library removed. Existing UI features (Repositories page, Label Manager) to be refactored. | Solo developer |
