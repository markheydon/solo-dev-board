namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents the outcome of remapping one label onto another within a repository.</summary>
/// <param name="RepositoryName">The short repository name where remap ran.</param>
/// <param name="SourceLabelName">The source label name that was removed from items.</param>
/// <param name="DestinationLabelName">The destination label name applied to those items.</param>
/// <param name="SucceededItemCount">The number of issues and pull requests that were retagged successfully.</param>
/// <param name="FailedItemCount">The number of issues and pull requests that could not be retagged.</param>
/// <param name="SourceDeleted">Whether the source label was deleted after every retag succeeded.</param>
/// <param name="DestinationCreated">Whether the destination label was created because it did not already exist.</param>
/// <param name="Errors">Per-item errors encountered during retagging.</param>
public sealed record LabelRemapResultDto(
    string RepositoryName,
    string SourceLabelName,
    string DestinationLabelName,
    int SucceededItemCount,
    int FailedItemCount,
    bool SourceDeleted,
    bool DestinationCreated,
    IReadOnlyList<LabelRemapItemErrorDto> Errors)
{
    /// <summary>Gets a value indicating whether any retag errors were recorded.</summary>
    public bool HasErrors => Errors.Count > 0;
}
