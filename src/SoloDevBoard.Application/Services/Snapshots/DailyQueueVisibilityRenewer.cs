namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>
/// Renews queue visibility while a hosted pass is stored as <c>running</c> for the generation that started it,
/// so a live pass is not redelivered to another worker.
/// </summary>
public sealed class DailyQueueVisibilityRenewer(
    AccountScanStatusTransitionService accountScanStatus,
    TimeProvider timeProvider)
{
    /// <summary>How long a renewed message stays hidden.</summary>
    public static readonly TimeSpan Visibility = TimeSpan.FromSeconds(30);

    private readonly AccountScanStatusTransitionService _accountScanStatus =
        accountScanStatus ?? throw new ArgumentNullException(nameof(accountScanStatus));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Delay between renewals (about every ten seconds).</summary>
    internal TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Runs until cancelled, until the stored scan stops being <c>running</c>, or until a newer generation has won.
    /// </summary>
    public async Task RunAsync(
        string operatorLogin,
        Func<TimeSpan, CancellationToken, Task> updateVisibilityAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updateVisibilityAsync);

        int? capturedGeneration = null;
        try
        {
            while (true)
            {
                await Task.Delay(Interval, _timeProvider, cancellationToken).ConfigureAwait(false);

                var document = await _accountScanStatus.GetDocumentAsync(operatorLogin, cancellationToken).ConfigureAwait(false);
                if (document is null)
                {
                    return;
                }

                var running = string.Equals(
                    document.Status,
                    AccountScanStatusDocumentConstants.StatusRunning,
                    StringComparison.Ordinal);

                if (capturedGeneration is null)
                {
                    if (!running)
                    {
                        continue;
                    }

                    capturedGeneration = document.ScanGeneration;
                }
                else if (!running || document.ScanGeneration != capturedGeneration)
                {
                    return;
                }

                await updateVisibilityAsync(Visibility, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The handler finished.
        }
    }
}
