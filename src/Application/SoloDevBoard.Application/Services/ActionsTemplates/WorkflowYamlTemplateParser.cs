using System.Text.RegularExpressions;
using SoloDevBoard.Domain.Entities.ActionsTemplates;

namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Parses workflow YAML for display metadata and inferred template parameters.</summary>
public static partial class WorkflowYamlTemplateParser
{
    private const string PlaceholderPrefix = "{{";
    private const string PlaceholderSuffix = "}}";

    [GeneratedRegex(@"^\s*name\s*:\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex WorkflowNameRegex();

    [GeneratedRegex(@"\{\{([^}]+)\}\}")]
    private static partial Regex PlaceholderRegex();

    /// <summary>Resolves the display name from workflow YAML or the file name when no <c>name:</c> key is present.</summary>
    /// <param name="yamlContent">The workflow YAML content.</param>
    /// <param name="fileName">The workflow file name used as a fallback display name.</param>
    /// <returns>The resolved display name.</returns>
    public static string ResolveDisplayName(string yamlContent, string fileName)
    {
        ArgumentNullException.ThrowIfNull(yamlContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var match = WorkflowNameRegex().Match(yamlContent);
        if (!match.Success)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }

        var rawName = match.Groups[1].Value.Trim();
        return TrimYamlScalar(rawName);
    }

    /// <summary>Infers required string parameters from unique <c>{{token}}</c> placeholders in workflow YAML.</summary>
    /// <param name="yamlContent">The workflow YAML content.</param>
    /// <returns>The inferred workflow template parameters.</returns>
    public static IReadOnlyList<ActionsTemplateParameter> InferParameters(string yamlContent)
    {
        ArgumentNullException.ThrowIfNull(yamlContent);

        var tokenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in PlaceholderRegex().Matches(yamlContent))
        {
            var tokenName = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(tokenName))
            {
                tokenNames.Add(tokenName);
            }
        }

        return tokenNames
            .OrderBy(tokenName => tokenName, StringComparer.Ordinal)
            .Select(tokenName => new ActionsTemplateParameter
            {
                Name = tokenName,
                Label = tokenName,
                Description = string.Empty,
                DefaultValue = string.Empty,
                IsRequired = true,
            })
            .ToArray();
    }

    /// <summary>Determines whether the file name represents a top-level workflow YAML file.</summary>
    /// <param name="fileName">The workflow file name.</param>
    /// <returns><see langword="true" /> when the file name ends with <c>.yml</c> or <c>.yaml</c>; otherwise, <see langword="false" />.</returns>
    public static bool IsWorkflowYamlFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimYamlScalar(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"'))
                || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
