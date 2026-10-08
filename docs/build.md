# Build configuration

The solution targets .NET 6 and x64 nanoCAD. The default nanoCAD version is 24.1.

## Recommended command-line build

Regular PowerShell does not always have `MSBuild.exe` on `PATH`, even when Visual Studio can build the solution successfully. Use the repository build launcher instead:

```powershell
.\build.ps1
```

Release build:

```powershell
.\build.ps1 -Configuration Release
```

Build against another standard nanoCAD version:

```powershell
.\build.ps1 -NanoCadVersion 25.0
```

Or pass the installation directory explicitly:

```powershell
.\build.ps1 `
  -Configuration Release `
  -NanoCadInstallDir "D:\Nanosoft\nanoCAD x64 25.0"
```

`build.ps1` uses `vswhere.exe` from Visual Studio Installer to locate the MSBuild installation, restores packages with MSBuild `/restore`, and then builds `GraphPlugin.slnx` as `x64`.

After a successful build it also stages the plugin bundle into:

```text
artifacts\plugin\Debug
```

or:

```text
artifacts\plugin\Release
```

The bundle contains:

- `GraphPlugin.Domain.dll`
- `GraphPlugin.Application.dll`
- `GraphPlugin.Nanocad.dll`
- `GraphPlugin.Nanocad.IntegrationTests.dll`
- `GraphPlugin.Native.dll`
- `LOAD.txt`

Keep all DLLs together. Load `GraphPlugin.Nanocad.dll` and `GraphPlugin.Native.dll` with `NETLOAD`. Load `GraphPlugin.Nanocad.IntegrationTests.dll` as well when integration-test commands are needed. `GraphPlugin.Domain.dll` and `GraphPlugin.Application.dll` are dependencies and are not loaded directly.

To build without staging the bundle:

```powershell
.\build.ps1 -SkipStage
```

If the solution was built from Visual Studio, the same bundle can be created without rebuilding:

```powershell
.\stage-plugin.ps1 -Configuration Debug
```

or:

```powershell
.\stage-plugin.ps1 -Configuration Release
```

## Visual Studio Developer PowerShell

An alternative is to open **Developer PowerShell for Visual Studio**. That shell initializes the Visual Studio build environment, so `msbuild` can be invoked directly:

```powershell
msbuild GraphPlugin.slnx /m /p:Platform=x64 /p:Configuration=Debug
```

From an ordinary PowerShell session, the same MSBuild path can be resolved manually:

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere `
  -latest `
  -products * `
  -requires Microsoft.Component.MSBuild `
  -find "MSBuild\**\Bin\MSBuild.exe" |
  Select-Object -First 1

& $msbuild GraphPlugin.slnx /m /p:Platform=x64 /p:Configuration=Debug
```

`dotnet build` is not the canonical build command for this solution because `GraphPlugin.Native` is a C++/CLI project and is built by Visual Studio MSBuild.

## nanoCAD installation properties

The default install directory is resolved as:

```text
$(ProgramFiles)\Nanosoft\nanoCAD x64 24.1
```

The MSBuild properties can also be supplied directly:

```powershell
msbuild GraphPlugin.slnx `
  /p:Platform=x64 `
  /p:Configuration=Debug `
  /p:NanoCadInstallDir="D:\Nanosoft\nanoCAD x64 25.0"
```

Or override only the version when nanoCAD is installed under the standard Nanosoft directory:

```powershell
msbuild GraphPlugin.slnx `
  /p:Platform=x64 `
  /p:Configuration=Debug `
  /p:NanoCadVersion=25.0
```

`GraphPlugin.Nanocad`, `GraphPlugin.Nanocad.IntegrationTests`, and `GraphPlugin.Native` all use the same `NanoCadInstallDir` property for `hostmgd.dll` and `hostdbmgd.dll`.

## Native project

`GraphPlugin.Native` is intentionally x64-only and uses:

- .NET 6
- C++/CLI with `CLRSupport=NetCore`
- MSVC toolset `v143`
- C++17

The native DLL is loaded through `NETLOAD`, just like the managed nanoCAD plugin DLLs.
