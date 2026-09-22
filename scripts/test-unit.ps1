# Run unit and component tests (excludes Playwright E2E).
# Usage: .\scripts\test-unit.ps1 [dotnet test arguments]

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotnetTestArgs
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    dotnet test SoloDevBoard.UnitTests.slnf @DotnetTestArgs
}
finally {
    Pop-Location
}
