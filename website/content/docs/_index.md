---
title: User Guide
linkTitle: User Guide
weight: 1
cascade:
  params:
    reversePagination: false
---

Guides for using SoloDevBoard in the app. Developer and operator material (local setup, deployment, hosted authentication, observability, and related topics) lives in the repository [`docs/`](https://github.com/markheydon/solo-dev-board/tree/main/docs) folder and is not published on this site.

## Features

Feature guides follow the same order as the application navigation drawer (Audit Dashboard through Planning). Prev/next links walk this feature set only.

{{< guide-feature-table >}}

## App shell

Appearance and About are reached from the app bar (theme control and **More options**), not from the navigation drawer. They form a separate sidebar section with their own prev/next chain.

{{< cards cols="2" >}}
  {{< card link="app-shell/appearance/" title="Appearance" icon="sun" subtitle="Theme modes (Automatic, Light, and Dark)." >}}
  {{< card link="app-shell/about/" title="About" icon="information-circle" subtitle="Version, runtime, and authentication details." >}}
{{< /cards >}}

## Related repository docs

These guides are for contributors, self-hosters, and operators (not published on this site):

- [Getting Started](https://github.com/markheydon/solo-dev-board/blob/main/docs/getting-started.md) — prerequisites, Aspire-first local setup, and configuration.
- [Deployment](https://github.com/markheydon/solo-dev-board/blob/main/docs/deployment.md) — Azure Container Apps via Aspire.
- [Hosted Authentication](https://github.com/markheydon/solo-dev-board/blob/main/docs/hosted-authentication.md) — hosted sign-in and allow-lists.
- [GitHub App listing](https://github.com/markheydon/solo-dev-board/blob/main/docs/github-app.md) — logo, callback URLs, permissions, and listing copy.
- [PAT Connectivity](https://github.com/markheydon/solo-dev-board/blob/main/docs/pat-connectivity.md) — PAT validation and recovery UX.
- [Azure Deployment Costs](https://github.com/markheydon/solo-dev-board/blob/main/docs/azure-costs.md) — resource charges and cost optimisation.
- [Observability](https://github.com/markheydon/solo-dev-board/blob/main/docs/observability.md) — structured logging and Application Insights.
