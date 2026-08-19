import type { E2eAuthMode } from './e2eAuthMode';
import { getE2eAuthMode } from './e2eAuthMode';

/** Environment variables for the SoloDevBoard web server in E2E runs. */
export function getWebServerEnv(authMode: E2eAuthMode = getE2eAuthMode()): Record<string, string> {
  const base: Record<string, string> = {
    ASPNETCORE_URLS: 'http://localhost:5080',
    ASPNETCORE_ENVIRONMENT: 'Development',
  };

  if (authMode === 'hosted') {
    return {
      ...base,
      GitHubAuth__PersonalAccessToken: '-',
      GitHubAuth__HostedSignInEnabled: 'true',
      GitHubAuth__HostedGitHubAppClientId: 'ci-e2e-hosted-client-id',
      GitHubAuth__HostedGitHubAppClientSecret: 'ci-e2e-hosted-client-secret',
      HostedAdmissionControl__Enabled: 'true',
      HostedAdmissionControl__AllowedUserLogins: 'ci-test-user',
      HostedAdmissionControl__AllowedOrganisationLogins: '-',
    };
  }

  return {
    ...base,
    GitHubAuth__PersonalAccessToken: 'ci-e2e-placeholder',
    GitHubAuth__OwnerLogin: 'ci-test-user',
    GitHubAuth__HostedSignInEnabled: 'false',
    HostedAdmissionControl__Enabled: 'false',
  };
}
