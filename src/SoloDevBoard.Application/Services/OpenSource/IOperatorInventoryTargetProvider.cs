namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Enumerates operator keys for unattended inventory workers.</summary>
public interface IOperatorInventoryTargetProvider
{
    Task<IReadOnlyList<OperatorInventoryTarget>> GetTargetsAsync(CancellationToken cancellationToken);
}

/// <summary>One unattended inventory partition.</summary>
public sealed record OperatorInventoryTarget(
    string OperatorKey,
    bool UsesInstallationToken,
    long? InstallationId);