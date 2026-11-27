using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Lean posture: no account scan status blobs.</summary>
public sealed class NullAccountScanStatusStore : IAccountScanStatusStore
{
    /// <inheritdoc/>
    public Task<AccountScanStatusReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
        => Task.FromResult<AccountScanStatusReadResult>(new AccountScanStatusReadResult.NotFound());

    /// <inheritdoc/>
    public Task<AccountScanStatusWriteResult> TryCreateAsync(
        AccountScanStatusDocument document,
        CancellationToken cancellationToken = default)
        => Task.FromResult<AccountScanStatusWriteResult>(new AccountScanStatusWriteResult.ConcurrencyConflict());

    /// <inheritdoc/>
    public Task<AccountScanStatusWriteResult> TryReplaceAsync(
        AccountScanStatusDocument document,
        string expectedEtag,
        CancellationToken cancellationToken = default)
        => Task.FromResult<AccountScanStatusWriteResult>(new AccountScanStatusWriteResult.ConcurrencyConflict());
}
