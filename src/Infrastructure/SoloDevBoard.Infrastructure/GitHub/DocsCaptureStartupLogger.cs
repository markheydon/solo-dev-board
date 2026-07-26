using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Logs a startup warning when docs capture mode is active.</summary>
public sealed class DocsCaptureStartupLogger(
    IOptions<DocsCaptureOptions> docsCaptureOptions,
    ILogger<DocsCaptureStartupLogger> logger) : IHostedService
{
    private readonly DocsCaptureOptions _docsCaptureOptions =
        docsCaptureOptions?.Value ?? throw new ArgumentNullException(nameof(docsCaptureOptions));
    private readonly ILogger<DocsCaptureStartupLogger> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_docsCaptureOptions.Enabled)
        {
            _logger.LogWarning(
                "Docs capture mode is enabled. Repository and project board catalogues are restricted to public GitHub content only. This is for local documentation screenshots and is not a security boundary.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
