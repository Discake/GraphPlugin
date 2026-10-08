# Build configuration

The solution targets .NET 6 and x64 nanoCAD. The default nanoCAD version is 24.1.

## Default build

If nanoCAD is installed to the standard location, no extra properties are required:

```powershell
msbuild GraphPlugin.slnx /p:Platform=x64 /p:Configuration=Debug
```

The default install directory is resolved as:

```text
$(ProgramFiles)\Nanosoft\nanoCAD x64 24.1
```

## Building against another nanoCAD installation

Override the installation directory explicitly:

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
