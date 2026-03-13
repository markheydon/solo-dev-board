// SoloDevBoard — App Service Module
// Creates the App Service Plan and App Service for the SoloDevBoard Blazor Server application.

@description('The Azure region for deployment.')
param location string

@description('The environment name (e.g. dev, staging, prod). Used to suffix resource names.')
param environmentName string

@description('The SKU for the App Service Plan.')
param appServicePlanSku string = 'B1'

@description('The name of the Key Vault used to store secrets. The App Service will be granted access via managed identity.')
param keyVaultName string

@description('The Key Vault secret name that stores the GitHub token for the application.')
param gitHubTokenSecretName string = 'GitHub--Token'

@description('The App Service health check path. Use "/" until a dedicated health endpoint is introduced.')
param healthCheckPath string = '/'

var resourceSuffix = toLower(environmentName)
var appServicePlanName = 'asp-solodevboard-${resourceSuffix}'
var appServiceName = 'app-solodevboard-${resourceSuffix}'

// App Service Plan — Linux, .NET 10 runtime
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServicePlanSku
  }
  properties: {
    reserved: true // Required for Linux plans
  }
}

// App Service — hosts the SoloDevBoard Blazor Server application
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  identity: {
    // System-assigned managed identity — used to access Key Vault without storing credentials
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true // Enforce HTTPS; redirect HTTP to HTTPS
    siteConfig: {
      // .NET 10 on Linux
      linuxFxVersion: 'DOTNETCORE|10.0'
      // Always On keeps the app warm and prevents SignalR connection drops (not available on Free tier)
      alwaysOn: appServicePlanSku != 'F1'
      // Minimum TLS 1.2
      minTlsVersion: '1.2'
      // Route platform health checks to an endpoint that returns 200.
      healthCheckPath: healthCheckPath
      appSettings: [
        {
          // Configure the app to read GitHub token from Key Vault
          // The Key Vault reference syntax: @Microsoft.KeyVault(VaultName=<name>;SecretName=<secret>)
          name: 'GitHubAuth__PersonalAccessToken'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${gitHubTokenSecretName})'
        }
        {
          name: 'GitHub__BaseUrl'
          value: 'https://api.github.com'
        }
        {
          // Required for Blazor Server — sets the correct ASPNETCORE_ENVIRONMENT
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
      ]
    }
  }
}

// Output the App Service URL for use in the main template and CD pipeline
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
// Output the App Service name for workflow secret configuration.
output appServiceName string = appService.name
// Output the managed identity principal ID so RBAC can be assigned after deployment
output appServicePrincipalId string = appService.identity.principalId
