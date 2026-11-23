namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Holds the host application-stopping token for snapshot walks.</summary>
/// <param name="Token">Cancellation token from the host lifetime.</param>
public sealed record OperatorSnapshotApplicationStopping(CancellationToken Token);
