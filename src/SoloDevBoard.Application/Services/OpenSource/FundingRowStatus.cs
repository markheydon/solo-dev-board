namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Funding hygiene status shown on the Open Source page.</summary>
public enum FundingRowStatus
{
    Missing,
    Matches,
    Differs,
    NotCompared,
}