namespace SoloDevBoard.Application.Services.OpenSource;

/// <summary>Inventory feature configuration.</summary>
public sealed class InventoryOptions
{
    public const string SectionName = "Inventory";

    public string ScheduledCron { get; set; } = "0 0 2 * * *";

    public bool HostedInventoryConfigured { get; set; }

    public static readonly TimeSpan StaleAfter = TimeSpan.FromHours(36);
}