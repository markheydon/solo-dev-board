namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>Operator canonical funding file standard.</summary>
public sealed class CanonicalFundingStandard
{
    public string OperatorKey { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SavedAtUtc { get; set; }

    public string ContentHash { get; set; } = string.Empty;
}