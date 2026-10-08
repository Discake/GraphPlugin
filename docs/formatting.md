# Форматирование исходного кода

В репозитории используются два форматтера:

- **dotnet format** — для C#;
- **clang-format** — для C++/CLI.

Целевая ширина строки — около **120 символов**. Для C# это ориентир из `.editorconfig`: Roslyn не выполняет агрессивный перенос длинных выражений, зато сохраняет намеренные пустые строки и не переформатирует код так жёстко, как CSharpier.

## Почему `dotnet format`

После первоначального выравнивания C#-кода важно сохранить ручное смысловое разделение блоков пустыми строками. `dotnet format whitespace` исправляет отступы, пробелы и базовое форматирование Roslyn, но не перепечатывает всё синтаксическое дерево заново.

Это позволяет придерживаться такого стиля:

```csharp
var context = PluginServices.CurrentContext;
var document = context.Document;
var editor = document.Editor;

editor.WriteMessage("\nПостроение графа.");
```

При этом formatter не умеет определять смысловые группы локальных переменных и автоматически вставлять пустую строку между ними и следующим действием. Такие пустые строки считаются частью читаемого исходного кода и должны сохраняться при последующих запусках formatter.

## Конфигурация

В корне репозитория находятся:

```text
.editorconfig
.clang-format
format.ps1
```

### C#

`.editorconfig` задаёт основные правила C# и ширину строки 120 символов.

Для C# используется встроенная команда .NET SDK:

```text
dotnet format <project> whitespace
```

Дополнительный local tool устанавливать не требуется.

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

Скрипт последовательно форматирует все C#-проекты через `dotnet format whitespace`, затем находит `clang-format` в `PATH` или в установленной Visual Studio и форматирует `.cpp`, `.cxx`, `.h`, `.hpp` в `src/GraphPlugin.Native`.

C#-проекты форматируются отдельно, чтобы не передавать смешанный C#/C++ solution в `dotnet format`.

## Только проверка без изменения файлов

```powershell
.\format.ps1 -Check
```

Для C# используется:

```text
dotnet format <project> whitespace --verify-no-changes
```

Для C++ используется:

```text
clang-format --dry-run --Werror
```

Команда подходит для локальной проверки перед commit/PR и позже может быть перенесена в CI.

## Ручной запуск для C#

Например, для Domain:

```powershell
dotnet format .\src\GraphPlugin.Domain\GraphPlugin.Domain.csproj whitespace
```

Только проверка:

```powershell
dotnet format .\src\GraphPlugin.Domain\GraphPlugin.Domain.csproj whitespace --verify-no-changes
```

Аналогично можно запускать formatter для остальных C#-проектов. Обычно удобнее использовать `format.ps1`.

## Ручной запуск clang-format

Для одного файла:

```powershell
clang-format -i .\src\GraphPlugin.Native\Commands\GraphCommands.cpp
```

Если команда не найдена, установите LLVM или компонент Visual Studio **C++ Clang tools for Windows**. `format.ps1` также пытается найти `clang-format.exe` внутри последней установленной Visual Studio через `vswhere.exe`.

## После форматирования

После массового форматирования рекомендуется проверить diff и затем выполнить:

```powershell
.\build.ps1
dotnet test .\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj
```

После успешной сборки — перезапустить nanoCAD и выполнить:

```text
GRAPHTESTS
```

Форматирование следует коммитить отдельно от изменений поведения. Большой diff в таком случае можно однозначно рассматривать как механический.

## Стиль, к которому стремимся

Связанные объявления локальных переменных можно держать одним блоком, а следующий логический шаг отделять пустой строкой:

```csharp
var context = PluginServices.CurrentContext;
var document = context.Document;
var editor = document.Editor;

var vertices = context.Vertices.GetAll();

editor.WriteMessage($"\nНайдено вершин: {vertices.Count}");
```

Не требуется вставлять пустую строку между каждой локальной переменной. Разделение должно отражать смысловые блоки кода.
