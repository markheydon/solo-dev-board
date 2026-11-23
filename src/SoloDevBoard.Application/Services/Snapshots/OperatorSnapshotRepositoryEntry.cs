namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>One repository entry inside a successful snapshot.</summary>
public sealed class OperatorSnapshotRepositoryEntry
{
    /// <summary>GitHub repository id.</summary>
    public long GitHubId { get; set; }

    /// <summary>Owner login.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Repository name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Public or private visibility.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Whether the repository is archived.</summary>
    public bool Archived { get; set; }

    /// <summary>Whether the repository is a fork.</summary>
    public bool Fork { get; set; }

    /// <summary>GitHub html URL.</summary>
    public string HtmlUrl { get; set; } = string.Empty;
}
