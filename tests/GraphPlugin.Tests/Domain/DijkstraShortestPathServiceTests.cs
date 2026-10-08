using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class DijkstraShortestPathServiceTests
{
    private static DijkstraShortestPathService CreateService()
    {
        return new DijkstraShortestPathService(
            new EdgeLengthCalculator());
    }

    [Fact]
    public void Find_ReturnsDirectPath_WhenVerticesAreConnected()
    {
        var graph = new Graph();

        var a = GraphVertex.Create(new Point2(0, 0));
        var b = GraphVertex.Create(new Point2(10, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);

        var edge = GraphEdge.Create(a.Id, b.Id);

        graph.AddEdge(edge);

        var service = CreateService();

        var result =
            service.Find(
                graph,
                a.Id,
                b.Id);

        Assert.True(result.Found);

        Assert.Equal(
            new[] { a.Id, b.Id },
            result.VertexIds);

        Assert.Equal(
            new[] { edge.Id },
            result.EdgeIds);

        Assert.Equal(
            10.0,
            result.TotalLength,
            6);
    }

    [Fact]
    public void Find_SelectsShorterOfTwoAvailableRoutes()
    {
        var graph = new Graph();

        /*
               C
               |
               |
        A ---- B ---- D

        A-B-D = 10

        A-C-D =
        5 + sqrt(125)
        ≈ 16.18
        */

        var a = GraphVertex.Create(
            new Point2(0, 0));

        var b = GraphVertex.Create(
            new Point2(5, 0));

        var c = GraphVertex.Create(
            new Point2(0, 5));

        var d = GraphVertex.Create(
            new Point2(10, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);
        graph.AddVertex(c);
        graph.AddVertex(d);

        var ab = GraphEdge.Create(a.Id, b.Id);
        var bd = GraphEdge.Create(b.Id, d.Id);

        var ac = GraphEdge.Create(a.Id, c.Id);
        var cd = GraphEdge.Create(c.Id, d.Id);

        graph.AddEdge(ab);
        graph.AddEdge(bd);
        graph.AddEdge(ac);
        graph.AddEdge(cd);

        var service = CreateService();

        var result =
            service.Find(
                graph,
                a.Id,
                d.Id);

        Assert.True(result.Found);

        Assert.Equal(
            new[] { a.Id, b.Id, d.Id },
            result.VertexIds);

        Assert.Equal(
            new[] { ab.Id, bd.Id },
            result.EdgeIds);

        Assert.Equal(
            10.0,
            result.TotalLength,
            6);
    }

    [Fact]
    public void Find_ReturnsNoPath_ForDisconnectedComponents()
    {
        var graph = new Graph();

        var a = GraphVertex.Create(
            new Point2(0, 0));

        var b = GraphVertex.Create(
            new Point2(10, 0));

        var c = GraphVertex.Create(
            new Point2(100, 0));

        var d = GraphVertex.Create(
            new Point2(110, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);
        graph.AddVertex(c);
        graph.AddVertex(d);

        graph.AddEdge(
            GraphEdge.Create(
                a.Id,
                b.Id));

        graph.AddEdge(
            GraphEdge.Create(
                c.Id,
                d.Id));

        var service = CreateService();

        var result =
            service.Find(
                graph,
                a.Id,
                d.Id);

        Assert.False(result.Found);

        Assert.Empty(result.VertexIds);
        Assert.Empty(result.EdgeIds);

        Assert.True(
            double.IsPositiveInfinity(
                result.TotalLength));
    }

    [Fact]
    public void Find_ReturnsZeroLength_WhenStartEqualsEnd()
    {
        var graph = new Graph();

        var a = GraphVertex.Create(
            new Point2(5, 10));

        graph.AddVertex(a);

        var service = CreateService();

        var result =
            service.Find(
                graph,
                a.Id,
                a.Id);

        Assert.True(result.Found);

        Assert.Equal(
            new[] { a.Id },
            result.VertexIds);

        Assert.Empty(
            result.EdgeIds);

        Assert.Equal(
            0,
            result.TotalLength);
    }

    [Fact]
    public void Find_WorksInBothDirections_ForUndirectedGraph()
    {
        var graph = new Graph();

        var a = GraphVertex.Create(
            new Point2(0, 0));

        var b = GraphVertex.Create(
            new Point2(10, 0));

        var c = GraphVertex.Create(
            new Point2(20, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);
        graph.AddVertex(c);

        var ab =
            GraphEdge.Create(
                a.Id,
                b.Id);

        var bc =
            GraphEdge.Create(
                b.Id,
                c.Id);

        graph.AddEdge(ab);
        graph.AddEdge(bc);

        var service = CreateService();

        var result =
            service.Find(
                graph,
                c.Id,
                a.Id);

        Assert.True(result.Found);

        Assert.Equal(
            new[] { c.Id, b.Id, a.Id },
            result.VertexIds);

        Assert.Equal(
            new[] { bc.Id, ab.Id },
            result.EdgeIds);

        Assert.Equal(
            20.0,
            result.TotalLength,
            6);
    }

    [Fact]
    public void Find_UsesImprovedTentativeDistance()
    {
        var graph = new Graph();

        /*
                    B (0,3)
                  /   |
                 /    |
             X(1,0)   Y(0,2)
                \    /
                 \  /
                  A

        Через X:
        1 + sqrt(10) ≈ 4.16

        Через Y:
        2 + 1 = 3
        */

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var x =
            GraphVertex.Create(
                new Point2(1, 0));

        var y =
            GraphVertex.Create(
                new Point2(0, 2));

        var b =
            GraphVertex.Create(
                new Point2(0, 3));

        graph.AddVertex(a);
        graph.AddVertex(x);
        graph.AddVertex(y);
        graph.AddVertex(b);

        var ax =
            GraphEdge.Create(
                a.Id,
                x.Id);

        var xb =
            GraphEdge.Create(
                x.Id,
                b.Id);

        var ay =
            GraphEdge.Create(
                a.Id,
                y.Id);

        var yb =
            GraphEdge.Create(
                y.Id,
                b.Id);

        graph.AddEdge(ax);
        graph.AddEdge(xb);
        graph.AddEdge(ay);
        graph.AddEdge(yb);

        var service = CreateService();

        var result =
            service.Find(
                graph,
                a.Id,
                b.Id);

        Assert.True(result.Found);

        Assert.Equal(
            new[] { a.Id, y.Id, b.Id },
            result.VertexIds);

        Assert.Equal(
            new[] { ay.Id, yb.Id },
            result.EdgeIds);

        Assert.Equal(
            3.0,
            result.TotalLength,
            6);
    }

    [Fact]
    public void Find_RecalculatesShortestPath_AfterVertexMoves()
    {
        var graph = new Graph();

        /*
        Изначально:

        A ---- B ---- D

        Альтернативный маршрут:
        A -> C -> D
        */

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(5, 0));

        var c =
            GraphVertex.Create(
                new Point2(0, 5));

        var d =
            GraphVertex.Create(
                new Point2(10, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);
        graph.AddVertex(c);
        graph.AddVertex(d);

        var ab =
            GraphEdge.Create(
                a.Id,
                b.Id);

        var bd =
            GraphEdge.Create(
                b.Id,
                d.Id);

        var ac =
            GraphEdge.Create(
                a.Id,
                c.Id);

        var cd =
            GraphEdge.Create(
                c.Id,
                d.Id);

        graph.AddEdge(ab);
        graph.AddEdge(bd);
        graph.AddEdge(ac);
        graph.AddEdge(cd);

        var service = CreateService();

        var beforeMove =
            service.Find(
                graph,
                a.Id,
                d.Id);

        Assert.Equal(
            new[] { a.Id, b.Id, d.Id },
            beforeMove.VertexIds);

        // Уводим B далеко от прямого маршрута.
        b.MoveTo(
            new Point2(5, 20));

        var afterMove =
            service.Find(
                graph,
                a.Id,
                d.Id);

        Assert.Equal(
            new[] { a.Id, c.Id, d.Id },
            afterMove.VertexIds);

        Assert.Equal(
            new[] { ac.Id, cd.Id },
            afterMove.EdgeIds);

        Assert.True(
            afterMove.TotalLength <
            Math.Sqrt(425) * 2);
    }

    [Fact]
    public void Find_Throws_WhenStartVertexDoesNotExist()
    {
        var graph = new Graph();

        var vertex =
            GraphVertex.Create(
                new Point2(0, 0));

        graph.AddVertex(vertex);

        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Find(
                graph,
                Guid.NewGuid(),
                vertex.Id));
    }

    [Fact]
    public void Find_Throws_WhenEndVertexDoesNotExist()
    {
        var graph = new Graph();

        var vertex =
            GraphVertex.Create(
                new Point2(0, 0));

        graph.AddVertex(vertex);

        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Find(
                graph,
                vertex.Id,
                Guid.NewGuid()));
    }
}