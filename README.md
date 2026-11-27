# Solo Dev Board

[![CI](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml/badge.svg)](https://github.com/markheydon/solo-dev-board/actions/workflows/ci.yml)

> **A single pane of glass for solo developers managing GitHub workloads across multiple repositories.**

## What Is Solo Dev Board?

Solo Dev Board is a **.NET 10 Blazor Server** application that consolidates your GitHub repository management into a single, unified interface. If you maintain multiple GitHub repositories as a solo developer, Solo Dev Board eliminates the context-switching between repository tabs, project boards, settings pages, and workflow runs.


| Feature | Description | Status |
|---------|-------------|--------|
| **Audit Dashboard** | Consolidated view of issues, open PRs, workflow health, and label consistency across selected repositories. | Available |
| **Label Manager** | Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface. | Available |
| **Repositories** | View, search, refresh, and filter repositories accessible to your GitHub account (including open-source topic filters). On-demand **Collect snapshot** saves a per-operator catalogue JSON (local file on lean paths; Azure Blob when hosted sign-in is enabled). Add, remove, bulk, and row catalogue actions remain stubs. | Partially Available |
| **One-Click Migration** | Migrate labels, milestones, and Projects v2 Status columns from one repository to another in a single preview-first action. | Available |
| **Board Rules Visualiser** | Visualise supported board states and transitions for GitHub Project v2 boards. Full GitHub automation-rule retrieval remains a later slice. | Partially Available |
| **Triage UI** | Keyboard-friendly interface for triaging incoming issues quickly. | Available |
| **Actions Templates** | Browse, customise, and apply built-in GitHub Actions workflow templates across repositories, plus load one custom GitHub `owner/name` source. | Partially Available |
| **Planning** | Daily Focus, backlog review, iteration planning, and repository participation for your project board. | Available |


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

Solo Dev Board uses **Aspire for local orchestration and production deployment** to Azure Container Apps. Two authentication modes are available: **PAT-only local trusted mode** (default, for solo local development and trusted personal self-hosting) and **hosted sign-in** (GitHub App, for shared or public deployments). See [Getting Started — PAT-only local trusted mode](docs/getting-started.md#pat-only-local-trusted-mode) for the mode comparison.

**Quick start with Aspire (recommended):**

```bash
# Clone the repository
git clone https://github.com/markheydon/solo-dev-board.git
cd solo-dev-board

# Restore dependencies
dotnet restore SoloDevBoard.slnx

# Configure GitHub auth — one secret for PAT-only local trusted mode
aspire secret set Parameters:gh-pat "<your-pat>"

# Start with Aspire
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj

# Get the allocated endpoint
aspire describe
```

Open the `web` resource URL shown by `aspire describe` in your browser.

**Optional legacy path (without Aspire):**
```bash
dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-pat>" --project src/SoloDevBoard.Web
dotnet run --project src/SoloDevBoard.Web
```

For **hosted sign-in** (GitHub App), see [docs/getting-started.md](docs/getting-started.md#hosted-sign-in-setup) or [`src/SoloDevBoard.AppHost/README.md`](src/SoloDevBoard.AppHost/README.md).

Aspire is used for both local orchestration and production deployment to Azure Container Apps. See [docs/deployment.md](docs/deployment.md) for Azure deployment instructions, including the [self-hoster PAT path](docs/deployment.md#self-hoster-deployment-pat-only-local-trusted-mode) for a personal instance on your own subscription.

## Project Structure

```
solo-dev-board/
├── GOALS.md, SCOPE.md, CONVENTIONS.md, AGENTS.md  # Scope and contributor constitution
├── src/SoloDevBoard.Web/         # Blazor Server UI (presentation layer)
├── src/SoloDevBoard.Application/ # Use cases and service interfaces
├── src/SoloDevBoard.Composition/ # DI composition root
├── src/SoloDevBoard.Domain/      # Domain entities and value objects
├── src/SoloDevBoard.Infrastructure/ # GitHub API clients, external integrations
├── src/SoloDevBoard.AppHost/     # Aspire AppHost (local orchestration and Azure deployment)
├── src/SoloDevBoard.Functions/   # Ice-boxed Functions placeholder (not in AppHost; ADR-0046 withdrawn)
├── src/SoloDevBoard.ServiceDefaults/  # Aspire service defaults (telemetry, health checks)
├── tests/
│   ├── SoloDevBoard.Web.Tests/   # bUnit component tests
│   └── SoloDevBoard.*.Tests/     # Unit tests per layer; SoloDevBoard.E2E.Tests for Playwright
├── docs/                       # Developer / operator documentation (see docs/README.md)
├── website/                    # Public product site (Hugo / Hextra → GitHub Pages)
│   ├── hugo.yaml               # Hugo site configuration
│   └── content/                # Landing, User Guide, and About pages
├── scripts/
│   ├── invoke-hugo-site.sh     # Local Hugo build/serve via Docker/Podman (bash)
│   └── Invoke-HugoSite.ps1     # Windows PowerShell equivalent
├── CHANGELOG.md                # Keep a Changelog (release notes source)
├── plan/
│   ├── SCOPE.md                # Project scope and constraints
│   ├── DECISIONS.md            # Retired pointer to adr/
│   ├── README.md               # Index of plan/ docs
│   ├── BACKLOG.md              # Pointer to Issues (not a work queue)
│   ├── archive/                # Historical plans and Copilot-era bundles
│   ├── RELEASE_PLAN.md         # Versioning and release process
│   ├── LABEL_STRATEGY.md       # GitHub label taxonomy
│   ├── PROJECT_MANAGEMENT.md   # Issues, milestones, and project board guide
│   ├── PROJECT_BOARD_DESIGN.md # Project board column and automation design
│   └── DOCS_STRATEGY.md        # Documentation conventions
├── adr/
│   ├── README.md               # ADR conventions
│   └── adr-NNNN-*.md           # Architectural decisions
├── .github/
│   ├── ISSUE_TEMPLATE/          # Issue templates (feature, bug, chore)
│   ├── pull_request_template.md
│   └── workflows/
│       ├── ci.yml               # .NET build, format, and test on every PR
│       ├── playwright.yml       # Playwright E2E (PAT and hosted matrix)
│       ├── cd.yml               # CD: Aspire deploy to Azure
│       ├── aspire-deploy-validate.yml  # Validates AppHost deployment model on PR
│       ├── bash-validate.yml    # ShellCheck on scripts/**/*.sh
│       ├── powershell-validate.yml  # PSScriptAnalyzer on scripts/**/*.ps1
│       ├── github-actions-validate.yml  # actionlint on workflow YAML
│       ├── github-scripts-validate.yml  # ESLint and tests for .github/scripts
│       ├── hugo-validate.yml    # Hugo end-user docs build validation on PR
│       └── hugo-deploy.yml      # Hugo end-user docs deploy to GitHub Pages
├── .agents/
│   └── skills/                  # Agent skills (code review, ADRs, MudBlazor, etc.)
├── .cursor/
│   ├── rules/                   # Cursor rules (C#, Blazor, AppHost, testing, ADR, markdown, skills)
│   └── commands/                # Cursor commands (code review loop, dependabot)
└── AGENTS.md                    # Agent operating instructions
```

---

## Contributing

Solo Dev Board welcomes contributions from all developers. Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md).

To contribute:

1. Fork the repository and create a new branch for your changes.
2. Review the project structure and existing documentation to understand the architecture and conventions.
3. Ensure all code comments, string literals, and documentation use UK English spelling.
4. Follow the coding conventions outlined in [`AGENTS.md`](AGENTS.md).
5. Use the issue templates in `.github/ISSUE_TEMPLATE/` to report bugs or request features.
6. When implementing a feature or fix, reference the relevant issue in your commit message.
7. Add or update tests in the appropriate test project under `tests/`.
8. Submit your changes via a pull request that follows [`plan/PULL_REQUEST_POLICY.md`](plan/PULL_REQUEST_POLICY.md) and `.github/pull_request_template.md`.
9. The CI workflow will build and test your changes automatically.
10. The project maintainer will review your PR and provide feedback or merge when ready.

For guidance on labels, see `plan/LABEL_STRATEGY.md`.
For help with setup, see `docs/getting-started.md`.
End-user documentation is published from `website/` to [GitHub Pages](https://solodevboard.com/) on `v*` release tags.

---

## AI-Driven Development

Solo Dev Board is developed with AI agents as active collaborators. [`AGENTS.md`](AGENTS.md), [`CONVENTIONS.md`](CONVENTIONS.md), project skills under [`.agents/skills/`](.agents/skills/), and Cursor plugins in [`.cursor/settings.json`](.cursor/settings.json) (dotnet/skills bundles, aspire-skills, and GitHub MCP) define architecture, conventions, and review workflow.

---

## Security

Please report vulnerabilities privately as described in [`SECURITY.md`](SECURITY.md). Do not file public issues for security problems.

## Licence

Solo Dev Board is released under the [MIT Licence](LICENSE).
