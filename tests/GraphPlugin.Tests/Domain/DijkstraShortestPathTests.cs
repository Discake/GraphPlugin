using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class DijkstraShortestPathTests
{
    private static DijkstraShortestPath CreateAlgorithm() =>
        new(new EdgeLengthCalculator());

    private static ShortestPathResult Find(
        IEnumerable<GraphVertex> vertices,
        IEnumerable<GraphEdge> edges,
        Guid startVertexId,
        Guid endVertexId)
    {
        return CreateAlgorithm().Find(
            vertices.ToArray(),
            edges.ToArray(),
            startVertexId,
            endVertexId);
    }

    [Fact]
    public void Find_ReturnsDirectPath_WhenVerticesAreConnected()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));
        var edge = GraphEdge.Create(a.Id, b.Id);

        var result = Find(
            new[] { a, b },
            new[] { edge },
            a.Id,
            b.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { a.Id, b.Id }, result.VertexIds);
        Assert.Equal(new[] { edge.Id }, result.EdgeIds);
        Assert.Equal(10.0, result.TotalLength, 6);
    }

    [Fact]
    public void Find_SelectsShorterOfTwoAvailableRoutes()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(5, 0));
        var c = GraphVertex.Create(new Point2(0, 5));
        var d = GraphVertex.Create(new Point2(10, 0));

        var ab = GraphEdge.Create(a.Id, b.Id);
        var bd = GraphEdge.Create(b.Id, d.Id);
        var ac = GraphEdge.Create(a.Id, c.Id);
        var cd = GraphEdge.Create(c.Id, d.Id);

        var result = Find(
            new[] { a, b, c, d },
            new[] { ab, bd, ac, cd },
            a.Id,
            d.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { a.Id, b.Id, d.Id }, result.VertexIds);
        Assert.Equal(new[] { ab.Id, bd.Id }, result.EdgeIds);
        Assert.Equal(10.0, result.TotalLength, 6);
    }

    [Fact]
    public void Find_ReturnsNoPath_ForDisconnectedComponents()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));
        var c = GraphVertex.Create(new Point2(100, 0));
        var d = GraphVertex.Create(new Point2(110, 0));

        var ab = GraphEdge.Create(a.Id, b.Id);
        var cd = GraphEdge.Create(c.Id, d.Id);

        var result = Find(
            new[] { a, b, c, d },
            new[] { ab, cd },
            a.Id,
            d.Id);

        Assert.False(result.Found);
        Assert.Empty(result.VertexIds);
        Assert.Empty(result.EdgeIds);
        Assert.True(double.IsPositiveInfinity(result.TotalLength));
    }

    [Fact]
    public void Find_ReturnsZeroLength_WhenStartEqualsEnd()
    {
        var vertex = GraphVertex.Create(new Point2(5, 10));

        var result = Find(
            new[] { vertex },
            Array.Empty<GraphEdge>(),
            vertex.Id,
            vertex.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { vertex.Id }, result.VertexIds);
        Assert.Empty(result.EdgeIds);
        Assert.Equal(0, result.TotalLength);
    }

    [Fact]
    public void Find_WorksInBothDirections_ForUndirectedGraph()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));
        var c = GraphVertex.Create(new Point2(20, 0));

        var ab = GraphEdge.Create(a.Id, b.Id);
        var bc = GraphEdge.Create(b.Id, c.Id);

        var result = Find(
            new[] { a, b, c },
            new[] { ab, bc },
            c.Id,
            a.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { c.Id, b.Id, a.Id }, result.VertexIds);
        Assert.Equal(new[] { bc.Id, ab.Id }, result.EdgeIds);
        Assert.Equal(20.0, result.TotalLength, 6);
    }

    [Fact]
    public void Find_UsesImprovedTentativeDistance()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var x = GraphVertex.Create(new Point2(1, 0));
        var y = GraphVertex.Create(new Point2(0, 2));
        var b = GraphVertex.Create(new Point2(0, 3));

        var ax = GraphEdge.Create(a.Id, x.Id);
        var xb = GraphEdge.Create(x.Id, b.Id);
        var ay = GraphEdge.Create(a.Id, y.Id);
        var yb = GraphEdge.Create(y.Id, b.Id);

        var result = Find(
            new[] { a, x, y, b },
            new[] { ax, xb, ay, yb },
            a.Id,
            b.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { a.Id, y.Id, b.Id }, result.VertexIds);
        Assert.Equal(new[] { ay.Id, yb.Id }, result.EdgeIds);
        Assert.Equal(3.0, result.TotalLength, 6);
    }

    [Fact]
    public void Find_RecalculatesShortestPath_AfterVertexMoves()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(5, 0));
        var c = GraphVertex.Create(new Point2(0, 5));
        var d = GraphVertex.Create(new Point2(10, 0));

        var ab = GraphEdge.Create(a.Id, b.Id);
        var bd = GraphEdge.Create(b.Id, d.Id);
        var ac = GraphEdge.Create(a.Id, c.Id);
        var cd = GraphEdge.Create(c.Id, d.Id);
        var vertices = new[] { a, b, c, d };
        var edges = new[] { ab, bd, ac, cd };

        var beforeMove = Find(vertices, edges, a.Id, d.Id);
        Assert.Equal(new[] { a.Id, b.Id, d.Id }, beforeMove.VertexIds);

        b.MoveTo(new Point2(5, 20));

        var afterMove = Find(vertices, edges, a.Id, d.Id);
        Assert.Equal(new[] { a.Id, c.Id, d.Id }, afterMove.VertexIds);
        Assert.Equal(new[] { ac.Id, cd.Id }, afterMove.EdgeIds);
    }

    [Fact]
    public void Find_UsesPolylineRouteLength()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(100, 0));
        var c = GraphVertex.Create(new Point2(50, 20));

        var longDirect = GraphEdge.Create(
            a.Id,
            b.Id,
            new EdgeRoute(
                new[]
                {
                    new Point2(0, 100),
                    new Point2(100, 100)
                }));

        var ac = GraphEdge.Create(a.Id, c.Id);
        var cb = GraphEdge.Create(c.Id, b.Id);

        var result = Find(
            new[] { a, b, c },
            new[] { longDirect, ac, cb },
            a.Id,
            b.Id);

        Assert.True(result.Found);
        Assert.Equal(new[] { a.Id, c.Id, b.Id }, result.VertexIds);
        Assert.DoesNotContain(longDirect.Id, result.EdgeIds);
    }

    [Fact]
    public void Find_Throws_WhenStartVertexDoesNotExist()
    {
        var vertex = GraphVertex.Create(new Point2(0, 0));

        Assert.Throws<ArgumentException>(() =>
            Find(
                new[] { vertex },
                Array.Empty<GraphEdge>(),
                Guid.NewGuid(),
                vertex.Id));
    }

    [Fact]
    public void Find_Throws_WhenEndVertexDoesNotExist()
    {
        var vertex = GraphVertex.Create(new Point2(0, 0));

        Assert.Throws<ArgumentException>(() =>
            Find(
                new[] { vertex },
                Array.Empty<GraphEdge>(),
                vertex.Id,
                Guid.NewGuid()));
    }

    [Fact]
    public void Find_Throws_WhenEdgeReferencesMissingVertex()
    {
        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));
        var edge = GraphEdge.Create(a.Id, b.Id);

        Assert.Throws<InvalidOperationException>(() =>
            Find(
                new[] { a },
                new[] { edge },
                a.Id,
                a.Id));
    }
}
