namespace SoloDevBoard.App.Components.Features.ActionsTemplates;

/// <summary>Persists the last-used custom Actions template source repository.</summary>
public interface IActionsTemplateSourceStorage
{
    /// <summary>Retrieves the last-used custom template source in owner/repository format.</summary>
    /// <returns>The stored source repository, or <see langword="null" /> when none is stored.</returns>
    Task<string?> GetLastUsedSourceAsync();

    /// <summary>Persists the last-used custom template source in owner/repository format.</summary>
    /// <param name="repositoryFullName">The source repository to remember.</param>
    Task SetLastUsedSourceAsync(string repositoryFullName);
}
