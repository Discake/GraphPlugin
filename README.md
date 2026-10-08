# GraphPlugin

Плагин для nanoCAD, реализующий редактируемый граф поверх обычных DWG-сущностей.

Вершины и рёбра остаются нативными объектами чертежа, а топология и служебные данные сохраняются в XRecord. Основная реализация написана на C#/.NET 6; часть операций с вершинами дополнительно реализована на C++/CLI и использует тот же DWG-контракт.

## Возможности

- создание вершин двух типов:
  - синяя окружность;
  - красный треугольник;
- перемещение вершин стандартными grip-point средствами nanoCAD;
- автоматическое обновление геометрии связанных рёбер после перемещения вершины;
- удаление вершины вместе со всеми инцидентными рёбрами;
- независимое удаление рёбер обычной командой `ERASE`;
- построение цепочки графа по кликам в чертеже;
- использование уже существующей вершины при построении;
- автоматическое разбиение ребра при выборе точки на нём;
- ручное разбиение ребра новой вершиной;
- полилинейные рёбра с промежуточными точками изгиба;
- добавление bend-точки выбором места на ребре;
- перемещение bend-точек штатными grip-point средствами полилинии;
- глобальная настройка цвета, типа и толщины линий рёбер;
- поиск кратчайшего пути алгоритмом Дейкстры с учётом фактической длины полилинии;
- визуальная подсветка найденного пути;
- прикрепление файлов к вершинам;
- сохранение графа, стилей и attachment-метаданных внутри DWG;
- восстановление runtime-индекса после повторного открытия DWG;
- C++/CLI реализация создания, чтения, смены формы и каскадного удаления вершины;
- автоматизированные integration/regression tests для C#, C++, ERASE/UNDO, interop и persistence.

## Архитектура

Решение разделено на несколько проектов:

```text
GraphPlugin.Domain
    чистая доменная модель, геометрия и алгоритм Дейкстры

GraphPlugin.Application
    use cases, сервисы и persistence-абстракции

GraphPlugin.Nanocad
    nanoCAD API, DWG repositories, runtime index,
    synchronization/watchers и пользовательские команды

GraphPlugin.Native
    C++/CLI реализация части операций с вершинами
    поверх того же DWG persistence contract

GraphPlugin.Tests
    unit tests Domain/Application

GraphPlugin.Nanocad.IntegrationTests
    integration/regression harness, выполняемый внутри nanoCAD
```

Зависимости основного C# кода направлены так:

```text
GraphPlugin.Domain
        ↑
GraphPlugin.Application
        ↑
GraphPlugin.Nanocad
```

`GraphPlugin.Domain` и `GraphPlugin.Application` не зависят от nanoCAD API.

### DWG как источник состояния

Граф не сериализуется отдельным объектным графом .NET. Состояние хранится в обычных сущностях nanoCAD:

- Vertex: `Circle` или закрытый треугольный `Polyline`;
- Edge: `Polyline`;
- стабильные GUID и topology metadata: XRecord в Extension Dictionary;
- глобальные настройки: XRecord в Named Objects Dictionary;
- attachment paths: отдельный XRecord вершины.

`GraphEntityIndex` является runtime-кэшем соответствия GUID ↔ `ObjectId` и связей Vertex ↔ Edge. Он перестраивается из DWG при инициализации документа и обновляется во время редактирования.

C# и C++ не обмениваются runtime-объектами напрямую. Их interoperability основана на общем DWG schema contract. Подробный формат описан в [docs/dwg-schema.md](docs/dwg-schema.md).

## Требования

Базовая конфигурация проекта:

- Windows x64;
- Visual Studio с MSBuild и toolset v143;
- .NET 6;
- C++/CLI (`CLRSupport=NetCore`);
- C++17;
- nanoCAD x64.

Версия nanoCAD по умолчанию — `24.1`. Путь к другой установленной версии можно передать сборочному скрипту явно. Совместимость с конкретной версией nanoCAD следует подтверждать сборкой и regression-прогоном против её `hostmgd.dll`/`hostdbmgd.dll`.

## Сборка

Рекомендуемый способ — корневой PowerShell launcher:

```powershell
.\build.ps1
```

Release:

```powershell
.\build.ps1 -Configuration Release
```

Другая версия nanoCAD:

```powershell
.\build.ps1 -NanoCadVersion 25.0
```

Или явный installation directory:

```powershell
.\build.ps1 `
  -Configuration Release `
  -NanoCadInstallDir "D:\Nanosoft\nanoCAD x64 25.0"
```

`build.ps1` находит Visual Studio MSBuild через `vswhere.exe`, выполняет restore, собирает `GraphPlugin.slnx` для x64 и после успешной сборки формирует готовую папку плагина:

```text
artifacts\plugin\Debug\
```

или:

```text
artifacts\plugin\Release\
```

В неё входят:

```text
GraphPlugin.Domain.dll
GraphPlugin.Application.dll
GraphPlugin.Nanocad.dll
GraphPlugin.Nanocad.IntegrationTests.dll
GraphPlugin.Native.dll
LOAD.txt
```

`dotnet build` не является каноническим способом сборки всего solution, поскольку `GraphPlugin.Native` — C++/CLI проект. Подробнее: [docs/build.md](docs/build.md).

## Загрузка в nanoCAD

После сборки держите DLL из `artifacts/plugin/<Configuration>` рядом друг с другом.

Через `NETLOAD` загрузите:

1. `GraphPlugin.Nanocad.dll` — основной C# plugin;
2. `GraphPlugin.Native.dll` — C++/CLI commands;
3. `GraphPlugin.Nanocad.IntegrationTests.dll` — только если нужны integration tests.

`GraphPlugin.Domain.dll` и `GraphPlugin.Application.dll` являются зависимостями и отдельно через `NETLOAD` не загружаются.

После пересборки плагина рекомендуется перезапустить nanoCAD перед повторной загрузкой DLL, чтобы исключить использование уже загруженной старой сборки.

## Основные пользовательские команды

### Вершины и рёбра

| Команда | Назначение |
| --- | --- |
| `GRAPHNODE` | Создать вершину Circle или Triangle |
| `GRAPHEDGE` | Соединить две существующие вершины ребром |
| `GRAPHBUILD` | Интерактивно строить цепочку графа по кликам |
| `GRAPHSPLITEDGE` | Разбить выбранное ребро новой вершиной |
| `GRAPHADDBEND` | Добавить bend-точку в выбранное место полилинейного ребра |
| `GRAPHEDGESTYLE` | Изменить глобальный цвет, тип линии и толщину всех рёбер |
| `GRAPHINFO` | Показать metadata выбранной вершины |
| `GRAPHVERTICES` | Вывести список вершин документа |

`GRAPHBUILD` различает три вида выбора:

- пустая область — создаётся новая вершина;
- существующая Vertex — используется существующая вершина;
- Edge — ребро разделяется новой вершиной в выбранном месте.

Команда завершается по `Enter` или `Esc`.

Удаление выполняется штатным `ERASE`. При удалении Vertex `GraphDatabaseWatcher` каскадно удаляет её incident edges; при удалении отдельного Edge остальные объекты графа сохраняются.

### Кратчайший путь

| Команда | Назначение |
| --- | --- |
| `GRAPHSHORTESTPATH` | Выбрать начальную и конечную вершины и найти кратчайший путь |
| `GRAPHCLEARPATH` | Убрать визуальную подсветку найденного пути |

Вес Edge равен его геометрической длине. Для рёбер с bend-точками используется полная длина маршрута полилинии, а не только расстояние между конечными вершинами.

### Файлы, прикреплённые к вершинам

| Команда | Назначение |
| --- | --- |
| `GRAPHATTACHFILE` | Прикрепить файл к выбранной вершине |
| `GRAPHVERTEXFILES` | Показать список прикреплённых файлов |
| `GRAPHOPENFILE` | Открыть прикреплённый файл |
| `GRAPHDETACHFILE` | Удалить ссылку на файл из вершины |

Удаление attachment не удаляет физический файл с диска.

### C++/CLI commands

`GraphPlugin.Native.dll` предоставляет отдельный набор команд, работающих с тем же `GRAPH_VERTEX` / `GRAPH_EDGE` schema:

| Команда | Назначение |
| --- | --- |
| `GRAPHCPPINFO` | Показать информацию о native plugin/schema |
| `GRAPHCPPVERTEX` | Создать синюю Circle-вершину из C++ |
| `GRAPHCPPVERTEXINFO` | Прочитать Vertex metadata из C++ |
| `GRAPHCPPVERTEXSTYLE` | Изменить форму вершины Circle ↔ Triangle из C++ |
| `GRAPHCPPDELETEVERTEX` | Удалить вершину и её incident edges из C++ |

Изменения, выполненные C++, видны C#-части через общий DWG persistence contract и наоборот.

## Кратчайший путь

Алгоритм находится в `GraphPlugin.Domain` и не зависит от nanoCAD.

`DijkstraShortestPath` получает snapshot коллекций вершин и рёбер, один раз строит индекс вершин и adjacency list, после чего выполняет поиск. Длина каждого Edge вычисляется `EdgeLengthCalculator` по текущим координатам его endpoint-вершин и route/bend geometry.

Application-уровень (`ShortestPathService`) отвечает только за получение данных из repositories и передачу их алгоритму.

## Синхронизация с редактированием DWG

`GraphDatabaseWatcher` наблюдает события базы и границы nanoCAD commands. Это позволяет поддерживать runtime topology при обычных действиях пользователя:

- перемещение Vertex;
- редактирование Edge polyline;
- `ERASE` Vertex/Edge;
- `UNDO`;
- замена Circle ↔ Triangle при смене стиля;
- добавление объектов native C++ кодом.

Обновление выполняется после завершения команды, поэтому связанные Edge синхронизируются с итоговым положением Vertex, а не с промежуточными grip events.

## Тестирование

### Unit tests

Pure .NET tests не требуют nanoCAD host:

```powershell
dotnet test .\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj
```

Они покрывают Domain/Application: модели, geometry, edge routes, services, persistence contracts и shortest path.

### Полная regression-проверка в текущем DWG

Загрузите также `GraphPlugin.Nanocad.IntegrationTests.dll` и выполните:

```text
GRAPHTESTS
```

Одна команда автоматически проверяет:

- C# integration suite;
- создание/чтение/редактирование графа;
- полилинейные Edge и bends;
- shortest path;
- attachments;
- Edge `ERASE` → verify → real nanoCAD `UNDO` → verify;
- Vertex cascade `ERASE`/`UNDO`;
- attachment `ERASE`/`UNDO`;
- native C++ tests;
- C++ → C# style interoperability;
- C++ cascade delete → nanoCAD `UNDO` → C# restore verification.

Harness специально сохраняет реальные nanoCAD command boundaries, поэтому watcher и настоящий command/UNDO stack тестируются, а не подменяются прямыми вызовами repositories.

### Persistence regression

Persistence проверяется отдельным двухфазным сценарием:

```text
GRAPHTESTS_PERSISTENCE
```

После первой команды:

1. `SAVE` DWG;
2. `CLOSE`;
3. открыть тот же DWG снова;
4. снова выполнить `GRAPHTESTS_PERSISTENCE`.

Вторая фаза проверяет topology, GUID metadata, runtime index rebuild, global settings, edge style и attachment XRecords, после чего очищает тестовые объекты.

Полное описание тестового workflow: [docs/testing.md](docs/testing.md).

## Структура репозитория

```text
.
├── build.ps1
├── stage-plugin.ps1
├── GraphPlugin.slnx
├── docs/
│   ├── build.md
│   ├── dwg-schema.md
│   └── testing.md
├── src/
│   ├── GraphPlugin.Domain/
│   ├── GraphPlugin.Application/
│   ├── GraphPlugin.Nanocad/
│   └── GraphPlugin.Native/
└── tests/
    ├── GraphPlugin.Tests/
    └── GraphPlugin.Nanocad.IntegrationTests/
```

## Документация

- [Build configuration](docs/build.md) — сборка, MSBuild, staging и nanoCAD installation properties.
- [DWG persistence schema](docs/dwg-schema.md) — XRecord layout, enum contracts и C#/C++ interoperability.
- [Integration test workflow](docs/testing.md) — `GRAPHTESTS` и двухфазная persistence regression.

## Примечания по совместимости

Persistence format имеет собственную версию schema и не привязан к версии assembly. Текущая версия всех GraphPlugin XRecords — `1`.

Читатели не интерпретируют неизвестную явную версию как текущую: unsupported schema version считается ошибкой. Это защищает DWG от тихого чтения несовместимого формата.

Базовая сборочная конфигурация использует nanoCAD x64 24.1. Для другой версии следует собирать проект против соответствующих `hostmgd.dll` и `hostdbmgd.dll` и затем выполнять `GRAPHTESTS` и `GRAPHTESTS_PERSISTENCE` на целевой установке.
