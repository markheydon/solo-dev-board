namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Canonical funding standard storage.</summary>
public interface ICanonicalFundingStandardService
{
    Task<CanonicalFundingStandardDto?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<CanonicalFundingStandardDto> SaveAsync(SaveCanonicalFundingStandardRequest request, CancellationToken cancellationToken);
}