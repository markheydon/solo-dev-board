# ADR-0005: GitHub API Strategy — REST + GraphQL with PAT and GitHub App Authentication

**Date:** 2025-01-01
**Status:** Accepted

---

## Context

SoloDevBoard integrates deeply with the GitHub API to read and write repository data (issues, labels, milestones, workflows, project boards). The integration strategy must address:

1. **Which GitHub API to use:** REST API (v3) or GraphQL API (v4), or both.
2. **How to authenticate:** Personal Access Token (PAT) or GitHub App.
3. **How to handle rate limiting.**
4. **Which .NET client library to use**, if any.

---

## Decision

### API Strategy
- Use the **GitHub REST API (v3)** as the primary API for most operations (CRUD on labels, issues, milestones, workflows).
- Use the **GitHub GraphQL API (v4)** for operations that are not well-served by REST, specifically:
  - GitHub Projects (v2) — the Projects v2 API is GraphQL-only.
  - Complex queries requiring multiple nested resources in a single request (e.g. fetching issues with their labels, assignees, and milestone in one call).

### Authentication Strategy
- **Local development:** Personal Access Token (PAT) stored in .NET User Secrets or environment variables.
- **Production:** GitHub App authentication using a private key and installation access tokens.
- The `IAuthenticationService` interface in `Application` abstracts both methods; the concrete implementation in `Infrastructure` selects the method based on configuration.

### Client Library
- Use **Octokit.NET** for REST API calls. Octokit.NET is the official .NET client library maintained by GitHub. It handles authentication, pagination, and rate limit headers.
- Use a lightweight **GraphQL HTTP client** (e.g. `System.Net.Http.HttpClient` with `System.Text.Json`) for GraphQL queries, keeping the dependency footprint small.

---

## Rationale

### REST vs GraphQL
- REST is simpler for straightforward CRUD operations and is better supported by Octokit.NET.
- GraphQL is required for GitHub Projects v2 (there is no REST equivalent for most Projects v2 operations).
- A hybrid approach uses each API where it is the better fit, rather than forcing all operations through one API.

### PAT vs GitHub App
- PAT is simpler for local development and initial deployment.
- GitHub App provides fine-grained, repository-specific permissions and does not expire. It is the correct choice for a production application that a developer may use across many repositories.
- Supporting both in the same codebase (via the `IAuthenticationService` abstraction) allows a smooth transition from PAT to GitHub App without changing the rest of the application.

### Octokit.NET
- Octokit.NET is the official GitHub client for .NET and is actively maintained.
- It handles common concerns: authentication headers, pagination (`IReadOnlyList<T>` with `GetAllPages`), and rate limit tracking.
- Using Octokit.NET reduces the amount of HTTP plumbing code that must be written and maintained.

### Rate Limiting
- Octokit.NET exposes rate limit information via `GitHubClient.GetLastApiInfo()`.
- The `Infrastructure` layer will implement response caching (using `IMemoryCache`) to reduce the number of API calls for read-heavy operations (e.g. repository listing, label fetching).
- When the rate limit is approached, the application will surface a warning in the UI.

---

## Consequences

- `SoloDevBoard.Infrastructure` references `Octokit` NuGet package.
- `IAuthenticationService` is defined in `SoloDevBoard.Application` with implementations for PAT and GitHub App in `SoloDevBoard.Infrastructure`.
- GraphQL queries are written as string constants in `Infrastructure` classes and executed via `HttpClient`.
- Rate limit state is tracked and exposed via the `IAuthenticationService` so that the UI can display a warning.
- Real API calls are made only in integration tests. Unit tests use NSubstitute substitutes for `IGitHubService` and `ILabelRepository`.
