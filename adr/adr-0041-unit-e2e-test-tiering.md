# ADR-0041: Unit/E2E test tiering for local and CI workflows

**Date:** 2026-09-22
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-041 during the repository workflow alignment chore (2026-11-14).

## Decision

Local and CI test entry points are tiered for speed and full coverage. `SoloDevBoard.UnitTests.slnf` excludes E2E for the fast default (`scripts/test-unit`, `ci.yml` test step). E2E is opt-in locally via `scripts/test-e2e` / `scripts/test-all` or `dotnet test` on `tests/E2E/SoloDevBoard.E2E.Tests`. `ci.yml` builds the full solution but tests only the unit filter; `playwright.yml` owns E2E with xUnit v3 MTP `--filter-query` filters (PAT: `/[Category=E2E]`; hosted: `/[(Category=E2E)&(AuthMode=Hosted)]`). `E2eHostedPipelineSanityTests` guarantees the hosted filter always matches at least one test. Playwright browser install defaults to skipped on build (`SkipPlaywrightInstall=true`). Reject trait-exclusion filters across the full solution as the primary fast path (xUnit v3 MTP exit code 8 when an assembly runs zero tests). Plain `dotnet test` on `SoloDevBoard.slnx` may still run E2E under MTP — prefer the unit filter or scripts.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
