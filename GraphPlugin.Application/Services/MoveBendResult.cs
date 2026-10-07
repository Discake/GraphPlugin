using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed record MoveBendResult(
    GraphEdge Edge,
    int BendIndex,
    Point2 OldPosition,
    Point2 NewPosition);