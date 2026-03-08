---
name: dotnet-best-practices
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
---

# .NET/C# Best Practices

Your task is to ensure .NET/C# code in `${selection}` meets the SoloDevBoard standards for .NET 10 and C# 14.

## Platform Baseline

- Target framework: `.NET 10` (`net10.0`)
- Language version: `C# 14`
- Nullable reference types and implicit usings are enabled
- Architecture: clean/layered (`App`, `Application`, `Domain`, `Infrastructure`)

## Documentation & Structure

- Create comprehensive XML documentation comments for all public classes, interfaces, methods, and properties
- Include parameter descriptions and return value descriptions in XML comments
- Follow repository namespace and folder structure conventions
- Use file-scoped namespaces

## Design Patterns & Architecture

- Keep domain logic out of Razor components
- Use primary constructor syntax for dependency injection where it improves readability
- Use interface segregation with clear naming conventions (prefix interfaces with 'I')
- Respect dependency direction:
	- `Domain` has no external dependencies
	- `Application` depends on `Domain` only
	- `Infrastructure` depends on `Application` and `Domain`
	- `App` depends on `Application`

## Dependency Injection & Services

- Use constructor dependency injection with null checks via `ArgumentNullException.ThrowIfNull(...)`
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- Implement service interfaces for testability

## Async/Await Patterns

- Use async/await for all I/O operations and long-running tasks
- Return Task or Task<T> from async methods
- Use `ConfigureAwait(false)` in reusable library code where appropriate
- Handle async exceptions properly

## Testing Standards

- Use `xUnit` for tests
- Use `Moq` for mocking dependencies
- Use xUnit's built-in `Assert.*` methods for all assertions. **Do not add FluentAssertions** — it requires a commercial licence and is prohibited in this open-source project (see ADR-0008)
- Follow AAA pattern (Arrange, Act, Assert)
- Use test naming convention: `MethodUnderTest_Scenario_ExpectedOutcome`
- Test both success and failure scenarios
- Include null parameter validation tests

## Configuration & Settings

- Use strongly-typed configuration classes with data annotations
- Implement validation attributes (Required, NotEmptyOrWhitespace)
- Use IConfiguration binding for settings
- Support appsettings.json configuration files

## Error Handling & Logging

- Use structured logging with Microsoft.Extensions.Logging
- Include scoped logging with meaningful context
- Throw specific exceptions with descriptive messages
- Use try-catch blocks for expected failure scenarios

## Performance & Security

- Use modern C# features that improve clarity without reducing maintainability
- Implement proper input validation and sanitisation
- Use parameterized queries for database operations
- Never introduce secrets or credentials into source-controlled files

## Code Quality

- Ensure SOLID principles compliance
- **Avoid code duplication (DRY):** Before writing any helper, paging loop, error-handling utility, or serialisation logic, search the same assembly (and sibling assemblies at the same layer) for an existing method that already does it. Prefer promoting an existing `private static` to `internal static` over copy-pasting. New helpers are only justified when no equivalent exists.
- Use meaningful names that reflect domain concepts
- Keep methods focused and cohesive
- Implement proper disposal patterns for resources
