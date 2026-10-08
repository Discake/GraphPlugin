using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public class MoveBendServiceTests
{
    [Fact]
    public void Move_ChangesSelectedBendOnly()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 = new Point2(5, 5);

        var p2 = new Point2(15, 5);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { p1, p2 }));

        edges.Add(edge);

        var service = new MoveBendService(vertices, edges);

        var newPosition = new Point2(5, 10);

        var result = service.Move(edge.Id, 0, newPosition);

        Assert.Equal(0, result.BendIndex);

        Assert.Equal(p1, result.OldPosition);

        Assert.Equal(newPosition, result.NewPosition);

        Assert.Equal(newPosition, edge.Route.IntermediatePoints[0]);

        Assert.Equal(p2, edge.Route.IntermediatePoints[1]);
    }

    [Fact]
    public void Move_FirstBendOntoVertexA_Throws()
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

        var service = new MoveBendService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Move(edge.Id, 0, a.Position));

        Assert.Equal(bend, edge.Route.IntermediatePoints[0]);
    }

    [Fact]
    public void Move_BendOntoAdjacentBend_Throws()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(20, 0));

        vertices.Add(a);
        vertices.Add(b);

        var p1 = new Point2(5, 5);

        var p2 = new Point2(15, 5);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { p1, p2 }));

        edges.Add(edge);

        var service = new MoveBendService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Move(edge.Id, 0, p2));

        Assert.Equal(p1, edge.Route.IntermediatePoints[0]);

        Assert.Equal(p2, edge.Route.IntermediatePoints[1]);
    }

    [Fact]
    public void Move_InvalidBendIndex_Throws()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var edge = GraphEdge.Create(a.Id, b.Id, new EdgeRoute(new[] { new Point2(5, 5) }));

        edges.Add(edge);

        var service = new MoveBendService(vertices, edges);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Move(edge.Id, 1, new Point2(5, 10)));
    }
}
