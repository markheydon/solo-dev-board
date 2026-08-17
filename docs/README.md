# SoloDevBoard developer and operator documentation

This folder holds **repository-centric** documentation for contributors, self-hosters, and operators.

The public product site (landing, User Guide, About) is published from [`website/`](../website/) via Hugo and GitHub Pages:
`https://solodevboard.com/`.

## Contents

| Document | Description |
|----------|-------------|
| [Getting Started](getting-started.md) | Prerequisites, Aspire-first local setup, and authentication modes. |
| [Deployment](deployment.md) | Azure Container Apps via Aspire, including the self-hoster PAT path. |
| [Hosted Authentication](hosted-authentication.md) | Hosted sign-in, operator allow-lists, and fallback modes. |
| [GitHub App listing](github-app.md) | Logo, callback URLs, permissions, and listing copy for the hosted-sign-in GitHub App. |
| [PAT Connectivity](pat-connectivity.md) | PAT validation, shell status, recovery UX, and `/health/github`. |
| [Azure Deployment Costs](azure-costs.md) | Resource charges, SKUs, and cost optimisation for self-hosted Azure deployments. |
| [Observability](observability.md) | Structured logging, Application Insights telemetry, and operational diagnostics. |

## Previewing the end-user site locally

```bash
./scripts/invoke-hugo-site.sh serve
```

On Windows PowerShell, use `.\scripts\Invoke-HugoSite.ps1 serve`.

See also [DEC-019](../plan/DECISIONS.md#dec-019-hugo-hextra-for-end-user-docs-on-github-pages) and [DOCS_STRATEGY.md](../plan/DOCS_STRATEGY.md).
