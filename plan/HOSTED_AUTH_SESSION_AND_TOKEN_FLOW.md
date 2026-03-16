# Hosted Authentication Session and Token Flow

This document defines the hosted authentication and admission-control boundaries delivered for issues #111, #112, #113, #117, and #123.

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

## Token Lifecycle Expectations (#111)

- Hosted access tokens are read from per-request claims supplied by the hosted authentication boundary.
- Optional expiry claims are validated as UTC timestamps.
- Expired tokens are rejected and require a fresh hosted sign-in.
- Invalid or missing hosted token claims fail fast with explicit exceptions to prevent silent downgrade to insecure behaviour.

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

## OAuth App Fallback Boundary (#113)

- A separate OAuth App fallback path is now explicitly supported but remains disabled by default.
- The fallback is controlled by the `GitHubAuth:HostedOAuthAppFallbackEnabled` configuration key.
- OAuth App fallback is only used if enabled and the primary GitHub App authentication path is unavailable.
- PAT-only local trusted mode is preserved and unaffected by hosted fallback settings.

## Rollout Notes

- The delivered implementation adds hosted-mode DI switching, hosted sign-in handshake and callback routes, hosted claim mapping configuration, per-request token validation with optional installation context, and hosted admission control.
- Hosted admission control is deny-by-default and operator-managed.
- The delivered implementation keeps PAT-only local trusted mode unchanged.
- The hosted sign-in handshake now validates anti-forgery state, surfaces callback failure responses explicitly, and only creates a session when claim mapping succeeds.
