namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents one label and the repositories from which it should be deleted.</summary>
/// <param name="LabelName">The label name to delete.</param>
/// <param name="RepositoryFullNames">The owner/repository full names where the label should be removed.</param>
public sealed record LabelBulkDeleteTargetDto(string LabelName, IReadOnlyList<string> RepositoryFullNames);
