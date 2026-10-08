using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Fakes;

public sealed class FakeVertexRepository : IVertexRepository
{
    private readonly Dictionary<Guid, GraphVertex> _vertices = new();

    public int Count => _vertices.Count;

    public GraphVertex? Get(Guid id)
    {
        return _vertices.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<GraphVertex> GetAll()
    {
        return _vertices.Values.ToArray();
    }

    public void Add(GraphVertex vertex)
    {
        _vertices.Add(vertex.Id, vertex);
    }

    public void Delete(Guid id)
    {
        _vertices.Remove(id);
    }
}