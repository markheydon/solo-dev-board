# ADR-0030: Self-documenting CI workflow split

**Date:** 2026-08-19
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-030 during the repository workflow alignment chore (2026-11-14).

## Decision

Quality gates use one workflow file per concern with a self-documenting name. `ci.yml` runs .NET restore, format, build, and test only. `playwright.yml` runs the C# E2E project (`dotnet test` on `SoloDevBoard.E2E.Tests`) with a PAT/hosted auth matrix and `pwsh …/playwright.ps1 install chromium` (see DEC-040). The Blazor app is started by the E2E assembly fixture (`dotnet run` on port 5080) so local `dotnet test` matches CI. Other gates use `{subject}-validate.yml` (for example `bash-validate.yml`, `powershell-validate.yml`, `github-actions-validate.yml`, `github-scripts-validate.yml`, `hugo-validate.yml`). Reject monolithic CI files that mix unrelated jobs or opaque workflow names such as `automation-lint.yml` or `shell.yml`.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
