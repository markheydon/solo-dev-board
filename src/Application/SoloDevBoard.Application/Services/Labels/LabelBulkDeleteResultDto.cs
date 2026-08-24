namespace SoloDevBoard.Application.Services.Labels;

/// <summary>Represents the outcome of a bulk label delete operation.</summary>
/// <param name="DeletedCount">The number of label-repository deletions that succeeded.</param>
/// <param name="SkippedCount">The number of label-repository pairs skipped because the label was not present.</param>
/// <param name="Errors">Per-label and per-repository errors encountered during the batch.</param>
public sealed record LabelBulkDeleteResultDto(
    int DeletedCount,
    int SkippedCount,
    IReadOnlyList<LabelBulkDeleteErrorDto> Errors)
{
    /// <summary>Gets a value indicating whether any delete errors were recorded.</summary>
    public bool HasErrors => Errors.Count > 0;
}
