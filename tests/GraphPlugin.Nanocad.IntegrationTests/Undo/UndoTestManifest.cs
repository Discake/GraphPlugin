using GraphPlugin.Domain.Geometry;

namespace GraphPlugin.Nanocad.Persistence;

public enum UndoTestScenario
{
    Edge = 0,
    Vertex = 1,
}

public sealed record UndoTestManifest(
    int Version,
    Guid TestId,
    UndoTestScenario Scenario,
    Guid VertexAId,
    Guid VertexBId,
    Guid? VertexCId,
    Guid EdgeABId,
    Guid? EdgeBCId,
    Point2 PositionA,
    Point2 PositionB,
    Point2? PositionC
);
