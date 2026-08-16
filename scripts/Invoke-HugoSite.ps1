# Build or preview the Hugo public product site via Podman/Docker
# (no local Hugo or Go install required).
# Usage: .\scripts\Invoke-HugoSite.ps1 build|serve|preview

param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "serve", "preview")]
    [string]$Command = "build",

    [string]$Runtime = "podman",
    [int]$ServePort = 1313,
    [int]$PreviewPort = 8080
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$rootForMount = $repoRoot -replace '\\', '/'
$siteDir = Join-Path $repoRoot "website"
$hugoImage = "docker.io/hugomods/hugo:latest"

function Test-ContainerRuntime {
    <#
    .SYNOPSIS
        Verifies that the requested container runtime is available on PATH.
    #>
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Error "Container runtime '$Name' not found. Install Podman Desktop or Docker Desktop, or pass -Runtime docker."
    }
}

function Invoke-HugoBuild {
    <#
    .SYNOPSIS
        Builds the Hugo site into website/public using a containerised Hugo runtime.
    #>
    & $Runtime run --rm `
        -v "${rootForMount}:/src:Z" `
        -w /src/website `
        $hugoImage `
        hugo --minify
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Hugo build failed (exit $LASTEXITCODE). Fix errors above before previewing."
    }
    $publicPath = Join-Path $siteDir "public"
    if (-not (Test-Path (Join-Path $publicPath "index.html"))) {
        Write-Error "Hugo did not produce website/public/index.html."
    }
}

Test-ContainerRuntime -Name $Runtime

if (-not (Test-Path $siteDir)) {
    Write-Error "Hugo site directory not found at $siteDir."
}

switch ($Command) {
    "build" {
        Write-Host "Building Hugo site to website/public..." -ForegroundColor Cyan
        Invoke-HugoBuild
        Write-Host "Done. Output: $repoRoot\website\public" -ForegroundColor Green
    }

    "serve" {
        Write-Host "Starting Hugo dev server at http://localhost:$ServePort ..." -ForegroundColor Cyan
        & $Runtime run --rm -p "${ServePort}:1313" `
            -v "${rootForMount}:/src:Z" `
            -w /src/website `
            $hugoImage `
            hugo server --bind 0.0.0.0 --baseURL "http://localhost:$ServePort/"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    "preview" {
        Write-Host "Building Hugo site..." -ForegroundColor Cyan
        Invoke-HugoBuild
        Write-Host "Serving website/public at http://localhost:$PreviewPort ..." -ForegroundColor Cyan
        & $Runtime run --rm -p "${PreviewPort}:80" `
            -v "${rootForMount}/website/public:/usr/share/nginx/html:ro,Z" `
            docker.io/library/nginx:alpine
    }
}
