---
title: How the project is run
weight: 20
landing: true
landingSubtitle: "An honest account of AI-collaborator planning and delivery in this repository — not a marketing claim about autonomous software."
---

## AI-collaborator experiment

SoloDevBoard began as a deliberate experiment in **AI-fully-controlled delivery**: planning artefacts, GitHub issues, wireframes, implementation, tests, and documentation are produced in-repo with AI agents as active collaborators, under human direction.

That is not a marketing claim about autonomous software — it describes how the repository is actually organised:

- **Planning** lives in `plan/` (scope, decisions, wireframes, runbooks).
- **Work tracking** uses GitHub Issues and [Project #8](https://github.com/users/markheydon/projects/8).
- **Agents** follow shared contracts in `.agents/` and constitution rules in `AGENTS.md`.
- **Quality gates** include unit tests, Playwright journeys, and Hugo build validation for the public site.

## What that means in practice

- Features are planned with acceptance criteria before implementation.
- Page-producing UI work references wireframes in `plan/wireframes/`.
- User Guide pages stay aligned with Playwright coverage (see `tests/E2E/USER_DOCS_ALIGNMENT.md` in the repository).
- Releases are tagged (`v*`) so the public site and deployed app describe the same version.

## Contributing

The project is open source. If you want to run, self-host, or contribute, start with the repository [`docs/getting-started.md`](https://github.com/markheydon/solo-dev-board/blob/main/docs/getting-started.md). Operator and deployment guides remain in the repository `docs/` tree and are not published on this product domain.
