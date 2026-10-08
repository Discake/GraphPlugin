using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Graph;

public sealed class EdgeService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public EdgeService(IVertexRepository vertices, IEdgeRepository edges)
    {
        _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));

        _edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }

    public GraphEdge CreateEdge(Guid vertexAId, Guid vertexBId)
    {
        return CreateEdge(vertexAId, vertexBId, EdgeRoute.Straight);
    }

    public GraphEdge CreateEdge(Guid vertexAId, Guid vertexBId, EdgeRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var vertexA =
            _vertices.Get(vertexAId) ?? throw new InvalidOperationException($"Vertex '{vertexAId}' does not exist.");

        var vertexB =
            _vertices.Get(vertexBId) ?? throw new InvalidOperationException($"Vertex '{vertexBId}' does not exist.");

        var edge = GraphEdge.Create(vertexA.Id, vertexB.Id, route);

        _edges.Add(edge);

        return edge;
    }

    public void DeleteEdge(Guid edgeId)
    {
        _edges.Delete(edgeId);
    }
}
