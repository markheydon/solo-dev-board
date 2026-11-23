using Microsoft.Extensions.DependencyInjection;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <summary>Creates scopes whose <see cref="Application.Identity.ICurrentUserContext"/> uses captured credentials.</summary>
public sealed class SnapshotCollectionCredentialScopeFactory(IServiceScopeFactory serviceScopeFactory) : ISnapshotCollectionCredentialScopeFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    /// <inheritdoc/>
    public IServiceScope CreateScope(string ownerLogin, string accessToken)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var slot = scope.ServiceProvider.GetRequiredService<SnapshotCollectionCredentialSlot>();
        slot.Set(ownerLogin, accessToken);
        return scope;
    }
}
