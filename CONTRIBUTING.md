# Contributing to SoloDevBoard

Thank you for your interest in contributing to SoloDevBoard! This document provides guidance for participating in the project, whether you're reporting issues, suggesting features, or submitting code.

---

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). Please be respectful, constructive, and professional in all interactions. Report unacceptable behaviour to the maintainer as described in that document.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Aspire CLI](https://aspire.dev/) (`aspire`) for local orchestration
- A GitHub account

### Development Setup

1. **Fork and clone the repository:**
   ```bash
   git clone https://github.com/<your-username>/solo-dev-board.git
   cd solo-dev-board
   ```

2. **Restore dependencies and build:**
   ```bash
   dotnet build
   ```

3. **Configure GitHub authentication (Aspire, recommended):**

   For solo local development, use **PAT-only local trusted mode** (the default). Set your token via AppHost parameters:

   ```bash
   aspire secret set Parameters:gh-pat "<your-github-pat>"
   aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   ```

   Your GitHub login is resolved automatically from the PAT. See [Getting Started — PAT-only local trusted mode](docs/getting-started.md#pat-only-local-trusted-mode) and [`src/SoloDevBoard.AppHost/README.md`](src/SoloDevBoard.AppHost/README.md) for the mode comparison and hosted sign-in parameters.
4. **Open the app:**
   ```bash
   aspire describe
   ```

   Open the `app` resource URL from `aspire describe`.

   **Legacy path (without Aspire):**
   ```bash
   dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-github-pat>" --project src/App/SoloDevBoard.App
   dotnet run --project src/App/SoloDevBoard.App
   ```

   See [docs/getting-started.md](docs/getting-started.md) for hosted sign-in mode and Azure deployment.

5. **Run tests:**
   ```bash
   dotnet test
   ```

---

## Reporting Issues

### Bug Reports

When reporting a bug, please include:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behaviour
- Actual behaviour
- Environment (OS, .NET version, etc.)
- Screenshots or logs (if applicable)

### Feature Requests

When suggesting a feature:
- Provide a clear description of the problem it solves
- Explain the proposed solution and alternatives
- Link to any relevant issues or discussions

### Security Issues

**Do not file public security issues.** Follow [`SECURITY.md`](SECURITY.md) and use [GitHub private vulnerability reporting](https://github.com/markheydon/solo-dev-board/security/advisories/new).

---

## Pull Request Process

1. **Before starting work:**
   - Check existing issues and pull requests to avoid duplication
   - For significant changes, open an issue first to discuss the approach

2. **Code quality:**
   - Follow the coding conventions in [`AGENTS.md`](AGENTS.md)
   - All code must use **UK English spelling** for comments, strings, and documentation
   - Ensure all public members have XML doc comments (`///`)
   - Use nullable reference types (`#nullable enable`)
   - Apply file-scoped namespaces

3. **Testing:**
   - Include unit tests for new functionality (xUnit v3 + NSubstitute)
   - Use the naming convention: `MethodUnderTest_Scenario_ExpectedOutcome`
   - All tests must pass: `dotnet test`
   - Aim for meaningful test coverage, not just coverage percentage
   - Use Playwright for end-to-end user journeys where unit tests cannot validate the full workflow — see [`tests/E2E/README.md`](tests/E2E/README.md) and [`tests/E2E/CRITICAL_JOURNEYS.md`](tests/E2E/CRITICAL_JOURNEYS.md) for the CI journey inventory, local run steps, and selector conventions.
   - Accessibility regression for primary journeys uses axe-core in Playwright (`accessibility.spec.ts`); findings are summarised in [`plan/ACCESSIBILITY_AUDIT.md`](plan/ACCESSIBILITY_AUDIT.md).
   - Prefer `data-testid` attributes on loading, error, and empty UI states when adding journeys that will run in CI with placeholder auth.
   - Do not test .NET Aspire AppHost modelling or orchestration.

4. **Architecture:**
   - Follow the clean/layered architecture: Domain, Application, Infrastructure, Composition, and App with strict dependency direction (see [AGENTS.md — Architecture](AGENTS.md#architecture) and [DEC-002](plan/DECISIONS.md#dec-002-layered--clean-architecture)).
   - Domain has no external dependencies.
   - Infrastructure implements interfaces defined in Application.
   - Composition wires Application and Infrastructure via `AddSoloDevBoard`; App must not reference Infrastructure.
   - No business logic in Razor components.
   - Prefer immutable records for domain entities.

5. **Commits and history:**
   - Write clear, concise commit messages in UK English
   - Each commit should be a logical unit of work
   - Reference issue numbers when relevant (e.g., `Fixes #42`)

6. **Pull request submission:**
   - Follow [`plan/PULL_REQUEST_POLICY.md`](plan/PULL_REQUEST_POLICY.md) for title, template body, labels, linking, draft state, assignee, and milestone.
   - Title form: `[<Type>] <Imperative summary> (#N)` (for example `[Story] Select a repository and project board to visualise (#183)`).
   - Complete `.github/pull_request_template.md`; do not replace it with a vendor walkthrough.
   - Apply at least `type/` and `priority/` labels, plus `status/in-review` while the PR is open.
   - Ensure all CI checks pass (build, tests, linting).
   - Request a code review from the maintainers.

---

## Secrets & Security

This is an **open source public repository**. To maintain security:

- **Never commit secrets, credentials, API keys, or personal information**
- Use `.gitignore` to exclude local configuration files (`.env`, `*.user`, `secrets.json`)
- GitHub Tokens must be stored in GitHub Environment secrets (production Aspire deploy), Aspire user secrets (local AppHost), or .NET User Secrets (legacy `dotnet run` path)
- See [`AGENTS.md#open-source--security`](AGENTS.md#open-source--security) for detailed security guidelines

---

## Documentation

When submitting a pull request that changes code, ensure related documentation is updated:

| Change | Documentation to Update |
|--------|------------------------|
| New end-user feature | `website/content/docs/<feature>.md`, `website/content/_index.md` (and docs landing), GitHub Issue + Project #8 sync |
| New developer / operator guidance | `docs/<topic>.md` and `docs/README.md` as needed |
| New decision | `plan/DECISIONS.md` (if architectural decision required; see `repo-decision-log` skill) |
| Scope change | `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md` |
| New env variable | `docs/getting-started.md`, `SoloDevBoard.AppHost/README.md`, `docs/deployment.md` |
| Tagged release | [`CHANGELOG.md`](CHANGELOG.md) and [`plan/RELEASE_PLAN.md`](plan/RELEASE_PLAN.md) |

---

## Licensing

By contributing to SoloDevBoard, you agree that your contributions will be licensed under the [MIT licence](LICENSE). This means your code can be used freely by others under the terms of that licence.

---

## Questions?

If you have questions about contributing, feel free to:
- Open a discussion on GitHub
- Check existing issues and documentation
- Review the decision log in [`plan/DECISIONS.md`](plan/DECISIONS.md)

Thank you for helping make SoloDevBoard better!
