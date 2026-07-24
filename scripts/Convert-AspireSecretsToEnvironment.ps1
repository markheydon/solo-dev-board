#Requires -Version 5.1

<#
.SYNOPSIS
    Maps local Aspire AppHost user secrets to `aspire deploy` environment variables.

.DESCRIPTION
    Reads values stored via `aspire secret set` and sets the corresponding process
    environment variables expected by `aspire deploy` (for example,
    `Parameters:gh-pat` becomes `Parameters__gh_pat`).

    Dot-source this script in your current PowerShell session so the variables
    persist for a subsequent deploy command:

        . ./scripts/Convert-AspireSecretsToEnvironment.ps1
        aspire deploy --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj --environment Development

    Secret values are never written to the console.

.PARAMETER AppHost
    Path to the AppHost project file, relative to the repository root.

.PARAMETER AspireCli
    Path to the Aspire CLI executable. When omitted, the script checks `aspire` on
    PATH and then the default install location under the user profile.

.PARAMETER ResourceGroup
    Azure resource group used when `Azure:ResourceGroup` is not stored in aspire
    secrets. Pass an empty string to leave `Azure__ResourceGroup` unset.

.PARAMETER PassThru
    Returns a summary object listing which environment variables were set, skipped,
    or missing (names only).

.EXAMPLE
    . ./scripts/Convert-AspireSecretsToEnvironment.ps1

.EXAMPLE
    . ./scripts/Convert-AspireSecretsToEnvironment.ps1 -ResourceGroup 'rg-solodevboard-dev' -Verbose
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string] $AppHost = 'src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj',

    [Parameter()]
    [string] $AspireCli,

    [Parameter()]
    [string] $ResourceGroup = 'rg-solodevboard-prod',

    [Parameter()]
    [switch] $PassThru
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$AppHostPath = Join-Path $RepoRoot $AppHost

if (-not (Test-Path -LiteralPath $AppHostPath)) {
    throw "AppHost project not found at '$AppHostPath'. Run this script from the repository root or pass -AppHost."
}

function Resolve-AspireCliPath {
    param([string] $ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "Aspire CLI not found at '$ExplicitPath'."
        }

        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $command = Get-Command aspire -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $defaultPath = Join-Path $env:USERPROFILE '.aspire\bin\aspire.exe'
    if (Test-Path -LiteralPath $defaultPath) {
        return (Resolve-Path -LiteralPath $defaultPath).Path
    }

    throw @(
        'Aspire CLI not found. Install it from https://aspire.dev or pass -AspireCli.'
        'Checked: PATH, and $env:USERPROFILE\.aspire\bin\aspire.exe'
    ) -join ' '
}

function Get-AspireSecretValue {
    param(
        [Parameter(Mandatory)]
        [string] $Key,

        [Parameter(Mandatory)]
        [string] $CliPath,

        [Parameter(Mandatory)]
        [string] $AppHostProject
    )

    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'

    try {
        $output = & $CliPath secret get $Key --apphost $AppHostProject --nologo --non-interactive 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        $value = ($output | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($value)) {
            return $null
        }

        return $value
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

# Aspire secret key -> environment variable name for aspire deploy.
# Keep in sync with src/SoloDevBoard.AppHost/AppHost.cs and .github/workflows/cd.yml.
$SecretMappings = [ordered]@{
    'Azure:SubscriptionId'              = 'Azure__SubscriptionId'
    'Azure:Location'                    = 'Azure__Location'
    'Azure:TenantId'                    = 'Azure__TenantId'
    'Azure:ResourceGroup'               = 'Azure__ResourceGroup'
    'Parameters:hosted-sign-in-enabled' = 'Parameters__hosted_sign_in_enabled'
    'Parameters:hosted-admission-enabled' = 'Parameters__hosted_admission_enabled'
    'Parameters:gh-pat'                 = 'Parameters__gh_pat'
    'Parameters:gh-app-client-id'       = 'Parameters__gh_app_client_id'
    'Parameters:gh-app-client-secret'   = 'Parameters__gh_app_client_secret'
    'Parameters:allowed-user-logins'    = 'Parameters__allowed_user_logins'
    'Parameters:allowed-org-logins'     = 'Parameters__allowed_org_logins'
}

$cliPath = Resolve-AspireCliPath -ExplicitPath $AspireCli
$setNames = @()
$skippedNames = @()

Write-Verbose "Using Aspire CLI: $cliPath"
Write-Verbose "Using AppHost: $AppHostPath"

foreach ($mapping in $SecretMappings.GetEnumerator()) {
    $secretKey = $mapping.Key
    $environmentName = $mapping.Value
    $value = Get-AspireSecretValue -Key $secretKey -CliPath $cliPath -AppHostProject $AppHostPath

    if ($environmentName -eq 'Azure__ResourceGroup' -and [string]::IsNullOrWhiteSpace($value)) {
        $value = $ResourceGroup
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        $skippedNames += $environmentName
        Write-Verbose "Skipped '$environmentName' (no aspire secret value)."
        continue
    }

    Set-Item -Path "Env:$environmentName" -Value $value
    $setNames += $environmentName
    Write-Verbose "Set '$environmentName'."
}

$requiredForDeploy = @(
    'Azure__SubscriptionId',
    'Azure__Location',
    'Azure__ResourceGroup',
    'Parameters__gh_pat',
    'Parameters__gh_app_client_secret'
)

$missingRequired = @($requiredForDeploy | Where-Object { $_ -notin $setNames })

Write-Host "Loaded $($setNames.Count) environment variable(s) from aspire secrets."

if ($missingRequired.Count -gt 0) {
    Write-Warning @(
        'The following deploy inputs are still unset:',
        ($missingRequired -join ', '),
        'Set them with aspire secret set, pass -ResourceGroup, or assign $env: variables before running aspire deploy.'
    ) -join ' '
}

if ($PassThru) {
    [pscustomobject]@{
        Set     = @($setNames)
        Skipped = @($skippedNames)
        Missing = @($missingRequired)
    }
}
