namespace SoloDevBoard.Application.Services.Triage;

/// <summary>Defines supported triage action types for Application-layer DTOs.</summary>
public enum TriageActionTypeDto
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
