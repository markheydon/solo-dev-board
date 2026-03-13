# Hosted Authentication Session and Token Flow

This document defines the hosted authentication boundaries delivered for issues #111 and #112.

## Hosted Sign-In Application Boundary (#112)

- Hosted sign-in is enabled by configuration using `GitHubAuth:HostedSignInEnabled=true`.
- When hosted sign-in is enabled, the application registers cookie authentication and exposes explicit sign-in and sign-out boundary routes (`/auth/sign-in`, `/auth/sign-out`).
- The `/auth/sign-in` route intentionally returns a not-implemented response until a hosted gateway integration is connected, so the sign-in handshake responsibility remains explicit and isolated.

## Per-Request User Context Boundary (#112)

- `ICurrentUserContext` now resolves per request when hosted sign-in mode is enabled.
- Hosted mode uses request claims for owner login and access token values.
- PAT-only local trusted mode remains unchanged and continues to resolve `ICurrentUserContext` from static `GitHubAuth` configuration.

## Installation Lookup Requirements (#111)

- Hosted requests must include a GitHub installation identifier claim.
- Token retrieval fails fast if installation context is missing.
- This claim requirement defines the minimum installation lookup contract for hosted requests.

## Token Lifecycle Expectations (#111)

- Hosted access tokens are read from per-request claims supplied by the hosted authentication boundary.
- Optional expiry claims are validated as UTC timestamps.
- Expired tokens are rejected and require a fresh hosted sign-in.
- Invalid or missing hosted token claims fail fast with explicit exceptions to prevent silent downgrade to insecure behaviour.

## Secure Storage and Cache Boundaries (#111)

- This slice does not persist hosted access tokens to application storage.
- Hosted token material is consumed from request claims only.
- PAT-only local trusted mode continues to use local configuration and user secrets, with no hosted-token persistence.

## Dependencies on Admission Control (#117)

- Hosted authentication is a prerequisite for admission control.
- Admission control remains a separate enforcement layer, planned in issue #117, that evaluates authenticated GitHub identity against operator-managed allow-lists.

## OAuth App Fallback Boundary

- A separate OAuth App fallback remains isolated and non-default.
- No OAuth App fallback path is automatically enabled by this slice.
- Any fallback enablement requires explicit configuration and justification in a follow-on change.

## Rollout Notes

- The delivered implementation adds hosted-mode DI switching, hosted claim mapping configuration, and per-request token/installation validation.
- The delivered implementation keeps PAT-only local trusted mode unchanged.
- The delivered implementation defines, but does not yet integrate, the external hosted gateway handshake at `/auth/sign-in`.
