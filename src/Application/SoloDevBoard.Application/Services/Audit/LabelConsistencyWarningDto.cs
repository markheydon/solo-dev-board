namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Represents a label that diverges from the SoloDevBoard canonical taxonomy.</summary>
/// <param name="RepositoryFullName">The fully-qualified repository name in owner/name format.</param>
/// <param name="LabelName">The taxonomy label name that is missing or divergent.</param>
/// <param name="Kind">Whether the label is missing or present with different values.</param>
/// <param name="Detail">A UK English description of the divergence.</param>
public sealed record LabelConsistencyWarningDto(
    string RepositoryFullName,
    string LabelName,
    LabelConsistencyWarningKind Kind,
    string Detail);
