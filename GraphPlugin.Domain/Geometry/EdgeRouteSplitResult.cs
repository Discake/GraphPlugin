using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Geometry;

public sealed record EdgeRouteSplitResult(
    EdgeRoute LeftRoute,
    EdgeRoute RightRoute,
    Point2 SplitPoint,
    int SegmentIndex);