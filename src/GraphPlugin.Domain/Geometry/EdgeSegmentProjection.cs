using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Geometry;

public readonly record struct EdgeSegmentProjection(
    int SegmentIndex,
    Point2 Point,
    double Distance);