# ADR-0003: Use Bicep for Azure Infrastructure as Code

**Date:** 2025-01-01
**Status:** Accepted

---

## Context

SoloDevBoard is deployed to Azure App Service. The Azure infrastructure must be defined as code so that it can be:
- Version controlled alongside the application source code.
- Deployed repeatably to multiple environments (dev, staging, prod).
- Reviewed in pull requests.
- Managed without manual steps in the Azure Portal.

The following Infrastructure as Code (IaC) options were considered:

| Option | Description |
|--------|-------------|
| **Bicep** | Microsoft's domain-specific language (DSL) for Azure Resource Manager (ARM). |
| **ARM Templates (JSON)** | The underlying ARM JSON format that Bicep compiles to. |
| **Terraform (AzureRM provider)** | HashiCorp's multi-cloud IaC tool. |
| **Pulumi (C# SDK)** | IaC using familiar programming languages including C#. |

---

## Decision

**Use Bicep for all Azure infrastructure definitions.**

---

## Rationale

- **Azure-native:** Bicep is Microsoft's first-class IaC language for Azure. It has full parity with ARM templates and is actively developed alongside new Azure features.
- **Simplicity over ARM JSON:** Bicep's syntax is significantly more concise and readable than raw ARM JSON, reducing cognitive overhead for a solo developer.
- **No external tooling:** Bicep is installed as part of the Azure CLI (`az bicep install`) and requires no separate account, state file, or backend — unlike Terraform.
- **Modularity:** Bicep supports modules (`module` keyword) that allow the infrastructure to be split into logical units (e.g. `appservice.bicep`, `keyvault.bicep`).
- **AI tooling support:** GitHub Copilot has strong support for Bicep, making it a good fit for AI-driven development.
- **Pulumi considered but deferred:** Pulumi's C# SDK is appealing for a .NET developer, but adds complexity (Pulumi account, state management, SDK dependencies) that is not justified for a single-environment deployment.

---

## Consequences

- All Azure infrastructure is defined in the `infra/` directory using `.bicep` files.
- Modules are organised under `infra/modules/`.
- All Bicep parameters must have `@description` decorators.
- Symbolic resource names are used throughout (modern Bicep syntax).
- Secrets (e.g. GitHub tokens) are stored in Azure Key Vault, not in Bicep parameter files or app settings.
- Deployments are triggered via the GitHub Actions CD pipeline (`.github/workflows/cd.yml`) using OIDC authentication (no long-lived credentials stored as GitHub secrets where possible).
- The Bicep `main.bicep` file is the entry point; it calls modules for individual resource groups.
