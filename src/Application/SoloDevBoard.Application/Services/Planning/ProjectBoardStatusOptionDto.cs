namespace SoloDevBoard.Application.Services.Planning;

/// <summary>DTO for a Status option discovered on a Projects v2 board.</summary>
/// <param name="OptionId">The status option node identifier.</param>
/// <param name="Name">The status option display name.</param>
public sealed record ProjectBoardStatusOptionDto(string OptionId, string Name);
