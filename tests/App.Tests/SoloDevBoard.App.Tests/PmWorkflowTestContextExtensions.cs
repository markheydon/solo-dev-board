using Bunit;
using Microsoft.AspNetCore.Components;
using SoloDevBoard.App.Components.Features.PmWorkflow;

namespace SoloDevBoard.App.Tests;

/// <summary>Shared bUnit helpers for PM Workflow layout-backed pages.</summary>
internal static class PmWorkflowTestContextExtensions
{
    /// <summary>Renders a PM Workflow page inside <see cref="PmWorkflowLayout"/>.</summary>
    /// <typeparam name="TPage">The routable page component type.</typeparam>
    /// <param name="context">The bUnit test context.</param>
    /// <returns>The rendered layout fragment containing the page.</returns>
    internal static IRenderedComponent<PmWorkflowLayout> RenderPmWorkflowPage<TPage>(this BunitContext context)
        where TPage : IComponent
        => context.Render<PmWorkflowLayout>(parameters =>
            parameters.Add(
                layout => layout.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TPage>(0);
                    builder.CloseComponent();
                })));
}
