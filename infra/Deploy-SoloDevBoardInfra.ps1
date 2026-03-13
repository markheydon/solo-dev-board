[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrWhiteSpace()]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrWhiteSpace()]
    [string]$Location,

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$EnvironmentName = 'prod',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$AppServicePlanSku = 'B1',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$GitHubTokenSecretName = 'GitHub--Token',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$HealthCheckPath = '/',

    [Parameter()]
    [ValidateRange(7, 90)]
    [int]$KeyVaultSoftDeleteRetentionInDays = 90,

    [Parameter()]
    [bool]$KeyVaultEnablePurgeProtection = $true,

    [Parameter()]
    [ValidateSet('Enabled', 'Disabled')]
    [string]$KeyVaultPublicNetworkAccess = 'Enabled',

    [Parameter()]
    [string]$SubscriptionId,

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$GitHubRepository = 'markheydon/solo-dev-board'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-CommandAvailable {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    if (-not (Get-Command -Name $CommandName -ErrorAction SilentlyContinue)) {
        throw "Required command '$CommandName' was not found on PATH."
    }
}

Assert-CommandAvailable -CommandName 'az'

Write-Host "Starting SoloDevBoard infrastructure deployment..." -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $currentSubscriptionId = az account show --query id --output tsv
    if ([string]::IsNullOrWhiteSpace($currentSubscriptionId)) {
        throw 'No active Azure subscription was found. Run az login and then retry.'
    }

    $SubscriptionId = $currentSubscriptionId
}
else {
    az account set --subscription $SubscriptionId | Out-Null
}

$tenantId = az account show --query tenantId --output tsv
if ([string]::IsNullOrWhiteSpace($tenantId)) {
    throw 'Unable to resolve Azure tenant ID from the current context.'
}

az group create --name $ResourceGroupName --location $Location | Out-Null

$scriptDirectory = Split-Path -Parent $PSCommandPath
$templateFilePath = Join-Path -Path $scriptDirectory -ChildPath 'main.bicep'
$deploymentName = "solodevboard-$EnvironmentName-$(Get-Date -Format 'yyyyMMddHHmmss')"

$deploymentArguments = @(
    'deployment', 'group', 'create',
    '--name', $deploymentName,
    '--resource-group', $ResourceGroupName,
    '--template-file', $templateFilePath,
    '--parameters',
    "environmentName=$EnvironmentName",
    "location=$Location",
    "appServicePlanSku=$AppServicePlanSku",
    "gitHubTokenSecretName=$GitHubTokenSecretName",
    "healthCheckPath=$HealthCheckPath",
    "keyVaultSoftDeleteRetentionInDays=$KeyVaultSoftDeleteRetentionInDays",
    "keyVaultEnablePurgeProtection=$KeyVaultEnablePurgeProtection",
    "keyVaultPublicNetworkAccess=$KeyVaultPublicNetworkAccess",
    '--output', 'json'
)

$deploymentResultJson = & az @deploymentArguments

$deploymentResult = $deploymentResultJson | ConvertFrom-Json
$outputs = $deploymentResult.properties.outputs

$appServiceUrl = $outputs.appServiceUrl.value
$appServiceName = $outputs.appServiceName.value
$keyVaultName = $outputs.keyVaultName.value
$appServicePrincipalId = $outputs.appServicePrincipalId.value
$roleAssignmentId = $outputs.keyVaultSecretsUserRoleAssignmentId.value

Write-Host ''
Write-Host 'Deployment completed successfully.' -ForegroundColor Green
Write-Host "Resource group: $ResourceGroupName"
Write-Host "Environment: $EnvironmentName"
Write-Host "App Service name: $appServiceName"
Write-Host "App Service URL: $appServiceUrl"
Write-Host "Key Vault name: $keyVaultName"
Write-Host "Managed identity principal ID: $appServicePrincipalId"
Write-Host "Key Vault role assignment ID: $roleAssignmentId"

Write-Host ''
Write-Host 'These are your next steps.' -ForegroundColor Yellow
Write-Host ''
Write-Host '1. Store the GitHub token in Key Vault:' -ForegroundColor Cyan
Write-Host "   az keyvault secret set --vault-name $keyVaultName --name '$GitHubTokenSecretName' --value '<your-github-pat>'"

Write-Host ''
Write-Host '2. Verify the Key Vault role assignment exists:' -ForegroundColor Cyan
Write-Host "   az role assignment list --scope \"`$(az keyvault show --name $keyVaultName --query id -o tsv)\" --query \"[?roleDefinitionName=='Key Vault Secrets User']\""

Write-Host ''
Write-Host '3. Configure local development settings using .NET user secrets:' -ForegroundColor Cyan
Write-Host "   dotnet user-secrets set \"GitHubAuth:OwnerLogin\" \"<your-github-owner>\" --project src/App/SoloDevBoard.App"
Write-Host "   dotnet user-secrets set \"GitHubAuth:PersonalAccessToken\" \"<your-github-pat>\" --project src/App/SoloDevBoard.App"

Write-Host ''
Write-Host '4. Configure GitHub Actions secrets for the CD workflow:' -ForegroundColor Cyan
Write-Host "   gh secret set AZURE_CLIENT_ID --repo $GitHubRepository --body \"<azure-client-id>\""
Write-Host "   gh secret set AZURE_TENANT_ID --repo $GitHubRepository --body \"$tenantId\""
Write-Host "   gh secret set AZURE_SUBSCRIPTION_ID --repo $GitHubRepository --body \"$SubscriptionId\""
Write-Host "   gh secret set AZURE_WEBAPP_NAME --repo $GitHubRepository --body \"$appServiceName\""

Write-Host ''
Write-Host '5. Configure GitHub environment protection for production:' -ForegroundColor Cyan
Write-Host "   - Create or update the 'production' environment in repository settings."
Write-Host '   - Require reviewer approval for deployments.'
Write-Host "   - Restrict deployment branches to 'main'."

Write-Host ''
Write-Host '6. Configure Azure federated credentials for OIDC:' -ForegroundColor Cyan
Write-Host "   - Issuer: https://token.actions.githubusercontent.com"
Write-Host "   - Subject: repo:$GitHubRepository:environment:production"
Write-Host "   - Audience: api://AzureADTokenExchange"

Write-Host ''
Write-Host '7. Trigger deployment using GitHub Actions:' -ForegroundColor Cyan
Write-Host "   - Workflow: .github/workflows/cd.yml"
Write-Host "   - Deployment URL: $appServiceUrl"
