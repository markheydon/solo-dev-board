// SoloDevBoard — Main Bicep Template
// Deploys all Azure resources required to run SoloDevBoard.
// See infra/README.md for deployment instructions and parameter descriptions.

@description('The environment name (e.g. dev, staging, prod). Used to suffix resource names.')
param environmentName string = 'dev'

@description('The Azure region for deployment. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('The SKU for the App Service Plan. Use B1 for development, P1v3 for production.')
param appServicePlanSku string = 'B1'

@description('The Key Vault secret name that stores the GitHub token used by the application.')
param gitHubTokenSecretName string = 'GitHubAuth--PersonalAccessToken'

@description('The App Service health check path. Keep this as "/" until a dedicated endpoint is available.')
param healthCheckPath string = '/'

@description('The retention period for soft-deleted Key Vault objects, in days.')
@minValue(7)
@maxValue(90)
param keyVaultSoftDeleteRetentionInDays int = 90

@description('Controls whether Key Vault purge protection is enabled. Keep enabled for production workloads.')
param keyVaultEnablePurgeProtection bool = true

@description('Controls whether Key Vault public network access is enabled.')
@allowed([
  'Enabled'
  'Disabled'
])
param keyVaultPublicNetworkAccess string = 'Enabled'

@description('Optional list of CIDR ranges allowed to access the App Service. When empty, inbound access remains open.')
param appServiceAllowedCidrs array = []

@description('GitHub repository in <owner>/<repo> format used for OIDC federation subject matching.')
param gitHubRepository string

@description('GitHub environment name used by the CD workflow for OIDC federation subject matching.')
param gitHubEnvironmentName string = 'production'

@description('Optional short suffix appended to globally constrained resource names (for example app and Key Vault) to avoid naming collisions across subscriptions.')
param resourceNameSuffix string = ''

// Derive a short, consistent suffix for resource naming
var resourceSuffix = toLower(environmentName)
var nameSuffix = empty(resourceNameSuffix) ? '' : '-${toLower(resourceNameSuffix)}'

// Key Vault name must be globally unique and 3–24 characters
var keyVaultName = 'kv-solodevboard-${resourceSuffix}${nameSuffix}'
var gitHubOidcIdentityName = 'id-solodevboard-cd-${resourceSuffix}${nameSuffix}'
var gitHubOidcFederatedCredentialName = 'github-${resourceSuffix}${nameSuffix}'

// Built-in role definition for Key Vault Secrets User.
var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'
// Built-in role definition for Contributor.
var contributorRoleDefinitionId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'

// Deploy the App Service resources via a module for separation of concerns
module appServiceModule 'modules/appservice.bicep' = {
  params: {
    location: location
    environmentName: environmentName
    appServicePlanSku: appServicePlanSku
    keyVaultName: keyVaultName
    gitHubTokenSecretName: gitHubTokenSecretName
    healthCheckPath: healthCheckPath
    appServiceAllowedCidrs: appServiceAllowedCidrs
    resourceNameSuffix: resourceNameSuffix
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
    softDeleteRetentionInDays: keyVaultSoftDeleteRetentionInDays
    // Purge protection should remain enabled for production-grade recoverability.
    enablePurgeProtection: keyVaultEnablePurgeProtection
    // Allow the App Service managed identity to read secrets via the role assignment in this template.
    publicNetworkAccess: keyVaultPublicNetworkAccess
  }
}

// Grants the App Service managed identity read access to Key Vault secrets.
resource keyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, keyVaultSecretsUserRoleDefinitionId, 'appservice-secrets-user')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalId: appServiceModule.outputs.appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// User-assigned identity used by GitHub Actions OIDC for deployment.
resource gitHubOidcIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: gitHubOidcIdentityName
  location: location
}

// Federated credential that trusts tokens from the configured GitHub repository environment.
resource gitHubOidcFederatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: gitHubOidcIdentity
  name: gitHubOidcFederatedCredentialName
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${gitHubRepository}:environment:${gitHubEnvironmentName}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

// Grant deployment rights to the GitHub OIDC identity at resource group scope.
resource gitHubOidcContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, contributorRoleDefinitionId, gitHubOidcIdentity.id, 'github-oidc-contributor')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleDefinitionId)
    principalId: gitHubOidcIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs — used by the CD pipeline and post-deployment configuration scripts
output appServiceUrl string = appServiceModule.outputs.appServiceUrl
output appServiceName string = appServiceModule.outputs.appServiceName
output keyVaultName string = keyVault.name
output appServicePrincipalId string = appServiceModule.outputs.appServicePrincipalId
output keyVaultSecretsUserRoleAssignmentId string = keyVaultSecretsUserRoleAssignment.id
output gitHubOidcClientId string = gitHubOidcIdentity.properties.clientId
output gitHubOidcPrincipalId string = gitHubOidcIdentity.properties.principalId
output gitHubOidcIdentityName string = gitHubOidcIdentity.name
output gitHubOidcFederatedCredentialName string = gitHubOidcFederatedCredential.name
output gitHubOidcFederatedSubject string = gitHubOidcFederatedCredential.properties.subject
output gitHubOidcContributorRoleAssignmentId string = gitHubOidcContributorRoleAssignment.id
