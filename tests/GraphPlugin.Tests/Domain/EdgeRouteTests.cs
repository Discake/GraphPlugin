using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Domain;
public sealed class EdgeRouteTests
{
    [Fact]
    public void Straight_HasNoIntermediatePoints()
    {
        var route =
            EdgeRoute.Straight;

        Assert.True(
            route.IsStraight);

        Assert.Empty(
            route.IntermediatePoints);
    }

    [Fact]
    public void EnumeratePath_ReturnsEndpointsAndIntermediatePointsInOrder()
    {
        var route =
            new EdgeRoute(
                new[]
                {
                new Point2(10, 0),
                new Point2(10, 10)
                });

        var points =
            route
                .EnumeratePath(
                    new Point2(0, 0),
                    new Point2(20, 10))
                .ToArray();

        Assert.Equal(
            4,
            points.Length);

        Assert.Equal(
            new Point2(0, 0),
            points[0]);

        Assert.Equal(
            new Point2(10, 0),
            points[1]);

        Assert.Equal(
            new Point2(10, 10),
            points[2]);

        Assert.Equal(
            new Point2(20, 10),
            points[3]);
    }

    [Fact]
    public void InsertAtSegment_InsertsPointAtCorrectPosition()
    {
        var route =
            new EdgeRoute(
                new[]
                {
                new Point2(10, 10),
                new Point2(20, 10)
                });

        var updated =
            route.InsertAtSegment(
                1,
                new Point2(15, 15));

        Assert.Equal(
            3,
            updated.Count);

        Assert.Equal(
            new Point2(10, 10),
            updated.IntermediatePoints[0]);

        Assert.Equal(
            new Point2(15, 15),
            updated.IntermediatePoints[1]);

        Assert.Equal(
            new Point2(20, 10),
            updated.IntermediatePoints[2]);

        // Исходный route не мутировал.
        Assert.Equal(
            2,
            route.Count);
    }

    [Fact]
    public void InsertAtSegment_AllowsFirstAndLastSegment()
    {
        var route =
            new EdgeRoute(
                new[]
                {
                new Point2(10, 0)
                });

        var first =
            route.InsertAtSegment(
                0,
                new Point2(5, 5));

        Assert.Equal(
            new Point2(5, 5),
            first.IntermediatePoints[0]);

        var last =
            route.InsertAtSegment(
                1,
                new Point2(15, 5));

        Assert.Equal(
            new Point2(15, 5),
            last.IntermediatePoints[1]);
    }

    [Fact]
    public void MovePoint_ChangesOnlySelectedIntermediatePoint()
    {
        var route =
            new EdgeRoute(
                new[]
                {
                new Point2(10, 0),
                new Point2(20, 0)
                });

        var updated =
            route.MovePoint(
                0,
                new Point2(10, 10));

        Assert.Equal(
            new Point2(10, 10),
            updated.IntermediatePoints[0]);

        Assert.Equal(
            new Point2(20, 0),
            updated.IntermediatePoints[1]);

        Assert.Equal(
            new Point2(10, 0),
            route.IntermediatePoints[0]);
    }

    [Fact]
    public void RemovePoint_RemovesSelectedIntermediatePoint()
    {
        var route =
            new EdgeRoute(
                new[]
                {
                new Point2(10, 0),
                new Point2(20, 0)
                });

        var updated =
            route.RemovePoint(0);

        Assert.Single(
            updated.IntermediatePoints);

        Assert.Equal(
            new Point2(20, 0),
            updated.IntermediatePoints[0]);
    }

    [Fact]
    public void Split_BentEdge_DistributesIntermediatePoints()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 =
            new Point2(0, 10);

        var p2 =
            new Point2(10, 10);

        var p3 =
            new Point2(20, 10);

        var original =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    p1,
                    p2,
                    p3
                    }));

        edges.Add(original);

        var service =
            new SplitEdgeService(
                vertices,
                edges);

        var result =
            service.Split(
                original.Id,
                new Point2(15, 10));

        Assert.Equal(
            new Point2(15, 10),
            result.NewVertex.Position);

        Assert.Equal(
            a.Id,
            result.EdgeA.VertexAId);

        Assert.Equal(
            result.NewVertex.Id,
            result.EdgeA.VertexBId);

        Assert.Equal(
            result.NewVertex.Id,
            result.EdgeB.VertexAId);

        Assert.Equal(
            b.Id,
            result.EdgeB.VertexBId);

        Assert.Equal(
            2,
            result.EdgeA.Route.Count);

        Assert.Equal(
            p1,
            result.EdgeA.Route
                .IntermediatePoints[0]);

        Assert.Equal(
            p2,
            result.EdgeA.Route
                .IntermediatePoints[1]);

        Assert.Single(
            result.EdgeB.Route
                .IntermediatePoints);

        Assert.Equal(
            p3,
            result.EdgeB.Route
                .IntermediatePoints[0]);

        Assert.Null(
            edges.Get(original.Id));
    }

    [Fact]
    public void Split_AtExistingBend_RemovesBendFromNewRoutes()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 =
            new Point2(0, 10);

        var p2 =
            new Point2(10, 10);

        var p3 =
            new Point2(20, 10);

        var original =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    p1,
                    p2,
                    p3
                    }));

        edges.Add(original);

        var service =
            new SplitEdgeService(
                vertices,
                edges);

        var result =
            service.Split(
                original.Id,
                p2);

        Assert.Equal(
            p2,
            result.NewVertex.Position);

        Assert.Single(
            result.EdgeA.Route
                .IntermediatePoints);

        Assert.Equal(
            p1,
            result.EdgeA.Route
                .IntermediatePoints[0]);

        Assert.Single(
            result.EdgeB.Route
                .IntermediatePoints);

        Assert.Equal(
            p3,
            result.EdgeB.Route
                .IntermediatePoints[0]);

        Assert.DoesNotContain(
            p2,
            result.EdgeA.Route
                .IntermediatePoints);

        Assert.DoesNotContain(
            p2,
            result.EdgeB.Route
                .IntermediatePoints);
    }

    [Fact]
    public void Split_BentEdge_PreservesTotalLength()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    new Point2(0, 10),
                    new Point2(10, 10),
                    new Point2(20, 10)
                    }));

        edges.Add(original);

        var originalLength =
            EdgeRouteGeometry.CalculateLength(
                a.Position,
                b.Position,
                original.Route);

        var service =
            new SplitEdgeService(
                vertices,
                edges);

        var result =
            service.Split(
                original.Id,
                new Point2(15, 10));

        var leftLength =
            EdgeRouteGeometry.CalculateLength(
                a.Position,
                result.NewVertex.Position,
                result.EdgeA.Route);

        var rightLength =
            EdgeRouteGeometry.CalculateLength(
                result.NewVertex.Position,
                b.Position,
                result.EdgeB.Route);

        Assert.Equal(
            originalLength,
            leftLength + rightLength,
            6);
    }

    [Fact]
    public void Split_AtEdgeEndpoint_Throws()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    new Point2(5, 5)
                    }));

        edges.Add(edge);

        var service =
            new SplitEdgeService(
                vertices,
                edges);

        Assert.Throws<ArgumentException>(
            () =>
                service.Split(
                    edge.Id,
                    a.Position));

        Assert.NotNull(
            edges.Get(edge.Id));

        Assert.Equal(
            2,
            vertices.GetAll().Count);
    }

    [Fact]
    public void Split_PointOutsideRoute_ThrowsWithoutChangingGraph()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original =
            GraphEdge.Create(
                a.Id,
                b.Id);

        edges.Add(original);

        var service =
            new SplitEdgeService(
                vertices,
                edges);

        Assert.Throws<ArgumentException>(
            () =>
                service.Split(
                    original.Id,
                    new Point2(5, 50)));

        Assert.NotNull(
            edges.Get(original.Id));

        Assert.Single(
            edges.GetAll());

        Assert.Equal(
            2,
            vertices.GetAll().Count);
    }


}
