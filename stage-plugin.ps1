param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot "artifacts\plugin\$Configuration"
}

function Resolve-BuiltAssembly {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectDirectory,

        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    $binDirectory = Join-Path $ProjectDirectory "bin"

    if (-not (Test-Path $binDirectory)) {
        throw "Build output directory '$binDirectory' does not exist. Build the solution first."
    }

    $configurationSegment =
        [IO.Path]::DirectorySeparatorChar +
        $Configuration +
        [IO.Path]::DirectorySeparatorChar

    $match = Get-ChildItem `
        -Path $binDirectory `
        -Filter $FileName `
        -File `
        -Recurse |
        Where-Object {
            $_.FullName.IndexOf(
                $configurationSegment,
                [StringComparison]::OrdinalIgnoreCase) -ge 0
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $match) {
        throw "Built assembly '$FileName' for configuration '$Configuration' was not found under '$binDirectory'."
    }

    return $match.FullName
}

function Resolve-NativeAssembly {
    $fileName = "GraphPlugin.Native.dll"

    $candidates = @(
        (Join-Path $PSScriptRoot "x64\$Configuration\$fileName"),
        (Join-Path $PSScriptRoot "src\GraphPlugin.Native\x64\$Configuration\$fileName"),
        (Join-Path $PSScriptRoot "src\GraphPlugin.Native\bin\$Configuration\$fileName")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $configurationSegment =
        [IO.Path]::DirectorySeparatorChar +
        $Configuration +
        [IO.Path]::DirectorySeparatorChar

    $artifactsRoot = Join-Path $PSScriptRoot "artifacts"

    $match = Get-ChildItem `
        -Path $PSScriptRoot `
        -Filter $fileName `
        -File `
        -Recurse |
        Where-Object {
            $_.FullName.IndexOf(
                $configurationSegment,
                [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
            -not $_.FullName.StartsWith(
                $artifactsRoot,
                [StringComparison]::OrdinalIgnoreCase)
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $match) {
        throw "Built native assembly '$fileName' for configuration '$Configuration' was not found."
    }

    return $match.FullName
}

$assemblies = @(
    @{
        Name = "GraphPlugin.Domain.dll"
        Source = Resolve-BuiltAssembly `
            -ProjectDirectory (Join-Path $PSScriptRoot "src\GraphPlugin.Domain") `
            -FileName "GraphPlugin.Domain.dll"
    },
    @{
        Name = "GraphPlugin.Application.dll"
        Source = Resolve-BuiltAssembly `
            -ProjectDirectory (Join-Path $PSScriptRoot "src\GraphPlugin.Application") `
            -FileName "GraphPlugin.Application.dll"
    },
    @{
        Name = "GraphPlugin.Nanocad.dll"
        Source = Resolve-BuiltAssembly `
            -ProjectDirectory (Join-Path $PSScriptRoot "src\GraphPlugin.Nanocad") `
            -FileName "GraphPlugin.Nanocad.dll"
    },
    @{
        Name = "GraphPlugin.Nanocad.IntegrationTests.dll"
        Source = Resolve-BuiltAssembly `
            -ProjectDirectory (Join-Path $PSScriptRoot "tests\GraphPlugin.Nanocad.IntegrationTests") `
            -FileName "GraphPlugin.Nanocad.IntegrationTests.dll"
    },
    @{
        Name = "GraphPlugin.Native.dll"
        Source = Resolve-NativeAssembly
    }
)

if (Test-Path $OutputDirectory) {
    Remove-Item $OutputDirectory -Recurse -Force
}

New-Item `
    -ItemType Directory `
    -Path $OutputDirectory `
    -Force | Out-Null

foreach ($assembly in $assemblies) {
    Copy-Item `
        -LiteralPath $assembly.Source `
        -Destination (Join-Path $OutputDirectory $assembly.Name) `
        -Force
}

$loadInstructions = @"
GraphPlugin $Configuration bundle
Target nanoCAD: x64 24.1

Keep all DLL files in this directory together.

Load into nanoCAD with NETLOAD:
1. GraphPlugin.Nanocad.dll
2. GraphPlugin.Native.dll

After loading GraphPlugin.Nanocad.dll, run GRAPHCONTROL to open the main GraphPlugin Control Center.
The same actions remain available as GRAPH* commands; see README.md in the repository for the UI-to-command mapping.

Optional integration-test commands:
3. GraphPlugin.Nanocad.IntegrationTests.dll

GraphPlugin.Application.dll and GraphPlugin.Domain.dll are dependencies and should remain next to the loadable assemblies.
"@

Set-Content `
    -LiteralPath (Join-Path $OutputDirectory "LOAD.txt") `
    -Value $loadInstructions `
    -Encoding UTF8

Write-Host ""
Write-Host "Plugin bundle created: $OutputDirectory"

Get-ChildItem `
    -Path $OutputDirectory `
    -Filter "*.dll" `
    -File |
    Sort-Object Name |
    ForEach-Object {
        Write-Host "  $($_.Name)"
    }
