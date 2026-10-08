using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Editing.Bends;

public sealed record AddBendResult(
    GraphEdge Edge,
    Point2 BendPoint,
    int BendIndex,
    int SegmentIndex);
