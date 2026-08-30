using Microsoft.Extensions.Configuration;
using SoloDevBoard.Application.Authentication;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class HostedAdmissionControlOptionsBindingTests
{
    [Fact]
    public void Bind_FromAspireStyleEnvironmentVariables_BindsCommaSeparatedAllowLists()
    {
        var previousUserLogins = Environment.GetEnvironmentVariable("HostedAdmissionControl__AllowedUserLogins");
        var previousOrgLogins = Environment.GetEnvironmentVariable("HostedAdmissionControl__AllowedOrganisationLogins");

        try
        {
            Environment.SetEnvironmentVariable("HostedAdmissionControl__AllowedUserLogins", "markheydon,other-user");
            Environment.SetEnvironmentVariable("HostedAdmissionControl__AllowedOrganisationLogins", "-");

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var options = new HostedAdmissionControlOptions();
            configuration.GetSection(HostedAdmissionControlOptions.SectionName).Bind(options);

            Assert.True(HostedAdmissionAllowList.HasConfiguredEntries(options.AllowedUserLogins));
            Assert.False(HostedAdmissionAllowList.HasConfiguredEntries(options.AllowedOrganisationLogins));

            var allowedUsers = HostedAdmissionAllowList.BuildNormalisedSet(options.AllowedUserLogins);
            Assert.Equal(2, allowedUsers.Count);
            Assert.Contains("markheydon", allowedUsers);
            Assert.Contains("other-user", allowedUsers);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HostedAdmissionControl__AllowedUserLogins", previousUserLogins);
            Environment.SetEnvironmentVariable("HostedAdmissionControl__AllowedOrganisationLogins", previousOrgLogins);
        }
    }
}
