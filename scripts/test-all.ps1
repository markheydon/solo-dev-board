# Full CI parity: unit/component tests, then E2E PAT, then E2E hosted (sequential, fail fast).
# Usage: .\scripts\test-all.ps1 [-Release] [extra dotnet test arguments for unit phase]

param(
    [switch]$Release,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotnetTestArgs
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot

& (Join-Path $scriptDir "test-unit.ps1") @DotnetTestArgs
& (Join-Path $scriptDir "test-e2e.ps1") -Release:$Release
& (Join-Path $scriptDir "test-e2e.ps1") -Release:$Release -Hosted
