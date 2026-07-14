---
name: pm-feature-workflow
description: Orchestrates end-to-end feature delivery for SoloDevBoard: planning, GitHub issue management, implementation gating, testing, documentation, and closure.
---

# PM Feature Workflow

Use this skill when the user asks for high-level execution, such as "what is next", "plan and implement", or "take the next issue".

## Goal

Run a deterministic feature workflow so the user can act as product manager while the agent handles planning and delivery steps.

## Workflow

1. Select the next candidate from `plan/BACKLOG.md`.
2. Confirm scope alignment in `plan/SCOPE.md`.
3. Trigger planning with `breakdown-plan`.
4. For any feature that will result in a new page, a new major page region, or a substantive page refresh, create a wireframe in `plan/wireframes/` and update `plan/wireframes/README.md` before implementation begins.
5. Create or update GitHub issues with `github-issues` (use `gh-cli` for bulk changes), then sync each issue to the project board with `github-project` skill.
6. Trigger quality planning with `breakdown-test`.
7. Implement using repository standards (`dotnet-best-practices`, and `mudblazor` for UI work), with MudBlazor components and utility classes preferred over custom CSS or raw HTML.
8. Create or update ADRs when architectural decisions are introduced.
9. Update user-facing documentation and index links.
10. Verify completion gates and then close issue states.

## Mandatory Gates

- Do not code before planning artefacts and issue records are complete.
- Do not start page-producing UI implementation until the planning wireframe exists and is referenced by the relevant issue or artefact.
- Do not close work before tests and documentation updates are complete.
- Any scope change must update `plan/SCOPE.md` and `plan/BACKLOG.md`.
- Any architectural decision must create or update an ADR in `adr/`.

## Required Artefacts Per Feature

- Backlog entry update: `plan/BACKLOG.md`
- Scope update when needed: `plan/SCOPE.md`
- Wireframe: `plan/wireframes/*.md` and `plan/wireframes/README.md` for any page-producing feature or substantive page refresh
- ADR when needed: `adr/*.md`
- User guide stub when user-facing: `docs/user-guide/*.md`
- Docs index quick links when a new page is added: `docs/index.md`

## Prompt Starters

- "What is the next issue? Plan it and execute the implementation workflow."
- "Take the next backlog story and run full PM feature workflow."
- "Plan, create issues, implement, test, and document this feature end-to-end."
