---
description: 'Bicep authoring guidance for SoloDevBoard Azure infrastructure.'
applyTo: 'infra/**/*.bicep'
---

# Bicep Implementation

## Purpose

Use these instructions when creating or updating Bicep templates for SoloDevBoard infrastructure in the `infra/` directory.

## Scope

- Azure resources required to host and operate SoloDevBoard.
- Parameters and outputs consumed by deployment automation and CI/CD workflows.
- Security controls, network access rules, managed identity, and Key Vault integration.

## Guardrails

- Follow current Azure Bicep best practices and repository ADR decisions.
- Add `@description` decorators to all Bicep parameters.
- Use symbolic resource names and clear module boundaries.
- Keep templates minimal, focused, and maintainable.
- Do not hardcode secrets, credentials, tenant IDs, or subscription IDs.
- Use secure parameterisation and Key Vault-based secret handling.
- Ensure templates remain aligned with `infra/README.md` and user guidance.

## Quality Requirements

- Bicep changes must compile successfully before commit.
- Lint warnings should be treated as failures unless a documented exception is approved.
- Resource naming and outputs must remain stable for dependent scripts.
- F1 plan limitations and conditional resource settings must be handled explicitly where relevant.

## Validation Checklist

Run before committing:

```bash
az bicep build --file infra/main.bicep --stdout
```

If module entry points are changed, validate each changed `.bicep` file in `infra/`.

## Definition of Done

- Templates compile with no errors.
- Lint checks pass with no warnings.
- No secrets are hardcoded.
- Documentation reflects actual deployed behaviour.
