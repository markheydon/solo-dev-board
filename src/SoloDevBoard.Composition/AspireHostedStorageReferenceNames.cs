namespace SoloDevBoard.Composition;

/// <summary>Connection names for AppHost <c>WithReference</c> storage resources (Aspire client registration).</summary>
public static class AspireHostedStorageReferenceNames
{
    /// <summary>Operator snapshot JSON blob container.</summary>
    public const string OperatorSnapshots = "operator-snapshots";

    /// <summary>Durable hosted sign-in records.</summary>
    public const string HostedSignIns = "hosted-sign-ins";

    /// <summary>Occasion markers and runner lease blobs.</summary>
    public const string CollectionSchedule = "collection-schedule";

    /// <summary>Storage queues on the operator snapshot account.</summary>
    public const string OperatorSnapshotQueues = "operator-snapshot-queues";
}
