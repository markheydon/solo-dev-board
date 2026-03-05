// SoloDevBoard — Main Bicep Template
// Deploys all Azure resources required to run SoloDevBoard.
// See infra/README.md for deployment instructions and parameter descriptions.

@description('The environment name (e.g. dev, staging, prod). Used to suffix resource names.')
param environmentName string = 'dev'

@description('The Azure region for deployment. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('The SKU for the App Service Plan. Use B1 for development, P1v3 for production.')
param appServicePlanSku string = 'B1'

// Derive a short, consistent suffix for resource naming
var resourceSuffix = toLower(environmentName)

// Key Vault name must be globally unique and 3–24 characters
var keyVaultName = 'kv-solodevboard-${resourceSuffix}'

// Deploy the App Service resources via a module for separation of concerns
module appServiceModule 'modules/appservice.bicep' = {
  name: 'appservice-deployment'
  params: {
    location: location
    environmentName: environmentName
    appServicePlanSku: appServicePlanSku
    keyVaultName: keyVaultName
  }
}

// Key Vault — stores the GitHub token and any other secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // Enable RBAC-based access control (preferred over access policies)
    enableRbacAuthorization: true
    // Protect against accidental deletion
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Allow the App Service managed identity to read secrets (grant via RBAC after deployment)
    publicNetworkAccess: 'Enabled'
  }
}

// Outputs — used by the CD pipeline and post-deployment configuration scripts
output appServiceUrl string = appServiceModule.outputs.appServiceUrl
output keyVaultName string = keyVault.name
