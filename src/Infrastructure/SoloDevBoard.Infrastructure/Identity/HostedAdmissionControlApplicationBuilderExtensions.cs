using Microsoft.AspNetCore.Builder;

namespace SoloDevBoard.Infrastructure.Identity;

/// <summary>
/// Extension methods for hosted admission control middleware.
/// </summary>
public static class HostedAdmissionControlApplicationBuilderExtensions
{
    /// <summary>
    /// Adds hosted admission control middleware to the request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseHostedAdmissionControl(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<HostedAdmissionControlMiddleware>();
    }
}