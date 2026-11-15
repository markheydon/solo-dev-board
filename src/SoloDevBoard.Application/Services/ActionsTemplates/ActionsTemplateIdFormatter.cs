namespace SoloDevBoard.Application.Services.ActionsTemplates;

/// <summary>Formats and parses stable workflow template identifiers.</summary>
public static class ActionsTemplateIdFormatter
{
    private const string BuiltInPrefix = "builtin:";
    private const string CustomPrefix = "custom:";

    /// <summary>Formats a built-in workflow template identifier.</summary>
    /// <param name="builtInNumber">The built-in template number.</param>
    /// <returns>The stable built-in template identifier.</returns>
    public static string FormatBuiltIn(int builtInNumber) => $"{BuiltInPrefix}{builtInNumber}";

    /// <summary>Formats a custom workflow template identifier.</summary>
    /// <param name="repositoryFullName">The source repository in owner/repository format.</param>
    /// <param name="workflowFilePath">The relative workflow file path.</param>
    /// <returns>The stable custom template identifier.</returns>
    public static string FormatCustom(string repositoryFullName, string workflowFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryFullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowFilePath);

        return $"{CustomPrefix}{repositoryFullName}:{workflowFilePath}";
    }

    /// <summary>Determines whether the identifier refers to a built-in workflow template.</summary>
    /// <param name="templateId">The workflow template identifier.</param>
    /// <returns><see langword="true" /> when the identifier is a built-in template id; otherwise, <see langword="false" />.</returns>
    public static bool IsBuiltIn(string templateId)
        => templateId.StartsWith(BuiltInPrefix, StringComparison.Ordinal);

    /// <summary>Determines whether the identifier refers to a custom workflow template.</summary>
    /// <param name="templateId">The workflow template identifier.</param>
    /// <returns><see langword="true" /> when the identifier is a custom template id; otherwise, <see langword="false" />.</returns>
    public static bool IsCustom(string templateId)
        => templateId.StartsWith(CustomPrefix, StringComparison.Ordinal);

    /// <summary>Parses a built-in workflow template number from its identifier.</summary>
    /// <param name="templateId">The built-in workflow template identifier.</param>
    /// <returns>The built-in template number.</returns>
    public static int ParseBuiltInNumber(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        if (!IsBuiltIn(templateId)
            || !int.TryParse(templateId[BuiltInPrefix.Length..], out var builtInNumber)
            || builtInNumber < 1)
        {
            throw new FormatException($"Workflow template identifier '{templateId}' is not a valid built-in template id.");
        }

        return builtInNumber;
    }

    /// <summary>Parses a custom workflow template identifier into its source repository and file path.</summary>
    /// <param name="templateId">The custom workflow template identifier.</param>
    /// <returns>The source repository and workflow file path.</returns>
    public static (string RepositoryFullName, string WorkflowFilePath) ParseCustom(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        if (!IsCustom(templateId))
        {
            throw new FormatException($"Workflow template identifier '{templateId}' is not a valid custom template id.");
        }

        var payload = templateId[CustomPrefix.Length..];
        var separatorIndex = payload.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
        {
            throw new FormatException($"Workflow template identifier '{templateId}' is not a valid custom template id.");
        }

        var repositoryFullName = payload[..separatorIndex];
        var workflowFilePath = payload[(separatorIndex + 1)..];

        if (!repositoryFullName.Contains('/', StringComparison.Ordinal))
        {
            throw new FormatException($"Workflow template identifier '{templateId}' is not a valid custom template id.");
        }

        return (repositoryFullName, workflowFilePath);
    }
}
