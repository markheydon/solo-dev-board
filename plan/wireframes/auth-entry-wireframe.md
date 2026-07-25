# Auth Entry and Connectivity Wireframe

## Purpose

- Provide a dedicated hosted sign-in landing page so unauthenticated visitors see an explicit entry state instead of the dashboard.
- Surface GitHub personal access token (PAT) connectivity status and recovery guidance in PAT-only local trusted mode.
- Distinguish hosted sign-in problems from PAT connectivity problems in copy and recovery routes.

## Related Issues

- [#249](https://github.com/markheydon/solo-dev-board/issues/249) — Hosted unauthenticated landing page and sign-out return path.
- [#314](https://github.com/markheydon/solo-dev-board/issues/314) — PAT-mode GitHub connectivity readiness.

## User Goals

- Understand that SoloDevBoard requires GitHub sign-in before accessing features (hosted mode).
- Sign out and return to a clear public entry state (hosted mode).
- See whether GitHub is connected before starting feature work (PAT mode).
- Recover from revoked or expired PAT credentials with operator-facing guidance (PAT mode).

## Hosted Landing Page (`/welcome`)

```
+-------------------------------------------------------------+
| App Bar: SoloDevBoard                                       |
+-------------------------------------------------------------+
|                                                             |
|              SoloDevBoard                                   |
|   Your single pane of glass for GitHub workloads.           |
|                                                             |
|   Sign in with your GitHub account to access this           |
|   deployment. Access is limited to operator-approved        |
|   users and organisations.                                  |
|                                                             |
|              [ Sign in with GitHub ]                        |
|                                                             |
+-------------------------------------------------------------+
```

### Interaction Notes

- Route: `/welcome` (public, bypasses admission control).
- Primary action navigates to `/auth/sign-in` (OAuth handshake).
- Authenticated users visiting `/welcome` are redirected to `/` (dashboard).
- Cookie authentication `LoginPath` points to `/welcome` so challenges land here first.
- Sign-out (`/auth/sign-out`) redirects to `/welcome`.
- Uses `LandingLayout` (minimal chrome: app bar only, no navigation drawer).

## PAT Connectivity Shell Indicator

```
+-------------------------------------------------------------+
| App Bar: SoloDevBoard                    [ Connected as @login ] |
+-------------------------------------------------------------+
|                                                             |
| Home                                                        |
| ...feature cards...                                         |
+-------------------------------------------------------------+
```

### Interaction Notes

- Shown only when `HostedSignInEnabled` is false (PAT mode).
- The app bar chip is the single connectivity indicator; it is not duplicated on individual feature pages.
- App bar chip shows `Connected as @login`.
- Copy references PAT configuration (Aspire `gh-pat`, user secrets) — not hosted sign-in.

## PAT Connectivity Recovery Page (`/auth/connectivity-error`)

```
+-------------------------------------------------------------+
| App Bar: SoloDevBoard                                       |
+-------------------------------------------------------------+
|                                                             |
|   GitHub connection problem                                 |
|                                                             |
|   SoloDevBoard could not authenticate with GitHub using   |
|   the configured personal access token. Update the token    |
|   via Aspire parameters or user secrets, then restart     |
|   the application.                                          |
|                                                             |
|              [ Return to home ]                             |
|                                                             |
+-------------------------------------------------------------+
```

### Interaction Notes

- Static HTML page (parallel to hosted `/auth/error`).
- Reason codes distinguish token rejection and unknown failures.
- Missing PAT configuration fails at startup before this page is reached.
- Copy explicitly states this is a PAT configuration problem, not a hosted sign-in session problem.
- Feature pages redirect here when GitHub returns `401` in PAT mode.

## State Variants

| State | Hosted (`/welcome`) | PAT (shell) |
|-------|---------------------|-------------|
| Unauthenticated / not connected | Landing with sign-in CTA | Startup fails fast if PAT invalid at launch |
| Authenticated / connected | Redirect to dashboard | App bar chip shows `@login` |
| Runtime auth failure | `/auth/error` or session-expired flow | `/auth/connectivity-error` |

## Accessibility Notes

- Landing primary button has descriptive label: "Sign in with GitHub".
- Connection status uses `aria-label` on the app bar chip.
- Recovery pages use semantic headings and sufficient colour contrast.
- Focus order: app bar → main heading → explanatory text → primary action.

## Responsive Behaviour

- Landing content is centred with max-width constraint; button remains full-width on narrow viewports.
- App bar connection chip collapses to icon + tooltip on very narrow screens if needed.

## Manual Test Scenarios

### Hosted landing (#249)

1. Enable hosted sign-in and admission control; open the app URL in a private window — expect `/welcome`, not the dashboard.
2. Click **Sign in with GitHub** — expect OAuth flow and return to dashboard after success.
3. Sign out from the app menu — expect return to `/welcome`.

### PAT connectivity (#314)

1. Set `OwnerLogin` and an invalid `gh-pat` — expect startup failure with a clear PAT message.
2. Set a valid PAT — expect the app bar chip showing `Connected as @login`.
3. Revoke the PAT while the app is running, then open Repositories — expect redirect to `/auth/connectivity-error`, not a generic snackbar.
4. Call `GET /health/github` — expect `Healthy` when PAT is valid, `Unhealthy` when not.
