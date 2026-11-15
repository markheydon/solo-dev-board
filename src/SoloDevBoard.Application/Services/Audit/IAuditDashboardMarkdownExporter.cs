namespace SoloDevBoard.Application.Services.Audit;

/// <summary>Generates Markdown exports for audit dashboard snapshots.</summary>
public interface IAuditDashboardMarkdownExporter
{
    /// <summary>Generates a Markdown summary for the supplied audit dashboard snapshot.</summary>
    /// <param name="request">The audit dashboard data to include in the export.</param>
    /// <returns>A Markdown document suitable for pasting into planning documents.</returns>
    string GenerateSummaryMarkdown(AuditDashboardMarkdownExportRequest request);
}
