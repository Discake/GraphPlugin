using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class EdgeLengthCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsGeometricDistance()
    {
        var graph =
            new Graph();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(3, 4));

        graph.AddVertex(a);
        graph.AddVertex(b);

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id);

        graph.AddEdge(edge);

        var calculator =
            new EdgeLengthCalculator();

        var length =
            calculator.Calculate(
                edge,
                a,
                b);

        Assert.Equal(
            5.0,
            length,
            6);
    }

    [Fact]
    public void Calculate_UsesCurrentVertexPositions()
    {
        var graph =
            new Graph();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(3, 4));

        graph.AddVertex(a);
        graph.AddVertex(b);

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id);

        graph.AddEdge(edge);

        var calculator =
            new EdgeLengthCalculator();

        Assert.Equal(
            5.0,
            calculator.Calculate(
                edge,
                a,
                b),
            6);

        b.MoveTo(
            new Point2(6, 8));

        Assert.Equal(
            10.0,
            calculator.Calculate(
                edge, a, b),
            6);
    }

    [Fact]
    public void Calculate_WithCurveSummsLength()
    {
        var graph =
            new Graph();

        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        graph.AddVertex(a);
        graph.AddVertex(b);

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(new[] 
                {
                    new Point2(0, 10),
                    new Point2(10, 10) 
                }));

        graph.AddEdge(edge);

        var calculator =
            new EdgeLengthCalculator();

        Assert.Equal(
            30.0,
            calculator.Calculate(
                edge, a, b),
            6);
    }

    [Fact]
    public void Calculate_StraightEdge_ReturnsDirectDistance()
    {
        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id);

        var calculator =
            new EdgeLengthCalculator();

        var length =
            calculator.Calculate(
                edge,
                a,
                b);

        Assert.Equal(
            10,
            length,
            6);
    }

    [Fact]
    public void Calculate_EdgeWithOneBend_ReturnsPolylineLength()
    {
        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 10));

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    new Point2(0, 10)
                    }));

        var calculator =
            new EdgeLengthCalculator();

        var length =
            calculator.Calculate(
                edge,
                a,
                b);

        Assert.Equal(
            20,
            length,
            6);
    }

    [Fact]
    public void Calculate_EdgeWithSeveralBends_ReturnsSumOfSegments()
    {
        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    new Point2(0, 10),
                    new Point2(10, 10)
                    }));

        var calculator =
            new EdgeLengthCalculator();

        var length =
            calculator.Calculate(
                edge,
                a,
                b);

        Assert.Equal(
            30,
            length,
            6);
    }

    [Fact]
    public void Calculate_ReversedEndpoints_ReturnsSameLength()
    {
        var a =
            GraphVertex.Create(
                new Point2(0, 0));

        var b =
            GraphVertex.Create(
                new Point2(10, 0));

        var edge =
            GraphEdge.Create(
                a.Id,
                b.Id,
                new EdgeRoute(
                    new[]
                    {
                    new Point2(0, 10),
                    new Point2(10, 10)
                    }));

        var calculator =
            new EdgeLengthCalculator();

        var forward =
            calculator.Calculate(
                edge,
                a,
                b);

        var backward =
            calculator.Calculate(
                edge,
                b,
                a);

        Assert.Equal(
            forward,
            backward,
            6);
    }
}