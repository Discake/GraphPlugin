using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class GraphBuildServiceTests
{
    [Fact]
    public void AdvanceTo_FirstVertex_StartsBuildWithoutCreatingEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var service = new GraphBuildService(edgeService, edges);

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        var result = service.AdvanceTo(vertex);

        Assert.Null(result);

        Assert.Equal(vertex.Id, service.CurrentVertex!.Id);

        Assert.True(service.IsActive);

        Assert.Equal(0, edges.Count);
    }

    [Fact]
    public void AdvanceTo_SecondVertex_CreatesEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var service = new GraphBuildService(edgeService, edges);

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        service.AdvanceTo(a);

        var edge = service.AdvanceTo(b);

        Assert.NotNull(edge);

        Assert.True(edge.IsIncidentTo(a.Id));

        Assert.True(edge.IsIncidentTo(b.Id));

        Assert.Equal(b.Id, service.CurrentVertex!.Id);

        Assert.Equal(1, edges.Count);
    }

    [Fact]
    public void AdvanceTo_SameVertex_DoesNotCreateEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var service = new GraphBuildService(edgeService, edges);

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        service.AdvanceTo(vertex);

        var result = service.AdvanceTo(vertex);

        Assert.Null(result);

        Assert.Equal(0, edges.Count);

        Assert.Equal(vertex.Id, service.CurrentVertex!.Id);
    }

    [Fact]
    public void AdvanceTo_AlreadyConnectedVertex_DoesNotDuplicateEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var existingEdge = GraphEdge.Create(a.Id, b.Id);

        edges.Add(existingEdge);

        var service = new GraphBuildService(edgeService, edges);

        service.AdvanceTo(a);

        var result = service.AdvanceTo(b);

        Assert.Null(result);

        Assert.Equal(1, edges.Count);

        Assert.Equal(b.Id, service.CurrentVertex!.Id);
    }

    [Fact]
    public void AdvanceTo_VertexCreatedByIncidentSplit_DoesNotDuplicateEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var buildService = new GraphBuildService(edgeService, edges);

        var splitService = new SplitEdgeService(vertices, edges);

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        vertices.Add(a);
        vertices.Add(b);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        buildService.AdvanceTo(a);

        var split = splitService.Split(original.Id, new Point2(4, 0));

        Assert.Equal(2, edges.Count);

        var result = buildService.AdvanceTo(split.NewVertex);

        Assert.Null(result);

        // Всё ещё только A-C и C-B.
        Assert.Equal(2, edges.Count);

        Assert.Equal(split.NewVertex.Id, buildService.CurrentVertex!.Id);
    }

    [Fact]
    public void AdvanceTo_VertexCreatedByUnrelatedSplit_CreatesConnectingEdge()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var buildService = new GraphBuildService(edgeService, edges);

        var splitService = new SplitEdgeService(vertices, edges);

        var a = GraphVertex.Create(new Point2(0, 0));

        var b = GraphVertex.Create(new Point2(10, 0));

        var d = GraphVertex.Create(new Point2(5, 10));

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(d);

        var original = GraphEdge.Create(a.Id, b.Id);

        edges.Add(original);

        buildService.AdvanceTo(d);

        var split = splitService.Split(original.Id, new Point2(5, 0));

        Assert.Equal(2, edges.Count);

        var connectingEdge = buildService.AdvanceTo(split.NewVertex);

        Assert.NotNull(connectingEdge);

        Assert.Equal(3, edges.Count);

        Assert.True(connectingEdge.IsIncidentTo(d.Id));

        Assert.True(connectingEdge.IsIncidentTo(split.NewVertex.Id));
    }

    [Fact]
    public void Finish_ClearsCurrentVertex()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var service = new GraphBuildService(edgeService, edges);

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        service.AdvanceTo(vertex);

        Assert.True(service.IsActive);

        service.Finish();

        Assert.False(service.IsActive);

        Assert.Null(service.CurrentVertex);
    }

    [Fact]
    public void Finish_WhenAlreadyFinished_DoesNothing()
    {
        var vertices = new FakeVertexRepository();

        var edges = new FakeEdgeRepository();

        var edgeService = new EdgeService(vertices, edges);

        var service = new GraphBuildService(edgeService, edges);

        service.Finish();
        service.Finish();

        Assert.False(service.IsActive);

        Assert.Null(service.CurrentVertex);
    }
}
