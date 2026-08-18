namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Result of loading the cross-repository PM work-item catalogue.</summary>
/// <param name="Items">Open issues and pull requests from included repositories.</param>
/// <param name="Failures">Per-repository failures that did not prevent other repositories from loading.</param>
public sealed record PmWorkItemCatalogueResultDto(
    IReadOnlyList<PmWorkItemDto> Items,
    IReadOnlyList<PmRepositoryCatalogueFailureDto> Failures);
