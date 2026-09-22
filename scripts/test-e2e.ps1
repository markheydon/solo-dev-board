# Run Playwright E2E tests (PAT mode by default).
# Usage: .\scripts\test-e2e.ps1 [-Release] [-Hosted] [extra dotnet test arguments]

param(
    [switch]$Release,
    [switch]$Hosted,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotnetTestArgs
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$config = if ($Release) { "Release" } else { "Debug" }
$e2eProject = Join-Path $repoRoot "tests/E2E/SoloDevBoard.E2E.Tests/SoloDevBoard.E2E.Tests.csproj"
$playwrightScript = Join-Path $repoRoot "tests/E2E/SoloDevBoard.E2E.Tests/bin/$config/net10.0/playwright.ps1"

Push-Location $repoRoot
try {
    dotnet build $e2eProject -c $config -p:RunE2ETests=true -p:SkipPlaywrightInstall=true
    pwsh $playwrightScript install chromium

    if ($Hosted) {
        $env:E2E_AUTH_MODE = "hosted"
        $filterArgs = @("--", "--filter-query", "/[(Category=E2E)&(AuthMode=Hosted)]")
    }
    else {
        $env:E2E_AUTH_MODE = "pat"
        $filterArgs = @("--", "--filter-query", "/[Category=E2E]")
    }

    dotnet test $e2eProject --no-build -c $config `
        -p:RunE2ETests=true `
        -p:SkipPlaywrightInstall=true `
        @filterArgs `
        @DotnetTestArgs
}
finally {
    Pop-Location
}
