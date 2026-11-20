# Solo Dev Board developer and operator documentation

This folder holds **repository-centric** documentation for contributors, self-hosters, and operators. UI conventions, theme tokens, and MudBlazor patterns live in [`CONVENTIONS.md`](../CONVENTIONS.md) and [`.agents/skills/mudblazor/`](../.agents/skills/mudblazor/).

The public product site (landing, User Guide, About) is published from [`website/`](../website/) via Hugo and GitHub Pages:
`https://solodevboard.com/`.

Release notes live in [`CHANGELOG.md`](../CHANGELOG.md) at the repository root.

## Contents

| Document | Description |
|----------|-------------|
| [Getting Started](getting-started.md) | Prerequisites, Aspire-first local setup, and authentication modes. |
| [Maintaining agent skills](maintaining-agent-skills.md) | Vendored Aspire 17 + Cursor skills under `.agents/skills/` (sync and refresh). |
| [Deployment](deployment.md) | Azure Container Apps via Aspire, including the self-hoster PAT path. |
| [Hosted Authentication](hosted-authentication.md) | Hosted sign-in, operator allow-lists, and fallback modes. |
| [GitHub App listing](github-app.md) | Logo, callback URLs, permissions, and listing copy for the hosted-sign-in GitHub App. |
| [PAT Connectivity](pat-connectivity.md) | PAT validation, shell status, recovery UX, and `/health/github`. |
| [Azure Key Vault setup](azure-key-vault-setup.md) | Shared dev vault (Path B) and deploy-time secret references. |
| [Azure Deployment Costs](azure-costs.md) | Resource charges, SKUs, and cost optimisation for self-hosted Azure deployments. |
| [Observability](observability.md) | Structured logging, Application Insights telemetry, and operational diagnostics. |

## Product feature work (Spec Kit)

From **`v1.4.0`** onward, new capabilities are specified with **GitHub Spec Kit** under `specs/<feature>/` (workflow skills: `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, and related commands). Non-negotiable rules live in [`.specify/memory/constitution.md`](../.specify/memory/constitution.md) and [AGENTS.md](../AGENTS.md). Maintainer cut-over notes: [plan/SPEC_KIT_MIGRATION.md](../plan/SPEC_KIT_MIGRATION.md).

Operator guides in this folder describe **how to run and deploy** the app; they do not replace per-feature specs.

## Previewing the end-user site locally

```bash
./scripts/invoke-hugo-site.sh serve
```

On Windows PowerShell, use `.\scripts\Invoke-HugoSite.ps1 serve`.

See also [DEC-019](../adr/adr-0019-hugo-hextra-end-user-docs.md) and [DOCS_STRATEGY.md](../plan/DOCS_STRATEGY.md).
