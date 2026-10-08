# Архитектура GraphPlugin

Документ описывает текущее устройство GraphPlugin, границы ответственности между проектами и основные потоки данных внутри nanoCAD.

## Общая схема

Основная C#-часть построена слоями с направлением зависимостей от host-specific к чистому коду:

```text
GraphPlugin.Domain
        ↑
GraphPlugin.Application
        ↑
GraphPlugin.Nanocad
```

Дополнительно существуют два независимых проекта:

```text
GraphPlugin.Native
    C++/CLI реализация части операций с вершинами
    через тот же DWG persistence contract

GraphPlugin.Nanocad.IntegrationTests
    regression/integration harness, выполняемый внутри nanoCAD
```

Главный принцип: `Domain` и `Application` не знают о nanoCAD API. Все зависимости на `Document`, `Database`, `ObjectId`, `Entity`, transactions и события host находятся в `GraphPlugin.Nanocad` или `GraphPlugin.Native`.

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
- `EdgeRoute` — промежуточная геометрия ребра, включая bends;
- `VertexStyle`, `EdgeStyle`, `GraphSettings` — доменные настройки;
- `VertexAttachment` — ссылка на файл, связанная с вершиной.

Domain не хранит собственный долгоживущий объект `Graph`. Persisted state принадлежит DWG, а алгоритмы получают нужный snapshot данных от application layer.

### Геометрия

`Geometry` содержит host-independent типы и вычисления:

- `Point2`;
- операции с route/bend geometry;
- проекции и разбиение маршрутов.

Это позволяет unit-тестировать геометрию без nanoCAD runtime.

### Кратчайший путь

`DijkstraShortestPath` — чистый алгоритм. Он получает коллекции вершин и рёбер, один раз строит:

```text
VertexId -> GraphVertex
VertexId -> incident GraphEdge[]
```

После этого выполняется поиск Дейкстры.

`EdgeLengthCalculator` вычисляет вес ребра по текущим координатам endpoint-вершин и геометрии `EdgeRoute`. Для bent edge используется полная длина полилинейного маршрута.

Отдельный интерфейс `IShortestPathService` не используется: алгоритм является чистой реализацией Domain и не представляет инфраструктурную границу.

## 2. GraphPlugin.Application

Application layer реализует use cases и объявляет абстракции persistence.

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

### Persistence abstractions

Основные интерфейсы:

- `IVertexRepository`;
- `IEdgeRepository`;
- `IGraphSettingsRepository`;
- `IVertexAttachmentRepository`;
- host-independent style/application abstractions.

Именно эти интерфейсы отделяют use cases от DWG persistence.

### Graph services

`VertexService` отвечает за lifecycle Vertex:

- создание;
- каскадное удаление Vertex вместе с incident edges.

`EdgeService` отвечает за lifecycle Edge:

- создание;
- удаление.

Отдельный универсальный `GraphService` не используется: операции принадлежат более конкретным сервисам.

`GraphBuildService` хранит кратковременное состояние интерактивного построения цепочки и создаёт связи между последовательно выбранными вершинами.

### Editing

Application services для редактирования изолированы по use case:

- `SplitEdgeService` — разбиение ребра новой вершиной;
- `AddBendService` — добавление bend;
- `MoveBendService` — обновление bend geometry;
- `RemoveBendService` — удаление bend.

Эти сервисы работают через repositories и не открывают nanoCAD transactions самостоятельно.

### Routing

`ShortestPathService` является application-level use case:

```text
repositories
    ↓
GetAll vertices + edges
    ↓
DijkstraShortestPath
    ↓
ShortestPathResult
```

Он не содержит алгоритм поиска сам, а отвечает за получение актуального snapshot из persistence.

### Attachments и styling

`VertexAttachmentService` управляет attachment paths и их разрешением относительно DWG.

`GraphSettingsService` читает/изменяет глобальные настройки и инициирует применение Edge style через abstraction, реализованную host layer.

## 3. GraphPlugin.Nanocad

Это host adapter и composition root C#-части.

Основные области ответственности:

```text
Nanocad/
├── Commands/
├── Drawing/
├── Persistence/
├── Runtime/
└── Bootstrap/
```

### Commands

Команды nanoCAD являются внешней точкой входа. Они:

1. получают текущий `GraphDocumentContext`;
2. читают пользовательский ввод через Editor API;
3. вызывают application/runtime service;
4. выводят результат пользователю.

Бизнес-правила не должны переноситься в command classes.

### Persistence

DWG repositories реализуют application abstractions:

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

Persisted представление:

- Vertex — `Circle` или закрытый triangular `Polyline`;
- Edge — `Polyline`;
- topology и stable GUID — XRecords;
- attachments — отдельный XRecord Vertex;
- global settings — XRecord в Named Objects Dictionary.

Полный бинарный контракт описан в `docs/dwg-schema.md`.

## 4. DWG как source of truth

GraphPlugin не сериализует отдельный .NET object graph.

Источник persisted state — сам DWG:

```text
DWG entities + XRecords
        ↓
GraphEntityIndexBuilder
        ↓
GraphEntityIndex
```

`GraphEntityIndex` — только runtime cache. Он хранит быстрые отображения:

```text
VertexId <-> ObjectId
EdgeId   <-> ObjectId
VertexId -> incident EdgeId[]
```

При открытии документа индекс перестраивается по metadata из DWG. Это значит, что runtime state можно восстановить без дополнительного sidecar-файла или отдельной сериализации.

## 5. Document-scoped context

Сервисы создаются один раз на открытый nanoCAD `Document`.

Composition root — `GraphDocumentContextFactory`.

Он последовательно создаёт:

```text
metadata/index
    ↓
repositories
    ↓
application services
    ↓
runtime services
    ↓
GraphDocumentContext
```

В `GraphDocumentContext` находятся repositories и сервисы, относящиеся к одному DWG-документу.

`GraphDocumentContextManager` хранит:

```text
Document -> GraphDocumentContext
```

При первом обращении context создаётся, а `GraphDatabaseWatcher` запускается. При закрытии документа watcher останавливается и context удаляется. При shutdown все document contexts очищаются.

DI container намеренно не используется: composition root небольшой и явно собран в одном factory, поэтому зависимости легко проследить по коду.

## 6. Runtime synchronization

Пользователь может изменять DWG не только через GraphPlugin commands, но и обычными средствами nanoCAD: grip editing, `ERASE`, `UNDO`.

За синхронизацию отвечает `GraphDatabaseWatcher`.

Он слушает:

- `Database.ObjectModified`;
- `Database.ObjectErased`;
- `Database.ObjectAppended`;
- `Document.CommandEnded`;
- `Document.CommandCancelled`;
- `Document.CommandFailed`.

События объектов во время команды сначала накапливаются. После `CommandEnded` watcher выполняет обработку в порядке:

```text
1. restored objects
2. appended/replacement objects
3. erased objects
4. dirty vertices
5. dirty edges
```

Это важно, потому что одна пользовательская команда может породить несколько связанных DB events.

### Перемещение Vertex

```text
native grip edit
    ↓
ObjectModified(Vertex)
    ↓
mark Vertex dirty
    ↓
CommandEnded
    ↓
EdgeGeometrySynchronizer
    ↓
incident Edge polylines updated
```

Синхронизация происходит по окончательному положению Vertex после завершения команды.

### Удаление Vertex

При обычном `ERASE` сама Vertex entity уже удаляется nanoCAD. Watcher после завершения команды:

1. определяет logical `VertexId` через runtime index;
2. удаляет incident Edge через `EdgeService`;
3. удаляет Vertex mapping из index.

Vertex повторно не стирается через repository.

### UNDO

При unerase watcher получает восстановленные `ObjectId`, читает их XRecords и в два прохода восстанавливает runtime topology:

```text
PASS 1: Vertex
PASS 2: Edge
```

Так Edge добавляются в index только после восстановления endpoint vertices.

### Circle <-> Triangle replacement

Смена формы Vertex физически заменяет DWG entity, но logical `VertexId` сохраняется.

Watcher отличает replacement от настоящего удаления: если тот же `VertexId` уже связан с новым живым `ObjectId`, erase старого объекта не считается удалением логической вершины.

## 7. Edge geometry

Topology Edge и drawing geometry разделены:

```text
Topology:
VertexAId + VertexBId
        хранится в GRAPH_EDGE

Geometry:
Polyline vertices
        хранится нативно в DWG
```

Endpoint polyline positions синхронизируются с Vertex positions.

Промежуточные вершины polyline являются bends и сохраняются при перемещении endpoint Vertex.

Это позволяет использовать стандартные nanoCAD grip points для редактирования bend geometry без отдельной custom entity implementation.

## 8. C++/CLI architecture

`GraphPlugin.Native` не зависит от C# runtime objects. Interop строится через общий persistence contract.

```text
GraphPlugin.Native/
├── Commands/
├── Persistence/
├── Services/
└── Tests/
```

### Production

`Commands/GraphCommands.*` содержит только пользовательские native commands.

`Persistence` содержит native readers/writers для `GRAPH_VERTEX` / `GRAPH_EDGE` metadata.

`Services` содержит операции:

- `NativeVertexStyleService`;
- `NativeVertexDeletionService`.

### Tests

Test infrastructure физически отделена от production code:

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

`NativeTestCommands` является тонким command host и делегирует работу runner/scenario classes.

`NativeTestDwgHelpers` содержит только test-specific DWG helpers и не используется production code.

### C# / C++ interoperability

Общими должны оставаться:

- XRecord names;
- schema versions;
- field order;
- DXF value types;
- numeric enum contracts;
- GUID semantics.

C++-изменение Vertex сразу становится доступно C#-части после обработки DWG events, потому что обе реализации читают один и тот же persisted contract.

## 9. Основные data flows

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

### Shortest path

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

### Persistence reopen

```text
SAVE / CLOSE / OPEN
    ↓
DWG entities + XRecords
    ↓
GraphDocumentContextFactory
    ↓
GraphEntityIndexBuilder
    ↓
restored runtime context
```

## 10. Testing architecture

Тестирование разделено по уровню.

### Unit tests

`GraphPlugin.Tests` зависит только от Domain/Application и не требует host assemblies для предметных тестов.

Проверяются:

- domain models;
- geometry;
- Dijkstra;
- application services;
- persistence contracts/enums.

### nanoCAD regression harness

`GraphPlugin.Nanocad.IntegrationTests` загружается отдельно и предоставляет два основных workflow:

```text
GRAPHTESTS
GRAPHTESTS_PERSISTENCE
```

`GRAPHTESTS` сохраняет реальные nanoCAD command boundaries и проверяет, в частности, настоящий `ERASE`/`UNDO`, watcher synchronization и C++/C# interoperability.

Persistence test специально требует реального `SAVE -> CLOSE -> OPEN`, чтобы не подменять проверку сериализации чтением того же runtime state.

Подробнее: `docs/testing.md`.

## 11. Архитектурные инварианты

При дальнейшем развитии проекта важно сохранять следующие правила:

1. `Domain` не должен ссылаться на nanoCAD assemblies.
2. `Application` не должен знать про `Document`, `Database`, `ObjectId` или `Entity`.
3. Persisted state хранится в DWG; `GraphEntityIndex` не является persistence storage.
4. Stable logical identity — `Guid`, а не `ObjectId` или `Handle`.
5. Edge topology задаётся metadata, а не выводится из совпадения координат polyline endpoints.
6. Удаление Vertex всегда означает удаление incident edges.
7. C# и C++ обязаны соблюдать один DWG schema contract.
8. Изменение schema layout требует решения о версии и interop regression test.
9. Host event synchronization должна учитывать command boundaries и `UNDO`.
10. Test-only helpers и commands не должны попадать обратно в production command classes.

## 12. Осознанные упрощения

Текущая архитектура не вводит абстракции без реальной границы ответственности:

- нет отдельного persisted `Graph` aggregate;
- нет `IShortestPathService` при единственном чистом алгоритме;
- нет общего `GraphService` с разнородными операциями;
- нет DI container;
- нет custom DWG entity type для Edge/Vertex;
- нет общей runtime-модели между C# и C++.

Эти решения уменьшают количество слоёв и делают критичные для nanoCAD операции — persistence, transactions, command boundaries и synchronization — явными в коде.
