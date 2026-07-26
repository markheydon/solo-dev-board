---
title: User Guide
linkTitle: Documentation
weight: 1
---

Guides for using SoloDevBoard in the app. Developer and operator material (local setup, deployment, hosted authentication, observability, and related topics) lives in the repository [`docs/`](https://github.com/markheydon/solo-dev-board/tree/main/docs) folder and is not published on this site.

## Features

| Feature | Description | Status |
|---------|-------------|--------|
| [Audit Dashboard](audit-dashboard/) | Consolidated view of issues, open PRs, label consistency, and workflow health across repositories. | Available |
| [Label Manager](label-manager/) | Create, edit, synchronise, and enforce label taxonomies across multiple repositories. | Available |
| [Repositories](repositories/) | View and manage repositories accessible to your GitHub account. | Available |
| [One-Click Migration](one-click-migration/) | Migrate labels and milestones from one repository to another in a single action. | Partially Available |
| [Board Rules Visualiser](board-rules-visualiser/) | Visualise supported board states and transitions for GitHub Project v2 boards. | Partially Available |
| [Triage UI](triage-ui/) | Keyboard-friendly interface for triaging incoming issues quickly. | Available |
| [Workflow Templates](workflow-templates/) | Browse, customise, and apply GitHub Actions workflow templates across repositories. | Partially Available |
| [Appearance](appearance/) | Automatic, light, and dark theme modes with browser persistence. | Available |
| [About](about/) | Application version, runtime environment, and repository link. | Available |

## Coming later

- Cross-Repo PM Workflow — structured project management and daily/weekly operating modes across multiple repositories (draft retained in source; not published yet).

## Related repository docs

These guides are for contributors, self-hosters, and operators (not published on this site):

- [Getting Started](https://github.com/markheydon/solo-dev-board/blob/main/docs/getting-started.md) — prerequisites, Aspire-first local setup, and configuration.
- [Deployment](https://github.com/markheydon/solo-dev-board/blob/main/docs/deployment.md) — Azure Container Apps via Aspire.
- [Hosted Authentication](https://github.com/markheydon/solo-dev-board/blob/main/docs/hosted-authentication.md) — hosted sign-in and allow-lists.
- [PAT Connectivity](https://github.com/markheydon/solo-dev-board/blob/main/docs/pat-connectivity.md) — PAT validation and recovery UX.
- [Azure Deployment Costs](https://github.com/markheydon/solo-dev-board/blob/main/docs/azure-costs.md) — resource charges and cost optimisation.
- [Observability](https://github.com/markheydon/solo-dev-board/blob/main/docs/observability.md) — structured logging and Application Insights.
