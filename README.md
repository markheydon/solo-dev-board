# SoloDevBoard

[![CI](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml/badge.svg)](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml)

> **A single pane of glass for solo developers managing GitHub workloads across multiple repositories.**

---

## What Is SoloDevBoard?

 SoloDevBoard is a **.NET 10 Blazor Server** application that consolidates your GitHub repository management into a single, unified interface. If you maintain multiple GitHub repositories as a solo developer, SoloDevBoard eliminates the context-switching between repository tabs, project boards, settings pages, and workflow runs.


## Features

| Feature | Description | Status |
|---------|-------------|--------|
| **Audit Dashboard** | Consolidated view of issues, PRs, label health, and workflow statuses across all repositories | 🔨 Planned — Phase 2 |
| **One-Click Migration** | Copy labels, milestones, and board configurations from one repository to another | 🔨 Planned — Phase 3 |
| **Label Manager** | Create, edit, sync, and enforce label taxonomies across multiple repositories | 🔨 Planned — Phase 2 |
| **Board Rules Visualiser** | Interactive diagram of GitHub project board automation rules | 🔨 Planned — Phase 4 |
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly | 🔨 Planned — Phase 3 |
| **Workflow Templates** | Browse, customise, and apply GitHub Actions workflow templates across repositories | 🔨 Planned — Phase 4 |


## Tech Stack



## Getting Started

### Prerequisites


### Run Locally

```bash
# Clone the repository
git clone https://github.com/markheydon/solo-dev-board.git
cd solo-dev-board

# Restore dependencies
dotnet restore SoloDevBoard.slnx

# Set your GitHub token using .NET User Secrets
dotnet user-secrets set "GitHub:Token" "<your-pat>" --project src/App/SoloDevBoard.App

# Run the application
dotnet run --project src/App/SoloDevBoard.App
```

Then open `https://localhost:5001` in your browser.

See the full [Getting Started guide](docs/getting-started.md) for configuration options and Azure deployment instructions.


## Project Structure

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