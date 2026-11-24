using Aspire.Hosting.Azure;

/// <summary>Shared auth and admission bindings for the scheduled collection Functions worker.</summary>
internal static class HostedSnapshotSchedulerBindings
{
    /// <summary>Applies hosted GitHub App OAuth and admission settings required for token refresh.</summary>
    public static IResourceBuilder<AzureFunctionsProjectResource> Apply(
        SoloDevBoardAppHost host,
        IResourceBuilder<AzureFunctionsProjectResource> functions)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(functions);

        var parameters = host.Parameters;
        return functions
            .WithEnvironment("GitHubAuth__HostedSignInEnabled", parameters.HostedSignInEnabled)
            .WithEnvironment("GitHubAuth__HostedGitHubAppClientId", parameters.GhAppClientId)
            .WithEnvironment("HostedAdmissionControl__Enabled", parameters.HostedAdmissionEnabled)
            .WithEnvironment("HostedAdmissionControl__AllowedUserLogins", parameters.AllowedUserLogins)
            .WithEnvironment("HostedAdmissionControl__AllowedOrganisationLogins", parameters.AllowedOrgLogins);
    }
}
