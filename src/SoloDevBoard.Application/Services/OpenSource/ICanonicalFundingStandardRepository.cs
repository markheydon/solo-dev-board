using SoloDevBoard.Domain.Entities.Inventory;

namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Canonical funding standard persistence.</summary>
public interface ICanonicalFundingStandardRepository
{
    Task<CanonicalFundingStandard?> GetByOperatorKeyAsync(string operatorKey, CancellationToken cancellationToken);

    Task UpsertAsync(CanonicalFundingStandard standard, CancellationToken cancellationToken);
}