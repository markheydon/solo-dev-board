---
title: User Guide
linkTitle: Documentation
weight: 1
---

The SoloDevBoard user guide provides detailed documentation for available features and core workflows. Use the links below to access guides for areas currently documented. New guides are added as features are released.

Developer and operator material (local setup, deployment, hosted authentication, observability, and related topics) lives in the repository [`docs/`](https://github.com/markheydon/solo-dev-board/tree/main/docs) folder and is not published on this site.

## Key Features

| Feature | Description | Status |
|---------|-------------|--------|
| [Audit Dashboard](audit-dashboard/) | Consolidated view of issues, open PRs, label consistency, and workflow health across all repositories. | Available |
| [Label Manager](label-manager/) | Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface. | Available |
| [Repositories](repositories/) | View and manage repositories accessible to your GitHub account. | Available |
| [One-Click Migration](one-click-migration/) | Migrate labels and milestones from one repository to another in a single action. Project board migration is planned. | Partially Available |
| [Board Rules Visualiser](board-rules-visualiser/) | Visualise supported board states and transitions for GitHub Project v2 boards. | Partially Available |
| [Triage UI](triage-ui/) | Keyboard-friendly interface for triaging incoming issues quickly. | Available |
| [Workflow Templates](workflow-templates/) | Browse built-in GitHub Actions workflow templates. Customise, apply, and track template coverage across repositories. | Partially Available |

## General Navigation

- [Home Page](dashboard-shell/) — Overview of the main dashboard and navigation structure.
- [Appearance](appearance/) — Automatic, light, and dark theme modes with browser persistence.

## Available Features

- [Audit Dashboard](audit-dashboard/) — View issues, pull requests, and repository health across all your repositories.
- [Label Manager](label-manager/) — Create, edit, synchronise, and enforce label taxonomies across multiple repositories.
- [Repositories](repositories/) — View and manage repositories accessible to your GitHub account.
- [One-Click Migration](one-click-migration/) — Migrate labels and milestones from one repository to another in a single action.
- [Board Rules Visualiser](board-rules-visualiser/) — Visualise automation rules configured on GitHub project boards (partial delivery; full rule inspection and compare mode coming later).
- [Triage UI](triage-ui/) — Keyboard-friendly interface for triaging incoming issues quickly.
- [About](about/) — Application version, runtime environment, and repository link.

## Coming Soon

- [Workflow Templates](workflow-templates/) — Browse, customise, and apply GitHub Actions workflow templates across repositories.

## Coming Later

- Cross-Repo PM Workflow — Structured project management and daily/weekly operating modes across multiple repositories (draft retained in source; not published yet).

## Related repository docs

These guides are for contributors, self-hosters, and operators (not published on this site):

- [Getting Started](https://github.com/markheydon/solo-dev-board/blob/main/docs/getting-started.md) — prerequisites, Aspire-first local setup, and configuration.
- [Deployment](https://github.com/markheydon/solo-dev-board/blob/main/docs/deployment.md) — Azure Container Apps via Aspire.
- [Hosted Authentication](https://github.com/markheydon/solo-dev-board/blob/main/docs/hosted-authentication.md) — hosted sign-in and allow-lists.
- [PAT Connectivity](https://github.com/markheydon/solo-dev-board/blob/main/docs/pat-connectivity.md) — PAT validation and recovery UX.
- [Azure Deployment Costs](https://github.com/markheydon/solo-dev-board/blob/main/docs/azure-costs.md) — resource charges and cost optimisation.
- [Observability](https://github.com/markheydon/solo-dev-board/blob/main/docs/observability.md) — structured logging and Application Insights.
