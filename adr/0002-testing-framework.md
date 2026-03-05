# ADR-0002: Use xUnit and NSubstitute for Testing

**Date:** 2025-01-01
**Status:** Superseded by [ADR-0006](0006-moq-mocking-library.md)

---

## Context

SoloDevBoard requires a testing framework and mocking library for unit and integration tests. The chosen tools must:
- Integrate well with .NET 10 and the `dotnet test` CLI.
- Support the `Arrange / Act / Assert` pattern cleanly.
- Allow isolation of dependencies (GitHub API clients, repositories) via mocking/substitution.
- Be widely used in the .NET ecosystem so that Copilot and other AI tools can generate accurate test code.

The following options were considered:

| Option | Description |
|--------|-------------|
| **xUnit + NSubstitute** | xUnit as test runner; NSubstitute for creating substitutes (mocks/stubs). |
| **xUnit + Moq** | xUnit as test runner; Moq for mocking. |
| **NUnit + NSubstitute** | NUnit as test runner; NSubstitute for mocking. |
| **MSTest + Moq** | Microsoft's test framework with Moq. |

---

## Decision

**Use xUnit as the test framework and NSubstitute as the mocking library.**

Optionally use **FluentAssertions** for expressive assertion syntax.

---

## Rationale

### xUnit

- xUnit is the recommended test framework for .NET and is used by Microsoft internally for ASP.NET Core, Blazor, and Entity Framework tests.
- It integrates seamlessly with `dotnet test`, GitHub Actions, and all major IDEs.
- Constructor injection for test fixtures and the `IDisposable` pattern for cleanup are idiomatic and clean.
- xUnit's `[Theory]` and `[InlineData]` attributes support data-driven tests neatly.

### NSubstitute

- NSubstitute provides a fluent, readable API for creating substitutes: `Substitute.For<IMyService>()`.
- Its setup syntax reads naturally: `mySubstitute.MyMethod(Arg.Any<string>()).Returns("result")`.
- It avoids the "Mock.Setup" verbosity of Moq and is less likely to produce confusing failure messages.
- NSubstitute works well with records and interfaces, which are extensively used in SoloDevBoard's domain layer.

### FluentAssertions (Optional)

- FluentAssertions makes assertions self-documenting: `result.Should().Be(expected)`.
- It produces descriptive failure messages that include the actual and expected values.
- It is used where the improved readability is worth the dependency; not mandatory for every test.

---

## Consequences

- All test projects use `<PackageReference Include="xunit" />` and `<PackageReference Include="NSubstitute" />`.
- Test method naming convention: `MethodUnderTest_Scenario_ExpectedOutcome`.
- Arrange / Act / Assert sections are separated by blank lines (no `// Arrange` comments required).
- Substitutes are created for all external dependencies (GitHub API clients, repositories). No real HTTP calls are made in unit tests.
- Integration tests that call the real GitHub API are placed in a separate test project and are excluded from the CI run by default (they require a valid PAT and are run manually or in a dedicated environment).
