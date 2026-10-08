param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$NanoCadVersion = "24.1",

    [string]$NanoCadInstallDir,

    [switch]$SkipStage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$solutionPath = Join-Path $PSScriptRoot "GraphPlugin.slnx"
$vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

$namespaceCheckRoots = @(
    (Join-Path $PSScriptRoot "src"),
    (Join-Path $PSScriptRoot "tests")
)

$legacyNamespaceMatches =
    Get-ChildItem `
        -Path $namespaceCheckRoots `
        -Filter "*.cs" `
        -File `
        -Recurse |
    Select-String -Pattern "GraphPlugin\.NanoCad"

if ($legacyNamespaceMatches) {
    Write-Host "Legacy GraphPlugin.NanoCad namespace references were found:"

    foreach ($match in $legacyNamespaceMatches) {
        $relativePath =
            [IO.Path]::GetRelativePath(
                $PSScriptRoot,
                $match.Path)

        Write-Host "  $relativePath`:$($match.LineNumber): $($match.Line.Trim())"
    }

    throw "Use the canonical GraphPlugin.Nanocad namespace before building."
}

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

if (-not $SkipStage) {
    $stageScript = Join-Path $PSScriptRoot "stage-plugin.ps1"

    & $stageScript -Configuration $Configuration
}
