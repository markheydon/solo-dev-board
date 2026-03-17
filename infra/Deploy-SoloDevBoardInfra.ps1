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
    [switch]$EnableAppServiceIpHardening,

    [Parameter()]
    [switch]$AllowPublicAppAccess,

    [Parameter()]
    [string]$SubscriptionId,

    [Parameter()]
    [string]$ResourceNameSuffix,

    [Parameter()]
    [string]$GitHubRepository,

    [Parameter()]
    [ValidateNotNullOrWhiteSpace()]
    [string]$GitHubEnvironmentName = 'production',

    [Parameter()]
    [switch]$IncludeNextSteps
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

function Get-GitHubRepositoryFromGitRemote {
    $originUrl = $null

    try {
        $originUrl = (& git config --get remote.origin.url 2>$null | Select-Object -First 1)
    }
    catch {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($originUrl)) {
        return $null
    }

    $trimmedOriginUrl = $originUrl.Trim()

    if ($trimmedOriginUrl -match '^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$') {
        return "{0}/{1}" -f $Matches.owner, $Matches.repo
    }

    if ($trimmedOriginUrl -match '^git@github\.com:(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$') {
        return "{0}/{1}" -f $Matches.owner, $Matches.repo
    }

    return $null
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

if ([string]::IsNullOrWhiteSpace($GitHubRepository)) {
    $detectedGitHubRepository = Get-GitHubRepositoryFromGitRemote

    if ([string]::IsNullOrWhiteSpace($detectedGitHubRepository)) {
        throw (
            'Unable to detect GitHub repository from git remote origin.' +
            [Environment]::NewLine +
            'Specify -GitHubRepository in <owner>/<repo> format and rerun the script.'
        )
    }

    $GitHubRepository = $detectedGitHubRepository
    Write-Output "Detected GitHub repository from git remote origin: $GitHubRepository"
}

$isFreeTierPlan = $AppServicePlanSku.Equals('F1', [System.StringComparison]::OrdinalIgnoreCase)
$explicitAppServiceAllowedCidrs = @($AppServiceAllowedCidrs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$effectiveAppServiceAllowedCidrs = @($explicitAppServiceAllowedCidrs)
$ipHardeningEnabled = $EnableAppServiceIpHardening.IsPresent -or $explicitAppServiceAllowedCidrs.Count -gt 0

if ($isFreeTierPlan -and $explicitAppServiceAllowedCidrs.Count -gt 0) {
    throw (
        'App Service inbound access restrictions are not supported on the F1 plan in this deployment path.' +
        [Environment]::NewLine +
        'Use -AppServicePlanSku B1 (or higher) to apply -AppServiceAllowedCidrs.'
    )
}

if ($ipHardeningEnabled -and $effectiveAppServiceAllowedCidrs.Count -eq 0 -and -not $AllowPublicAppAccess -and -not $isFreeTierPlan) {
    $detectedCallerCidr = Get-CurrentPublicIpv4Cidr
    $effectiveAppServiceAllowedCidrs = @($detectedCallerCidr)
    Write-Output "No -AppServiceAllowedCidrs values were provided; defaulting to your current public IPv4: $detectedCallerCidr"
}
elseif ($ipHardeningEnabled -and $effectiveAppServiceAllowedCidrs.Count -eq 0 -and -not $AllowPublicAppAccess -and $isFreeTierPlan) {
    Write-Output 'F1 plan detected; keeping app publicly reachable because inbound access restrictions are not available on this plan.'
    Write-Output 'Use -AppServicePlanSku B1 (or higher) to enable CIDR restrictions, including auto-detected /32 defaulting.'
}
elseif (-not $ipHardeningEnabled) {
    Write-Output 'App Service IP hardening is disabled by default.'
    Write-Output 'Pass -EnableAppServiceIpHardening to auto-allow your current public IPv4, or set -AppServiceAllowedCidrs explicitly.'
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
    "keyVaultPublicNetworkAccess=$KeyVaultPublicNetworkAccess",
    "gitHubRepository=$GitHubRepository",
    "gitHubEnvironmentName=$GitHubEnvironmentName"
)

if (-not [string]::IsNullOrWhiteSpace($ResourceNameSuffix)) {
    $deploymentArguments += "resourceNameSuffix=$ResourceNameSuffix"
}

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
$gitHubOidcClientId = $outputs.gitHubOidcClientId.value
$gitHubOidcPrincipalId = $outputs.gitHubOidcPrincipalId.value
$gitHubOidcIdentityName = $outputs.gitHubOidcIdentityName.value
$gitHubOidcFederatedCredentialName = $outputs.gitHubOidcFederatedCredentialName.value
$gitHubOidcFederatedSubject = $outputs.gitHubOidcFederatedSubject.value
$gitHubOidcContributorRoleAssignmentId = $outputs.gitHubOidcContributorRoleAssignmentId.value

Write-Output ''
Write-Output 'Deployment completed successfully.'
Write-Output "Resource group: $ResourceGroupName"
Write-Output "Environment: $EnvironmentName"
Write-Output "App Service name: $appServiceName"
Write-Output "App Service URL: $appServiceUrl"
Write-Output "Key Vault name: $keyVaultName"
Write-Output "App Service managed identity principal ID: $appServicePrincipalId"
Write-Output "Key Vault role assignment ID: $roleAssignmentId"
Write-Output "GitHub OIDC identity name: $gitHubOidcIdentityName"
Write-Output "GitHub OIDC client ID: $gitHubOidcClientId"
Write-Output "GitHub OIDC principal ID: $gitHubOidcPrincipalId"
Write-Output "GitHub OIDC federated credential: $gitHubOidcFederatedCredentialName"
Write-Output "GitHub OIDC subject: $gitHubOidcFederatedSubject"
Write-Output "GitHub OIDC role assignment ID: $gitHubOidcContributorRoleAssignmentId"
if ($effectiveAppServiceAllowedCidrs.Count -gt 0) {
    Write-Output "App Service inbound restriction mode: Allow list"
    Write-Output ("Allowed CIDRs: {0}" -f ($effectiveAppServiceAllowedCidrs -join ', '))
}
else {
    Write-Output 'App Service inbound restriction mode: Open to public internet'
}

if ($IncludeNextSteps) {
    Write-Output ''
    Write-Output 'These are your next steps.'
    Write-Output ''
    Write-Output '1. Configure GitHub Actions environment secrets (required for CD):'
    Write-Output ('   gh secret set AZURE_CLIENT_ID --repo {0} --env {1} --body "{2}"' -f $GitHubRepository, $GitHubEnvironmentName, $gitHubOidcClientId)
    Write-Output ('   gh secret set AZURE_TENANT_ID --repo {0} --env {1} --body "{2}"' -f $GitHubRepository, $GitHubEnvironmentName, $tenantId)
    Write-Output ('   gh secret set AZURE_SUBSCRIPTION_ID --repo {0} --env {1} --body "{2}"' -f $GitHubRepository, $GitHubEnvironmentName, $SubscriptionId)
    Write-Output ('   gh secret set AZURE_WEBAPP_NAME --repo {0} --env {1} --body "{2}"' -f $GitHubRepository, $GitHubEnvironmentName, $appServiceName)

    Write-Output ''
    Write-Output "2. Configure GitHub environment protection for ${GitHubEnvironmentName}:"
    Write-Output ("   - Create or update the '{0}' environment in repository settings." -f $GitHubEnvironmentName)
    Write-Output '   - Require reviewer approval for deployments.'
    Write-Output "   - Restrict deployment branches to 'main'."

    Write-Output ''
    Write-Output '3. Verify Azure OIDC configuration created by this deployment:'
    Write-Output '   - Issuer: https://token.actions.githubusercontent.com'
    Write-Output "   - Subject: $gitHubOidcFederatedSubject"
    Write-Output '   - Audience: api://AzureADTokenExchange'
    Write-Output ('   az identity federated-credential list --resource-group {0} --identity-name {1} --output table' -f $ResourceGroupName, $gitHubOidcIdentityName)

    Write-Output ''
    Write-Output '4. Optional: store a GitHub PAT in Key Vault for PAT-based app auth:'
    Write-Output ('   az keyvault secret set --vault-name {0} --name "{1}" --value "<your-github-pat>"' -f $keyVaultName, $GitHubTokenSecretName)

    Write-Output ''
    Write-Output '5. Trigger deployment using GitHub Actions:'
    Write-Output '   - Workflow: .github/workflows/cd.yml'
    Write-Output "   - Deployment URL: $appServiceUrl"
}
else {
    Write-Output ''
    Write-Output 'Next steps are hidden by default. Use -IncludeNextSteps to print setup guidance.'
}
