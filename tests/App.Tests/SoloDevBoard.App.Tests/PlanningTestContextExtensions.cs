using Bunit;
using Microsoft.AspNetCore.Components;
using SoloDevBoard.App.Components.Features.Planning;

namespace SoloDevBoard.App.Tests;

/// <summary>Shared bUnit helpers for Planning layout-backed pages.</summary>
internal static class PlanningTestContextExtensions
{
    /// <summary>Renders a Planning page inside <see cref="PlanningLayout"/>.</summary>
    /// <typeparam name="TPage">The routable page component type.</typeparam>
    /// <param name="context">The bUnit test context.</param>
    /// <returns>The rendered layout fragment containing the page.</returns>
    internal static IRenderedComponent<PlanningLayout> RenderPlanningPage<TPage>(this BunitContext context)
        where TPage : IComponent
        => context.Render<PlanningLayout>(parameters =>
            parameters.Add(
                layout => layout.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TPage>(0);
                    builder.CloseComponent();
                })));
}
