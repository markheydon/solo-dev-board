namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Resolves the inventory partition key for the current operator.</summary>
public interface IOperatorKeyResolver
{
    Task<string> ResolveCurrentOperatorKeyAsync(CancellationToken cancellationToken);
}