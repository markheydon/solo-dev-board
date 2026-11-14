# ADR-0040: C# Playwright E2E suite

**Date:** 2026-09-22
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-040 during the repository workflow alignment chore (2026-11-14).

## Decision

End-to-end tests live in `tests/E2E/SoloDevBoard.E2E.Tests` as xUnit v3 `PageTest` classes using `Microsoft.Playwright`, `Microsoft.Playwright.Xunit.v3`, and `Deque.AxeCore.Playwright` for accessibility scans. Run via `dotnet test` (filter `Category=E2E` for CI journeys; `Category=DocsCapture` with `DOCS_CAPTURE_ENABLED=1` for manual Hugo screenshots). The assembly fixture starts `SoloDevBoard.App` on HTTP port 5080 with placeholder auth env vars; `PLAYWRIGHT_REUSE_SERVER=1` reuses a running instance. Reject maintaining a separate Node.js Playwright toolchain in this repository for E2E.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
