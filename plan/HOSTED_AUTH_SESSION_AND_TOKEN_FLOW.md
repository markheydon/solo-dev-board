# Hosted Authentication Session and Token Flow

This document defines the hosted authentication and admission-control boundaries delivered for issues #111, #112, #117, and #123.

## Hosted Sign-In Application Boundary (#112)

- Hosted sign-in is enabled by configuration using `GitHubAuth:HostedSignInEnabled=true`.
- When hosted sign-in is enabled, the application registers cookie authentication and exposes explicit sign-in and sign-out boundary routes (`/auth/sign-in`, `/auth/sign-out`).
- The `/auth/sign-in` and `/auth/callback` routes now implement the real hosted sign-in handshake, establishing a session with mapped claims for owner login, access token, installation ID, optional token expiry, and organisation logins.
- State validation is enforced during the sign-in handshake, and callback failures result in explicit error responses and no session creation.

## Per-Request User Context Boundary (#112)

- `ICurrentUserContext` now resolves per request when hosted sign-in mode is enabled.
- Hosted mode uses request claims for owner login and access token values.
- PAT-only local trusted mode remains unchanged and continues to resolve `ICurrentUserContext` from static `GitHubAuth` configuration.

## Installation Lookup Requirements (#111)

- Hosted requests must include a GitHub installation identifier claim.
- Token retrieval fails fast if installation context is missing.
- This claim requirement defines the minimum installation lookup contract for hosted requests.

## Token Lifecycle Expectations (#111, #349)

- Hosted access tokens are read from per-request claims supplied by the hosted authentication boundary.
- Optional expiry claims are validated as UTC timestamps.
- Expired access tokens are refreshed automatically when a valid refresh token is present in the hosted claims.
- Invalid or missing hosted token claims fail fast with explicit exceptions to prevent silent downgrade to insecure behaviour.

## Runtime Session Recovery

- When GitHub returns `401 Unauthorized` for a hosted API request, the application throws `HostedAuthenticationRequiredException` and feature pages initiate recovery through `/auth/session-expired`.
- The `/auth/session-expired` route signs out the stale auth cookie and redirects to `/auth/error?reason=session-expired` with user-facing copy and a **Sign in again** action that preserves the original page where possible.
- Cookie authentication validates the token expiry claim on each request. When the access token is expired or within a five-minute refresh skew, the application attempts to exchange the refresh token for a new access token. On success, the cookie principal is replaced and the cookie is renewed.
- If the refresh token is missing, expired, or rejected, the principal is rejected and the next request is challenged to `/welcome`, then redirected to the session expired page.
- Admission control middleware bypasses public infrastructure paths, including `/welcome` and `/_blazor`, so unauthenticated visitors can render the hosted landing page and establish a Blazor circuit.
- The main application shell exposes a **Sign out** action when hosted sign-in is enabled and the user is authenticated.

## Secure Storage and Cache Boundaries (#111)

- This slice does not persist hosted access tokens to application storage.
- Hosted token material is consumed from request claims only.
- PAT-only local trusted mode continues to use local configuration and user secrets, with no hosted-token persistence.


## Hosted Admission Control (Implemented in #117)

- Hosted authentication is a prerequisite for admission control.
- Admission control is now implemented as a separate enforcement layer that evaluates authenticated GitHub identity against operator-managed allow-lists.
- Admission control is deny-by-default: only users and organisations explicitly listed in the allow-lists are granted access in hosted mode.
- Operator-managed allow-lists are configured via `HostedAdmissionControl:AllowedUserLogins` and `HostedAdmissionControl:AllowedOrganisationLogins`.
- The `HostedAdmissionControl:Enabled` flag controls whether admission control is active in hosted deployments.
- Organisation claims for hosted admission are mapped using the `HostedAdmissionControl:HostedOrganisationLoginsClaimType` key.

### Audit and Logging of Denied Admission Requests

- All denied hosted admission requests are logged with the attempted user login, organisation claims, and the reason for denial.
- Operators are expected to review denied admission attempts regularly to detect unauthorised access attempts or misconfiguration.
- Audit logs should be retained according to organisational policy and reviewed for suspicious activity.

## Refresh Token Support (#349)

- The hosted sign-in token exchange now captures `refresh_token` and `refresh_token_expires_in` from the GitHub access-token response.
- `HostedGitHubAuthSession` carries the refresh token and its expiry, and `CreatePrincipal` maps these to encrypted cookie claims.
- A new `HostedGitHubAuthGateway.RefreshSessionAsync` method exchanges `grant_type=refresh_token` with the GitHub access-token endpoint and returns a refreshed session.
- `HostedCookieAuthenticationEvents` triggers refresh when the access token is near or past expiry. Successful refresh replaces the principal and renews the cookie; failure falls back to the existing **Session expired** flow.
- PAT-only local trusted mode does not use refresh tokens and is unaffected by this change.

## Removed OAuth App Fallback Boundary (#113)

Hosted sign-in now uses GitHub App user authentication only. The separate OAuth App fallback path has been removed. PAT-only local trusted mode is preserved and unaffected by hosted sign-in settings.

## Test Coverage Expectations (Issue #114)

This section documents the test coverage requirements for GitHub App-first hosted authentication, as delivered in [DEC-012](DECISIONS.md#dec-012-github-app-first-hosted-authentication) and related issues.

- Hosted sign-in and per-request user context must be covered by unit tests for session establishment, claim mapping, and context resolution.
- Installation discovery, token issuance, expiry, and failure handling are covered by unit tests and integration tests. Expiry and invalid token scenarios must be tested for explicit failure and no silent fallback.
- Admission control and allow-list enforcement are covered by unit tests for edge cases (deny-by-default, allow-list misconfiguration, organisation claims mapping). Operator-managed allow-lists must be tested for both user and organisation paths.
- Unit-test and mocked seams cover session creation, claim mapping, and admission control logic. Environment-dependent tests cover real GitHub App installation, token issuance, and expiry scenarios.
- Documentation-sensitive regressions are covered by tests ensuring PAT-only local trusted mode is preserved.

See [DEC-012](DECISIONS.md#dec-012-github-app-first-hosted-authentication) and GitHub Issues [#247](https://github.com/markheydon/solo-dev-board/issues/247)–[#250](https://github.com/markheydon/solo-dev-board/issues/250) for references to completed coverage and test boundaries.

## Remaining V1 Auth Polish

The hosted sign-in and admission-control boundaries above are delivered. Operator secrets remain Aspire parameters, user secrets, and Key Vault — in-app secret editing is out of scope.

| Issue | Scope | Status |
|-------|-------|--------|
| [#249](https://github.com/markheydon/solo-dev-board/issues/249) | Hosted unauthenticated landing page and sign-out return path | Done |
| [#314](https://github.com/markheydon/solo-dev-board/issues/314) | PAT-mode GitHub connectivity readiness (startup probe, shell status, recovery UX, optional `/health/github`) | Done |
| [#247](https://github.com/markheydon/solo-dev-board/issues/247) | PAT-only local trusted mode documentation | Docs in [PR #324](https://github.com/markheydon/solo-dev-board/pull/324); see [getting-started.md](../docs/getting-started.md#pat-only-local-trusted-mode) |
| [#248](https://github.com/markheydon/solo-dev-board/issues/248) | Self-hoster deployment documentation | Docs in [PR #324](https://github.com/markheydon/solo-dev-board/pull/324); see [deployment.md](../docs/deployment.md#self-hoster-deployment-pat-mode) |

## Rollout Notes

- The delivered implementation adds hosted-mode DI switching, hosted sign-in handshake and callback routes, hosted claim mapping configuration, per-request token validation with optional installation context, and hosted admission control.
- Hosted admission control is deny-by-default and operator-managed.
- The delivered implementation keeps PAT-only local trusted mode unchanged.
- The hosted sign-in handshake now validates anti-forgery state, surfaces callback failure responses explicitly, and only creates a session when claim mapping succeeds.
