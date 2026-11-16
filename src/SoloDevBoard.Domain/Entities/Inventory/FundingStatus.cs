namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>Funding file comparison outcome for a checked repository.</summary>
public enum FundingStatus
{
    Missing,
    MatchesStandard,
    Differs,
    NotCompared,
}