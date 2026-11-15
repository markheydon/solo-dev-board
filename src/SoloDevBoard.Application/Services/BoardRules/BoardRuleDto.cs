namespace SoloDevBoard.Application.Services.BoardRules;

/// <summary>Represents a board automation rule in the Application layer.</summary>
/// <param name="Id">The rule identifier.</param>
/// <param name="Name">The display name of the rule.</param>
/// <param name="Trigger">The trigger condition expression for the rule.</param>
/// <param name="Action">The action expression performed when the trigger matches.</param>
/// <param name="IsEnabled">A value indicating whether the rule is enabled.</param>
public sealed record BoardRuleDto(
    int Id,
    string Name,
    string Trigger,
    string Action,
    bool IsEnabled);
