using Microsoft.Extensions.Logging.Abstractions;
using SoloDevBoard.Application.Services.PmWorkflow;

namespace SoloDevBoard.Application.Tests;

/// <summary>Tests for <see cref="PmSettingsService"/>.</summary>
public sealed class PmSettingsServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_WhenStorageEmpty_ReturnsDefaults()
    {
        var storage = new FakePmSettingsStorage();
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal(PmSettingsDefaults.Create(), settings);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenValidJson_ReturnsNormalisedSettings()
    {
        var storage = new FakePmSettingsStorage
        {
            StoredJson = """
                {
                  "planningBoardNodeId": "PVT_board123",
                  "excludedRepositories": ["owner/dotfiles", "owner/personal-site"],
                  "capacity": 10,
                  "stallDays": 5,
                  "neglectDays": 21
                }
                """,
        };
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal("PVT_board123", settings.PlanningBoardNodeId);
        Assert.Equal(["owner/dotfiles", "owner/personal-site"], settings.ExcludedRepositories);
        Assert.Equal(10, settings.Capacity);
        Assert.Equal(5, settings.StallDays);
        Assert.Equal(21, settings.NeglectDays);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenCorruptJson_ReturnsDefaults()
    {
        var storage = new FakePmSettingsStorage
        {
            StoredJson = "{not-valid-json",
        };
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal(PmSettingsDefaults.Create(), settings);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenPartialJson_AppliesDefaultsForMissingFields()
    {
        var storage = new FakePmSettingsStorage
        {
            StoredJson = """
                {
                  "planningBoardNodeId": "PVT_board123"
                }
                """,
        };
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal("PVT_board123", settings.PlanningBoardNodeId);
        Assert.Empty(settings.ExcludedRepositories);
        Assert.Equal(PmSettingsDefaults.Capacity, settings.Capacity);
        Assert.Equal(PmSettingsDefaults.StallDays, settings.StallDays);
        Assert.Equal(PmSettingsDefaults.NeglectDays, settings.NeglectDays);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenInvalidThresholds_UsesDefaults()
    {
        var storage = new FakePmSettingsStorage
        {
            StoredJson = """
                {
                  "capacity": 0,
                  "stallDays": -1,
                  "neglectDays": null
                }
                """,
        };
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal(PmSettingsDefaults.Capacity, settings.Capacity);
        Assert.Equal(PmSettingsDefaults.StallDays, settings.StallDays);
        Assert.Equal(PmSettingsDefaults.NeglectDays, settings.NeglectDays);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenExcludedRepositoriesContainBlanks_FiltersAndDedupes()
    {
        var storage = new FakePmSettingsStorage
        {
            StoredJson = """
                {
                  "excludedRepositories": ["owner/dotfiles", "", "OWNER/dotfiles", " owner/personal-site "]
                }
                """,
        };
        var service = CreateService(storage);

        var settings = await service.GetSettingsAsync();

        Assert.Equal(["owner/dotfiles", "owner/personal-site"], settings.ExcludedRepositories);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsNormalisedJson()
    {
        var storage = new FakePmSettingsStorage();
        var service = CreateService(storage);
        var settings = new PmSettingsDto(
            "  PVT_board123  ",
            [" owner/dotfiles ", "OWNER/dotfiles", ""],
            12,
            4,
            30);

        await service.SaveSettingsAsync(settings);

        Assert.Contains("\"planningBoardNodeId\":\"PVT_board123\"", storage.StoredJson, StringComparison.Ordinal);
        Assert.Contains("\"capacity\":12", storage.StoredJson, StringComparison.Ordinal);
        Assert.Contains("\"stallDays\":4", storage.StoredJson, StringComparison.Ordinal);
        Assert.Contains("\"neglectDays\":30", storage.StoredJson, StringComparison.Ordinal);
        Assert.Contains("owner/dotfiles", storage.StoredJson, StringComparison.Ordinal);
        Assert.DoesNotContain("OWNER/dotfiles", storage.StoredJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveSettingsAsync_NullSettings_ThrowsArgumentNullException()
    {
        var service = CreateService(new FakePmSettingsStorage());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveSettingsAsync(null!));
    }

    [Fact]
    public async Task SaveThenGet_RoundTripsSettings()
    {
        var storage = new FakePmSettingsStorage();
        var service = CreateService(storage);
        var expected = new PmSettingsDto(
            "PVT_board123",
            ["owner/dotfiles"],
            9,
            2,
            7);

        await service.SaveSettingsAsync(expected);
        var actual = await service.GetSettingsAsync();

        Assert.Equal(expected.PlanningBoardNodeId, actual.PlanningBoardNodeId);
        Assert.Equal(expected.ExcludedRepositories, actual.ExcludedRepositories);
        Assert.Equal(expected.Capacity, actual.Capacity);
        Assert.Equal(expected.StallDays, actual.StallDays);
        Assert.Equal(expected.NeglectDays, actual.NeglectDays);
    }

    private static PmSettingsService CreateService(FakePmSettingsStorage storage) =>
        new(storage, NullLogger<PmSettingsService>.Instance);

    private sealed class FakePmSettingsStorage : IPmSettingsStorage
    {
        public string? StoredJson { get; set; }

        public Task<string?> GetStoredJsonAsync() => Task.FromResult(StoredJson);

        public Task SetStoredJsonAsync(string json)
        {
            StoredJson = json;
            return Task.CompletedTask;
        }
    }
}
