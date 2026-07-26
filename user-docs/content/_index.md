---
layout: hextra-home
title: SoloDevBoard
---

{{< hextra/hero-badge >}}
  <div class="hx:w-2 hx:h-2 hx:rounded-full hx:bg-primary-400"></div>
  <span>Early access</span>
{{< /hextra/hero-badge >}}

<div class="hx:mt-6 hx:mb-6">
{{< hextra/hero-headline >}}
  SoloDevBoard
{{< /hextra/hero-headline >}}
</div>

<div class="hx:mb-12">
{{< hextra/hero-subtitle >}}
  A single pane of glass for solo developers managing GitHub workloads across multiple repositories.
{{< /hextra/hero-subtitle >}}
</div>

<div class="hx:mb-6">
{{< hextra/hero-button text="User Guide" link="docs" >}}
</div>

{{< hextra/feature-grid columns="3" >}}
  {{< hextra/feature-card
    title="Audit Dashboard"
    subtitle="Consolidated view of issues, open PRs, label consistency, and workflow health across all repositories."
    class="hx:aspect-auto"
  >}}
  {{< hextra/feature-card
    title="Label Manager"
    subtitle="Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface."
    class="hx:aspect-auto"
  >}}
  {{< hextra/feature-card
    title="Repositories"
    subtitle="View and manage repositories accessible to your GitHub account."
    class="hx:aspect-auto"
  >}}
  {{< hextra/feature-card
    title="One-Click Migration"
    subtitle="Migrate labels and milestones from one repository to another in a single action."
    class="hx:aspect-auto"
  >}}
  {{< hextra/feature-card
    title="Board Rules Visualiser"
    subtitle="Visualise supported board states and transitions for GitHub Project v2 boards."
    class="hx:aspect-auto"
  >}}
  {{< hextra/feature-card
    title="Triage UI"
    subtitle="Keyboard-friendly interface for triaging incoming issues quickly."
    class="hx:aspect-auto"
  >}}
{{< /hextra/feature-grid >}}

## Active Development â€” Early Access

SoloDevBoard is in active early development and is not yet feature complete. Some capabilities are available to use today, while others are still being built or refined. Please refer to the [user guide]({{% ref "docs" %}}) for the latest documentation.

## What Is SoloDevBoard?

SoloDevBoard is a .NET 10 Blazor Server application designed for individual developers who work across multiple GitHub repositories. Rather than jumping between repository tabs, project boards, and settings pages, SoloDevBoard surfaces everything in one place â€” giving you a unified view of your issues, labels, workflows, and project health.

It is built with the solo developer in mind: opinionated defaults, minimal configuration, and AI-assisted workflows.

## Key Features

| Feature | Description | Status |
|---------|-------------|--------|
| **Audit Dashboard** | Consolidated view of issues, open PRs, label consistency, and workflow health across all repositories. | Available |
| **Label Manager** | Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface. | Available |
| **Repositories** | View and manage repositories accessible to your GitHub account. | Available |
| **One-Click Migration** | Migrate labels and milestones from one repository to another in a single action. Project board migration is planned. | Partially Available |
| **Board Rules Visualiser** | Visualise supported board states and transitions for GitHub Project v2 boards. | Partially Available |
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly. | Available |
| **Workflow Templates** | Browse built-in GitHub Actions workflow templates. Customise, apply, and track template coverage across repositories. | Partially Available |

## Related repository docs

Developer and operator documentation lives in the repository `docs/` folder (not published on this site):

- [Getting Started](https://github.com/markheydon/solo-dev-board/blob/main/docs/getting-started.md) â€” prerequisites, Aspire-first local setup, and configuration.
- [Deployment](https://github.com/markheydon/solo-dev-board/blob/main/docs/deployment.md) â€” Azure Container Apps via Aspire.
- [Hosted Authentication](https://github.com/markheydon/solo-dev-board/blob/main/docs/hosted-authentication.md) â€” hosted sign-in and allow-lists.
- [PAT Connectivity](https://github.com/markheydon/solo-dev-board/blob/main/docs/pat-connectivity.md) â€” PAT validation and recovery UX.
- [Azure Deployment Costs](https://github.com/markheydon/solo-dev-board/blob/main/docs/azure-costs.md) â€” resource charges and cost optimisation.
- [Observability](https://github.com/markheydon/solo-dev-board/blob/main/docs/observability.md) â€” structured logging and Application Insights.

## Licence

SoloDevBoard is released under the [MIT Licence](https://github.com/markheydon/solo-dev-board/blob/main/LICENSE).
