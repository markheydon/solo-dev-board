using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.GitHub;

/// <summary>Validates GitHub authentication configuration during application startup.</summary>
public sealed class GitHubAuthConfigurationValidator(
    IOptions<GitHubAuthOptions> authOptions,
    IOptions<HostedAdmissionControlOptions> admissionOptions,
    ILogger<GitHubAuthConfigurationValidator> logger) : IHostedService
{
    private readonly GitHubAuthOptions _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
    private readonly HostedAdmissionControlOptions _admissionOptions = admissionOptions?.Value ?? throw new ArgumentNullException(nameof(admissionOptions));
    private readonly ILogger<GitHubAuthConfigurationValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        GitHubAuthConfigurationValidation.Validate(_authOptions, _admissionOptions);

        if (_authOptions.HostedSignInEnabled)
        {
            _logger.LogInformation(
                "GitHub auth: hosted sign-in mode is active. Admission control is {AdmissionState}.",
                _admissionOptions.Enabled ? "enabled" : "disabled");
        }
        else
        {
            _logger.LogInformation(
                "GitHub auth: PAT mode is active. Owner login will be resolved from the personal access token when needed.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
