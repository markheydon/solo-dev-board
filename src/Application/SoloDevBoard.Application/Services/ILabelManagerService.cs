using SoloDevBoard.Domain.Entities;

namespace SoloDevBoard.Application.Services;

/// <summary>Provides label management operations.</summary>
public interface ILabelManagerService
{
    Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task SyncLabelsAsync(string sourceOwner, string sourceRepo, string targetOwner, string targetRepo, CancellationToken cancellationToken = default);
}
