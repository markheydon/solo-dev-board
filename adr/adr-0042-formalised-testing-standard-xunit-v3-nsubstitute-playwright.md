# ADR-0042: Formalised testing standard — xUnit v3, NSubstitute, Playwright E2E

**Date:** 2026-07-21
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-016 during the repository workflow alignment chore (2026-11-14).

## Decision

Use xUnit v3 for all automated unit and component tests. Use NSubstitute for mocks, stubs, and test doubles. Use built-in xUnit `Assert.*` methods only. Use bUnit for Blazor component tests (DEC-007). Use Playwright for end-to-end tests covering key user journeys via `Microsoft.Playwright` and `Microsoft.Playwright.Xunit.v3` in the `SoloDevBoard.E2E.Tests` project (see DEC-040) — not as a replacement for unit tests. Do not test .NET Aspire AppHost modelling or orchestration. Reject FluentAssertions, AwesomeAssertions, Shouldly, Moq, NUnit, and MSTest.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
