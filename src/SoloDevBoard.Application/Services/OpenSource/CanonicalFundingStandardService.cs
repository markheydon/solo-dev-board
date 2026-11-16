namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Persists the operator canonical funding standard.</summary>
public sealed class CanonicalFundingStandardService(
    IOperatorKeyResolver operatorKeyResolver,
    ICanonicalFundingStandardRepository repository) : ICanonicalFundingStandardService
{
    private readonly IOperatorKeyResolver _operatorKeyResolver = operatorKeyResolver ?? throw new ArgumentNullException(nameof(operatorKeyResolver));
    private readonly ICanonicalFundingStandardRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<CanonicalFundingStandardDto?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var operatorKey = await _operatorKeyResolver.ResolveCurrentOperatorKeyAsync(cancellationToken).ConfigureAwait(false);
        var standard = await _repository.GetByOperatorKeyAsync(operatorKey, cancellationToken).ConfigureAwait(false);
        return standard is null
            ? null
            : new CanonicalFundingStandardDto(standard.Content, standard.SavedAtUtc, standard.ContentHash);
    }

    public async Task<CanonicalFundingStandardDto> SaveAsync(SaveCanonicalFundingStandardRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Canonical funding standard cannot be empty.", nameof(request));
        }

        var operatorKey = await _operatorKeyResolver.ResolveCurrentOperatorKeyAsync(cancellationToken).ConfigureAwait(false);
        var savedAt = DateTimeOffset.UtcNow;
        var hash = FundingFileComparer.ComputeContentHash(request.Content);
        var entity = new Domain.Entities.Inventory.CanonicalFundingStandard
        {
            OperatorKey = operatorKey,
            Content = request.Content,
            SavedAtUtc = savedAt,
            ContentHash = hash,
        };

        await _repository.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);
        return new CanonicalFundingStandardDto(entity.Content, entity.SavedAtUtc, entity.ContentHash);
    }
}