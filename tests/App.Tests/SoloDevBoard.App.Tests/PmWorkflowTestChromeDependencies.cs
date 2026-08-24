using NSubstitute;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.App.Tests;

/// <summary>Shared substitutes for Planning chrome component tests.</summary>
internal static class PmWorkflowTestChromeDependencies
{
    /// <summary>Creates a board compatibility service that returns an empty report by default.</summary>
    /// <param name="reportFactory">Optional factory that builds the report for a board identifier.</param>
    /// <returns>The substitute service.</returns>
    public static IPlanningBoardCompatibilityService CreateBoardCompatibilityService(
        Func<string, PlanningBoardCompatibilityReportDto>? reportFactory = null)
    {
        var service = Substitute.For<IPlanningBoardCompatibilityService>();
        service.GetReportAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var boardId = call.Arg<string>();
                return reportFactory?.Invoke(boardId) ?? new PlanningBoardCompatibilityReportDto(boardId, []);
            });

        return service;
    }
}
