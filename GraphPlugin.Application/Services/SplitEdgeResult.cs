using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed record SplitEdgeResult(
    GraphEdge OriginalEdge,
    GraphVertex NewVertex,
    GraphEdge EdgeA,
    GraphEdge EdgeB);