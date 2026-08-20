namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Classifies a label consistency warning against the canonical taxonomy.</summary>
public enum LabelConsistencyWarningKind
{
    /// <summary>The taxonomy label is not present in the repository.</summary>
    Missing = 0,

    /// <summary>The label exists but its colour or description differs from the taxonomy.</summary>
    Divergent = 1,
}
