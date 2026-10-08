using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Editing.Splitting;

public sealed record SplitEdgeResult(
    GraphEdge OriginalEdge,
    GraphVertex NewVertex,
    GraphEdge EdgeA,
    GraphEdge EdgeB);
