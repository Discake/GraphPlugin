namespace GraphPlugin.Domain.Models;

public sealed class Graph
{
    private readonly Dictionary<Guid, GraphVertex> _vertices = new();
    private readonly Dictionary<Guid, GraphEdge> _edges = new();

    public IReadOnlyCollection<GraphVertex> Vertices =>
        _vertices.Values;

    public IReadOnlyCollection<GraphEdge> Edges =>
        _edges.Values;

    public void AddVertex(GraphVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);

        if (!_vertices.TryAdd(vertex.Id, vertex))
        {
            throw new InvalidOperationException(
                $"Vertex {vertex.Id} already exists.");
        }
    }

    public void AddEdge(GraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        if (!_vertices.ContainsKey(edge.VertexAId))
        {
            throw new InvalidOperationException(
                $"Vertex {edge.VertexAId} does not exist.");
        }

        if (!_vertices.ContainsKey(edge.VertexBId))
        {
            throw new InvalidOperationException(
                $"Vertex {edge.VertexBId} does not exist.");
        }

        if (!_edges.TryAdd(edge.Id, edge))
        {
            throw new InvalidOperationException(
                $"Edge {edge.Id} already exists.");
        }
    }

    public GraphVertex? GetVertex(Guid id)
    {
        return _vertices.GetValueOrDefault(id);
    }

    public GraphEdge? GetEdge(Guid id)
    {
        return _edges.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<GraphEdge> GetIncidentEdges(
        Guid vertexId)
    {
        return _edges.Values
            .Where(edge => edge.IsIncidentTo(vertexId))
            .ToArray();
    }
}