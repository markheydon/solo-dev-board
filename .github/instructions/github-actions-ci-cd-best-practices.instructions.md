---
description: 'Focused guidance for secure, reliable GitHub Actions workflows in SoloDevBoard.'
applyTo: '.github/workflows/*.yml,.github/workflows/*.yaml'
---

# GitHub Actions CI/CD Best Practices

Use this instruction for authoring and reviewing workflows in this repository.

## Repository Baseline

- Use .NET 10 SDK setup for build and test jobs.
- Keep workflow intent explicit and easy to scan.
- Prefer small, composable jobs with clear `needs` dependencies.

## Security

- Set explicit `permissions` and default to least privilege.
- Use OIDC for Azure authentication (`id-token: write`) instead of long-lived credentials.
- Never hardcode secrets; use `secrets.*` and environment protections.
- Pin actions to stable versions (`@v4`, `@v3`, etc.) and avoid floating branches.

## Reliability

- Use `concurrency` for workflows where overlapping runs cause risk.
- Use `timeout-minutes` on long-running jobs.
- Add clear step names for debugging and maintenance.
- Keep deployment workflows gated by protected environments.

## Performance

- Use dependency caching when it materially improves runtime.
- Avoid unnecessary full-history checkouts unless required.
- Keep build and test steps deterministic and reproducible.

## Testing and Quality Gates

- Run restore, build, and test in CI for pull requests and main branch pushes.
- Fail fast on build/test failures.
- Surface test outputs clearly in logs and artefacts when useful.

## Deployment Safety

- Require successful CI before deployment.
- Keep production deploys explicit and auditable.
- Include rollback-aware operational guidance in deployment comments or documentation.

## Review Checklist

- Are `permissions` explicit and minimal?
- Is OIDC used correctly for Azure sign-in?
- Are triggers (`on`) scoped correctly?
- Are workflows easy to read and maintain?
- Are build and test steps aligned with .NET 10 conventions?
