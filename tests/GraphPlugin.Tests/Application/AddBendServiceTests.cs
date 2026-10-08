using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public class AddBendServiceTests
{
    [Fact]
    public void Add_ToStraightEdge_AddsOneBend()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        var result = service.Add(edge.Id, new Point2(4, 0));

        Assert.Equal(1, result.Edge.Route.Count);

        Assert.Equal(new Point2(4, 0), result.Edge.Route.IntermediatePoints[0]);

        Assert.Equal(0, result.BendIndex);

        Assert.Equal(0, result.SegmentIndex);
    }

    [Fact]
    public void Add_ToBentEdge_InsertsIntoCorrectSegment()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 = new Point2(0, 10);

        var p2 = new Point2(20, 10);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { p1, p2 }));

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        var result = service.Add(edge.Id, new Point2(10, 10));

        Assert.Equal(3, result.Edge.Route.Count);

        Assert.Equal(p1, result.Edge.Route.IntermediatePoints[0]);

        Assert.Equal(new Point2(10, 10), result.Edge.Route.IntermediatePoints[1]);

        Assert.Equal(p2, result.Edge.Route.IntermediatePoints[2]);

        Assert.Equal(1, result.BendIndex);

        Assert.Equal(1, result.SegmentIndex);
    }

    [Fact]
    public void Add_PointWithinTolerance_UsesProjectedPoint()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        var result = service.Add(edge.Id, new Point2(5, 0.001), tolerance: 0.01);

        Assert.Equal(new Point2(5, 0), result.BendPoint);

        Assert.Equal(new Point2(5, 0), result.Edge.Route.IntermediatePoints[0]);
    }

    [Fact]
    public void Add_PointOutsideRoute_ThrowsWithoutChangingEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Add(edge.Id, new Point2(5, 10)));

        Assert.True(edge.Route.IsStraight);
    }

    [Fact]
    public void Add_AtEndpoint_ThrowsWithoutChangingRoute()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Add(edge.Id, a.Position));

        Assert.True(edge.Route.IsStraight);
    }

    [Fact]
    public void Add_AtExistingBend_ThrowsWithoutChangingRoute()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var bend = new Point2(5, 5);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { bend }));

        edges.Add(edge);

        var service = new AddBendService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Add(edge.Id, bend));

        Assert.Single(edge.Route.IntermediatePoints);
    }
}
