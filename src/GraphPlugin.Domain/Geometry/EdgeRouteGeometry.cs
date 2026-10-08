using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Geometry;

public static class EdgeRouteGeometry
{
    public const double DefaultTolerance =
        1e-6;

    public static double CalculateLength(
        Point2 start,
        Point2 end,
        EdgeRoute route)
    {
        ArgumentNullException.ThrowIfNull(
            route);

        double length =
            0;

        var previous =
            start;

        foreach (var point in
                 route.IntermediatePoints)
        {
            length +=
                previous.DistanceTo(
                    point);

            previous =
                point;
        }

        length +=
            previous.DistanceTo(
                end);

        return length;
    }

    public static Point2 ProjectOntoSegment(
        Point2 segmentStart,
        Point2 segmentEnd,
        Point2 point)
    {
        var dx =
            segmentEnd.X -
            segmentStart.X;

        var dy =
            segmentEnd.Y -
            segmentStart.Y;

        var lengthSquared =
            dx * dx +
            dy * dy;

        // Нулевой сегмент.
        if (lengthSquared <=
            DefaultTolerance *
            DefaultTolerance)
        {
            return segmentStart;
        }

        var px =
            point.X -
            segmentStart.X;

        var py =
            point.Y -
            segmentStart.Y;

        var t =
            (px * dx + py * dy) /
            lengthSquared;

        // Проекция должна лежать именно
        // на конечном сегменте, а не
        // на бесконечной прямой.
        t =
            Math.Clamp(
                t,
                0.0,
                1.0);

        return new Point2(
            segmentStart.X +
                t * dx,
            segmentStart.Y +
                t * dy);
    }

    public static EdgeSegmentProjection
        FindClosestSegment(
            Point2 start,
            Point2 end,
            EdgeRoute route,
            Point2 point)
    {
        ArgumentNullException.ThrowIfNull(
            route);

        var path =
            route
                .EnumeratePath(
                    start,
                    end)
                .ToArray();

        if (path.Length < 2)
        {
            throw new InvalidOperationException(
                "Edge route must contain at least two path points.");
        }

        var bestSegmentIndex =
            -1;

        var bestPoint =
            default(Point2);

        var bestDistance =
            double.PositiveInfinity;

        for (var i = 0;
             i < path.Length - 1;
             i++)
        {
            var projected =
                ProjectOntoSegment(
                    path[i],
                    path[i + 1],
                    point);

            var distance =
                projected.DistanceTo(
                    point);

            if (distance <
                bestDistance)
            {
                bestDistance =
                    distance;

                bestPoint =
                    projected;

                bestSegmentIndex =
                    i;
            }
        }

        if (bestSegmentIndex < 0)
        {
            throw new InvalidOperationException(
                "Unable to find an edge segment.");
        }

        return new EdgeSegmentProjection(
            bestSegmentIndex,
            bestPoint,
            bestDistance);
    }

    public static EdgeRouteSplitResult Split(
        Point2 start,
        Point2 end,
        EdgeRoute route,
        Point2 splitPoint,
        double tolerance =
            DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(
            route);

        ValidateTolerance(
            tolerance);

        var projection =
            FindClosestSegment(
                start,
                end,
                route,
                splitPoint);

        if (projection.Distance >
            tolerance)
        {
            throw new ArgumentException(
                "Split point does not lie on the edge route.",
                nameof(splitPoint));
        }

        // Используем именно точку маршрута,
        // а не потенциально слегка неточную
        // входную координату.
        var actualSplitPoint =
            projection.Point;

        if (AreEqual(
                actualSplitPoint,
                start,
                tolerance) ||
            AreEqual(
                actualSplitPoint,
                end,
                tolerance))
        {
            throw new ArgumentException(
                "Split point cannot coincide with an edge endpoint.",
                nameof(splitPoint));
        }

        var intermediate =
            route.IntermediatePoints;

        // Отдельно проверяем случай,
        // когда split происходит прямо
        // на существующем bend.
        for (var i = 0;
             i < intermediate.Count;
             i++)
        {
            if (!AreEqual(
                    actualSplitPoint,
                    intermediate[i],
                    tolerance))
            {
                continue;
            }

            var leftPoints =
                intermediate
                    .Take(i)
                    .ToArray();

            var rightPoints =
                intermediate
                    .Skip(i + 1)
                    .ToArray();

            return new EdgeRouteSplitResult(
                new EdgeRoute(
                    leftPoints),
                new EdgeRoute(
                    rightPoints),
                intermediate[i],
                projection.SegmentIndex);
        }

        // Split находится внутри сегмента.
        //
        // A -> P1 -> P2 -> P3 -> B
        //
        // segmentIndex = 2:
        //
        // P2 -------- P3
        //       C
        //
        // left:
        // P1, P2
        //
        // right:
        // P3
        var leftIntermediate =
            intermediate
                .Take(
                    projection.SegmentIndex)
                .ToArray();

        var rightIntermediate =
            intermediate
                .Skip(
                    projection.SegmentIndex)
                .ToArray();

        return new EdgeRouteSplitResult(
            new EdgeRoute(
                leftIntermediate),
            new EdgeRoute(
                rightIntermediate),
            actualSplitPoint,
            projection.SegmentIndex);
    }

    private static bool AreEqual(
        Point2 a,
        Point2 b,
        double tolerance)
    {
        return
            a.DistanceTo(b) <=
            tolerance;
    }

    private static void ValidateTolerance(
        double tolerance)
    {
        if (double.IsNaN(tolerance) ||
            double.IsInfinity(tolerance) ||
            tolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance));
        }
    }
}