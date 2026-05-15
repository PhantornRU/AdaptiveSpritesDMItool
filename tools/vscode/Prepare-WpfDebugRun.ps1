param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot,

    [string]$Configuration = "Debug"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectName = "AdaptiveSpritesDmiTool.Presentation.Wpf"
$buildOutput = Join-Path $WorkspaceRoot "src/$projectName/bin/$Configuration/net8.0-windows"
$programPath = Join-Path $buildOutput "$projectName.dll"

if (-not (Test-Path $programPath)) {
    throw "Build output was not found: $programPath"
}

$debugRoot = Join-Path $WorkspaceRoot ".vscode/.debug"
$runsRoot = Join-Path $debugRoot "runs"
$currentLink = Join-Path $debugRoot "current"
$runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$stagedRun = Join-Path $runsRoot $runId

New-Item -ItemType Directory -Force -Path $stagedRun | Out-Null
Copy-Item -Path (Join-Path $buildOutput "*") -Destination $stagedRun -Recurse -Force

if (Test-Path $currentLink) {
    Remove-Item -LiteralPath $currentLink -Force -Recurse
}

New-Item -ItemType Junction -Path $currentLink -Target $stagedRun | Out-Null

$staleRuns = Get-ChildItem -Path $runsRoot -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -Skip 5

foreach ($staleRun in $staleRuns) {
    try {
        Remove-Item -LiteralPath $staleRun.FullName -Force -Recurse
    }
    catch {
        Write-Host "Skipping locked debug run cache: $($staleRun.FullName)"
    }
}

Write-Host "Prepared WPF debug run at $stagedRun"
