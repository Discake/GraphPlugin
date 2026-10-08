using GraphPlugin.Application.Graph;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class VertexServiceTests
{
    [Fact]
    public void CreateVertex_AddsVertexToRepository()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var vertex = service.CreateVertex(new Point2(100, 200));

        Assert.Equal(1, vertices.Count);
        Assert.Same(vertex, vertices.Get(vertex.Id));
    }

    [Fact]
    public void CreateVertex_DefaultShape_IsCircle()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var vertex = service.CreateVertex(new Point2(0, 0));

        Assert.Equal(VertexShape.Circle, vertex.Style.Shape);
    }

    [Fact]
    public void CreateVertex_Circle_IsBlue()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var vertex = service.CreateVertex(new Point2(0, 0), VertexShape.Circle);

        Assert.Equal(GraphColor.Blue, vertex.Style.Color);
    }

    [Fact]
    public void CreateVertex_Triangle_IsRed()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var vertex = service.CreateVertex(new Point2(0, 0), VertexShape.Triangle);

        Assert.Equal(GraphColor.Red, vertex.Style.Color);
    }

    [Fact]
    public void DeleteVertex_RemovesVertexAndAllIncidentEdges()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var center = GraphVertex.Create(new Point2(0, 0));
        var a = GraphVertex.Create(new Point2(10, 0));
        var b = GraphVertex.Create(new Point2(0, 10));

        vertices.Add(center);
        vertices.Add(a);
        vertices.Add(b);

        var edgeA = GraphEdge.Create(center.Id, a.Id);
        var edgeB = GraphEdge.Create(center.Id, b.Id);
        edges.Add(edgeA);
        edges.Add(edgeB);

        service.DeleteVertex(center.Id);

        Assert.Null(vertices.Get(center.Id));
        Assert.Null(edges.Get(edgeA.Id));
        Assert.Null(edges.Get(edgeB.Id));
        Assert.NotNull(vertices.Get(a.Id));
        Assert.NotNull(vertices.Get(b.Id));
    }

    [Fact]
    public void DeleteVertex_DoesNotAffectAnotherConnectedComponent()
    {
        var vertices = new FakeVertexRepository();
        var edges = new FakeEdgeRepository();
        var service = new VertexService(vertices, edges);

        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));
        var c = GraphVertex.Create(new Point2(100, 0));
        var d = GraphVertex.Create(new Point2(110, 0));

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        var edgeAB = GraphEdge.Create(a.Id, b.Id);
        var edgeCD = GraphEdge.Create(c.Id, d.Id);
        edges.Add(edgeAB);
        edges.Add(edgeCD);

        service.DeleteVertex(a.Id);

        Assert.Null(vertices.Get(a.Id));
        Assert.Null(edges.Get(edgeAB.Id));
        Assert.NotNull(vertices.Get(c.Id));
        Assert.NotNull(vertices.Get(d.Id));
        Assert.NotNull(edges.Get(edgeCD.Id));
    }
}
