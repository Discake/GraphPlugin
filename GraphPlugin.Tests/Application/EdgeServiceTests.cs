using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class EdgeServiceTests
{
    [Fact]
    public void CreateEdge_AddsEdgeToRepository()
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

        var service =
            new EdgeService(
                vertices,
                edges);

        var edge =
            service.CreateEdge(
                vertexA.Id,
                vertexB.Id);

        Assert.Equal(
            1,
            edges.Count);

        Assert.Same(
            edge,
            edges.Get(edge.Id));
    }

    [Fact]
    public void CreateEdge_Throws_WhenFirstVertexDoesNotExist()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexB =
            GraphVertex.Create(
                new Point2(10, 0));

        vertices.Add(vertexB);

        var service =
            new EdgeService(
                vertices,
                edges);

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateEdge(
                Guid.NewGuid(),
                vertexB.Id));

        Assert.Equal(
            0,
            edges.Count);
    }

    [Fact]
    public void CreateEdge_Throws_WhenSecondVertexDoesNotExist()
    {
        var vertices =
            new FakeVertexRepository();

        var edges =
            new FakeEdgeRepository();

        var vertexA =
            GraphVertex.Create(
                new Point2(0, 0));

        vertices.Add(vertexA);

        var service =
            new EdgeService(
                vertices,
                edges);

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateEdge(
                vertexA.Id,
                Guid.NewGuid()));

        Assert.Equal(
            0,
            edges.Count);
    }

    [Fact]
    public void CreateEdge_Throws_WhenConnectingVertexToItself()
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
            new EdgeService(
                vertices,
                edges);

        Assert.Throws<ArgumentException>(() =>
            service.CreateEdge(
                vertex.Id,
                vertex.Id));

        Assert.Equal(
            0,
            edges.Count);
    }
}