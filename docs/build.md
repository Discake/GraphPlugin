# Сборка проекта

Решение рассчитано на .NET 6, x64 и nanoCAD. Версия nanoCAD по умолчанию — 24.1.

## Рекомендуемая сборка из командной строки

В обычном PowerShell `MSBuild.exe` не всегда присутствует в `PATH`, даже если Visual Studio успешно собирает проект. Поэтому рекомендуется использовать корневой скрипт:

```powershell
.\build.ps1
```

Сборка Release:

```powershell
.\build.ps1 -Configuration Release
```

Сборка для другой стандартно установленной версии nanoCAD:

```powershell
.\build.ps1 -NanoCadVersion 25.0
```

Можно также явно передать каталог установки:

```powershell
.\build.ps1 `
  -Configuration Release `
  -NanoCadInstallDir "D:\Nanosoft\nanoCAD x64 25.0"
```

`build.ps1` находит Visual Studio MSBuild через `vswhere.exe`, выполняет восстановление зависимостей (`/restore`) и собирает `GraphPlugin.slnx` для платформы x64.

После успешной сборки скрипт формирует готовый набор плагина в каталоге:

```text
artifacts\plugin\Debug
```

или:

```text
artifacts\plugin\Release
```

В набор входят:

- `GraphPlugin.Domain.dll`;
- `GraphPlugin.Application.dll`;
- `GraphPlugin.Nanocad.dll`;
- `GraphPlugin.Nanocad.IntegrationTests.dll`;
- `GraphPlugin.Native.dll`;
- `LOAD.txt`.

Все DLL должны находиться рядом. Через `NETLOAD` загружаются `GraphPlugin.Nanocad.dll` и `GraphPlugin.Native.dll`. Для запуска интеграционных тестов дополнительно загружается `GraphPlugin.Nanocad.IntegrationTests.dll`. `GraphPlugin.Domain.dll` и `GraphPlugin.Application.dll` являются зависимостями и отдельно не загружаются.

Чтобы собрать проект без формирования каталога `artifacts/plugin`, используйте:

```powershell
.\build.ps1 -SkipStage
```

Если решение уже собрано из Visual Studio, готовый набор можно сформировать отдельно:

```powershell
.\stage-plugin.ps1 -Configuration Debug
```

или:

```powershell
.\stage-plugin.ps1 -Configuration Release
```

## Сборка из Developer PowerShell for Visual Studio

Альтернативный вариант — открыть **Developer PowerShell for Visual Studio**. В этом окружении `msbuild` уже доступен:

```powershell
msbuild GraphPlugin.slnx /m /p:Platform=x64 /p:Configuration=Debug
```

Из обычного PowerShell путь к MSBuild можно определить вручную:

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

`dotnet build` не является основным способом сборки всего решения, потому что `GraphPlugin.Native` — C++/CLI-проект, для которого используется Visual Studio MSBuild.

## Параметры установки nanoCAD

Если параметры не переопределены, проект ищет nanoCAD по пути:

```text
$(ProgramFiles)\Nanosoft\nanoCAD x64 24.1
```

Путь можно передать напрямую в MSBuild:

```powershell
msbuild GraphPlugin.slnx `
  /p:Platform=x64 `
  /p:Configuration=Debug `
  /p:NanoCadInstallDir="D:\Nanosoft\nanoCAD x64 25.0"
```

Если nanoCAD установлен в стандартный каталог Nanosoft, достаточно переопределить только версию:

```powershell
msbuild GraphPlugin.slnx `
  /p:Platform=x64 `
  /p:Configuration=Debug `
  /p:NanoCadVersion=25.0
```

Проекты `GraphPlugin.Nanocad`, `GraphPlugin.Nanocad.IntegrationTests` и `GraphPlugin.Native` используют один и тот же параметр `NanoCadInstallDir` для ссылок на `hostmgd.dll` и `hostdbmgd.dll`.

## Нативный проект

`GraphPlugin.Native` намеренно собирается только для x64 и использует:

- .NET 6;
- C++/CLI с `CLRSupport=NetCore`;
- MSVC toolset `v143`;
- C++17.

`GraphPlugin.Native.dll` загружается в nanoCAD через `NETLOAD`, как и управляемые DLL плагина.

## Рекомендуемая проверка после сборки

После изменений рекомендуется выполнить:

```powershell
.\build.ps1
dotnet test .\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj
```

Затем перезапустить nanoCAD, загрузить свежие DLL из `artifacts/plugin/<Configuration>` и выполнить:

```text
GRAPHTESTS
```

Для проверки сохранения и повторного открытия DWG дополнительно используется двухэтапный сценарий `GRAPHTESTS_PERSISTENCE`; он описан в [testing.md](testing.md).
