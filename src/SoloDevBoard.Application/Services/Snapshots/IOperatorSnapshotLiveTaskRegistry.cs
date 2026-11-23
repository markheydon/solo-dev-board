namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Reports whether this process still owns an in-flight collection for a login.</summary>
public interface IOperatorSnapshotLiveTaskRegistry
{
    /// <summary>Returns whether a live walk exists for <paramref name="sanitisedLogin"/>.</summary>
    bool HasLiveTask(string sanitisedLogin);

    /// <summary>Gets the in-memory recorded count while a live walk exists.</summary>
    bool TryGetLiveProgress(string sanitisedLogin, out DateTimeOffset startedAt, out int repositoriesRecorded);
}
