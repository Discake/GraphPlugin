using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Graph;

public sealed class VertexService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public VertexService(
        IVertexRepository vertices,
        IEdgeRepository edges)
    {
        _vertices =
            vertices ??
            throw new ArgumentNullException(
                nameof(vertices));

        _edges =
            edges ??
            throw new ArgumentNullException(
                nameof(edges));
    }

    public GraphVertex CreateVertex(
        Point2 position,
        VertexShape shape = VertexShape.Circle)
    {
        var vertex =
            GraphVertex.Create(
                position,
                VertexStyle.DefaultFor(shape));

        _vertices.Add(vertex);

        return vertex;
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
}
