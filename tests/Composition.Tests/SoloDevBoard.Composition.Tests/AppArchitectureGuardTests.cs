using System.Xml.Linq;

namespace SoloDevBoard.Composition.Tests;

public sealed class AppArchitectureGuardTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AppProject_DoesNotReferenceInfrastructureProject()
    {
        // Arrange
        var appProjectPath = Path.Combine(RepositoryRoot, "src", "App", "SoloDevBoard.App", "SoloDevBoard.App.csproj");
        var projectDocument = XDocument.Load(appProjectPath);

        // Act
        var infrastructureReferences = projectDocument
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(path => path.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        Assert.Empty(infrastructureReferences);
    }

    [Fact]
    public void AppAssembly_DoesNotDefineInfrastructureUsings()
    {
        // Arrange
        var appSourceRoot = Path.Combine(RepositoryRoot, "src", "App", "SoloDevBoard.App");
        var offenders = Directory
            .EnumerateFiles(appSourceRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(appSourceRoot, "*.razor", SearchOption.AllDirectories))
            .Select(path => (Path: path, Lines: File.ReadAllLines(path)))
            .SelectMany(file => file.Lines
                .Select((line, index) => (file.Path, LineNumber: index + 1, Line: line.Trim()))
                .Where(entry => entry.Line.StartsWith("using SoloDevBoard.Infrastructure", StringComparison.Ordinal)
                    || entry.Line.StartsWith("@using SoloDevBoard.Infrastructure", StringComparison.Ordinal)))
            .Select(entry => $"{entry.Path}:{entry.LineNumber}: {entry.Line}")
            .ToList();

        // Assert
        Assert.True(
            offenders.Count == 0,
            "App must not reference Infrastructure namespaces directly." + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SoloDevBoard.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing SoloDevBoard.slnx.");
    }
}
