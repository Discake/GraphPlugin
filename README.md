# GraphPlugin

Плагин для nanoCAD, реализующий редактируемый граф поверх обычных DWG-сущностей.

Вершины и рёбра остаются нативными объектами чертежа, а topology и служебные данные сохраняются в XRecord. Основная реализация написана на C#/.NET 6; часть операций с вершинами дополнительно реализована на C++/CLI и использует тот же DWG-контракт.

## Возможности

- создание вершин двух типов:
  - синяя окружность;
  - красный треугольник;
- перемещение вершин стандартными grip-point средствами nanoCAD;
- автоматическое обновление геометрии связанных рёбер после перемещения вершины;
- удаление вершины вместе со всеми incident Edge;
- независимое удаление рёбер обычной командой `ERASE`;
- построение цепочки графа по кликам в чертеже;
- использование уже существующей вершины при построении;
- автоматическое разбиение Edge при выборе точки на нём;
- ручное разбиение Edge новой Vertex;
- полилинейные Edge с промежуточными bend-точками;
- добавление bend-точки выбором места на Edge;
- перемещение bend-точек штатными grip-point средствами Polyline;
- глобальная настройка цвета, типа и толщины линий Edge;
- поиск кратчайшего пути алгоритмом Дейкстры с учётом фактической длины Polyline;
- визуальная подсветка найденного пути;
- прикрепление файлов к Vertex;
- сохранение графа, стилей и attachment metadata внутри DWG;
- восстановление runtime index после повторного открытия DWG;
- C++/CLI-реализация создания, чтения, смены формы и каскадного удаления Vertex;
- автоматизированные интеграционные и регрессионные тесты для C#, C++, `ERASE/UNDO`, interoperability и persistence.

## Архитектура

Решение разделено на несколько проектов:

```text
GraphPlugin.Domain
    чистая предметная модель, геометрия и алгоритм Дейкстры

GraphPlugin.Application
    пользовательские сценарии, сервисы и persistence-абстракции

GraphPlugin.Nanocad
    nanoCAD API, DWG repositories, runtime index,
    synchronization/watcher и пользовательские команды

GraphPlugin.Native
    C++/CLI-реализация части операций с вершинами
    поверх того же DWG persistence contract

GraphPlugin.Tests
    unit tests для Domain/Application

GraphPlugin.Nanocad.IntegrationTests
    интеграционный и регрессионный harness,
    выполняемый внутри nanoCAD
```

Зависимости основной C#-части направлены так:

```text
GraphPlugin.Domain
        ↑
GraphPlugin.Application
        ↑
GraphPlugin.Nanocad
```

`GraphPlugin.Domain` и `GraphPlugin.Application` не зависят от nanoCAD API.

Подробное описание архитектуры: [docs/architecture.md](docs/architecture.md).

### DWG как источник состояния

Граф не сериализуется отдельным объектным графом .NET. Состояние хранится в обычных сущностях nanoCAD:

- Vertex: `Circle` или закрытый треугольный `Polyline`;
- Edge: `Polyline`;
- стабильные GUID и topology metadata: XRecord в Extension Dictionary;
- глобальные настройки: XRecord в Named Objects Dictionary;
- attachment paths: отдельный XRecord Vertex.

`GraphEntityIndex` является runtime-кэшем соответствий GUID ↔ `ObjectId` и связей Vertex ↔ Edge. Он перестраивается из DWG при инициализации документа и обновляется во время редактирования.

C# и C++ не обмениваются runtime-объектами напрямую. Их взаимодействие основано на общем DWG schema contract. Подробный формат описан в [docs/dwg-schema.md](docs/dwg-schema.md).

## Требования

Базовая конфигурация проекта:

- Windows x64;
- Visual Studio с MSBuild и toolset v143;
- .NET 6;
- C++/CLI (`CLRSupport=NetCore`);
- C++17;
- nanoCAD x64.

Версия nanoCAD по умолчанию — `24.1`. Путь к другой установленной версии можно передать сборочному скрипту явно. Совместимость с конкретной версией nanoCAD следует подтверждать сборкой и регрессионным прогоном против её `hostmgd.dll`/`hostdbmgd.dll`.

## Сборка

Рекомендуемый способ — корневой PowerShell-скрипт:

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

Или явный каталог установки:

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

`dotnet build` не является основным способом сборки всего solution, поскольку `GraphPlugin.Native` — C++/CLI-проект. Подробнее: [docs/build.md](docs/build.md).

## Загрузка в nanoCAD

После сборки держите DLL из `artifacts/plugin/<Configuration>` рядом друг с другом.

Через `NETLOAD` загрузите:

1. `GraphPlugin.Nanocad.dll` — основной C#-плагин;
2. `GraphPlugin.Native.dll` — C++/CLI-команды;
3. `GraphPlugin.Nanocad.IntegrationTests.dll` — только если нужны интеграционные тесты.

`GraphPlugin.Domain.dll` и `GraphPlugin.Application.dll` являются зависимостями и отдельно через `NETLOAD` не загружаются.

После пересборки рекомендуется перезапустить nanoCAD перед повторной загрузкой DLL, чтобы исключить использование уже загруженной старой сборки.

## Основные пользовательские команды

### Вершины и рёбра

| Команда | Назначение |
| --- | --- |
| `GRAPHNODE` | Создать Vertex типа Circle или Triangle |
| `GRAPHEDGE` | Соединить две существующие Vertex ребром |
| `GRAPHBUILD` | Интерактивно строить цепочку графа по кликам |
| `GRAPHSPLITEDGE` | Разбить выбранный Edge новой Vertex |
| `GRAPHADDBEND` | Добавить bend-точку в выбранное место Edge |
| `GRAPHEDGESTYLE` | Изменить глобальный цвет, тип линии и толщину всех Edge |
| `GRAPHINFO` | Показать metadata выбранной Vertex |
| `GRAPHVERTICES` | Вывести список Vertex документа |

`GRAPHBUILD` различает три вида выбора:

- пустая область — создаётся новая Vertex;
- существующая Vertex — используется существующая вершина;
- Edge — ребро разделяется новой Vertex в выбранном месте.

Команда завершается по `Enter` или `Esc`.

Удаление выполняется штатным `ERASE`. При удалении Vertex `GraphDatabaseWatcher` каскадно удаляет её incident Edge; при удалении отдельного Edge остальные объекты графа сохраняются.

### Кратчайший путь

| Команда | Назначение |
| --- | --- |
| `GRAPHSHORTESTPATH` | Выбрать начальную и конечную Vertex и найти кратчайший путь |
| `GRAPHCLEARPATH` | Убрать визуальную подсветку найденного пути |

Вес Edge равен его геометрической длине. Для рёбер с bend-точками используется полная длина маршрута Polyline, а не только расстояние между конечными Vertex.

### Файлы, прикреплённые к Vertex

| Команда | Назначение |
| --- | --- |
| `GRAPHATTACHFILE` | Прикрепить файл к выбранной Vertex |
| `GRAPHVERTEXFILES` | Показать список прикреплённых файлов |
| `GRAPHOPENFILE` | Открыть прикреплённый файл |
| `GRAPHDETACHFILE` | Удалить ссылку на файл из Vertex |

Удаление attachment не удаляет физический файл с диска.

### Команды C++/CLI

`GraphPlugin.Native.dll` предоставляет отдельный набор команд, работающих с тем же `GRAPH_VERTEX` / `GRAPH_EDGE` schema:

| Команда | Назначение |
| --- | --- |
| `GRAPHCPPINFO` | Показать информацию о native plugin/schema |
| `GRAPHCPPVERTEX` | Создать синюю Circle-Vertex из C++ |
| `GRAPHCPPVERTEXINFO` | Прочитать Vertex metadata из C++ |
| `GRAPHCPPVERTEXSTYLE` | Изменить форму Vertex Circle ↔ Triangle из C++ |
| `GRAPHCPPDELETEVERTEX` | Удалить Vertex и её incident Edge из C++ |

Изменения, выполненные C++, видны C#-части через общий DWG persistence contract и наоборот.

## Кратчайший путь

Алгоритм находится в `GraphPlugin.Domain` и не зависит от nanoCAD.

`DijkstraShortestPath` получает snapshot коллекций Vertex и Edge, один раз строит индекс вершин и adjacency list, после чего выполняет поиск. Длина каждого Edge вычисляется `EdgeLengthCalculator` по текущим координатам его endpoint Vertex и route/bend geometry.

Сервис `ShortestPathService` на уровне Application отвечает только за получение данных из repositories и передачу их алгоритму.

## Синхронизация с редактированием DWG

`GraphDatabaseWatcher` наблюдает события базы и границы команд nanoCAD. Это позволяет поддерживать runtime topology при обычных действиях пользователя:

- перемещение Vertex;
- редактирование Edge Polyline;
- `ERASE` Vertex/Edge;
- `UNDO`;
- замена Circle ↔ Triangle при смене формы;
- добавление объектов C++-кодом.

Обновление выполняется после завершения команды, поэтому связанные Edge синхронизируются с итоговым положением Vertex, а не с промежуточными grip events.

## Тестирование

### Unit tests

Обычные .NET-тесты не требуют запуска nanoCAD:

```powershell
dotnet test .\tests\GraphPlugin.Tests\GraphPlugin.Tests.csproj
```

Они покрывают Domain/Application: модели, geometry, Edge routes, services, persistence contracts и shortest path.

### Полная регрессионная проверка в текущем DWG

Загрузите также `GraphPlugin.Nanocad.IntegrationTests.dll` и выполните:

```text
GRAPHTESTS
```

Одна команда автоматически проверяет:

- основной набор C# integration tests;
- создание, чтение и редактирование графа;
- полилинейные Edge и bends;
- shortest path;
- attachments;
- Edge `ERASE` → проверка → настоящий nanoCAD `UNDO` → проверка;
- Vertex cascade `ERASE/UNDO`;
- attachment `ERASE/UNDO`;
- native C++ tests;
- C++ → C# style interoperability;
- C++ cascade delete → nanoCAD `UNDO` → C# verification.

Harness сохраняет реальные границы команд nanoCAD, поэтому watcher и настоящий стек `UNDO` действительно тестируются, а не подменяются прямыми вызовами repositories.

### Проверка persistence

Persistence проверяется отдельным двухфазным сценарием:

```text
GRAPHTESTS_PERSISTENCE
```

После первой команды:

1. `SAVE` DWG;
2. `CLOSE`;
3. открыть тот же DWG снова;
4. снова выполнить `GRAPHTESTS_PERSISTENCE`.

Вторая фаза проверяет topology, GUID metadata, перестроенный runtime index, глобальные настройки, Edge style и attachment XRecords, после чего очищает тестовые объекты.

Полное описание тестового процесса: [docs/testing.md](docs/testing.md).

## Структура репозитория

```text
.
├── build.ps1
├── stage-plugin.ps1
├── GraphPlugin.slnx
├── docs/
│   ├── architecture.md
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

- [Архитектура](docs/architecture.md) — слои, потоки данных, runtime synchronization и C++/C# interoperability.
- [Сборка проекта](docs/build.md) — MSBuild, staging и параметры установки nanoCAD.
- [Схема хранения данных в DWG](docs/dwg-schema.md) — XRecord layout, enum contracts и правила версионирования.
- [Интеграционное тестирование](docs/testing.md) — `GRAPHTESTS` и двухфазная проверка persistence.

## Примечания по совместимости

Persistence format имеет собственную версию schema и не привязан к версии assembly. Текущая версия всех XRecord GraphPlugin — `1`.

Readers не интерпретируют неизвестную явно указанную версию как текущую: unsupported schema version считается ошибкой. Это защищает DWG от скрытого чтения несовместимого формата.

Базовая конфигурация сборки использует nanoCAD x64 24.1. Для другой версии следует собирать проект против соответствующих `hostmgd.dll` и `hostdbmgd.dll`, а затем выполнять `GRAPHTESTS` и `GRAPHTESTS_PERSISTENCE` на целевой установке.
