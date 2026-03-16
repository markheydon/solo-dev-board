using System.Security.Claims;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Evaluates whether an authenticated hosted identity is authorised for application admission.
/// </summary>
public interface IHostedAdmissionEvaluator
{
    /// <summary>
    /// Evaluates hosted admission for the supplied principal.
    /// </summary>
    /// <param name="principal">The principal from the current hosted request.</param>
    /// <returns>A decision describing whether admission is allowed.</returns>
    HostedAdmissionDecision Evaluate(ClaimsPrincipal principal);
}