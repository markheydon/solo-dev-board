using Microsoft.AspNetCore.Builder;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Composition;

/// <summary>Extension methods for configuring the SoloDevBoard HTTP request pipeline.</summary>
public static class SoloDevBoardApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Infrastructure middleware required by SoloDevBoard (for example hosted admission control).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseSoloDevBoardInfrastructure(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseHostedAdmissionControl();
    }
}
