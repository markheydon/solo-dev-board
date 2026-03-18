namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a triage queue item at the Application→App boundary.</summary>
public sealed record TriageItemDto(
    TriageItemTypeDto ItemType,
    int Id,
    int Number,
    string RepositoryFullName,
    string Title,
    string HtmlUrl,
    string Body,
    string State,
    string AuthorLogin,
    IReadOnlyList<string> Labels,
    int? MilestoneNumber,
    string MilestoneTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
