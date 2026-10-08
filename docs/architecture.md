# Архитектура GraphPlugin

Документ описывает текущее устройство GraphPlugin, границы ответственности между проектами и основные потоки данных внутри nanoCAD.

## Общая схема

Основная C#-часть построена по слоям. Зависимости направлены от интеграционного слоя к чистой предметной логике:

```text
GraphPlugin.Domain
        ↑
GraphPlugin.Application
        ↑
GraphPlugin.Nanocad
```

Дополнительно существуют два проекта:

```text
GraphPlugin.Native
    C++/CLI-реализация части операций с вершинами
    через тот же DWG-контракт хранения данных

GraphPlugin.Nanocad.IntegrationTests
    набор интеграционных и регрессионных тестов,
    выполняемый внутри nanoCAD
```

Главный принцип: `GraphPlugin.Domain` и `GraphPlugin.Application` не зависят от nanoCAD API. Все зависимости на `Document`, `Database`, `ObjectId`, `Entity`, транзакции и события nanoCAD находятся в `GraphPlugin.Nanocad` или `GraphPlugin.Native`.

## 1. GraphPlugin.Domain

`GraphPlugin.Domain` содержит модель графа, геометрию и алгоритмы, не зависящие от DWG и nanoCAD.

Основные группы:

```text
Domain/
├── Models/
├── Geometry/
└── Algorithms/
```

### Модель

Ключевые типы:

- `GraphVertex` — вершина со стабильным `Guid`, положением и стилем;
- `GraphEdge` — ребро между двумя `VertexId`;
- `EdgeRoute` — маршрут ребра с промежуточными bend-точками;
- `VertexStyle`, `EdgeStyle`, `GraphSettings` — предметные настройки;
- `VertexAttachment` — ссылка на файл, связанную с вершиной.

Domain не хранит отдельный долгоживущий объект `Graph`. Постоянное состояние принадлежит DWG, а алгоритмы получают необходимый снимок данных от слоя Application.

### Геометрия

`Geometry` содержит независимые от nanoCAD типы и вычисления:

- `Point2`;
- операции с маршрутом Edge и bend-точками;
- проекции;
- разбиение маршрутов.

Это позволяет тестировать геометрию обычными unit tests без запуска nanoCAD.

### Кратчайший путь

`DijkstraShortestPath` — чистый алгоритм. Он получает коллекции вершин и рёбер и один раз строит:

```text
VertexId -> GraphVertex
VertexId -> incident GraphEdge[]
```

После этого выполняется поиск Дейкстры.

`EdgeLengthCalculator` вычисляет вес ребра по текущим координатам конечных Vertex и геометрии `EdgeRoute`. Для ребра с bend-точками используется полная длина полилинейного маршрута.

Отдельный интерфейс `IShortestPathService` не используется: алгоритм не является инфраструктурной границей и существует как конкретная чистая реализация Domain.

## 2. GraphPlugin.Application

`GraphPlugin.Application` реализует пользовательские сценарии и объявляет абстракции доступа к постоянному состоянию.

```text
Application/
├── Abstractions/
├── Graph/
├── Routing/
├── Editing/
│   ├── Bends/
│   └── Splitting/
├── Attachments/
└── Styling/
```

### Абстракции persistence

Основные интерфейсы:

- `IVertexRepository`;
- `IEdgeRepository`;
- `IGraphSettingsRepository`;
- `IVertexAttachmentRepository`;
- независимые от nanoCAD абстракции применения стилей.

Именно эти интерфейсы отделяют пользовательские сценарии от конкретного хранения данных в DWG.

### Сервисы графа

`VertexService` отвечает за жизненный цикл Vertex:

- создание;
- каскадное удаление Vertex вместе со всеми incident Edge.

`EdgeService` отвечает за жизненный цикл Edge:

- создание;
- удаление.

Отдельный универсальный `GraphService` не используется: операции принадлежат более конкретным сервисам.

`GraphBuildService` хранит кратковременное состояние интерактивного построения цепочки и создаёт связи между последовательно выбранными вершинами.

### Редактирование

Сервисы редактирования разделены по пользовательским сценариям:

- `SplitEdgeService` — разбиение Edge новой Vertex;
- `AddBendService` — добавление bend-точки;
- `MoveBendService` — обновление положения bend-точки;
- `RemoveBendService` — удаление bend-точки.

Эти сервисы работают через repositories и не открывают nanoCAD transactions самостоятельно.

### Поиск пути

`ShortestPathService` — сервис уровня Application:

```text
repositories
    ↓
получение всех Vertex и Edge
    ↓
DijkstraShortestPath
    ↓
ShortestPathResult
```

Он не реализует сам алгоритм поиска, а получает актуальный снимок графа из repositories и передаёт его в Domain.

### Attachments и стили

`VertexAttachmentService` управляет ссылками на прикреплённые файлы и разрешает относительные пути относительно DWG.

`GraphSettingsService` читает и изменяет глобальные настройки графа, а также инициирует применение Edge style через абстракцию, реализованную на стороне nanoCAD.

## 3. GraphPlugin.Nanocad

`GraphPlugin.Nanocad` — интеграционный слой C#-части и место сборки зависимостей.

Основные области ответственности:

```text
Nanocad/
├── Commands/
├── Drawing/
├── Persistence/
├── Runtime/
└── Bootstrap/
```

### Команды

Команды nanoCAD являются внешней точкой входа. Они:

1. получают текущий `GraphDocumentContext`;
2. читают пользовательский ввод через Editor API;
3. вызывают нужный Application или Runtime service;
4. выводят результат пользователю.

Предметные правила не должны переноситься в классы команд.

### Persistence

DWG repositories реализуют интерфейсы Application:

```text
IVertexRepository
    -> NanoCadVertexRepository

IEdgeRepository
    -> NanoCadEdgeRepository

IGraphSettingsRepository
    -> NanoCadGraphSettingsRepository

IVertexAttachmentRepository
    -> NanoCadVertexAttachmentRepository
```

Постоянное представление:

- Vertex — `Circle` или закрытый треугольный `Polyline`;
- Edge — `Polyline`;
- topology и стабильные GUID — XRecord;
- attachments — отдельный XRecord Vertex;
- глобальные настройки — XRecord в Named Objects Dictionary.

Подробный формат описан в [dwg-schema.md](dwg-schema.md).

## 4. DWG как источник постоянного состояния

GraphPlugin не сериализует отдельный .NET object graph.

Источник постоянного состояния — сам DWG:

```text
DWG entities + XRecords
        ↓
GraphEntityIndexBuilder
        ↓
GraphEntityIndex
```

`GraphEntityIndex` — только runtime-кэш. Он хранит быстрые соответствия:

```text
VertexId <-> ObjectId
EdgeId   <-> ObjectId
VertexId -> incident EdgeId[]
```

При открытии документа индекс перестраивается по metadata из DWG. Поэтому runtime state можно восстановить без отдельного sidecar-файла и без дополнительной сериализации графа.

## 5. Контекст на один документ

Сервисы создаются один раз для каждого открытого nanoCAD `Document`.

Точка сборки зависимостей — `GraphDocumentContextFactory`.

Она последовательно создаёт:

```text
metadata + index
    ↓
repositories
    ↓
Application services
    ↓
Runtime services
    ↓
GraphDocumentContext
```

`GraphDocumentContext` содержит repositories и сервисы, относящиеся к одному DWG-документу.

`GraphDocumentContextManager` хранит соответствие:

```text
Document -> GraphDocumentContext
```

При первом обращении context создаётся, а `GraphDatabaseWatcher` запускается. При закрытии документа watcher останавливается и context удаляется. При завершении работы плагина все contexts очищаются.

DI container намеренно не используется: composition root небольшой и явно собран в одном factory, поэтому зависимости легко проследить по коду.

## 6. Синхронизация с редактированием DWG

Пользователь может изменять DWG не только командами GraphPlugin, но и штатными средствами nanoCAD: grip editing, `ERASE`, `UNDO`.

За синхронизацию отвечает `GraphDatabaseWatcher`.

Он слушает:

- `Database.ObjectModified`;
- `Database.ObjectErased`;
- `Database.ObjectAppended`;
- `Document.CommandEnded`;
- `Document.CommandCancelled`;
- `Document.CommandFailed`.

События объектов, возникающие во время команды, сначала накапливаются. После `CommandEnded` watcher обрабатывает их в следующем порядке:

```text
1. восстановленные объекты
2. добавленные или заменённые объекты
3. удалённые объекты
4. изменённые Vertex
5. изменённые Edge
```

Это важно, поскольку одна пользовательская команда может породить несколько связанных событий базы данных.

### Перемещение Vertex

```text
grip editing
    ↓
ObjectModified(Vertex)
    ↓
Vertex помечается как изменённая
    ↓
CommandEnded
    ↓
EdgeGeometrySynchronizer
    ↓
обновляются incident Edge polylines
```

Синхронизация выполняется по итоговому положению Vertex после завершения команды.

### Удаление Vertex

При обычном `ERASE` сущность Vertex уже удаляется самим nanoCAD. После завершения команды watcher:

1. определяет логический `VertexId` через runtime index;
2. удаляет incident Edge через `EdgeService`;
3. удаляет соответствие Vertex из index.

Повторно вызывать удаление Vertex через repository не требуется.

### UNDO

При восстановлении удалённого объекта watcher получает его `ObjectId`, читает XRecord и в два прохода восстанавливает runtime topology:

```text
ПРОХОД 1: Vertex
ПРОХОД 2: Edge
```

Так Edge добавляются в index только после восстановления их endpoint Vertex.

### Замена Circle <-> Triangle

Смена формы Vertex физически заменяет DWG-сущность, но логический `VertexId` сохраняется.

Watcher отличает такую замену от настоящего удаления: если тот же `VertexId` уже связан с новым живым `ObjectId`, удаление старой сущности не считается удалением логической Vertex.

## 7. Геометрия Edge

Topology Edge и drawing geometry разделены:

```text
Topology:
VertexAId + VertexBId
    хранится в GRAPH_EDGE

Geometry:
Polyline vertices
    хранится нативно в DWG
```

Конечные точки Polyline синхронизируются с положением endpoint Vertex.

Промежуточные вершины Polyline являются bend-точками и сохраняются при перемещении конечных Vertex.

Благодаря этому для редактирования bends можно использовать стандартные grip points nanoCAD без собственной custom entity implementation.

## 8. Архитектура C++/CLI

`GraphPlugin.Native` не зависит от C# runtime objects. Взаимодействие строится через общий DWG persistence contract.

```text
GraphPlugin.Native/
├── Commands/
├── Persistence/
├── Services/
└── Tests/
```

### Рабочий код

`Commands/GraphCommands.*` содержит только пользовательские native commands.

`Persistence` содержит C++ readers/writers для metadata `GRAPH_VERTEX` и `GRAPH_EDGE`.

`Services` содержит операции:

- `NativeVertexStyleService`;
- `NativeVertexDeletionService`.

### Тестовый код

Тестовая инфраструктура физически отделена от рабочего кода:

```text
Tests/
├── NativeTestCommands.*
├── NativeIntegrationTestRunner.*
├── NativeTestDwgHelpers.*
├── StyleInteropScenario.*
├── DeleteUndoScenario.*
├── NativeIntegrationTestException.h
└── NativeStagedTestSchema.h
```

`NativeTestCommands` является тонким хостом команд и делегирует работу runner/scenario classes.

`NativeTestDwgHelpers` содержит только вспомогательные операции для тестов и не используется рабочим кодом.

### Взаимодействие C# и C++

Между реализациями должны оставаться синхронизированными:

- имена XRecord;
- версии схемы;
- порядок полей;
- DXF-типы значений;
- числовые значения enum;
- правила хранения GUID.

Изменение Vertex из C++ становится доступно C#-части после обработки событий DWG, потому что обе реализации читают один и тот же постоянный контракт.

## 9. Основные потоки данных

### Создание Vertex

```text
GRAPHNODE
    ↓
VertexService
    ↓
IVertexRepository
    ↓
NanoCadVertexRepository
    ↓
Entity + GRAPH_VERTEX XRecord
    ↓
GraphEntityIndex
```

### Создание Edge

```text
GRAPHEDGE / GRAPHBUILD
    ↓
EdgeService
    ↓
IEdgeRepository
    ↓
NanoCadEdgeRepository
    ↓
Polyline + GRAPH_EDGE XRecord
    ↓
GraphEntityIndex adjacency
```

### Кратчайший путь

```text
GRAPHSHORTESTPATH
    ↓
ShortestPathService
    ↓
repositories.GetAll()
    ↓
DijkstraShortestPath
    ↓
ShortestPathResult
    ↓
ShortestPathHighlighter
```

### Повторное открытие DWG

```text
SAVE / CLOSE / OPEN
    ↓
DWG entities + XRecords
    ↓
GraphDocumentContextFactory
    ↓
GraphEntityIndexBuilder
    ↓
восстановленный runtime context
```

## 10. Архитектура тестирования

Тестирование разделено по уровню.

### Unit tests

`GraphPlugin.Tests` зависит только от Domain/Application и не требует nanoCAD assemblies для предметных тестов.

Проверяются:

- domain models;
- geometry;
- Dijkstra;
- Application services;
- persistence contracts и enum values.

### Регрессионный harness внутри nanoCAD

`GraphPlugin.Nanocad.IntegrationTests` загружается отдельно и предоставляет два основных сценария:

```text
GRAPHTESTS
GRAPHTESTS_PERSISTENCE
```

`GRAPHTESTS` сохраняет реальные границы команд nanoCAD и проверяет, в частности, настоящий `ERASE`/`UNDO`, синхронизацию watcher и C++/C# interoperability.

Persistence-сценарий специально требует реального `SAVE -> CLOSE -> OPEN`, чтобы не подменять проверку сериализации чтением того же runtime state.

Подробнее: [testing.md](testing.md).

## 11. Архитектурные инварианты

При дальнейшем развитии проекта важно сохранять следующие правила:

1. `GraphPlugin.Domain` не должен ссылаться на nanoCAD assemblies.
2. `GraphPlugin.Application` не должен знать о `Document`, `Database`, `ObjectId` или `Entity`.
3. Постоянное состояние хранится в DWG; `GraphEntityIndex` не является persistence storage.
4. Стабильная логическая идентичность задаётся `Guid`, а не `ObjectId` или `Handle`.
5. Edge topology задаётся metadata, а не определяется совпадением координат концов Polyline.
6. Удаление Vertex всегда означает удаление всех incident Edge.
7. C# и C++ обязаны соблюдать единый DWG schema contract.
8. Изменение формата схемы требует решения о версии и повторного interop/regression-тестирования.
