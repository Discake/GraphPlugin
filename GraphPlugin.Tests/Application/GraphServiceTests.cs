using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class GraphServiceTests
{
    [Fact]
    public void DeleteVertex_RemovesVertex()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertex =
            GraphVertex.Create(
                new Point2(0, 0));

        vertices.Add(vertex);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(
            vertex.Id);

        Assert.Null(
            vertices.Get(vertex.Id));
    }

    [Fact]
    public void DeleteVertex_RemovesIncidentEdges()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexA =
            GraphVertex.Create(
                new Point2(0, 0));

        var vertexB =
            GraphVertex.Create(
                new Point2(10, 0));

        vertices.Add(vertexA);
        vertices.Add(vertexB);

        var edge =
            GraphEdge.Create(
                vertexA.Id,
                vertexB.Id);

        edges.Add(edge);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(
            vertexA.Id);

        Assert.Null(
            edges.Get(edge.Id));
    }

    [Fact]
    public void DeleteVertex_DoesNotRemoveUnrelatedEdges()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexA =
            GraphVertex.Create(
                new Point2(0, 0));

        var vertexB =
            GraphVertex.Create(
                new Point2(10, 0));

        var vertexC =
            GraphVertex.Create(
                new Point2(20, 0));

        vertices.Add(vertexA);
        vertices.Add(vertexB);
        vertices.Add(vertexC);

        var edgeAB =
            GraphEdge.Create(
                vertexA.Id,
                vertexB.Id);

        var edgeBC =
            GraphEdge.Create(
                vertexB.Id,
                vertexC.Id);

        edges.Add(edgeAB);
        edges.Add(edgeBC);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(
            vertexA.Id);

        Assert.Null(
            edges.Get(edgeAB.Id));

        Assert.NotNull(
            edges.Get(edgeBC.Id));
    }

    [Fact]
    public void DeleteVertex_RemovesAllIncidentEdges()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var center =
            GraphVertex.Create(
                new Point2(0, 0));

        var vertexA =
            GraphVertex.Create(
                new Point2(10, 0));

        var vertexB =
            GraphVertex.Create(
                new Point2(0, 10));

        var vertexC =
            GraphVertex.Create(
                new Point2(-10, 0));

        vertices.Add(center);
        vertices.Add(vertexA);
        vertices.Add(vertexB);
        vertices.Add(vertexC);

        var edgeA =
            GraphEdge.Create(
                center.Id,
                vertexA.Id);

        var edgeB =
            GraphEdge.Create(
                center.Id,
                vertexB.Id);

        var edgeC =
            GraphEdge.Create(
                center.Id,
                vertexC.Id);

        edges.Add(edgeA);
        edges.Add(edgeB);
        edges.Add(edgeC);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(
            center.Id);

        Assert.Equal(
            0,
            edges.Count);
    }

    [Fact]
    public void DeleteEdge_RemovesOnlyEdge()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexA =
            GraphVertex.Create(
                new Point2(0, 0));

        var vertexB =
            GraphVertex.Create(
                new Point2(10, 0));

        vertices.Add(vertexA);
        vertices.Add(vertexB);

        var edge =
            GraphEdge.Create(
                vertexA.Id,
                vertexB.Id);

        edges.Add(edge);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteEdge(
            edge.Id);

        Assert.Null(
            edges.Get(edge.Id));

        Assert.NotNull(
            vertices.Get(vertexA.Id));

        Assert.NotNull(
            vertices.Get(vertexB.Id));
    }

    [Fact]
    public void DeleteVertex_RemovesOnlyTargetVertexAndItsEdges()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexA =
            GraphVertex.Create(
                new Point2(0, 0));

        var vertexB =
            GraphVertex.Create(
                new Point2(10, 0));

        var vertexC =
            GraphVertex.Create(
                new Point2(20, 0));

        vertices.Add(vertexA);
        vertices.Add(vertexB);
        vertices.Add(vertexC);

        edges.Add(
            GraphEdge.Create(
                vertexA.Id,
                vertexB.Id));

        edges.Add(
            GraphEdge.Create(
                vertexB.Id,
                vertexC.Id));

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(
            vertexB.Id);

        Assert.NotNull(
            vertices.Get(vertexA.Id));

        Assert.Null(
            vertices.Get(vertexB.Id));

        Assert.NotNull(
            vertices.Get(vertexC.Id));

        Assert.Equal(
            0,
            edges.Count);
    }

    [Fact]
    public void DeleteVertex_DoesNotAffectAnotherConnectedComponent()
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

        var c =
            GraphVertex.Create(
                new Point2(100, 0));

        var d =
            GraphVertex.Create(
                new Point2(110, 0));

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        var edgeAB =
            GraphEdge.Create(
                a.Id,
                b.Id);

        var edgeCD =
            GraphEdge.Create(
                c.Id,
                d.Id);

        edges.Add(edgeAB);
        edges.Add(edgeCD);

        var service =
            new GraphService(
                vertices,
                edges);

        service.DeleteVertex(a.Id);

        Assert.Null(
            vertices.Get(a.Id));

        Assert.Null(
            edges.Get(edgeAB.Id));

        Assert.NotNull(
            vertices.Get(c.Id));

        Assert.NotNull(
            vertices.Get(d.Id));

        Assert.NotNull(
            edges.Get(edgeCD.Id));
    }
}