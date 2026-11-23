using Microsoft.Extensions.Options;
using SoloDevBoard.Application.Authentication;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Detects hosted schedule posture from configuration.</summary>
public sealed class ScheduledCollectionEnvironment(
    IOptions<GitHubAuthOptions> authOptions,
    IOptions<OperatorSnapshotDurabilityOptions> durabilityOptions) : IScheduledCollectionEnvironment
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly OperatorSnapshotDurabilityOptions _durabilityOptions =
        durabilityOptions?.Value ?? throw new ArgumentNullException(nameof(durabilityOptions));

    /// <inheritdoc/>
    public bool IsHostedScheduleEnabled =>
        _authOptions.HostedSignInEnabled
        && _durabilityOptions.DurabilityKind == OperatorSnapshotDurabilityKind.HostedBlob;
}
