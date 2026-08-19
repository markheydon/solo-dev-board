namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Ranked Daily Focus recommendations plus any partial catalogue failures.</summary>
/// <param name="Recommendations">Up to three ranked unblocked work items.</param>
/// <param name="Failures">
/// Per-repository catalogue failures. Ranking still proceeds when this list is non-empty and
/// the remaining items produced <see cref="Recommendations"/>.
/// </param>
public sealed record DailyFocusRecommendationResultDto(
    IReadOnlyList<DailyFocusRecommendationDto> Recommendations,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> Failures);
