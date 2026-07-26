> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `user-docs/`.

# Hosted Authentication

This guide explains the hosted sign-in model for SoloDevBoard, including user and operator expectations, prerequisites, and fallback paths.

## Overview

SoloDevBoard supports a GitHub App-first hosted authentication model for production deployments. This model provides secure, session-based access and enables operator-managed admission control.

## Accessing Hosted Sign-In

- Unauthenticated visitors are shown a dedicated landing page at `/welcome` with a **Sign in with GitHub** action.
- The OAuth handshake starts at `/auth/sign-in` when you choose to sign in from the landing page.
- You must have a valid GitHub App installation and be listed in the operator-managed allow-list to access hosted mode.
- Sign-in establishes a session with mapped claims for your GitHub login, access token, installation ID, and organisation memberships.
- Signing out returns you to `/welcome`.

## Operator Prerequisites

- Operators must configure the GitHub App and enable hosted sign-in in application settings (`GitHubAuth__HostedSignInEnabled=true`), or set the AppHost parameter `hosted-sign-in-enabled` to `true` when running via Aspire.
- Admission control is enforced via allow-lists for user and organisation logins.
- Only users and organisations explicitly listed are granted access; all others are denied by default.
- Operators should regularly review denied admission attempts in application logs (for example, Container Apps logs via Log Analytics).

## Local testing with Aspire

To exercise hosted sign-in locally (production-like, multi-tenant behaviour):

1. **Create a GitHub App** at [GitHub â†’ Settings â†’ Developer settings â†’ GitHub Apps](https://github.com/settings/apps). Note the **Client ID** and generate a **Client secret**.
2. **Start Aspire** to allocate an endpoint:
   ```bash
   aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
   aspire describe
   ```
3. **Register the callback URL** on your GitHub App: `{app-https-url}/auth/callback`. Aspire sets `GitHubAuth:HostedSignInCallbackBaseUri` from the allocated HTTPS endpoint automatically.
4. **Install the GitHub App** on the test users or organisations.
5. **Configure AppHost parameters** (dashboard, `src/SoloDevBoard.AppHost/appsettings.json`, or `aspire secret set`):
   - `hosted-sign-in-enabled` â†’ `true`
   - `gh-app-client-id` â†’ your client ID
   - `gh-app-client-secret` â†’ your client secret (via `aspire secret set`)
   - `allowed-user-logins` and/or `allowed-org-logins` â†’ comma-separated logins
   - Leave `gh-pat` unset
6. **Restart Aspire** and navigate to the app URL — you should see the `/welcome` landing page before signing in.

### Manual verification (hosted landing)

1. Open the app URL in a private browser window — expect the `/welcome` landing page, not the dashboard.
2. Choose **Sign in with GitHub** — expect the GitHub OAuth flow and return to the originally requested page (for example `/about`) after success.
3. Sign out from the app menu — expect return to `/welcome`.

See [`src/SoloDevBoard.AppHost/README.md`](../../src/SoloDevBoard.AppHost/README.md) and [Getting Started — hosted sign-in setup](getting-started.md#hosted-sign-in-mode-setup) for parameter details.

## Fallback and Local Trusted Modes

- **PAT-only local trusted mode** remains available for development and trusted personal self-hosting. It does not require hosted sign-in infrastructure. See [Getting Started — PAT-only local trusted mode](getting-started.md#pat-only-local-trusted-mode) and [PAT Connectivity](pat-connectivity.md).
- To run a personal Azure instance with a PAT (no GitHub App), follow [Self-hoster deployment (PAT mode)](deployment.md#self-hoster-deployment-pat-mode).
- OAuth App fallback is supported but disabled by default. It is only used if enabled and the primary GitHub App authentication path is unavailable.

## Session and Token Flow

- Hosted sign-in establishes a session with per-request user context and access token claims.
- Token expiry and failure handling are enforced; expired or invalid tokens require a fresh sign-in.
- When GitHub rejects the access token (for example after revocation) or the token expiry claim is in the past, the application signs you out automatically and shows a **Session expired** page with a **Sign in again** action.
- You can also sign out manually from the app menu at any time when hosted sign-in is enabled.
- Admission control is applied after authentication, based on allow-list configuration.

## Projects v2 access under hosted sign-in

GitHub App sign-in can load **public** Projects v2 boards linked to a repository, but **private user-owned** Projects v2 are often inaccessible to the app token even when you are project admin. GitHub may report linked boards that SoloDevBoard cannot read.

When this happens, Board Rules and Triage show a warning naming how many linked boards could not be loaded. Supported boards that are accessible are still available.

**Workarounds:**

- Use **PAT mode** with the `read:project` scope for full user-level Projects v2 access.
- Make the linked project **public** if GitHub App sign-in must remain the only auth path.

This is a known GitHub platform limitation for GitHub Apps, not a SoloDevBoard configuration defect. See [plan/GITHUB_PROJECTS_V2_ACCESS.md](../plan/GITHUB_PROJECTS_V2_ACCESS.md) and [Unlocking GitHub Apps: Why Bots Need Access to Private Projects v2](https://devactivity.com/posts/apps-tools/unlocking-github-apps-why-bots-need-access-to-private-projects-v2-for-enhanced-productivity/).

## Documentation References

- See [Getting Started](getting-started.md) for prerequisites and setup, including the PAT versus hosted comparison.
- See [DEC-011](../plan/DECISIONS.md#dec-011-hosted-access-control-for-public-deployments) and [DEC-012](../plan/DECISIONS.md#dec-012-github-app-first-hosted-authentication) for architectural rationale. Legacy ADR text: [ADR-0014](../adr/archive/0014-hosted-access-control-for-public-deployments.md), [ADR-0015](../adr/archive/0015-github-app-first-hosted-authentication.md).
- See [plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md](../plan/HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md) for session and token flow details.

---

> Hosted authentication is recommended for shared or public production deployments. PAT-only local trusted mode is preserved for development and trusted personal self-hosted use.
