# DWG persistence schema

GraphPlugin stores graph state in ordinary nanoCAD entities plus XRecords. The DWG file is the persistence boundary shared by the C# and C++ implementations.

All currently supported records use schema version `1`. A record that explicitly declares another version must be rejected instead of being interpreted as version `1`.

## Entity representation

### Vertex

A vertex is stored as a native drawing entity:

- `Circle` for `VertexShape.Circle`
- closed triangular `Polyline` for `VertexShape.Triangle`

The entity extension dictionary contains the `GRAPH_VERTEX` XRecord.

### Edge

An edge is always stored as a `Polyline`.

- a straight edge has two polyline vertices: A and B;
- intermediate polyline vertices are graph bends;
- topology is not inferred from the polyline endpoints and is stored explicitly in `GRAPH_EDGE`.

## `GRAPH_VERTEX`

Location: vertex entity extension dictionary.

| Index | DXF value | Meaning |
| --- | --- | --- |
| 0 | `Int32` | schema version (`1`) |
| 1 | `Text` | stable `VertexId` GUID in `D` format |
| 2 | `Int32` | `VertexShape` |
| 3 | `Int32` | `GraphColor` |
| 4 | `Real` | vertex size |

`VertexShape` numeric contract:

| Value | Shape |
| ---: | --- |
| 0 | Circle |
| 1 | Triangle |

`GraphColor` numeric contract:

| Value | Color |
| ---: | --- |
| 0 | Blue |
| 1 | Red |
| 2 | Green |
| 3 | White |
| 4 | Black |

The C++ `NativeVertexShape` and `NativeGraphColor` values must remain numerically identical to the C# domain enums.

## `GRAPH_EDGE`

Location: edge polyline extension dictionary.

| Index | DXF value | Meaning |
| --- | --- | --- |
| 0 | `Int32` | schema version (`1`) |
| 1 | `Text` | stable `EdgeId` GUID in `D` format |
| 2 | `Text` | endpoint A `VertexId` |
| 3 | `Text` | endpoint B `VertexId` |

The edge route itself is stored in the native polyline geometry and is therefore persisted by the DWG entity.

## `GRAPH_VERTEX_ATTACHMENTS`

Location: vertex entity extension dictionary.

| Index | DXF value | Meaning |
| --- | --- | --- |
| 0 | `Int32` | schema version (`1`) |
| 1 | `Int32` | attachment count `N` |
| 2..`N + 1` | `Text` | attachment paths |

The number of stored path values must exactly match the declared count.

## `GRAPH_PLUGIN_SETTINGS`

Location: Named Objects Dictionary.

| Index | DXF value | Meaning |
| --- | --- | --- |
| 0 | `Int32` | schema version (`1`) |
| 1 | `Int32` | edge `GraphColor` |
| 2 | `Int32` | `EdgeLineType` |
| 3 | `Real` | edge line weight in millimetres |

`EdgeLineType` numeric contract:

| Value | Line type |
| ---: | --- |
| 0 | Continuous |
| 1 | Dashed |
| 2 | Dotted |

## Versioning policy

The version stored in an XRecord describes that record's layout, not the plugin assembly version.

Rules:

1. Writers always emit the current schema version.
2. Readers accept only versions whose layout they explicitly understand.
3. An unsupported explicit version is an error; it must not silently fall back to defaults or be interpreted as the current layout.
4. Missing graph metadata means that the drawing entity is not a GraphPlugin entity.
5. Missing global settings mean `GraphSettings.Default`; this is different from an existing settings record with an unsupported version.
6. A future schema migration should add an explicit reader/migration path rather than weakening the version check.

## C# / C++ interoperability

C# and C++ do not share runtime model objects. They interoperate through the DWG contract above.

The following constants must remain synchronized between the two implementations:

- record names;
- record versions;
- field order and DXF types;
- `VertexShape` numeric values;
- `GraphColor` numeric values.

Changing any of these is a persistence-format change and requires a schema-version decision and interoperability regression tests.
