using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class EdgeSelectionService
{
    private readonly IEdgeRepository _edges;
    private readonly GraphEntityIndex _index;

    public EdgeSelectionService(IEdgeRepository edges, GraphEntityIndex index)
    {
        _edges = edges;
        _index = index;
    }

    public GraphEdge? ReadEdge(ObjectId objectId)
    {
        if (!_index.TryGetEdgeId(objectId, out var edgeId))
        {
            return null;
        }

        return _edges.Get(edgeId);
    }
}
