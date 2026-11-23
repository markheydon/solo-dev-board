using Microsoft.Extensions.DependencyInjection;

namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Creates a DI scope whose user context uses captured credentials.</summary>
public interface ISnapshotCollectionCredentialScopeFactory
{
    /// <summary>Creates a scope for a background collection walk.</summary>
    IServiceScope CreateScope(string ownerLogin, string accessToken);
}
