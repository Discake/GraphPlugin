using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class ShortestPathApplicationService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;
    private readonly IShortestPathService _shortestPath;

    public ShortestPathApplicationService(
        IVertexRepository vertices,
        IEdgeRepository edges,
        IShortestPathService shortestPath)
    {
        _vertices = vertices;
        _edges = edges;
        _shortestPath = shortestPath;
    }

    public ShortestPathResult Find(
        Guid startVertexId,
        Guid endVertexId)
    {
        var graph =
            BuildGraph();

        return _shortestPath.Find(
            graph,
            startVertexId,
            endVertexId);
    }

    private Graph BuildGraph()
    {
        var graph =
            new Graph();

        foreach (var vertex
                 in _vertices.GetAll())
        {
            graph.AddVertex(vertex);
        }

        foreach (var edge
                 in _edges.GetAll())
        {
            graph.AddEdge(edge);
        }

        return graph;
    }
}