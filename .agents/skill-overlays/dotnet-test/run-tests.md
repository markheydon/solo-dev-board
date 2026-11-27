---
core: dotnet-test/run-tests
binding-revision: "1"
mode: extend
---

# Repository test execution (Solo Dev Board)

- **Fast default (local and CI unit gate):** `dotnet test SoloDevBoard.UnitTests.slnf` or `./scripts/test-unit.sh` / `.\scripts\test-unit.ps1`.
- **Pre-merge (with Release build):** `dotnet build SoloDevBoard.slnx -c Release -warnaserror`, then `dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build`. Match [`AGENTS.md`](../../../AGENTS.md) Pull Request Workflow.
- **Full parity (unit + Playwright E2E):** `./scripts/test-all.sh` or `.\scripts\test-all.ps1`. Do not use plain `dotnet test SoloDevBoard.slnx` as the routine fast path; it includes slow E2E under xUnit MTP.
- **Framework:** xUnit v3 with Microsoft Testing Platform only. Do not substitute MSTest or NUnit runners or add test packages unless the user explicitly requests a framework change.
- **Filters:** When filter syntax is unclear, use the `platform-detection` skill before inventing flags.
- **E2E:** Playwright specs live in `tests/SoloDevBoard.E2E.Tests`; run via `test-all` or that project when the user scopes browser coverage. Default agent verification remains `SoloDevBoard.UnitTests.slnf`.