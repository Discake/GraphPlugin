param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$NanoCadVersion = "24.1",

    [string]$NanoCadInstallDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$solutionPath = Join-Path $PSScriptRoot "GraphPlugin.slnx"
$vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswherePath)) {
    throw "vswhere.exe was not found at '$vswherePath'. Install Visual Studio Installer or run the build from a Visual Studio Developer PowerShell."
}

$msbuildPath = & $vswherePath `
    -latest `
    -products * `
    -requires Microsoft.Component.MSBuild `
    -find "MSBuild\**\Bin\MSBuild.exe" |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($msbuildPath)) {
    throw "MSBuild.exe was not found in the installed Visual Studio instances."
}

$msbuildArguments = @(
    $solutionPath,
    "/m",
    "/p:Platform=x64",
    "/p:Configuration=$Configuration",
    "/p:NanoCadVersion=$NanoCadVersion"
)

if (-not [string]::IsNullOrWhiteSpace($NanoCadInstallDir)) {
    $msbuildArguments += "/p:NanoCadInstallDir=$NanoCadInstallDir"
}

Write-Host "MSBuild: $msbuildPath"
Write-Host "Configuration: $Configuration | Platform: x64"

& $msbuildPath @msbuildArguments

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
