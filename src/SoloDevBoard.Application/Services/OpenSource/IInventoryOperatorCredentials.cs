namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Credentials for one inventory walk in the Functions worker.</summary>
public interface IInventoryOperatorCredentials
{
    string OperatorKey { get; }

    bool UsesInstallationToken { get; }

    long? InstallationId { get; }

    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);

    void Configure(OperatorInventoryTarget target);
}