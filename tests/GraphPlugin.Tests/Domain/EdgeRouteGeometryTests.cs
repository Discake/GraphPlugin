using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class EdgeRouteGeometryTests
{
    [Fact]
    public void ProjectOntoSegment_ProjectsPointOntoSegment()
    {
        var result = EdgeRouteGeometry.ProjectOntoSegment(new Point2(0, 0), new Point2(10, 0), new Point2(4, 5));

        Assert.Equal(new Point2(4, 0), result);
    }

    [Fact]
    public void ProjectOntoSegment_ClampsProjectionToSegmentEndpoint()
    {
        var result = EdgeRouteGeometry.ProjectOntoSegment(new Point2(0, 0), new Point2(10, 0), new Point2(20, 5));

        Assert.Equal(new Point2(10, 0), result);
    }

    [Fact]
    public void FindClosestSegment_ReturnsCorrectSegment()
    {
        var route = new EdgeRoute(new[] { new Point2(0, 10), new Point2(10, 10) });

        // Маршрут:
        //
        // P1 -------- P2
        // |             |
        // |             |
        // A             B

        var result = EdgeRouteGeometry.FindClosestSegment(
            new Point2(0, 0),
            new Point2(10, 0),
            route,
            new Point2(6, 12)
        );

        Assert.Equal(1, result.SegmentIndex);

        Assert.Equal(new Point2(6, 10), result.Point);

        Assert.Equal(2, result.Distance, 6);
    }

    [Fact]
    public void Split_StraightRoute_CreatesTwoStraightRoutes()
    {
        var route = EdgeRoute.Straight;

        var result = EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(10, 0), route, new Point2(4, 0));

        Assert.True(result.LeftRoute.IsStraight);

        Assert.True(result.RightRoute.IsStraight);

        Assert.Equal(new Point2(4, 0), result.SplitPoint);

        Assert.Equal(0, result.SegmentIndex);
    }

    [Fact]
    public void Split_InsideSegment_DistributesBendsBetweenRoutes()
    {
        var p1 = new Point2(0, 10);

        var p2 = new Point2(10, 10);

        var p3 = new Point2(20, 10);

        var route = new EdgeRoute(new[] { p1, p2, p3 });

        var result = EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(20, 0), route, new Point2(15, 10));

        Assert.Equal(2, result.SegmentIndex);

        Assert.Equal(new Point2(15, 10), result.SplitPoint);

        Assert.Equal(2, result.LeftRoute.Count);

        Assert.Equal(p1, result.LeftRoute.IntermediatePoints[0]);

        Assert.Equal(p2, result.LeftRoute.IntermediatePoints[1]);

        Assert.Single(result.RightRoute.IntermediatePoints);

        Assert.Equal(p3, result.RightRoute.IntermediatePoints[0]);
    }

    [Fact]
    public void Split_AtExistingBend_RemovesBendFromBothRoutes()
    {
        var p1 = new Point2(0, 10);

        var p2 = new Point2(10, 10);

        var p3 = new Point2(20, 10);

        var route = new EdgeRoute(new[] { p1, p2, p3 });

        var result = EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(20, 0), route, p2);

        Assert.Single(result.LeftRoute.IntermediatePoints);

        Assert.Equal(p1, result.LeftRoute.IntermediatePoints[0]);

        Assert.Single(result.RightRoute.IntermediatePoints);

        Assert.Equal(p3, result.RightRoute.IntermediatePoints[0]);

        Assert.DoesNotContain(p2, result.LeftRoute.IntermediatePoints);

        Assert.DoesNotContain(p2, result.RightRoute.IntermediatePoints);

        Assert.Equal(p2, result.SplitPoint);
    }

    [Fact]
    public void Split_AtStart_Throws()
    {
        var route = new EdgeRoute(new[] { new Point2(5, 5) });

        Assert.Throws<ArgumentException>(() =>
            EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(10, 0), route, new Point2(0, 0))
        );
    }

    [Fact]
    public void Split_AtEnd_Throws()
    {
        var route = new EdgeRoute(new[] { new Point2(5, 5) });

        Assert.Throws<ArgumentException>(() =>
            EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(10, 0), route, new Point2(10, 0))
        );
    }

    [Fact]
    public void Split_PointOutsideTolerance_Throws()
    {
        var route = EdgeRoute.Straight;

        Assert.Throws<ArgumentException>(() =>
            EdgeRouteGeometry.Split(new Point2(0, 0), new Point2(10, 0), route, new Point2(5, 1), tolerance: 0.01)
        );
    }

    [Fact]
    public void Split_PreservesTotalRouteLength()
    {
        var start = new Point2(0, 0);

        var end = new Point2(20, 0);

        var route = new EdgeRoute(new[] { new Point2(0, 10), new Point2(10, 10), new Point2(20, 10) });

        var originalLength = EdgeRouteGeometry.CalculateLength(start, end, route);

        var split = EdgeRouteGeometry.Split(start, end, route, new Point2(15, 10));

        var leftLength = EdgeRouteGeometry.CalculateLength(start, split.SplitPoint, split.LeftRoute);

        var rightLength = EdgeRouteGeometry.CalculateLength(split.SplitPoint, end, split.RightRoute);

        Assert.Equal(originalLength, leftLength + rightLength, 6);
    }
}
