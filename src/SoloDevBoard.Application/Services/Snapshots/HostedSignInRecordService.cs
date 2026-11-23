namespace SoloDevBoard.Application.Services.Snapshots;

/// <inheritdoc/>
public sealed class HostedSignInRecordService(
    IOperatorSignInRecordStore store,
    ISignInRecordRefreshTokenProtector protector,
    TimeProvider timeProvider) : IHostedSignInRecordService
{
    private readonly IOperatorSignInRecordStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly ISignInRecordRefreshTokenProtector _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc/>
    public async Task RecordCompletedSignInAsync(
        string ownerLogin,
        IReadOnlyList<string> organisationLogins,
        string refreshToken,
        DateTimeOffset? refreshTokenExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var sanitised = OperatorLoginSanitiser.Sanitise(ownerLogin);
        if (sanitised is null || string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var record = new OperatorSignInRecord
        {
            OperatorLogin = sanitised,
            CompletedAt = _timeProvider.GetUtcNow(),
            OrganisationLogins = organisationLogins.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ProtectedRefreshToken = _protector.Protect(refreshToken),
            RefreshTokenExpiresAt = refreshTokenExpiresAtUtc,
            ScheduleHold = false,
        };

        await _store.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
    }
}
