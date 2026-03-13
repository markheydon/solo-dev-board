[CmdletBinding()]
param (
    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$EnvironmentName = 'prod',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$ResourceGroupName,

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$Location = 'uksouth',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$AppServicePlanSku = 'F1',

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$GitHubTokenSecretName = 'GitHubAuth--PersonalAccessToken',

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
    [string[]]$AppServiceAllowedCidrs = @(),

    [Parameter()]
    [switch]$AllowPublicAppAccess,

    [Parameter()]
    [string]$SubscriptionId,

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$GitHubRepository = '<owner>/<repo>'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw (
        'This deployment script requires PowerShell 7 or later.' +
        [Environment]::NewLine +
        'Run it with pwsh (PowerShell 7+), for example: pwsh infra/Deploy-SoloDevBoardInfra.ps1'
    )
}

$scriptDirectory = Split-Path -Parent $PSCommandPath
$templateFilePath = Join-Path -Path $scriptDirectory -ChildPath 'main.bicep'

function Get-PrerequisiteGuidance {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    switch ($CommandName) {
        'az' {
            return @(
                "Install Azure CLI: https://learn.microsoft.com/cli/azure/install-azure-cli",
                "After installation, restart your shell and verify with: az version"
            )
        }
        default {
            return @("Install '$CommandName' and ensure it is available on PATH.")
        }
    }
}

function Assert-CommandAvailable {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    if (-not (Get-Command -Name $CommandName -ErrorAction SilentlyContinue)) {
        $guidance = Get-PrerequisiteGuidance -CommandName $CommandName
        throw (
            "Prerequisite check failed: required command '$CommandName' was not found on PATH." +
            [Environment]::NewLine +
            ($guidance -join [Environment]::NewLine)
        )
    }
}

function Assert-AzBicepAvailable {
    & az bicep --help *> $null
    if ($LASTEXITCODE -ne 0) {
        throw (
            "Prerequisite check failed: Azure CLI Bicep support is not available." +
            [Environment]::NewLine +
            "Install or refresh Bicep for Azure CLI with: az bicep install" +
            [Environment]::NewLine +
            "If Bicep is already installed, verify with: az bicep lint --file main.bicep" +
            [Environment]::NewLine +
            "If you use a custom Bicep binary on PATH, review: az config get bicep.use_binary_from_path" +
            [Environment]::NewLine +
            "Reference: https://learn.microsoft.com/azure/azure-resource-manager/bicep/install"
        )
    }
}

function Assert-AzureLogin {
    & az account show --output none *> $null
    if ($LASTEXITCODE -ne 0) {
        throw (
            "Prerequisite check failed: no active Azure login was found." +
            [Environment]::NewLine +
            "Run: az login" +
            [Environment]::NewLine +
            "Then, if required, select a subscription with: az account set --subscription <subscription-id>" +
            [Environment]::NewLine +
            "Reference: https://learn.microsoft.com/cli/azure/authenticate-azure-cli"
        )
    }
}

function Assert-BicepLintPass {
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrWhiteSpace()]
        [string]$TemplateFilePath
    )

    $lintOutput = & az bicep lint --file $TemplateFilePath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw (
            "Prerequisite check failed: Bicep lint reported issues for '$TemplateFilePath'." +
            [Environment]::NewLine +
            "Fix the template issues and rerun the deployment." +
            [Environment]::NewLine +
            "Lint output:" +
            [Environment]::NewLine +
            ($lintOutput -join [Environment]::NewLine)
        )
    }
}

function Get-DefaultResourceGroupName {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Environment
    )

    $sanitisedEnvironment = $Environment.ToLowerInvariant() -replace '[^a-z0-9-]', '-'
    $sanitisedEnvironment = $sanitisedEnvironment.Trim('-')

    if ([string]::IsNullOrWhiteSpace($sanitisedEnvironment)) {
        $sanitisedEnvironment = 'prod'
    }

    return "rg-solodevboard-$sanitisedEnvironment"
}

function Get-DefaultAppServiceName {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Environment
    )

    $sanitisedEnvironment = $Environment.ToLowerInvariant() -replace '[^a-z0-9-]', '-'
    $sanitisedEnvironment = $sanitisedEnvironment.Trim('-')

    if ([string]::IsNullOrWhiteSpace($sanitisedEnvironment)) {
        $sanitisedEnvironment = 'prod'
    }

    return "app-solodevboard-$sanitisedEnvironment"
}

function Get-CurrentPublicIpv4Cidr {
    $ipLookupUris = @(
        'https://api.ipify.org',
        'https://ifconfig.me/ip',
        'https://icanhazip.com'
    )

    foreach ($ipLookupUri in $ipLookupUris) {
        try {
            $publicIp = (Invoke-RestMethod -Method Get -Uri $ipLookupUri -TimeoutSec 10).ToString().Trim()
            $parsedAddress = $null

            if ([System.Net.IPAddress]::TryParse($publicIp, [ref]$parsedAddress) -and $parsedAddress.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork) {
                return "$publicIp/32"
            }
        }
        catch {
            continue
        }
    }

    throw (
        'Unable to detect your current public IPv4 address for App Service access restrictions.' +
        [Environment]::NewLine +
        'Provide -AppServiceAllowedCidrs explicitly, or pass -AllowPublicAppAccess to keep the app public.'
    )
}

try {
    Assert-CommandAvailable -CommandName 'az'
    Assert-AzBicepAvailable
    Assert-AzureLogin
    Assert-BicepLintPass -TemplateFilePath $templateFilePath
}
catch {
    Write-Output ''
    Write-Output 'Unable to start deployment because a prerequisite check failed.'
    Write-Output $_.Exception.Message
    Write-Output ''
    Write-Output 'Fix the requirement above and rerun this script.'
    exit 1
}

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
    $ResourceGroupName = Get-DefaultResourceGroupName -Environment $EnvironmentName
}

$isFreeTierPlan = $AppServicePlanSku.Equals('F1', [System.StringComparison]::OrdinalIgnoreCase)
$explicitAppServiceAllowedCidrs = @($AppServiceAllowedCidrs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$effectiveAppServiceAllowedCidrs = @($explicitAppServiceAllowedCidrs)

if ($isFreeTierPlan -and $explicitAppServiceAllowedCidrs.Count -gt 0) {
    throw (
        'App Service inbound access restrictions are not supported on the F1 plan in this deployment path.' +
        [Environment]::NewLine +
        'Use -AppServicePlanSku B1 (or higher) to apply -AppServiceAllowedCidrs.'
    )
}

if ($effectiveAppServiceAllowedCidrs.Count -eq 0 -and -not $AllowPublicAppAccess -and -not $isFreeTierPlan) {
    $detectedCallerCidr = Get-CurrentPublicIpv4Cidr
    $effectiveAppServiceAllowedCidrs = @($detectedCallerCidr)
    Write-Output "No -AppServiceAllowedCidrs values were provided; defaulting to your current public IPv4: $detectedCallerCidr"
}
elseif ($effectiveAppServiceAllowedCidrs.Count -eq 0 -and -not $AllowPublicAppAccess -and $isFreeTierPlan) {
    Write-Output 'F1 plan detected; keeping app publicly reachable because inbound access restrictions are not available on this plan.'
    Write-Output 'Use -AppServicePlanSku B1 (or higher) to enable CIDR restrictions, including auto-detected /32 defaulting.'
}

Write-Output "Starting SoloDevBoard infrastructure deployment..."

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

# If an existing site is being moved to F1, Always On must be disabled first.
if ($isFreeTierPlan) {
    $appServiceName = Get-DefaultAppServiceName -Environment $EnvironmentName
    $existingSiteName = & az webapp show --resource-group $ResourceGroupName --name $appServiceName --query name --output tsv 2>$null

    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existingSiteName)) {
        $alwaysOnValue = & az webapp config show --resource-group $ResourceGroupName --name $appServiceName --query alwaysOn --output tsv 2>$null

        if ($LASTEXITCODE -eq 0 -and $alwaysOnValue -eq 'true') {
            Write-Output "Existing app '$appServiceName' has Always On enabled; disabling it for F1 compatibility..."
            & az webapp config set --resource-group $ResourceGroupName --name $appServiceName --always-on false --output none 2>&1 | Out-Null

            if ($LASTEXITCODE -ne 0) {
                throw (
                    "Unable to disable Always On for existing app '$appServiceName' before F1 deployment." +
                    [Environment]::NewLine +
                    "Rerun with -AppServicePlanSku B1 to avoid F1 restrictions, or disable Always On manually first."
                )
            }
        }
    }
}

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
    "keyVaultPublicNetworkAccess=$KeyVaultPublicNetworkAccess"
)

if ($effectiveAppServiceAllowedCidrs.Count -gt 0) {
    $allowedCidrsJson = ConvertTo-Json -InputObject $effectiveAppServiceAllowedCidrs -Compress -AsArray
    $deploymentArguments += "appServiceAllowedCidrs=$allowedCidrsJson"
}

$deploymentArguments += @('--output', 'json')

$deploymentResultJson = & az @deploymentArguments 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Output ''
    Write-Output 'Azure deployment failed.'
    Write-Output 'The CLI reported:'
    Write-Output ($deploymentResultJson -join [Environment]::NewLine)
    Write-Output ''
    Write-Output 'Failed deployment operations (if available):'
    & az deployment group operation list --resource-group $ResourceGroupName --name $deploymentName --query "[?properties.provisioningState=='Failed'].{resource:properties.targetResource.resourceName,status:properties.provisioningState,message:properties.statusMessage.error.message}" --output table 2>$null
    Write-Output ''
    Write-Output 'Tip: If you are using F1, existing apps must not have Always On enabled.'
    exit 1
}

$deploymentResult = ($deploymentResultJson -join [Environment]::NewLine) | ConvertFrom-Json
$outputs = $deploymentResult.properties.outputs

$appServiceUrl = $outputs.appServiceUrl.value
$appServiceName = $outputs.appServiceName.value
$keyVaultName = $outputs.keyVaultName.value
$appServicePrincipalId = $outputs.appServicePrincipalId.value
$roleAssignmentId = $outputs.keyVaultSecretsUserRoleAssignmentId.value

Write-Output ''
Write-Output 'Deployment completed successfully.'
Write-Output "Resource group: $ResourceGroupName"
Write-Output "Environment: $EnvironmentName"
Write-Output "App Service name: $appServiceName"
Write-Output "App Service URL: $appServiceUrl"
Write-Output "Key Vault name: $keyVaultName"
Write-Output "Managed identity principal ID: $appServicePrincipalId"
Write-Output "Key Vault role assignment ID: $roleAssignmentId"
if ($effectiveAppServiceAllowedCidrs.Count -gt 0) {
    Write-Output "App Service inbound restriction mode: Allow list"
    Write-Output ("Allowed CIDRs: {0}" -f ($effectiveAppServiceAllowedCidrs -join ', '))
}
else {
    Write-Output 'App Service inbound restriction mode: Open to public internet'
}

Write-Output ''
Write-Output 'These are your next steps.'
Write-Output ''
Write-Output '1. Store the GitHub token in Key Vault:'
Write-Output ('   az keyvault secret set --vault-name {0} --name "{1}" --value "<your-github-pat>"' -f $keyVaultName, $GitHubTokenSecretName)
Write-Output '   (This is used by the deployed App Service at runtime.)'
Write-Output '   (Secret name is independent of app setting key; this template defaults to GitHubAuth--PersonalAccessToken.)'

Write-Output ''
Write-Output '1a. If secret set returns Forbidden, your account needs Key Vault data-plane permissions:'
Write-Output ('   az role assignment list --scope "$(az keyvault show --name {0} --query id -o tsv)" --assignee "$(az ad signed-in-user show --query id -o tsv)" --output table' -f $keyVaultName)
Write-Output '   Required role: Key Vault Secrets Officer (or Key Vault Administrator) on the vault scope.'
Write-Output '   If you cannot assign roles yourself, ask a subscription/resource-group owner to grant that role, then retry step 1.'

Write-Output ''
Write-Output '1b. If you can self-assign roles, grant yourself temporary secret-write access:'
Write-Output ('   az role assignment create --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" --assignee-principal-type User --role "Key Vault Secrets Officer" --scope "$(az keyvault show --name {0} --query id -o tsv)"' -f $keyVaultName)
Write-Output '   Then rerun step 1 to set the secret.'
Write-Output '   Note: incremental Bicep deployments usually do not remove this extra user role assignment automatically.'
Write-Output '   After setting the secret, remove the temporary assignment to return to least-privilege:'
Write-Output ('   az role assignment delete --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" --role "Key Vault Secrets Officer" --scope "$(az keyvault show --name {0} --query id -o tsv)"' -f $keyVaultName)

Write-Output ''
Write-Output '2. Verify the Key Vault role assignment exists:'
Write-Output ('   az role assignment list --scope "$(az keyvault show --name {0} --query id -o tsv)" --query "[?roleDefinitionName==''Key Vault Secrets User'']"' -f $keyVaultName)

Write-Output ''
Write-Output '2a. Optional hardening: restrict app access to known IP ranges:'
Write-Output '   Rerun this script with: -AppServiceAllowedCidrs "<your-public-ip>/32"'
Write-Output '   You can provide multiple values, e.g. -AppServiceAllowedCidrs "1.2.3.4/32","5.6.7.0/24"'
Write-Output '   If omitted, this script now auto-detects your current public IPv4 and applies it as /32.'
Write-Output '   To intentionally keep public access, rerun with: -AllowPublicAppAccess'

Write-Output ''
Write-Output '3. Optional (local development only): configure .NET user secrets:'
Write-Output '   dotnet user-secrets set "GitHubAuth:OwnerLogin" "<your-github-owner>" --project src/App/SoloDevBoard.App'
Write-Output '   dotnet user-secrets set "GitHubAuth:PersonalAccessToken" "<your-github-pat>" --project src/App/SoloDevBoard.App'
Write-Output '   (The deployed App Service does not use .NET user secrets.)'

Write-Output ''
Write-Output '4. Configure GitHub Actions secrets for the CD workflow:'
Write-Output ('   gh secret set AZURE_CLIENT_ID --repo {0} --body "<azure-client-id>"' -f $GitHubRepository)
Write-Output ('   gh secret set AZURE_TENANT_ID --repo {0} --body "{1}"' -f $GitHubRepository, $tenantId)
Write-Output ('   gh secret set AZURE_SUBSCRIPTION_ID --repo {0} --body "{1}"' -f $GitHubRepository, $SubscriptionId)
Write-Output ('   gh secret set AZURE_WEBAPP_NAME --repo {0} --body "{1}"' -f $GitHubRepository, $appServiceName)

Write-Output ''
Write-Output '5. Configure GitHub environment protection for production:'
Write-Output "   - Create or update the 'production' environment in repository settings."
Write-Output '   - Require reviewer approval for deployments.'
Write-Output "   - Restrict deployment branches to 'main'."

Write-Output ''
Write-Output '6. Configure Azure federated credentials for OIDC:'
Write-Output "   - Issuer: https://token.actions.githubusercontent.com"
Write-Output "   - Subject: repo:${GitHubRepository}:environment:production"
Write-Output "   - Audience: api://AzureADTokenExchange"

Write-Output ''
Write-Output '7. Trigger deployment using GitHub Actions:'
Write-Output "   - Workflow: .github/workflows/cd.yml"
Write-Output "   - Deployment URL: $appServiceUrl"
