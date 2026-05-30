param(
    [string]$Version = "v2.2",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$versionLabel = $Version.Trim()

if ([string]::IsNullOrWhiteSpace($versionLabel)) {
    throw "Version must not be empty."
}

if (-not $versionLabel.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $versionLabel = "v$versionLabel"
}

$semanticVersion = $versionLabel.Substring(1)

if (-not ($semanticVersion -match '^\d+\.\d+(\.\d+)?$')) {
    throw "Version must look like v2.2 or v2.2.0. Actual: $Version"
}

$versionPartCount = $semanticVersion.Split('.').Count
$assemblyFileVersion = if ($versionPartCount -eq 2) {
    "$semanticVersion.0.0"
}
else {
    "$semanticVersion.0"
}

$solutionPath = Join-Path $repoRoot "AdaptiveSpritesDMItool.sln"
$projectPath = Join-Path $repoRoot "src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj"
$artifactName = "AdaptiveSpritesDMItool-$versionLabel-$Runtime"
$samplesArtifactName = "AdaptiveSpritesDMItool-samples-$versionLabel"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish"
$releaseRoot = Join-Path $artifactsRoot "release"
$smokeRoot = Join-Path $artifactsRoot "smoke"
$samplesSource = Join-Path $repoRoot "samples"
$publishDir = Join-Path $publishRoot $artifactName
$zipPath = Join-Path $releaseRoot "$artifactName.zip"
$shaPath = Join-Path $releaseRoot "$artifactName.sha256.txt"
$samplesZipPath = Join-Path $releaseRoot "$samplesArtifactName.zip"
$samplesShaPath = Join-Path $releaseRoot "$samplesArtifactName.sha256.txt"
$smokeDir = Join-Path $smokeRoot $artifactName
$samplesSmokeDir = Join-Path $smokeRoot $samplesArtifactName
$publishedExeName = "AdaptiveSpritesDmiTool.Presentation.Wpf.exe"
$releaseExeName = "AdaptiveDMITool-$versionLabel.exe"

function Assert-UnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $rootWithSeparator = "$fullRoot$([System.IO.Path]::DirectorySeparatorChar)"

    if (-not $fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase) -and
        -not [string]::Equals($fullPath, $fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside artifact root. Path: $fullPath Root: $fullRoot"
    }
}

function Remove-PathIfExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    Assert-UnderRoot -Path $Path -Root $Root

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$Arguments = @()
    )

    Write-Host ""
    Write-Host "==> $Name"
    & $FilePath @Arguments

    $lastExitCodeVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue

    if ($lastExitCodeVariable -and $lastExitCodeVariable.Value -ne 0) {
        throw "$Name failed with exit code $($lastExitCodeVariable.Value)."
    }
}

New-Item -ItemType Directory -Force -Path $publishRoot, $releaseRoot, $smokeRoot | Out-Null

Remove-PathIfExists -Path $publishDir -Root $publishRoot
Remove-PathIfExists -Path $zipPath -Root $releaseRoot
Remove-PathIfExists -Path $shaPath -Root $releaseRoot
Remove-PathIfExists -Path $samplesZipPath -Root $releaseRoot
Remove-PathIfExists -Path $samplesShaPath -Root $releaseRoot
Remove-PathIfExists -Path $smokeDir -Root $smokeRoot
Remove-PathIfExists -Path $samplesSmokeDir -Root $smokeRoot

Write-Host "Building $artifactName from $repoRoot"

if (-not (Test-Path -LiteralPath $samplesSource)) {
    throw "Samples source directory was not found: $samplesSource"
}

Invoke-External -Name "Hidden Unicode scan" -FilePath (Join-Path $PSScriptRoot "check-hidden-unicode.ps1")
Invoke-External -Name "Restore" -FilePath "dotnet" -Arguments @("restore", $solutionPath, "-m:1")
Invoke-External -Name "Build" -FilePath "dotnet" -Arguments @("build", $solutionPath, "-c", $Configuration, "-m:1", "-v", "minimal", "--no-restore")
Invoke-External -Name "Test" -FilePath "dotnet" -Arguments @("test", $solutionPath, "-c", $Configuration, "-m:1", "-v", "minimal", "--no-build")
Invoke-External -Name "Restore publish runtime" -FilePath "dotnet" -Arguments @("restore", $projectPath, "-r", $Runtime, "-m:1")
Invoke-External -Name "Publish" -FilePath "dotnet" -Arguments @(
    "publish",
    $projectPath,
    "-c",
    $Configuration,
    "-r",
    $Runtime,
    "-m:1",
    "--self-contained",
    "true",
    "--no-restore",
    "-o",
    $publishDir,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=embedded",
    "-p:Version=$semanticVersion",
    "-p:AssemblyVersion=$assemblyFileVersion",
    "-p:FileVersion=$assemblyFileVersion",
    "-p:InformationalVersion=$semanticVersion"
)

$publishedExe = Join-Path $publishDir $publishedExeName

if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published executable was not found: $publishedExe"
}

Write-Host ""
Write-Host "==> Rename release executable"
$releaseExePath = Join-Path $publishDir $releaseExeName
if (Test-Path -LiteralPath $releaseExePath) {
    Remove-Item -LiteralPath $releaseExePath -Force
}

Rename-Item -LiteralPath $publishedExe -NewName $releaseExeName

Write-Host ""
Write-Host "==> Package ZIP"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "==> Package samples ZIP"
Compress-Archive -Path $samplesSource -DestinationPath $samplesZipPath -Force

Write-Host ""
Write-Host "==> SHA256"
$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
"$($hash.Hash)  $(Split-Path -Leaf $zipPath)" | Set-Content -LiteralPath $shaPath -Encoding ASCII
$samplesHash = Get-FileHash -LiteralPath $samplesZipPath -Algorithm SHA256
"$($samplesHash.Hash)  $(Split-Path -Leaf $samplesZipPath)" | Set-Content -LiteralPath $samplesShaPath -Encoding ASCII

Write-Host ""
Write-Host "==> ZIP smoke"
New-Item -ItemType Directory -Force -Path $smokeDir | Out-Null
Expand-Archive -LiteralPath $zipPath -DestinationPath $smokeDir -Force

$smokeExe = Join-Path $smokeDir $releaseExeName

if (-not (Test-Path -LiteralPath $smokeExe)) {
    throw "Smoke executable was not found after ZIP extraction: $smokeExe"
}

Write-Host ""
Write-Host "==> Samples ZIP smoke"
New-Item -ItemType Directory -Force -Path $samplesSmokeDir | Out-Null
Expand-Archive -LiteralPath $samplesZipPath -DestinationPath $samplesSmokeDir -Force

$smokeSamples = Join-Path $samplesSmokeDir "samples"

if (-not (Test-Path -LiteralPath $smokeSamples)) {
    throw "Smoke samples directory was not found after ZIP extraction: $smokeSamples"
}

$sampleFileCount = @(Get-ChildItem -LiteralPath $smokeSamples -File -Recurse).Count

if ($sampleFileCount -eq 0) {
    throw "Smoke samples directory did not contain any files: $smokeSamples"
}

Write-Host ""
Write-Host "Release package created:"
Write-Host "  ZIP:          $zipPath"
Write-Host "  SHA256:       $shaPath"
Write-Host "  Samples ZIP:  $samplesZipPath"
Write-Host "  Samples SHA:  $samplesShaPath"
Write-Host "  Smoke:        $smokeDir"
Write-Host "  Samples smoke: $samplesSmokeDir"
