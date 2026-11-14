# ADR-0021: Two-tier CD pipeline

**Date:** 2026-07-29
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-021 during the repository workflow alignment chore (2026-11-14).

## Decision

GitHub Actions CD uses two protected GitHub Environments — `staging` and `production` — mapped to Aspire deploy environments `Staging` and `Production` respectively. The **app resource group** is an operator choice per GitHub Environment (`AZURE_RESOURCE_GROUP`); one RG for both tiers or separate RGs are both valid. Aspire `--environment` does not suffix Azure resource names; the AppHost must use distinct resource names for Staging (`aca-staging`, `app-staging`, `auth-secrets-staging`, `app-insights-staging`) while Production keeps the original names (`aca`, `app`, `auth-secrets`, `app-insights`) so an existing production Container App is not recreated when both tiers share an RG. **Staging** deploys automatically on push to `main` with GitHub App hosted sign-in for pre-release validation. **Production** deploys on `v*` release tags with GitHub App hosted sign-in and required environment reviewers. PAT-only authentication remains for local development and personal self-hosting via local `aspire deploy` only — not as a hosted CD tier, because a deployed PAT-mode instance exposes the token owner's full GitHub account to anyone who can reach the URL. End-user docs on GitHub Pages publish on `v*` tags only; pull requests validate Hugo builds without publishing. Reject deploying production on every `main` merge, publishing Pages on `main`, or a hosted CD tier that deploys PAT-only authentication.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
