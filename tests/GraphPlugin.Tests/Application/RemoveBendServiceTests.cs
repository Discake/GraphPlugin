using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public class RemoveBendServiceTests
{
    [Fact]
    public void Remove_RemovesSelectedBend()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 = new Point2(5, 5);

        var p2 = new Point2(10, 10);

        var p3 = new Point2(15, 5);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { p1, p2, p3 }));

        edges.Add(edge);

        var service = new RemoveBendService(edges);

        var result = service.Remove(edge.Id, 1);

        Assert.Equal(p2, result.RemovedPoint);

        Assert.Equal(1, result.BendIndex);

        Assert.Equal(2, edge.Route.Count);

        Assert.Equal(p1, edge.Route.IntermediatePoints[0]);

        Assert.Equal(p3, edge.Route.IntermediatePoints[1]);
    }

    [Fact]
    public void Remove_LastRemainingBend_MakesEdgeStraight()
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

        var service = new RemoveBendService(edges);

        var result = service.Remove(edge.Id, 0);

        Assert.Equal(bend, result.RemovedPoint);

        Assert.True(edge.Route.IsStraight);

        Assert.Empty(edge.Route.IntermediatePoints);
    }

    [Fact]
    public void Remove_FromStraightEdge_Throws()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(edge);

        var service = new RemoveBendService(edges);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Remove(edge.Id, 0));

        Assert.True(edge.Route.IsStraight);
    }
}
