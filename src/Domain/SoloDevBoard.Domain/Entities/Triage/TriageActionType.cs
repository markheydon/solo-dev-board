namespace SoloDevBoard.Domain.Entities.Triage;

/// <summary>Defines the supported triage actions recorded in a session.</summary>
public enum TriageActionType
{
    /// <summary>Represents a label assignment action.</summary>
    LabelApplied = 0,

    /// <summary>Represents a milestone assignment action.</summary>
    MilestoneAssigned = 1,

    /// <summary>Represents assigning an item to a project board.</summary>
    ProjectBoardAssigned = 2,

    /// <summary>Represents duplicate closure action.</summary>
    ClosedAsDuplicate = 3,

    /// <summary>Represents skipping an item for later review.</summary>
    Skipped = 4,
}
