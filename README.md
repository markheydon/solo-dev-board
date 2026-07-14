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
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly. | Available |
| **Workflow Templates** | Browse, customise, and apply GitHub Actions workflow templates across repositories. | Coming Soon |


## Tech Stack

- [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 10.0 or later.
- Git (any recent version).
- A GitHub account.
- A GitHub Personal Access Token (PAT) or GitHub App for API authentication.

## Getting Started

### Prerequisites

Ensure you have the following installed:
- .NET SDK 10.0 or later.
- Git.
- A GitHub account.
- A GitHub Personal Access Token (PAT) or GitHub App for API authentication.
- The Aspire CLI (`aspire`) for local orchestration.

### Run Locally

```bash
# Clone the repository
git clone https://github.com/markheydon/solo-dev-board.git
cd solo-dev-board

# Restore dependencies
dotnet restore SoloDevBoard.slnx

# Set your GitHub token using .NET User Secrets
dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-pat>" --project src/App/SoloDevBoard.App
dotnet user-secrets set "GitHubAuth:OwnerLogin" "<your-account-name>" --project src/App/SoloDevBoard.App

# Start with Aspire (recommended)
aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj

# Optional legacy path (without Aspire orchestration)
dotnet run --project src/App/SoloDevBoard.App
```

Then run `aspire describe` to view allocated endpoints and open the `app` resource URL in your browser.

Aspire is currently used to make local, dev-container, and Codespaces startup behaviour consistent. Issue #171 remains open for future evaluation of broader Aspire adoption, such as introducing additional worker or service processes.

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
    ├── copilot-instructions.md  # Copilot adapter → AGENTS.md
    ├── agents/                  # Copilot agent adapters
    ├── prompts/                 # Copilot prompt adapters
    ├── ISSUE_TEMPLATE/          # Issue templates (feature, bug, chore)
    ├── pull_request_template.md
    └── workflows/
        ├── ci.yml               # CI: build and test on every PR
        └── cd.yml               # CD: deploy to Azure on push to main
├── .agents/
│   ├── skills/                  # Shared skills (Copilot + Cursor)
│   ├── agents/                  # Canonical agent definitions
│   └── prompts/                 # Canonical workflow prompts
├── .cursor/
│   ├── rules/                   # Cursor path-scoped rules
│   └── commands/                # Cursor slash commands (/daily-start, etc.)
├── AGENTS.md                    # Platform-neutral AI collaborator standards
```

---

## Contributing

SoloDevBoard welcomes contributions from all developers.

To contribute:

1. Fork the repository and create a new branch for your changes.
2. Review the project structure and existing documentation to understand the architecture and conventions.
3. Ensure all code comments, string literals, and documentation use UK English spelling.
4. Follow the coding conventions outlined in [`AGENTS.md`](AGENTS.md) (and [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for Copilot-specific entry points).
5. Use the issue templates in `.github/ISSUE_TEMPLATE/` to report bugs or request features.
6. When implementing a feature or fix, reference the relevant issue in your commit message.
7. Add or update tests in the appropriate test project under `tests/`.
8. Submit your changes via a pull request using the provided template.
9. The CI workflow will build and test your changes automatically.
10. The project maintainer will review your PR and provide feedback or merge when ready.

For guidance on labels, see `plan/LABEL_STRATEGY.md`.
For help with setup, see `docs/getting-started.md`.

---

## AI-Driven Development

SoloDevBoard is developed by a solo developer with **GitHub Copilot** and **Cursor** as active AI collaborators. [`AGENTS.md`](AGENTS.md) contains platform-neutral standards for architecture, conventions, UK English, and documentation responsibilities. [`.github/copilot-instructions.md`](.github/copilot-instructions.md) and [`.cursor/rules/`](.cursor/rules/) are thin adapters for each platform. See [`docs/ai-collaboration.md`](docs/ai-collaboration.md) for invocation details.

---

## Licence

SoloDevBoard is released under the [MIT Licence](LICENSE).