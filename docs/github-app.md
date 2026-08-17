> **Audience:** Operators registering or polishing the hosted-sign-in GitHub App. This guide is repository documentation and is not part of the published end-user site in `website/`.

# GitHub App listing

The production hosted instance uses a **GitHub App** for user sign-in (`/auth/sign-in`) and GitHub API calls on the signed-in user's behalf. GitHub shows the app name, logo, homepage, and requested permissions on the authorise and install screens. A bare registration works, but a thin listing looks untrusted.

This page is the operator checklist for that listing. It follows [GitHub's GitHub App best practices](https://docs.github.com/en/apps/creating-github-apps/about-creating-github-apps/best-practices-for-creating-a-github-app) and maps them to how SoloDevBoard actually behaves.

You cannot apply most of these settings from this repository. Open **[GitHub → Settings → Developer settings → GitHub Apps](https://github.com/settings/apps)** and edit the app used by staging and production.

## Branding

Upload the square icon from this repository:

- Source: [`docs/assets/github-app/icon.svg`](assets/github-app/icon.svg)
- Upload file (at least 200×200 px): [`docs/assets/github-app/icon-512.png`](assets/github-app/icon-512.png)

GitHub crops logos to a square. Use the PNG, not the SVG. After upload, set the **badge background colour** to `#167c38` so it matches the SoloDevBoard primary green.

Suggested listing copy (UK English):

| Field | Suggested value |
|-------|-----------------|
| **GitHub App name** | `SoloDevBoard` (must be unique on GitHub; add a suffix such as `SoloDevBoard Hosted` if the short name is taken) |
| **Description** | `Sign in to SoloDevBoard to audit repositories, manage labels, triage issues, and apply workflow templates from one place.` |
| **Homepage URL** | `https://solodevboard.com/` |

## Identifying and authorising users

Hosted sign-in is an OAuth **authorization code** flow against this GitHub App. Configure:

| Setting | Value |
|---------|--------|
| **Callback URL** | One entry per public origin, each ending `/auth/callback`. Register **all** of them (GitHub allows up to ten). Do **not** enable wildcard matching. |
| **Expire user authorization tokens** | **On** (required). SoloDevBoard already refreshes user access tokens. On current GitHub App settings this is **Optional features → User-to-server token expiration** (leave it enabled; do not click **Opt-out**). |
| **Request user authorization (OAuth) during installation** | **On**. The hosted app needs a user-to-server token (`ghu_…`), not only an installation token. |
| **Enable Device Flow** | **Off**. Device flow is for CLIs and headless clients and increases phishing risk. |
| **Setup URL** | Leave blank if OAuth-during-install is on (GitHub sends users through the callback instead). Otherwise use `https://solodevboard.com/`. |
| **Webhook Active** | **Off** for v1. SoloDevBoard polls the GitHub API; it does not consume app webhooks. Re-enable only when a webhook receiver exists. |

### Callback URLs to register

Replace hostnames if yours differ. Local Aspire ports change; add the current `aspire describe` HTTPS origin when you test hosted sign-in locally.

- `https://<production-fqdn>/auth/callback`
- `https://<staging-fqdn>/auth/callback`
- Optional custom domains, for example `https://solodevboard.app/auth/callback` and `https://staging.solodevboard.app/auth/callback`
- Optional local: `https://localhost:<aspire-https-port>/auth/callback`

Match each callback origin with `HOSTED_CALLBACK_BASE_URI` on the corresponding GitHub Environment when using a custom domain. See [Deployment](deployment.md#custom-domain-container-apps).

## Permissions

Choose the **minimum** permissions SoloDevBoard needs. Installation and user tokens are both limited by these settings. Prefer **Read-only** unless a feature writes.

### Repository permissions

| Permission | Access | Why |
|------------|--------|-----|
| Metadata | Read-only | Required by GitHub for every app. |
| Contents | Read and write | Read repositories and write workflow files under `.github/workflows`. |
| Issues | Read and write | Audit, triage, labels, milestones, and comments. |
| Pull requests | Read and write | Audit and triage of pull requests. |
| Actions | Read-only | Audit Dashboard workflow-run health. |
| Workflows | Read and write | Apply and update GitHub Actions workflow templates. Contents write alone is not enough for workflow files. |
| Projects | Read and write | List linked Projects v2 boards, read board rules, and add triage items to a board. |

Leave **Administration** and other unused repository permissions at **No access**.

Private user-owned Projects v2 still often fail under GitHub App tokens even with Projects read/write. That is a GitHub platform limit, not a missing permission. See [Hosted Authentication — Projects v2](hosted-authentication.md#projects-v2-access-under-hosted-sign-in).

### Organisation permissions

| Permission | Access | Why |
|------------|--------|-----|
| Members | Read-only | Resolve organisation membership for admission control (`read:org`). |
| Projects | Read-only | Organisation-owned Projects v2 linked to repositories. |

### Account permissions

| Permission | Access | Why |
|------------|--------|-----|
| Profile | Read and write | `GET /user` for the signed-in login. GitHub App **Account** permissions offer only **No access** or **Read and write** for Profile; there is no read-only option. |
| Email addresses | No access | SoloDevBoard does not use email. |

Hosted OAuth scopes requested by the app are `read:user read:org` ([`GitHubAuth:HostedSignInScopes`](getting-started.md)). Repository work uses the GitHub App's repository permissions on the user-to-server token, not extra OAuth scopes.

## Installation visibility

| Option | Use when |
|--------|----------|
| **Only on this account** | The hosted instance is private to you (or one organisation). Tightest default for v1. |
| **Any account** | Allow-listed users must install the app on **their** user or organisation accounts. |

Admission control still denies users who are not on `allowed-user-logins` / `allowed-org-logins` even if the app is public.

## Security hygiene (already true in this repo, confirm on the app)

- Store the **client secret** in GitHub Environment secrets and Aspire Key Vault. Never commit it.
- Rotate the client secret if it leaks; update `GH_APP_CLIENT_SECRET` and redeploy.
- Do not generate a GitHub App **private key** unless you add installation-token (bot) flows. Hosted sign-in uses the OAuth client ID and secret only.
- Keep **Expire user authorization tokens** enabled.
- After you change permissions, existing installations keep the old set until the owner accepts the new permissions. Plan a short overlap.

GitHub also recommends webhooks instead of polling, caching tokens, and logging auth events. Token refresh and admission logging are already in the app. Webhooks are out of scope until a receiver exists.

## After you save the app

1. Confirm **Client ID** matches GitHub Environment variable `GH_APP_CLIENT_ID` on `staging` and `production`.
2. Confirm a current **Client secret** is stored as `GH_APP_CLIENT_SECRET`.
3. Install the app on every account that owns repositories you will manage.
4. Smoke hosted sign-in: `/welcome` → **Sign in with GitHub** → authorise screen shows the logo and description → return to the app.

## Related documentation

- [Hosted Authentication](hosted-authentication.md)
- [Getting Started — hosted sign-in](getting-started.md#hosted-sign-in-mode-setup)
- [Deployment](deployment.md)
- [DEC-012: GitHub App-first hosted authentication](../plan/DECISIONS.md#dec-012-github-app-first-hosted-authentication)
