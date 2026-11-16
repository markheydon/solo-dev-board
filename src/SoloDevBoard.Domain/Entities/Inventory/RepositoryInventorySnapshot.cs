namespace SoloDevBoard.Domain.Entities.Inventory;

/// <summary>Facts for one repository in one inventory run.</summary>
public sealed class RepositoryInventorySnapshot
{
    public long Id { get; set; }

    public Guid InventoryRunId { get; set; }

    public InventoryRun? InventoryRun { get; set; }

    public string OperatorKey { get; set; } = string.Empty;

    public string RepositoryFullName { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public bool IsFork { get; set; }

    public bool IsPrivate { get; set; }

    public bool InstallationVisible { get; set; } = true;

    public RepositoryScanState ScanState { get; set; }

    public string? FailureReason { get; set; }

    public FundingStatus? FundingStatus { get; set; }

    public string? ComparedPath { get; set; }

    public string? NotedExtraFundingPath { get; set; }

    public string? FundingPlatforms { get; set; }

    public bool OrganisationDefaultNoted { get; set; }

    public bool? HasDescription { get; set; }

    public bool? HasReadme { get; set; }

    public bool? CodeOfConductInRepository { get; set; }

    public bool? CodeOfConductOrganisationOnly { get; set; }

    public bool? ContributingInRepository { get; set; }

    public bool? ContributingOrganisationOnly { get; set; }

    public bool? LicenceInRepository { get; set; }

    public bool? LicenceOrganisationOnly { get; set; }

    public string? LicenceName { get; set; }

    public bool? SecurityPolicyInRepository { get; set; }

    public bool? SecurityPolicyOrganisationOnly { get; set; }

    public bool? IssueTemplatesInRepository { get; set; }

    public bool? IssueTemplatesOrganisationOnly { get; set; }

    public bool? PullRequestTemplateInRepository { get; set; }

    public bool? PullRequestTemplateOrganisationOnly { get; set; }
}