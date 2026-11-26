# ADR-0015: GitHub App-First Hosted Authentication

**Date:** 2026-03-13
**Status:** Accepted

---

## Context

Earlier planning and ADRs (notably ADR-0005 and ADR-0007) implied a hybrid hosted authentication direction for SoloDevBoard: production deployments would support both GitHub App and separate OAuth App sign-in, with multi-tenancy enabled via OAuth. This was intended to maximise compatibility and flexibility, but introduced complexity and duplicated authentication flows.

Recent analysis (see ADR-0014) and evolving GitHub platform capabilities now favour a GitHub App-first approach for hosted authentication. GitHub App user authentication is now the target hosted sign-in path where it satisfies SoloDevBoard's requirements, with operator-controlled admission layered on top. The previous hybrid plan is superseded by this decision.

## Decision

SoloDevBoard will target GitHub App-first hosted authentication for production deployments. Hosted authentication will be GitHub App-only where feasible, relying on GitHub App user authentication and installation-token-based server access. Separate OAuth App registration is retained only as a fallback if GitHub App user authentication does not satisfy a hosted sign-in requirement that SoloDevBoard must support.

This decision preserves:
- PAT-only local trusted mode for development and self-hosted use.
- Secure-by-default behaviour for hosted deployments, including deny-by-default admission control.
- Operator-managed allow-lists for users and organisations admitted to hosted deployments.

Hosted authentication will:
- Use GitHub App user authentication for hosted sign-in where feasible.
- Use installation tokens for server-side GitHub API access after sign-in.
- Enforce admission control via allow-lists (see ADR-0014).
- Deprecate the separate OAuth App dependency as the planned primary architecture.

## Rationale

- GitHub App authentication provides a secure, auditable, and operator-controlled mechanism for hosted deployments where it satisfies sign-in requirements.
- Reduces complexity by eliminating the need for parallel OAuth App flows.
- Aligns with GitHub's recommended best practices for third-party integrations.
- Enables fine-grained admission control and token lifecycle management.

## Consequences

- The earlier hybrid plan (GitHub App + OAuth App) is superseded; hosted authentication is GitHub App-first.
- Separate OAuth App registration is retained only as a fallback for edge cases.
- PAT-only local trusted mode remains for development.
- Admission control is enforced via operator-managed allow-lists.
- Hosted token lifecycle handling becomes a first-class part of the production authentication design.

## Addendum (2026-11-26): Worker credential for overnight collection

**Status:** Accepted (amendment recorded with scheduled snapshot collection delivery)

Overnight snapshot collection uses a **second** GitHub App user authorisation during hosted sign-in. The browser auth cookie keeps the refresh token from the first authorisation. The durable sign-in blob (schema version 2) stores only `workerRefreshToken` and `workerRefreshTokenExpiresAt` from that second grant. A schema 1 blob `refreshToken` field was the interactive grant and is not copied or exchanged as the worker token.

The worker exchanges `workerRefreshToken` only. Hand-started collection uses the signed-in session access token. Operators may refresh the worker token from `/repositories` (**Refresh overnight access**) without moving the 30-day sign-in window or replacing saved organisation logins.

## References

- [ADR-0005](0005-github-api-strategy.md)
- [ADR-0007](0007-multi-tenancy-authentication-phased-approach.md)
- [ADR-0014](0014-hosted-access-control-for-public-deployments.md)
