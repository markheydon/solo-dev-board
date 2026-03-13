---
description: 'PowerShell authoring guidance for SoloDevBoard infrastructure automation.'
applyTo: 'infra/**/*.ps1,infra/**/*.psm1'
---

# PowerShell Implementation

## Purpose

Use these instructions when implementing or updating PowerShell automation for SoloDevBoard infrastructure and deployment workflows.

## Scope

- Deployment scripts and helper modules under `infra/`.
- Azure CLI orchestration and prerequisite checks.
- Operator guidance output and post-deployment instructions.

## Guardrails

- Use approved PowerShell verbs and singular function nouns.
- Use `[CmdletBinding()]` for advanced scripts and functions.
- Use parameter validation attributes (for example `[ValidateNotNullOrWhiteSpace()]`).
- Keep error handling explicit and actionable.
- Do not hardcode secrets, credentials, tenant IDs, or subscription IDs.
- Prefer standard output streams (`Write-Output`, `Write-Warning`, `Write-Error`) over host-only output.
- Keep script behaviour consistent across Windows PowerShell and PowerShell 7 where feasible.
- Ensure user-facing text uses UK English.

## Quality Requirements

- Scripts should fail fast on prerequisite or deployment errors.
- Guidance messages should explain remediation steps clearly.
- Script changes must not break existing deployment parameter contracts without documentation updates.
- New functions should be small, focused, and testable.

## Validation Checklist

Run before committing:

```powershell
Invoke-ScriptAnalyzer -Path ./infra -Settings ./PSScriptAnalyzerSettings.psd1 -Recurse
```

Then run an end-to-end dry run where practical (for example local prerequisite validation).

## Definition of Done

- PSScriptAnalyzer reports no errors or warnings.
- No secrets are hardcoded.
- Script output and remediation guidance are clear.
- Related infrastructure documentation remains accurate.
