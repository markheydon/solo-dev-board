using MudBlazor;
using SoloDevBoard.Application.Services.Snapshots;
using SoloDevBoard.Web.Components.Features.Repositories;

namespace SoloDevBoard.Web.Tests;

/// <summary>Copy tests for operator snapshot status on the Repositories page.</summary>
public sealed class OperatorSnapshotStatusCopyTests
{
    private static readonly DateTimeOffset SampleUtc = new(2026, 11, 23, 16, 14, 55, TimeSpan.Zero);

    [Fact]
    public void BuildSnapshotSuffix_NeverRun_StatesNoSnapshotSaved()
    {
        var status = new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.NeverRun,
            DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            SchedulePosture = OperatorSnapshotSchedulePosture.Lean,
            NoScheduleReason = OperatorSnapshotNoScheduleReason.Lean,
        };

        Assert.Equal(
            "Nothing collects on a schedule. The snapshot is kept on this machine rather than promised on another machine. No snapshot exists.",
            OperatorSnapshotStatusCopy.BuildSnapshotSuffix(status, statusUnavailable: false));
    }

    [Fact]
    public void BuildSnapshotSuffix_Succeeded_UsesUnitedKingdomDateFormat()
    {
        var status = new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.Succeeded,
            LastSuccessfulCollectionAt = SampleUtc,
            SnapshotRepositoryCount = 42,
            DurabilityKind = OperatorSnapshotDurabilityKind.LocalMachine,
            SchedulePosture = OperatorSnapshotSchedulePosture.Lean,
            NoScheduleReason = OperatorSnapshotNoScheduleReason.Lean,
        };

        var suffix = OperatorSnapshotStatusCopy.BuildSnapshotSuffix(status, statusUnavailable: false);

        Assert.Contains("23/11/2026", suffix);
        Assert.Contains("42 repositories", suffix);
    }

    [Fact]
    public void ResolveCombinedSeverity_FailedRun_UsesError()
    {
        var status = new OperatorSnapshotStatusDto { Kind = OperatorSnapshotStatusKind.Failed };

        Assert.Equal(
            Severity.Error,
            OperatorSnapshotStatusCopy.ResolveCombinedSeverity(status, statusUnavailable: false, Severity.Success));
    }

    [Fact]
    public void BuildSnapshotSuffix_SucceededWithZero_CountIsZero()
    {
        var status = new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.Succeeded,
            LastSuccessfulCollectionAt = SampleUtc,
            SnapshotRepositoryCount = 0,
            DurabilityKind = OperatorSnapshotDurabilityKind.HostedBlob,
            SchedulePosture = OperatorSnapshotSchedulePosture.Hosted,
            NoScheduleReason = OperatorSnapshotNoScheduleReason.None,
            NextOccasionAtUtc = new DateTimeOffset(2026, 11, 24, 5, 0, 0, TimeSpan.Zero),
        };

        var suffix = OperatorSnapshotStatusCopy.BuildSnapshotSuffix(status, statusUnavailable: false);

        Assert.Contains("0 repositories", suffix);
        Assert.Contains("Snapshot saved at", suffix);
    }

    [Theory]
    [InlineData(OperatorSnapshotDurabilityKind.LocalMachine)]
    [InlineData(OperatorSnapshotDurabilityKind.EphemeralContainerDisk)]
    [InlineData(OperatorSnapshotDurabilityKind.HostedBlob)]
    public void BuildSnapshotSuffix_NeverRun_DoesNotMentionDurability(OperatorSnapshotDurabilityKind durability)
    {
        var status = new OperatorSnapshotStatusDto
        {
            Kind = OperatorSnapshotStatusKind.NeverRun,
            DurabilityKind = durability,
            SchedulePosture = OperatorSnapshotSchedulePosture.Lean,
            NoScheduleReason = OperatorSnapshotNoScheduleReason.Lean,
        };

        var suffix = OperatorSnapshotStatusCopy.BuildSnapshotSuffix(status, statusUnavailable: false);

        Assert.Contains("No snapshot exists.", suffix);
        Assert.DoesNotContain("blob", suffix, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PAT-only", suffix, StringComparison.OrdinalIgnoreCase);
    }
}
