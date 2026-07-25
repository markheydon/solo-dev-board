---
layout: default
title: PAT Connectivity
nav_order: 11
---

# PAT Connectivity

This guide explains how SoloDevBoard surfaces GitHub personal access token (PAT) connectivity in **PAT-only local trusted mode**. It complements [Hosted Authentication](hosted-authentication.md), which covers session-based sign-in for multi-user and public deployments.

For configuration, mode comparison, and when to choose PAT versus hosted sign-in, start with [Getting Started — PAT-only local trusted mode](../getting-started.md#pat-only-local-trusted-mode). For a personal Azure instance using the same mode, see [Self-hoster deployment (PAT mode)](../deployment.md#self-hoster-deployment-pat-mode).

## Overview

When `GitHubAuth:HostedSignInEnabled` is `false`, SoloDevBoard authenticates to GitHub using a configured personal access token. The application validates that token at startup and surfaces connectivity status in the shell before you reach feature workflows.

## Startup validation

- On startup, SoloDevBoard calls GitHub `GET /user` with the configured PAT — even when `GitHubAuth:OwnerLogin` is already set.
- If the token is missing, invalid, expired, or revoked, the application fails fast with an operator-facing message.
- Update the token via Aspire parameters (`gh-pat`), user secrets, or Key Vault references, then restart the application.

## Shell status indicator

- In PAT mode, the application shell shows a **Connected as @login** chip in the app bar.
- This is the single connectivity indicator for the deployment; it is not repeated on individual feature pages.
- The chip refreshes when you navigate between pages so runtime connectivity changes are reflected after recovery attempts.
- This confirms GitHub connectivity before you open Repositories, Labels, or other feature pages.

## Runtime recovery

- If GitHub returns `401 Unauthorized` during a PAT-mode API request (for example after token revocation), feature pages redirect to `/auth/connectivity-error`.
- The recovery page returns an HTTP status code that reflects the failure (`401` for a rejected token, `503` for unknown connectivity problems).
- Recovery copy explains that this is a **PAT configuration problem**, not a hosted sign-in session problem.
- Follow the guidance to update the token and restart the application.
- A missing PAT is handled at startup validation and does not use the connectivity error page.

## Health endpoint

| Endpoint | Purpose |
|----------|---------|
| `GET /health/github` | GitHub PAT connectivity readiness (distinct from `/health` and `/alive`) |

Use `/health/github` when you want Container Apps or external monitoring to verify that the configured PAT can reach GitHub. The general `/health` endpoint remains a lightweight readiness check without external dependency detail.

### Manual verification (PAT connectivity)

1. Configure `OwnerLogin` and an invalid `gh-pat` — expect startup to fail with a clear PAT message.
2. Configure a valid PAT — expect the shell to show **Connected as @login**.
3. Revoke the PAT while the app is running, then open **Repositories** — expect redirect to `/auth/connectivity-error`, not a generic feature-page error.
4. Call `GET /health/github` — expect `Healthy` when the PAT is valid.

## Documentation references

- See [Getting Started — PAT-only local trusted mode](../getting-started.md#pat-only-local-trusted-mode) for configuration and mode comparison.
- See [Self-hoster deployment (PAT mode)](../deployment.md#self-hoster-deployment-pat-mode) for deploying a personal Azure instance with a PAT.
- See [Hosted Authentication](hosted-authentication.md) for the hosted sign-in model.
- See [plan/wireframes/auth-entry-wireframe.md](../../plan/wireframes/auth-entry-wireframe.md) for layout reference.

---

> PAT-only local trusted mode is intended for development and trusted personal self-hosted use. Shared or public deployments should use hosted sign-in with admission control.
