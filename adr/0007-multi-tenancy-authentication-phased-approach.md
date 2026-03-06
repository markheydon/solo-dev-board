# ADR-0007: Multi-Tenancy Authentication — Phased Approach

**Date:** 2026-03-06
**Status:** Accepted

---

## Context

SoloDevBoard was originally conceived as a single-user tool for one developer to manage their own GitHub repositories. The GitHub authentication strategy in ADR-0005 describes PAT for local development and GitHub App for production, but treats the application as single-user throughout.

During Phase 1 planning (March 2026), the decision was made to consider multi-tenancy — allowing any developer to sign in with their own GitHub account and use the application independently — as a medium-term goal. This ADR records the phased strategy for achieving multi-tenancy without incurring unnecessary rework.

The key risks without a deliberate strategy are:

1. **Ambient auth assumptions:** If Phase 2 service implementations read the authenticated user's token directly from `IOptions<GitHubAuthOptions>` (ambient, single-user configuration), every service method will need to be refactored when multi-tenancy is introduced.
2. **Deferred GitHub App complexity:** Implementing GitHub App authentication (JWT generation, installation token exchange, private key management) in parallel with Phase 2 feature delivery would overload the phase without delivering user-visible value.

---

## Decision

Adopt a phased approach to authentication and multi-tenancy:

### Phase 1 (v0.1.0) — Current
- PAT authentication via `GitHubAuthHandler` reading `IOptions<GitHubAuthOptions>`.
- Single-user; no user identity abstraction required yet.

### Phase 2 (v0.2.0) — Interface Preparation
- **Do not implement multi-tenancy**, but introduce an `ICurrentUserContext` interface in `SoloDevBoard.Application` that represents the authenticated user's identity and API token.
- Refactor `IGitHubService` and all Application-layer service interfaces to resolve user context via `ICurrentUserContext` rather than consuming `IOptions<GitHubAuthOptions>` directly.
- The Phase 2 implementation of `ICurrentUserContext` is a simple single-user adapter that reads from `IOptions<GitHubAuthOptions>` — functionally identical to the current behaviour but behind the correct abstraction.
- **Constraint for Delivery Agent:** No Application or Domain service method may access `IOptions<GitHubAuthOptions>` directly. All auth context must flow through `ICurrentUserContext`.

### Phases 3–4 (v0.3.0–v0.4.0) — No Auth Changes
- Continue building features (One-Click Migration, Triage UI, Board Rules Visualiser, Workflow Templates) against the `ICurrentUserContext` abstraction.
- No authentication changes required; the interface work in Phase 2 makes these phases safe.

### Phase 5 (v1.0.0) — Full Multi-Tenancy
- Implement GitHub App authentication (private key, JWT, installation token exchange) per ADR-0005.
- Add a GitHub OAuth login flow: ASP.NET Core authentication middleware, cookie session, GitHub OAuth app registration.
- Replace the single-user `ICurrentUserContext` adapter with a per-request, per-user implementation backed by the authenticated session.
- Per-user token storage: Azure Key Vault with user-scoped naming (production), or equivalent encrypted store.
- Update `plan/SCOPE.md` at that point to move multi-tenancy from "future" to "in scope".

---

## Rationale

- **Minimum viable rework:** The single change in Phase 2 — introducing `ICurrentUserContext` — is small, fits naturally alongside the Label Manager and Audit Dashboard work, and eliminates the largest structural rework risk when multi-tenancy arrives.
- **Avoid premature complexity:** Full GitHub App authentication in Phase 2 would introduce significant complexity (key management, short-lived token refresh logic, GitHub App registration process) without delivering any user-visible feature.
- **Interface stability:** By defining `ICurrentUserContext` early, all Phase 3 and 4 features are written against the correct abstraction and require zero changes when Phase 5 swaps the implementation.
- **GitHub App alignment:** GitHub App authentication is the correct production strategy for a multi-user application — fine-grained, repository-scoped permissions and short-lived tokens (1-hour expiry) are a meaningfully better security posture than long-lived PATs. ADR-0005 already endorses this choice; this ADR adds the roadmap for when and how it is implemented.

---

## Consequences

- `SoloDevBoard.Application` gains an `ICurrentUserContext` interface in Phase 2.
- All Application-layer services are injected with `ICurrentUserContext`; none reference `IOptions<GitHubAuthOptions>` directly.
- The Phase 2 implementation in `SoloDevBoard.Infrastructure` is a trivial single-user adapter — no behaviour change from Phase 1.
- Phase 5 replaces only the `ICurrentUserContext` adapter and adds OAuth middleware; the Application and Domain layers require no changes.
- `plan/SCOPE.md` is updated to reflect multi-tenancy as a deferred goal (Phase 5) rather than permanently out of scope.
- Multi-user / team features remain out of scope until Phase 5 planning is formally approved.
- See also: [ADR-0005](0005-github-api-strategy.md) for the overall GitHub API and authentication strategy.
