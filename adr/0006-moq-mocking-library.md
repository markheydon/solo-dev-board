# ADR-0006: Switch Mocking Library from NSubstitute to Moq

**Date:** 2026-03-05
**Status:** Accepted — supersedes [ADR-0002](0002-testing-framework.md); assertion library aspect superseded by [ADR-0008](0008-remove-fluentassertions.md)

---

## Context

[ADR-0002](0002-testing-framework.md) selected **NSubstitute** as the mocking library for SoloDevBoard unit tests. Following a review of the project's tooling conventions (see `.github/skills/dotnet-best-practices/SKILL.md`) and the guidance in `.github/copilot-instructions.md`, a conflict was identified:

- The `dotnet-best-practices` skill and `copilot-instructions.md` both reference **Moq** as the project's mocking framework.
- The test projects were created using NSubstitute, creating an inconsistency that would confuse AI collaborators and lead to mixed mocking patterns in future tests.

The human product manager confirmed that **Moq** is the intended mocking library, and that ADR-0002 should be superseded accordingly.

---

## Decision

**Use Moq** as the mocking library for all SoloDevBoard unit tests, in place of NSubstitute.

The xUnit testing framework remains unchanged. Note: FluentAssertions was later removed by [ADR-0008](0008-remove-fluentassertions.md) due to its commercial licence being incompatible with the project's open-source status.

---

## Rationale

### Alignment with Tooling Documentation

The `dotnet-best-practices` skill (`SKILL.md`) and `copilot-instructions.md` — the primary AI collaborator guidance documents — both specify Moq. Using Moq ensures that AI-generated tests are consistent with documented conventions.

### Moq Strengths

- **Widely used**: Moq is the most popular .NET mocking library, with extensive community support, examples, and AI training data.
- **Familiar API**: The `Mock<T>`, `Setup`, `Returns`, and `Verify` patterns are well-established and widely understood.
- **Strong type safety**: Moq's compile-time setup expressions catch typos and interface changes at build time.
- **Active maintenance**: Moq is actively maintained and supports .NET 10.

### NSubstitute Comparison

NSubstitute remains a capable library, but switching to Moq eliminates the inconsistency between the code and the AI guidance documentation, which takes priority for this AI-driven project.

---

## Consequences

- All test project files (`*.csproj`) reference `<PackageReference Include="Moq" />` instead of `<PackageReference Include="NSubstitute" />`.
- Mocks are created using `new Mock<T>()` and accessed via `.Object`.
- Setup is expressed as: `mock.Setup(s => s.Method(It.IsAny<T>())).Returns(value)`.
- Verification is expressed as: `mock.Verify(s => s.Method(...), Times.Once())`.
- All existing tests using NSubstitute have been rewritten using the Moq API.
- ADR-0002 is marked as **Superseded** by this ADR.
- `copilot-instructions.md` and `.github/skills/dotnet-best-practices/SKILL.md` already reference Moq; no further documentation changes are required.
- FluentAssertions: see [ADR-0008](0008-remove-fluentassertions.md) for the decision to remove it.
