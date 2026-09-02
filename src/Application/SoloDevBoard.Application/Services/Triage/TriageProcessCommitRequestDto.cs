namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Captures optional process-metadata writes for a single triage commit.</summary>
/// <param name="LabelName">The quick label to apply, or <see langword="null"/> or whitespace to skip labelling.</param>
/// <param name="MilestoneNumber">The milestone number to assign, or <see langword="null"/> to clear an existing milestone.</param>
/// <param name="MilestoneTitle">The milestone title associated with <paramref name="MilestoneNumber"/>.</param>
/// <param name="ProjectBoardId">The project-board node identifier, or <see langword="null"/> when project placement is not requested.</param>
/// <param name="ProjectBoardTitle">The project-board display title.</param>
/// <param name="StatusFieldId">The project status-field node identifier.</param>
/// <param name="StatusOptionId">The selected project status-option node identifier.</param>
/// <param name="StatusOptionName">The selected project status-option display name.</param>
public sealed record TriageProcessCommitRequestDto(
    string? LabelName,
    int? MilestoneNumber,
    string? MilestoneTitle,
    string? ProjectBoardId,
    string? ProjectBoardTitle,
    string? StatusFieldId,
    string? StatusOptionId,
    string? StatusOptionName);
