# ADR-0008: Remove FluentAssertions — Use xUnit Built-in Assertions Only

**Date:** 2026-03-06
**Status:** Accepted — partially supersedes [ADR-0002](0002-testing-framework.md) and [ADR-0006](0006-moq-mocking-library.md) (assertion library aspect only)

---

## Context

[ADR-0002](0002-testing-framework.md) introduced **FluentAssertions** as an optional assertion library alongside xUnit, citing improved readability and descriptive failure messages. [ADR-0006](0006-moq-mocking-library.md) retained this decision when superseding ADR-0002, explicitly noting "the xUnit testing framework and optional FluentAssertions remain unchanged."

SoloDevBoard was subsequently made **public and open source** under the MIT licence. A review of FluentAssertions v8+ revealed that it is governed by the **Xceed Software Community Licence**, which requires a paid commercial subscription for use in commercial applications. Whilst the project itself is non-commercial, the licence terms create an unacceptable compliance risk for an open-source repository that may be forked, contributed to, or used by others — particularly AI collaborators and the CI pipeline generating artefacts on contributors' behalf.

Practical experience during the implementation of issue #7 (GitHub REST API calls, `GitHubService`) confirmed that xUnit's built-in `Assert.*` class is fully sufficient for all assertion needs in this project. The readability benefit of FluentAssertions does not outweigh the licence risk in an open-source context.

---

## Decision

**Remove FluentAssertions** from all test projects and **use xUnit's built-in `Assert.*` methods exclusively** for all assertions.

FluentAssertions must not be added as a dependency to any project in this solution.

---

## Rationale

### Licence Compliance

FluentAssertions v8+ requires the [Xceed Software Community Licence](https://github.com/fluentassertions/fluentassertions/blob/main/LICENSE). The licence restricts use and imposes conditions incompatible with a fully open-source MIT-licenced project that welcomes external contributors and forks. Removing the package eliminates all licence ambiguity.

### Sufficiency of xUnit Assertions

The xUnit `Assert` class provides all assertion methods required by this project:

| Assertion need | xUnit method |
|---|---|
| Value equality | `Assert.Equal` / `Assert.NotEqual` |
| Boolean conditions | `Assert.True` / `Assert.False` |
| Nullability | `Assert.Null` / `Assert.NotNull` |
| Collection membership | `Assert.Contains` / `Assert.DoesNotContain` |
| Exception verification | `Assert.Throws<T>` / `Assert.ThrowsAsync<T>` |
| Reference equality | `Assert.Same` |
| Collection count | `Assert.Single` / `Assert.Empty` |

xUnit's failure messages are descriptive enough for the test scenarios in this project.

### Open-Source Alignment

The project's MIT licence and public repository status require all dependencies to be licence-compatible. Preferring zero-cost, permissively-licenced dependencies is consistent with the project's open-source values and removes barriers for contributors.

---

## Consequences

- `FluentAssertions` is not referenced in any `*.csproj` file in the `tests/` directory.
- All tests use `Assert.*` from the xUnit `Xunit` namespace exclusively.
- The following instruction and skill files are updated to remove references to FluentAssertions and explicitly prohibit its future addition:
  - `.github/copilot-instructions.md`
  - `.github/instructions/dotnet-framework.instructions.md`
  - `.github/skills/dotnet-best-practices/SKILL.md`
  - `.github/skills/csharp-xunit/SKILL.md`
- [ADR-0002](0002-testing-framework.md) and [ADR-0006](0006-moq-mocking-library.md) are updated to reference this ADR on the assertion library aspect.
- AI collaborators (delivery agents) must not add FluentAssertions when writing or extending tests.
