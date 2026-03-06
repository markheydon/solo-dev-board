---
description: 'Guidance for working with .NET 10 projects. Includes project structure, C# language version, package management, and best practices.'
applyTo: '**/*.csproj, **/*.cs'
---

# .NET 10 Development

## Build and Compilation Requirements
- Use `dotnet build` and `dotnet test` for solution and project builds.
- Use `dotnet restore` before build/test when dependencies may have changed.

## Project File Management

### SDK-Style Project Structure
This repository uses SDK-style projects:

- **Implicit file inclusion**: New source files are usually auto-included by SDK-style projects.
- **Modern target framework**: Use `<TargetFramework>net10.0</TargetFramework>`.
- **Nullable and implicit usings**: Keep `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` enabled.

## NuGet Package Management
- Manage packages with `dotnet add package`, `dotnet remove package`, and central package versioning if introduced.
- Keep package choices aligned with the architecture and avoid unnecessary dependencies.
- Prefer stable package versions unless pre-release is explicitly required.

## C# Language Version
- Use C# 14 language features where they improve readability and maintainability.
- Follow repository conventions for file-scoped namespaces, nullable reference types, and primary constructors where appropriate.

## Testing Baseline
- Test framework: `xUnit`.
- Mocking framework: `Moq`.
- Assertions: xUnit built-in `Assert.*` methods. **Do not use FluentAssertions** — it requires a commercial licence under the Xceed Software licence and is prohibited in this open-source project (see ADR-0008).
- Naming convention: `MethodUnderTest_Scenario_ExpectedOutcome`.
- Structure tests using Arrange / Act / Assert with blank lines between sections.

## Environment Considerations (Windows environment)
- Use Windows-style paths with backslashes (e.g., `C:\path\to\file.cs`)
- Use Windows-appropriate commands when suggesting terminal operations
- Consider Windows-specific behaviour when working with file system operations

## Common .NET 10 Pitfalls and Best Practices

### Async/Await Patterns
- **ConfigureAwait(false)**: Use in reusable library code where a captured context is unnecessary:
  ```csharp
  var result = await SomeAsyncMethod().ConfigureAwait(false);
  ```
- **Avoid sync-over-async**: Do not use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.

### DateTime Handling
- **Use DateTimeOffset for timestamps**: Prefer `DateTimeOffset` over `DateTime` for absolute time points
- **Specify DateTimeKind**: When using `DateTime`, always specify `DateTimeKind.Utc` or `DateTimeKind.Local`
- **Culture-aware formatting**: Use `CultureInfo.InvariantCulture` for serialization/parsing

### String Operations
- **StringBuilder for concatenation**: Use `StringBuilder` for multiple string concatenations
- **StringComparison**: Always specify `StringComparison` for string operations:
  ```csharp
  string.Equals(other, StringComparison.OrdinalIgnoreCase)
  ```

### Memory Management
- **Dispose pattern**: Implement `IDisposable` properly for unmanaged resources
- **Using statements**: Always wrap `IDisposable` objects in using statements
- **Avoid large object heap**: Keep objects under 85KB to avoid LOH allocation

### Configuration
- **Use Options pattern**: Bind configuration to strongly typed options and validate startup configuration.
- **Use environment-specific settings**: Keep configuration in `appsettings.json` and `appsettings.{Environment}.json`.

### Exception Handling
- **Specific exceptions**: Catch specific exception types, not generic `Exception`
- **Don't swallow exceptions**: Always log or re-throw exceptions appropriately
- **Use using for disposable resources**: Ensures proper cleanup even when exceptions occur

### Performance Considerations
- **Avoid boxing**: Be aware of boxing/unboxing with value types and generics
- **String interning**: Use `string.Intern()` judiciously for frequently used strings
- **Lazy initialization**: Use `Lazy<T>` for expensive object creation
- **Avoid reflection in hot paths**: Cache `MethodInfo`, `PropertyInfo` objects when possible
