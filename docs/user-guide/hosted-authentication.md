---
layout: default
title: Hosted Authentication
nav_order: 10
---

# Hosted Authentication

This guide explains the hosted sign-in model for SoloDevBoard, including user and operator expectations, prerequisites, and fallback paths. It is aligned with ADR-0014 and ADR-0015.

## Overview

SoloDevBoard supports a GitHub App-first hosted authentication model for production deployments. This model provides secure, session-based access and enables operator-managed admission control.

## Accessing Hosted Sign-In

- Hosted sign-in is available at `/auth/sign-in` when enabled by the operator.
- You must have a valid GitHub App installation and be listed in the operator-managed allow-list to access hosted mode.
- Sign-in establishes a session with mapped claims for your GitHub login, access token, installation ID, and organisation memberships.

## Operator Prerequisites

- Operators must configure the GitHub App and enable hosted sign-in in application settings (`GitHubAuth__HostedSignInEnabled=true`), or set the AppHost parameter `hosted-sign-in-enabled` to `true` when running via Aspire.
- Admission control is enforced via allow-lists for user and organisation logins.
- Only users and organisations explicitly listed are granted access; all others are denied by default.
- Operators should regularly review denied admission attempts in application logs (for example, App Service logs).

## Local testing with Aspire

To exercise hosted sign-in locally (production-like, multi-tenant behaviour):

1. **Create a GitHub App** at [GitHub → Settings → Developer settings → GitHub Apps](https://github.com/settings/apps). Note the **Client ID** and generate a **Client secret**.
2. **Start Aspire** to allocate an endpoint:
   ```bash
   aspire start --apphost SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   aspire describe
   ```
3. **Register the callback URL** on your GitHub App: `{app-https-url}/auth/callback`. Aspire sets `GitHubAuth:HostedSignInCallbackBaseUri` from the allocated HTTPS endpoint automatically.
4. **Install the GitHub App** on the test users or organisations.
5. **Configure AppHost parameters** (dashboard, `SoloDevBoard.AppHost/appsettings.json`, or `aspire secret set`):
   - `hosted-sign-in-enabled` → `true`
   - `github-app-client-id` → your client ID
   - `github-app-client-secret` → your client secret (via `aspire secret set`)
   - `allowed-user-logins` and/or `allowed-org-logins` → comma-separated logins
   - Leave `github-pat` unset
6. **Restart Aspire** and navigate to `/auth/sign-in` on the `app` URL.

See [`SoloDevBoard.AppHost/README.md`](../../SoloDevBoard.AppHost/README.md) and [Getting Started — hosted sign-in setup](../getting-started.md#hosted-sign-in-mode-setup) for parameter details.

## Fallback and Local Trusted Modes

- PAT-only local trusted mode remains available for development and self-hosted use. It does not require hosted sign-in infrastructure.
- OAuth App fallback is supported but disabled by default. It is only used if enabled and the primary GitHub App authentication path is unavailable.

## Session and Token Flow

- Hosted sign-in establishes a session with per-request user context and access token claims.
- Token expiry and failure handling are enforced; expired or invalid tokens require a fresh sign-in.
- Admission control is applied after authentication, based on allow-list configuration.

## Documentation References

- See [Getting Started](../getting-started.md) for prerequisites and setup.
- See [ADR-0014](../../adr/0014-hosted-access-control-for-public-deployments.md) and [ADR-0015](../../adr/0015-github-app-first-hosted-authentication.md) for architectural rationale.
- See [plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md](../../plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md) for session and token flow details.

---

> Hosted authentication is recommended for production deployments. PAT-only local trusted mode is preserved for development and trusted self-hosted use.
