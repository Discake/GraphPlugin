param(
    [switch]$Check
)

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot

try {
    $csharpProjects = @(
        ".\src\GraphPlugin.Domain\GraphPlugin.Domain.csproj",
        ".\src\GraphPlugin.Application\GraphPlugin.Application.csproj",
        ".\src\GraphPlugin.Nanocad\GraphPlugin.Nanocad.csproj",
        ".\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj",
        ".\tests\GraphPlugin.Nanocad.IntegrationTests\GraphPlugin.Nanocad.IntegrationTests.csproj"
    )

    foreach ($project in $csharpProjects) {
        if ($Check) {
            Write-Host "Checking C# formatting: $project"
            & dotnet format $project whitespace --verify-no-changes
        }
        else {
            Write-Host "Formatting C#: $project"
            & dotnet format $project whitespace
        }

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet format failed for '$project' with exit code $LASTEXITCODE."
        }
    }

    $clangFormatPath = $null
    $clangFormatCommand = Get-Command clang-format -ErrorAction SilentlyContinue

    if ($clangFormatCommand) {
        $clangFormatPath = $clangFormatCommand.Source
    }
    else {
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

        if (Test-Path $vswhere) {
            $visualStudioPath = & $vswhere -latest -products * -property installationPath

            if ($visualStudioPath) {
                $clangCandidates = @(
                    (Join-Path $visualStudioPath "VC\Tools\Llvm\x64\bin\clang-format.exe"),
                    (Join-Path $visualStudioPath "VC\Tools\Llvm\bin\clang-format.exe")
                )

                $clangFormatPath =
                    $clangCandidates |
                    Where-Object { Test-Path $_ } |
                    Select-Object -First 1
            }
        }
    }

    if (-not $clangFormatPath) {
        throw @"
clang-format was not found.
Install LLVM or the Visual Studio component 'C++ Clang tools for Windows',
or add clang-format.exe to PATH.
"@
    }

    $nativeFiles =
        Get-ChildItem .\src\GraphPlugin.Native -Recurse -File |
        Where-Object { $_.Extension -in ".cpp", ".cxx", ".h", ".hpp" } |
        ForEach-Object { $_.FullName }

    if ($nativeFiles.Count -gt 0) {
        if ($Check) {
            Write-Host "Checking C++/CLI formatting with clang-format..."
            & $clangFormatPath --dry-run --Werror @nativeFiles
        }
        else {
            Write-Host "Formatting C++/CLI with clang-format..."
            & $clangFormatPath -i @nativeFiles
        }

        if ($LASTEXITCODE -ne 0) {
            throw "clang-format failed with exit code $LASTEXITCODE."
        }
    }

    if ($Check) {
        Write-Host "Formatting check completed successfully."
    }
    else {
        Write-Host "Formatting completed successfully."
    }
}
finally {
    Pop-Location
}
