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

if ([string]::IsNullOrWhiteSpace($NanoCadInstallDir)) {
    $NanoCadInstallDir = Join-Path $env:ProgramFiles "Nanosoft\nanoCAD x64 $NanoCadVersion"
}

$hostDbMgdPath = Join-Path $NanoCadInstallDir "bin\hostdbmgd.dll"
$hostMgdPath = Join-Path $NanoCadInstallDir "bin\hostmgd.dll"

if (-not (Test-Path $hostDbMgdPath)) {
    throw "nanoCAD hostdbmgd.dll was not found at '$hostDbMgdPath'. Pass -NanoCadInstallDir with the nanoCAD installation directory."
}

if (-not (Test-Path $hostMgdPath)) {
    throw "nanoCAD hostmgd.dll was not found at '$hostMgdPath'. Pass -NanoCadInstallDir with the nanoCAD installation directory."
}

$msbuildArguments = @(
    $solutionPath,
    "/restore",
    "/m",
    "/p:Platform=x64",
    "/p:Configuration=$Configuration",
    "/p:NanoCadVersion=$NanoCadVersion",
    "/p:NanoCadInstallDir=$NanoCadInstallDir"
)

Write-Host "MSBuild: $msbuildPath"
Write-Host "Configuration: $Configuration | Platform: x64"
Write-Host "nanoCAD: $NanoCadInstallDir"
Write-Host "NuGet restore: MSBuild /restore"

& $msbuildPath @msbuildArguments

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
