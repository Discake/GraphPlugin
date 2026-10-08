using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class VertexService
{
    private readonly IVertexRepository _vertices;

    public VertexService(IVertexRepository vertices)
    {
        _vertices = vertices;
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
}