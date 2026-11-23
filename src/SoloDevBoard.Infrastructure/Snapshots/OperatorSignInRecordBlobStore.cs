using Azure;
using Microsoft.Extensions.Logging;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class OperatorSignInRecordBlobStore(
    IOperatorSnapshotBlobContainer container,
    ILogger<OperatorSignInRecordBlobStore> logger) : IOperatorSignInRecordStore
{
    private const string Prefix = "schedule/sign-ins/";

    private readonly IOperatorSnapshotBlobContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly ILogger<OperatorSignInRecordBlobStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task UpsertAsync(OperatorSignInRecord record, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobName(record.OperatorLogin));
        var payload = ScheduleJson.SerialiseSignInRecord(record);
        var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        var conditions = exists && record.BlobEtag is not null
            ? new OperatorSnapshotBlobUploadConditions(record.BlobEtag, IfNoneMatchAll: false)
            : exists
                ? new OperatorSnapshotBlobUploadConditions(await blob.GetEtagAsync(cancellationToken).ConfigureAwait(false), IfNoneMatchAll: false)
                : new OperatorSnapshotBlobUploadConditions(null, IfNoneMatchAll: true);

        try
        {
            await blob.UploadAsync(payload, conditions, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            _logger.LogWarning("Sign-in record upsert conflict for {Login}.", record.OperatorLogin);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OperatorSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var names = await _container.ListBlobNamesAsync(Prefix, cancellationToken).ConfigureAwait(false);
        var records = new List<OperatorSignInRecord>(names.Count);

        foreach (var name in names)
        {
            if (!name.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            var blob = _container.GetBlobClient(name);
            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
            var record = ScheduleJson.DeserialiseSignInRecord(download.Content);
            if (record is null)
            {
                continue;
            }

            record.BlobEtag = download.Etag;
            records.Add(record);
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task<OperatorSignInRecord?> GetByLoginAsync(string sanitisedLogin, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobName(sanitisedLogin));
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
        var record = ScheduleJson.DeserialiseSignInRecord(download.Content);
        if (record is null)
        {
            return null;
        }

        record.BlobEtag = download.Etag;
        return record;
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateScheduleHoldAsync(
        string sanitisedLogin,
        bool scheduleHold,
        string ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        var record = await GetByLoginAsync(sanitisedLogin, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        record.ScheduleHold = scheduleHold;
        record.BlobEtag = ifMatchEtag;
        try
        {
            await UpsertAsync(record, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateProtectedRefreshTokenAsync(
        string sanitisedLogin,
        string protectedRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        var record = await GetByLoginAsync(sanitisedLogin, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        record.ProtectedRefreshToken = protectedRefreshToken;
        record.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        record.BlobEtag = ifMatchEtag;

        try
        {
            await UpsertAsync(record, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    private static string BlobName(string sanitisedLogin) => $"{Prefix}{sanitisedLogin}.json";
}
