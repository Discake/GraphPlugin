using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Editing.Bends;

public sealed record RemoveBendResult(
    GraphEdge Edge,
    int BendIndex,
    Point2 RemovedPoint);
