using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Fakes;

public sealed class FakeEdgeRepository : IEdgeRepository
{
    private readonly Dictionary<Guid, GraphEdge> _edges = new();

    public int Count => _edges.Count;

    public GraphEdge? Get(Guid id)
    {
        return _edges.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<GraphEdge> GetAll()
    {
        return _edges.Values.ToArray();
    }

    public IReadOnlyCollection<GraphEdge> GetByVertex(Guid vertexId)
    {
        return _edges.Values.Where(edge => edge.IsIncidentTo(vertexId)).ToArray();
    }

    public void Add(GraphEdge edge)
    {
        _edges.Add(edge.Id, edge);
    }

    public void Update(GraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        if (!_edges.ContainsKey(edge.Id))
        {
            throw new InvalidOperationException($"Edge '{edge.Id}' does not exist.");
        }

        _edges[edge.Id] = edge;
    }

    public void Delete(Guid id)
    {
        _edges.Remove(id);
    }
}
