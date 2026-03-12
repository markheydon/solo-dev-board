# SoloDevBoard

[![CI](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml/badge.svg)](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml)

> **A single pane of glass for solo developers managing GitHub workloads across multiple repositories.**


## What Is SoloDevBoard?

 SoloDevBoard is a **.NET 10 Blazor Server** application that consolidates your GitHub repository management into a single, unified interface. If you maintain multiple GitHub repositories as a solo developer, SoloDevBoard eliminates the context-switching between repository tabs, project boards, settings pages, and workflow runs.


| Feature | Description | Status |
|---------|-------------|--------|
| **Audit Dashboard** | Consolidated view of issues, open PRs, label consistency, and workflow health across all repositories. | Available |
| **Label Manager** | Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface. | Available |
| **Repositories** | View and manage repositories accessible to your GitHub account. | Available |
| **One-Click Migration** | Migrate labels and milestones from one repository to another in a single action. Project board migration is planned. | Partially Available |
| **Board Rules Visualiser** | Visualise automation rules configured on GitHub project boards. | Coming Soon |
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly. | Coming Soon |
| **Workflow Templates** | Browse, customise, and apply GitHub Actions workflow templates across repositories. | Coming Soon |
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly. | Coming Soon |
| **Workflow Templates** | Browse, customise, and apply GitHub Actions workflow templates across repositories. | Coming Soon |


## Tech Stack

To run SoloDevBoard locally:



- [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 10.0 or later.
- Git (any recent version).
- A GitHub account.
- A GitHub Personal Access Token (PAT) or GitHub App for API authentication.



```bash
# Clone the repository

## Getting Started

# Restore dependencies


# Set your GitHub token using .NET User Secrets
### Prerequisites

# Run the application

```

Then open `https://localhost:5001` in your browser.


### Run Locally

## Getting Started

See the full [Getting Started guide](docs/getting-started.md) for prerequisites, local setup, configuration, and Azure deployment instructions.
```
solo-dev-board/
├── src/
│   ├── App/                    # Blazor Server UI (presentation layer)
│   ├── Application/            # Use cases and service interfaces
│   ├── Domain/                 # Domain entities and value objects
│   └── Infrastructure/         # GitHub API clients, external integrations
├── tests/
│   └── App.Tests/              # xUnit test projects
├── infra/
│   ├── main.bicep              # Azure infrastructure entry point
│   └── modules/
│       └── appservice.bicep    # App Service Plan + App Service module
├── docs/
│   ├── index.md                # Project overview (GitHub Pages)
│   ├── getting-started.md      # Setup and configuration guide
│   └── user-guide/             # Per-feature user guides
├── plan/
│   ├── SCOPE.md                # Project scope and constraints
│   ├── IMPLEMENTATION_PLAN.md  # Phased development plan
│   ├── BACKLOG.md              # Feature backlog by epic
│   ├── RELEASE_PLAN.md         # Versioning and release process
│   ├── LABEL_STRATEGY.md       # GitHub label taxonomy
│   ├── PROJECT_MANAGEMENT.md   # Issues, milestones, and project board guide
│   ├── PROJECT_BOARD_DESIGN.md # Project board column and automation design
│   └── DOCS_STRATEGY.md        # Documentation conventions
├── adr/
│   ├── README.md               # ADR index
│   ├── 0001-blazor-server.md
│   ├── 0002-testing-framework.md
│   ├── 0003-bicep-infrastructure.md
│   ├── 0004-layered-architecture.md
│   └── 0005-github-api-strategy.md
└── .github/
    ├── copilot-instructions.md  # GitHub Copilot custom instructions
    ├── ISSUE_TEMPLATE/          # Issue templates (feature, bug, chore)
    ├── pull_request_template.md
    └── workflows/
        ├── ci.yml               # CI: build and test on every PR
        └── cd.yml               # CD: deploy to Azure on push to main
```

---

## Contributing / AI-Driven Development

SoloDevBoard is developed by a solo developer with **GitHub Copilot** as an active AI collaborator.

The `.github/copilot-instructions.md` file contains custom instructions that guide Copilot on architecture, conventions, UK English requirements, and documentation responsibilities. When using Copilot Chat to add features or make changes, these instructions ensure consistent, well-structured output.

If you are contributing:
1. Read `.github/copilot-instructions.md` for coding conventions.
2. All text (comments, strings, documentation) must be in **UK English**.
3. Follow the label strategy in `plan/LABEL_STRATEGY.md` when creating issues.
4. Raise a PR using the pull request template.

---

## Licence

SoloDevBoard is released under the [MIT Licence](LICENSE).