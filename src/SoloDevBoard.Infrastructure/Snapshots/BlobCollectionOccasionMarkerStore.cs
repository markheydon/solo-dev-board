using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Storage.Blobs.Models;
using SoloDevBoard.Application.Services.Snapshots;

namespace SoloDevBoard.Infrastructure.Snapshots;

/// <inheritdoc/>
public sealed class BlobCollectionOccasionMarkerStore(CollectionScheduleBlobContainer container) : ICollectionOccasionMarkerStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <inheritdoc/>
    public async Task<OccasionMarkerReadResult> GetAsync(string occasionDate, CancellationToken cancellationToken = default)
    {
        var blob = container.Client.GetBlobClient($"occasions/{occasionDate}.json");
        if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return new OccasionMarkerReadResult.NotFound();
        }

        var content = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        var marker = JsonSerializer.Deserialize<OccasionMarker>(content.Value.Content.ToString(), JsonOptions);
        return marker is null
            ? new OccasionMarkerReadResult.Unreadable()
            : new OccasionMarkerReadResult.Found(marker);
    }

    /// <inheritdoc/>
    public async Task<OccasionMarkerWriteResult> TryCreateAsync(OccasionMarker marker, CancellationToken cancellationToken = default)
    {
        var blob = container.Client.GetBlobClient($"occasions/{marker.OccasionDate}.json");
        try
        {
            await blob.UploadAsync(
                    BinaryData.FromString(JsonSerializer.Serialize(marker, JsonOptions)),
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All } },
                    cancellationToken)
                .ConfigureAwait(false);
            return new OccasionMarkerWriteResult.Created();
        }
        catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
        {
            return new OccasionMarkerWriteResult.AlreadyExists();
        }
    }
}
