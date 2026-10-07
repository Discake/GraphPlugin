using GraphPlugin.Application.Abstractions.Persistence;

namespace GraphPlugin.Application.Services;

public sealed class GraphService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public GraphService(
        IVertexRepository vertices,
        IEdgeRepository edges)
    {
        _vertices = vertices;
        _edges = edges;
    }

    public void DeleteVertex(Guid vertexId)
    {
        var incidentEdges =
            _edges.GetByVertex(
                vertexId);

        foreach (var edge in incidentEdges)
        {
            _edges.Delete(
                edge.Id);
        }

        _vertices.Delete(
            vertexId);
    }

    public void DeleteEdge(Guid edgeId)
    {
        _edges.Delete(edgeId);
    }
}