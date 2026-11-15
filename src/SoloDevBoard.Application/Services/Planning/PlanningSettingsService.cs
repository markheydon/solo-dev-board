using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SoloDevBoard.Application.Services.Planning;

/// <summary>Persists Cross-Repo Planning preferences via <see cref="IPlanningSettingsStorage"/>.</summary>
public sealed class PlanningSettingsService(IPlanningSettingsStorage storage, ILogger<PlanningSettingsService> logger) : IPlanningSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    /// <inheritdoc/>
    public async Task<PlanningSettingsDto> GetSettingsAsync()
    {
        var storedJson = await storage.GetStoredJsonAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return PlanningSettingsDefaults.Create();
        }

        try
        {
            var model = JsonSerializer.Deserialize<PlanningSettingsStorageModel>(storedJson, JsonOptions);
            return Normalise(model);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignoring invalid PM settings JSON in browser storage.");
            return PlanningSettingsDefaults.Create();
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(PlanningSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalised = Normalise(settings);
        var json = JsonSerializer.Serialize(ToStorageModel(normalised), JsonOptions);
        await storage.SetStoredJsonAsync(json).ConfigureAwait(false);
    }

    private static PlanningSettingsDto Normalise(PlanningSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Normalise(ToStorageModel(settings));
    }

    private static PlanningSettingsDto Normalise(PlanningSettingsStorageModel? model)
    {
        if (model is null)
        {
            return PlanningSettingsDefaults.Create();
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

        return new PlanningSettingsDto(
            planningBoardNodeId,
            excludedRepositories,
            ResolvePositiveOrDefault(model.Capacity, PlanningSettingsDefaults.Capacity),
            ResolvePositiveOrDefault(model.StallDays, PlanningSettingsDefaults.StallDays),
            ResolvePositiveOrDefault(model.NeglectDays, PlanningSettingsDefaults.NeglectDays),
            model.LimitRecommendationsToPlanningBoard ?? false);
    }

    private static int ResolvePositiveOrDefault(int? value, int defaultValue) =>
        value is > 0 ? value.Value : defaultValue;

    private static PlanningSettingsStorageModel ToStorageModel(PlanningSettingsDto settings) => new()
    {
        PlanningBoardNodeId = settings.PlanningBoardNodeId,
        ExcludedRepositories = settings.ExcludedRepositories.ToList(),
        Capacity = settings.Capacity,
        StallDays = settings.StallDays,
        NeglectDays = settings.NeglectDays,
        LimitRecommendationsToPlanningBoard = settings.LimitRecommendationsToPlanningBoard,
    };

    private sealed class PlanningSettingsStorageModel
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

        [JsonPropertyName("limitRecommendationsToPlanningBoard")]
        public bool? LimitRecommendationsToPlanningBoard { get; set; }
    }
}
