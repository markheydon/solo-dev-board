using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SoloDevBoard.Application.Services.PmWorkflow;

/// <summary>Persists Cross-Repo PM Workflow preferences via <see cref="IPmSettingsStorage"/>.</summary>
public sealed class PmSettingsService(IPmSettingsStorage storage, ILogger<PmSettingsService> logger) : IPmSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    /// <inheritdoc/>
    public async Task<PmSettingsDto> GetSettingsAsync()
    {
        var storedJson = await storage.GetStoredJsonAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return PmSettingsDefaults.Create();
        }

        try
        {
            var model = JsonSerializer.Deserialize<PmSettingsStorageModel>(storedJson, JsonOptions);
            return Normalise(model);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignoring invalid PM settings JSON in browser storage.");
            return PmSettingsDefaults.Create();
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(PmSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalised = Normalise(settings);
        var json = JsonSerializer.Serialize(ToStorageModel(normalised), JsonOptions);
        await storage.SetStoredJsonAsync(json).ConfigureAwait(false);
    }

    private static PmSettingsDto Normalise(PmSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Normalise(ToStorageModel(settings));
    }

    private static PmSettingsDto Normalise(PmSettingsStorageModel? model)
    {
        if (model is null)
        {
            return PmSettingsDefaults.Create();
        }

        var planningBoardNodeId = string.IsNullOrWhiteSpace(model.PlanningBoardNodeId)
            ? null
            : model.PlanningBoardNodeId.Trim();

        var excludedRepositories = (model.ExcludedRepositories ?? [])
            .Where(static repository => !string.IsNullOrWhiteSpace(repository))
            .Select(static repository => repository.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static repository => repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PmSettingsDto(
            planningBoardNodeId,
            excludedRepositories,
            ResolvePositiveOrDefault(model.Capacity, PmSettingsDefaults.Capacity),
            ResolvePositiveOrDefault(model.StallDays, PmSettingsDefaults.StallDays),
            ResolvePositiveOrDefault(model.NeglectDays, PmSettingsDefaults.NeglectDays));
    }

    private static int ResolvePositiveOrDefault(int? value, int defaultValue) =>
        value is > 0 ? value.Value : defaultValue;

    private static PmSettingsStorageModel ToStorageModel(PmSettingsDto settings) => new()
    {
        PlanningBoardNodeId = settings.PlanningBoardNodeId,
        ExcludedRepositories = settings.ExcludedRepositories.ToList(),
        Capacity = settings.Capacity,
        StallDays = settings.StallDays,
        NeglectDays = settings.NeglectDays,
    };

    private sealed class PmSettingsStorageModel
    {
        [JsonPropertyName("planningBoardNodeId")]
        public string? PlanningBoardNodeId { get; set; }

        [JsonPropertyName("excludedRepositories")]
        public List<string>? ExcludedRepositories { get; set; }

        [JsonPropertyName("capacity")]
        public int? Capacity { get; set; }

        [JsonPropertyName("stallDays")]
        public int? StallDays { get; set; }

        [JsonPropertyName("neglectDays")]
        public int? NeglectDays { get; set; }
    }
}
