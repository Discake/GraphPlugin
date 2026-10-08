using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Nanocad.Persistence;

public sealed record PersistenceTestManifest(
    int Version,
    Guid TestId,

    Guid VertexAId,
    Guid VertexBId,
    Guid VertexCId,

    Guid EdgeABId,
    Guid EdgeBCId,

    Point2 PositionA,
    Point2 PositionB,
    Point2 PositionC,

    GraphColor TestEdgeColor,
    EdgeLineType TestEdgeLineType,
    double TestEdgeLineWeightMm,

    GraphColor OriginalEdgeColor,
    EdgeLineType OriginalEdgeLineType,
    double OriginalEdgeLineWeightMm);