---
name: project-documentation
description: >-
  Write or update repository documentation (README, docs/, guides) using Diátaxis
  and project context (GOALS, SCOPE, CONVENTIONS, AGENTS). Use when drafting
  tutorials, how-tos, reference, explanations, or choosing README vs docs/ placement.
---

# Project documentation

## Instructions

1. Pick the Diátaxis document type and workflow in [references/diataxis.md](references/diataxis.md).
2. Read context before drafting: `GOALS.md`, `SCOPE.md`, `CONVENTIONS.md`, `AGENTS.md`.
3. Choose the file location using [references/document-placement.md](references/document-placement.md) (audience from `GOALS.md` / `SCOPE.md`; `docs/` is not only app user guides).
4. Use a starter from [assets/](assets/) when it fits the document type.
5. Finish with [references/review-checklist.md](references/review-checklist.md).

## Boundaries

- Not for ADRs. Use `create-architectural-decision-record` instead.
- When context files and existing docs disagree, flag the mismatch.

## Writing rules

- Concise, task-oriented prose; reflect the real repo layout.
- Prefer short README sections with links into `docs/` for depth.

## Resources

| Path | Purpose |
| --- | --- |
| [references/diataxis.md](references/diataxis.md) | Diátaxis types and writing workflow |
| [references/document-placement.md](references/document-placement.md) | README vs `docs/` vs other files |
| [references/review-checklist.md](references/review-checklist.md) | Final review |
| [assets/readme-section-template.md](assets/readme-section-template.md) | README sections |
| [assets/how-to-template.md](assets/how-to-template.md) | How-to guides |
| [assets/tutorial-template.md](assets/tutorial-template.md) | Tutorials |
| [assets/technical-blog-template.md](assets/technical-blog-template.md) | Technical blog posts |
