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

- Operators must configure the GitHub App and enable hosted sign-in in application settings (`GitHubAuth__HostedSignInEnabled=true`).
- Admission control is enforced via allow-lists for user and organisation logins.
- Only users and organisations explicitly listed are granted access; all others are denied by default.
- Operators should regularly review denied admission attempts in application logs (for example, App Service logs).

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
