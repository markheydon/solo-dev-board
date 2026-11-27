using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Lean posture store that never holds sign-in records.</summary>
public sealed class NullHostedSignInRecordStore : IHostedSignInRecordStore
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<HostedSignInRecord>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HostedSignInRecord>>([]);

    /// <inheritdoc/>
    public Task<HostedSignInRecordReadResult> GetAsync(string operatorLogin, CancellationToken cancellationToken = default)
        => Task.FromResult<HostedSignInRecordReadResult>(new HostedSignInRecordReadResult.NotFound());

    /// <inheritdoc/>
    public Task<HostedSignInRecordWriteResult> UpsertAsync(
        HostedSignInRecord record,
        string? expectedEtag,
        CancellationToken cancellationToken = default)
        => Task.FromResult<HostedSignInRecordWriteResult>(new HostedSignInRecordWriteResult.Succeeded(string.Empty));
}
