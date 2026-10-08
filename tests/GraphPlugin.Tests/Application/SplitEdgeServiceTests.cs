using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class SplitEdgeServiceTests
{
    [Fact]
    public void Split_ReplacesOneEdgeWithTwoEdges()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        var result = service.Split(original.Id, new Point2(4, 0));

        Assert.Null(edges.Get(original.Id));

        Assert.NotNull(vertices.Get(result.NewVertex.Id));

        Assert.Equal(new Point2(4, 0), result.NewVertex.Position);

        Assert.Equal(3, vertices.Count);

        Assert.Equal(2, edges.Count);
    }

    [Fact]
    public void Split_CreatesCorrectTopology()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        var result = service.Split(original.Id, new Point2(4, 0));

        Assert.Equal(a.Id, result.EdgeA.VertexAId);

        Assert.Equal(result.NewVertex.Id, result.EdgeA.VertexBId);

        Assert.Equal(result.NewVertex.Id, result.EdgeB.VertexAId);

        Assert.Equal(b.Id, result.EdgeB.VertexBId);
    }

    [Fact]
    public void Split_PreservesTotalGeometricLength()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        var result = service.Split(original.Id, new Point2(3, 0));

        var oldLength = a.Position.DistanceTo(b.Position);

        var newLength =
            a.Position.DistanceTo(result.NewVertex.Position) + result.NewVertex.Position.DistanceTo(b.Position);

        Assert.Equal(oldLength, newLength, 6);
    }

    [Fact]
    public void Split_CreatesVertexWithRequestedShape()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        var result = service.Split(original.Id, new Point2(5, 0), VertexShape.Triangle);

        Assert.Equal(VertexShape.Triangle, result.NewVertex.Style.Shape);

        Assert.Equal(GraphColor.Red, result.NewVertex.Style.Color);
    }

    [Fact]
    public void Split_Throws_WhenPositionIsOutsideEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Split(original.Id, new Point2(5, 2)));

        Assert.Equal(2, vertices.Count);

        Assert.Equal(1, edges.Count);

        Assert.NotNull(edges.Get(original.Id));
    }

    [Fact]
    public void Split_Throws_WhenPositionEqualsEndpoint()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        var service = new SplitEdgeService(vertices, edges);

        Assert.Throws<ArgumentException>(() => service.Split(original.Id, a.Position));

        Assert.Equal(2, vertices.Count);

        Assert.Equal(1, edges.Count);
    }

    [Fact]
    public void Split_Throws_WhenEdgeDoesNotExist()
    {
        var service = new SplitEdgeService(new FakeVertexRepository(), new FakeEdgeRepository());

        Assert.Throws<InvalidOperationException>(() => service.Split(Guid.NewGuid(), new Point2(5, 0)));
    }
}
