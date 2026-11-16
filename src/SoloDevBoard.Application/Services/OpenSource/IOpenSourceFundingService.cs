namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Read-only funding hygiene from stored inventory.</summary>
public interface IOpenSourceFundingService
{
    Task<OpenSourceFundingPageDto> GetFundingHygienePageAsync(CancellationToken cancellationToken);

    Task<FundingSummaryCountsDto> GetFundingSummaryAsync(CancellationToken cancellationToken);
}