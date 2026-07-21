# Contributing to SoloDevBoard

Thank you for your interest in contributing to SoloDevBoard! This document provides guidance for participating in the project, whether you're reporting issues, suggesting features, or submitting code.

---

## Code of Conduct

We are committed to providing a welcoming and inclusive environment for all contributors. Please be respectful, constructive, and professional in all interactions. If you witness behaviour that violates these principles, please reach out to the project maintainers.

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

   For solo local development, use **PAT mode** (the default). Set your token via AppHost parameters:

   ```bash
   aspire secret set Parameters:gh-pat "<your-github-pat>"
   aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

   Your GitHub login is resolved automatically from the PAT. See [`src/SoloDevBoard.AppHost/README.md`](src/SoloDevBoard.AppHost/README.md) for hosted sign-in parameters.
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

**Do not file public security issues.** If you discover a security vulnerability, please email the maintainers privately to allow for a fix before disclosure.

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
   - Use Playwright for end-to-end user journeys where unit tests cannot validate the full workflow.
   - Do not test .NET Aspire AppHost modelling or orchestration.

4. **Architecture:**
   - Follow the clean/layered architecture (Domain → Application → Infrastructure → App)
   - Domain has no external dependencies
   - Infrastructure implements interfaces defined in Application
   - No business logic in Razor components
   - Prefer immutable records for domain entities

5. **Commits and history:**
   - Write clear, concise commit messages in UK English
   - Each commit should be a logical unit of work
   - Reference issue numbers when relevant (e.g., `Fixes #42`)

6. **Pull request submission:**
   - Create a descriptive PR title and body
   - Reference any related issues
   - Ensure all CI checks pass (build, tests, linting)
   - Request a code review from the maintainers

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
| New feature | `docs/user-guide/<feature>.md`, `docs/index.md`, GitHub Issue + Project #8 sync |
| New decision | `plan/DECISIONS.md` (if architectural decision required; see `repo-decision-log` skill) |
| Scope change | `plan/SCOPE.md`, `plan/IMPLEMENTATION_PLAN.md` |
| New env variable | `docs/getting-started.md`, `SoloDevBoard.AppHost/README.md`, `docs/deployment.md` |

---

## Licensing

By contributing to SoloDevBoard, you agree that your contributions will be licensed under the [MIT License](LICENSE). This means your code can be used freely by others under the terms of that licence.

---

## Questions?

If you have questions about contributing, feel free to:
- Open a discussion on GitHub
- Check existing issues and documentation
- Review the architecture decision records in `adr/`

Thank you for helping make SoloDevBoard better!
