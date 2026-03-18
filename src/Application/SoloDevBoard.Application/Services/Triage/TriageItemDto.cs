namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Represents a triage queue item at the Application→App boundary.</summary>
/// <param name="ItemType">The triage item type.</param>
/// <param name="Id">The GitHub node identifier represented as an integer ID.</param>
/// <param name="Number">The repository-scoped number of the issue or pull request.</param>
/// <param name="RepositoryFullName">The full repository name in <c>owner/repo</c> format.</param>
/// <param name="Title">The issue or pull request title.</param>
/// <param name="HtmlUrl">The browser URL for the item.</param>
/// <param name="Body">The body content for the item.</param>
/// <param name="State">The current GitHub state value.</param>
/// <param name="AuthorLogin">The login of the item author.</param>
/// <param name="Labels">The current labels on the item.</param>
/// <param name="MilestoneNumber">The assigned milestone number, when present.</param>
/// <param name="MilestoneTitle">The assigned milestone title, or an empty string when no milestone is assigned.</param>
/// <param name="CreatedAt">The UTC timestamp when the item was created.</param>
/// <param name="UpdatedAt">The UTC timestamp when the item was last updated.</param>
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
