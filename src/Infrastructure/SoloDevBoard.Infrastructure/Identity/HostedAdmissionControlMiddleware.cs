using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Enforces hosted admission control for authenticated requests.
/// </summary>
public sealed class HostedAdmissionControlMiddleware(
    RequestDelegate next,
    IOptions<HostedAdmissionControlOptions> admissionOptions,
    ILogger<HostedAdmissionControlMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly HostedAdmissionControlOptions _admissionOptions = admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));
    private readonly ILogger<HostedAdmissionControlMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Applies hosted admission checks for the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="admissionEvaluator">The per-request admission evaluator.</param>
    public async Task InvokeAsync(HttpContext context, IHostedAdmissionEvaluator admissionEvaluator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(admissionEvaluator);

        if (!_admissionOptions.Enabled || IsBypassedPath(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return;
        }

        var decision = admissionEvaluator.Evaluate(context.User);
        if (decision.IsAllowed)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        _logger.LogWarning(
            "Hosted admission denied for user '{User}' on path '{Path}'. Reason: {Reason}",
            context.User.Identity?.Name ?? "unknown",
            context.Request.Path,
            decision.Reason);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Hosted access denied",
            Detail = "Your hosted identity is not authorised for this deployment.",
        }).ConfigureAwait(false);
    }

    private static bool IsBypassedPath(PathString path)
    {
        if (path.StartsWithSegments("/auth"))
        {
            return true;
        }

        if (path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content"))
        {
            return true;
        }

        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains('.', StringComparison.Ordinal);
    }
}