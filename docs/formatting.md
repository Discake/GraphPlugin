# Форматирование исходного кода

В репозитории используются два форматтера:

- **CSharpier** — для C#;
- **clang-format** — для C++/CLI.

Целевая ширина строки — около **120 символов**. Это не жёсткий запрет, но короткие выражения не должны искусственно раскладываться на множество строк.

## Почему не `dotnet format`

`dotnet format` хорошо применяет Roslyn-правила и анализаторы, но обычно сохраняет многие уже существующие ручные переносы строк. Для этого проекта этого недостаточно: значительная часть C#-кода ранее была разбита слишком вертикально.

CSharpier повторно печатает синтаксическое дерево и поэтому лучше подходит для одноразового выравнивания всего проекта и дальнейшего поддержания единого стиля.

`dotnet format` можно использовать отдельно для анализаторов и code-style fixes, но он не является основным formatter'ом исходников в этом репозитории.

## Конфигурация

В корне репозитория находятся:

```text
.editorconfig
.csharpierrc.json
.csharpierignore
.clang-format
.config/dotnet-tools.json
format.ps1
```

### C#

CSharpier закреплён как локальный .NET tool версии `1.3.0` в `.config/dotnet-tools.json`.

`.csharpierrc.json` задаёт:

- ширину печати 120 символов;
- 4 пробела на уровень отступа;
- пробелы вместо tab;
- сохранение типа перевода строк существующего файла.

`.csharpierignore` исключает `bin`, `obj`, `artifacts` и XML/MSBuild-файлы: CSharpier используется только для `.cs`.

### C++/CLI

`.clang-format` основан на Microsoft style и задаёт:

- C++17;
- 4 пробела;
- Allman braces;
- ширину строки 120 символов;
- запрет агрессивного bin-packing аргументов и параметров.

## Быстрый запуск

Из корня репозитория:

```powershell
.\format.ps1
```

Скрипт:

1. восстанавливает локальный CSharpier через `dotnet tool restore`;
2. форматирует C#;
3. находит `clang-format` в `PATH` или в установленной Visual Studio;
4. форматирует `.cpp`, `.cxx`, `.h`, `.hpp` в `src/GraphPlugin.Native`.

## Только проверка без изменения файлов

```powershell
.\format.ps1 -Check
```

Для C# используется `csharpier check`, для C++ — `clang-format --dry-run --Werror`.

Эта команда подходит для локальной проверки перед commit/PR и позже может быть перенесена в CI.

## Первый запуск

CSharpier устанавливать глобально не требуется. Достаточно:

```powershell
dotnet tool restore
```

Для C++ проверьте доступность clang-format:

```powershell
clang-format --version
```

Если команда не найдена, установите LLVM или компонент Visual Studio **C++ Clang tools for Windows**. `format.ps1` также пытается найти `clang-format.exe` внутри последней установленной Visual Studio через `vswhere.exe`.

## Ручной запуск CSharpier

Форматирование:

```powershell
dotnet csharpier format .
```

Проверка:

```powershell
dotnet csharpier check .
```

Благодаря `.csharpierignore` команда не изменяет `.csproj`, `.slnx`, `.vcxproj` и другие XML/MSBuild-файлы.

## Ручной запуск clang-format

Для одного файла:

```powershell
clang-format -i .\src\GraphPlugin.Native\Commands\GraphCommands.cpp
```

Для всей native-части удобнее использовать `format.ps1`.

## После массового форматирования

После первого форматирования всего репозитория рекомендуется проверить diff и затем выполнить:

```powershell
.\build.ps1
dotnet test .\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj
```

После успешной сборки — перезапустить nanoCAD и выполнить:

```text
GRAPHTESTS
```

Форматирование должно быть отдельным commit без изменений поведения. Это сильно упрощает review: большой diff можно однозначно рассматривать как механический.

## Стиль, к которому стремимся

Предпочтительно:

```csharp
var vertexService = new VertexService(vertexRepository, edgeRepository);

context.VertexService.CreateVertex(
    new Point2(point.X, point.Y),
    shape);
```

Вместо искусственно вертикального варианта:

```csharp
var vertexService =
    new VertexService(
        vertexRepository,
        edgeRepository);
```

Переносы должны появляться из-за реальной длины выражения и структуры кода, а не после каждого оператора или каждого аргумента.
