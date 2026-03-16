namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Represents the outcome of hosted admission evaluation.
/// </summary>
/// <param name="IsAllowed">Indicates whether the request is allowed to proceed.</param>
/// <param name="Reason">Provides the decision reason for audit and logging.</param>
public readonly record struct HostedAdmissionDecision(bool IsAllowed, string Reason);