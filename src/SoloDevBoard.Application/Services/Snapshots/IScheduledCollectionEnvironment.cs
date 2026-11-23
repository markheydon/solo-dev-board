namespace SoloDevBoard.Application.Services.Snapshots;

/// <summary>Whether hosted scheduled snapshot collection is active in this host.</summary>
public interface IScheduledCollectionEnvironment
{
    /// <summary>True when fill, queue, and sign-in blob writes are allowed.</summary>
    bool IsHostedScheduleEnabled { get; }
}
