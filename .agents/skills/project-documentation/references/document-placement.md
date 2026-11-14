# Document placement guide

Use this guide after choosing the Diátaxis document type in [diataxis.md](diataxis.md).

## Audiences first

Do not assume `docs/` means “end users of the app”. The reader depends on the product:

| Typical audience | Examples | Often lives in |
| --- | --- | --- |
| Repository visitors | Evaluators, new contributors, quick start | `README.md` |
| Extended doc site readers | App user guides, operator runbooks, public API reference for consumers | `docs/` |
| Integrators or library adopters | Package usage, migration guides, client SDK how-tos | `docs/` (or README + `docs/` split) |
| Project contributors | Deep architecture, local dev beyond README | `docs/`, `CONVENTIONS.md`, `adr/`, or `plan/` |

Ground the choice in `GOALS.md` and `SCOPE.md`. If the primary documentation audience is unclear, ask before placing a large doc in `docs/`.

## GitHub Pages and `docs/`

Many repos publish [GitHub Pages](https://docs.github.com/en/pages) from `docs/` (often with `docs/_config.yml`). That only defines **where** the site is built from, not **who** it is for. The published site might be:

- A **product** manual (how to use the shipped app or service)
- **Adopter** documentation (how to consume a library or API from another codebase)
- **Project** documentation (tutorials and reference for people working in the repo)

State the intended Pages audience in `GOALS.md` or the docs index page so agents and contributors do not mix product copy with contributor-only material.

## Put content in `README.md` when

- It explains what the repository is, why it exists, and how to get started.
- The primary audience is contributors, maintainers, or evaluators of the repository.
- The content needs to be visible immediately on the repository front page.

## Put content in `docs/` when

- The content serves the **primary documentation audience** named in project context (product, integrator, operator, or contributor depth), not a one-line README summary.
- It fits a Diátaxis category such as tutorial, how-to guide, reference, or explanation.
- It will grow over time and benefits from navigation, indexing, cross-linking, or GitHub Pages.

## Put content in another project file when

- It belongs with planning or operational material such as `RUNBOOK.md`, `CONVENTIONS.md`, or `plan/` files.
- It describes internal process, governance, or implementation constraints rather than the documentation audience above.

## Decision rules

- Favour `README.md` for repository orientation and entry points.
- Favour `docs/` for substantial documentation aimed at the audience defined in `GOALS.md` / `SCOPE.md`.
- Avoid duplicating the same guidance in both places.
- If a README section is becoming long, task-heavy, or reference-like, move the detail into `docs/` and leave a short summary with links.
- Keep contributor-only or agent context in `GOALS.md`, `SCOPE.md`, `CONVENTIONS.md`, and `AGENTS.md` rather than a public-facing product manual unless that is intentional.
