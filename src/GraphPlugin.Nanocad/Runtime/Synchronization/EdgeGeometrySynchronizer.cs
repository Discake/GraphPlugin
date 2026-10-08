using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Nanocad.Drawing;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class EdgeGeometrySynchronizer
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;
    private readonly GraphEntityIndex _index;

    private readonly EdgeEntityMapper _mapper;

    public EdgeGeometrySynchronizer(
        IVertexRepository vertices,
        IEdgeRepository edges,
        GraphEntityIndex index,
        EdgeEntityMapper mapper
    )
    {
        _vertices = vertices;
        _edges = edges;
        _index = index;
        _mapper = mapper;
    }

    public void UpdateIncidentEdges(Guid vertexId)
    {
        var edges = _edges.GetByVertex(vertexId);

        foreach (var edge in edges)
            UpdateEdge(edge.Id);
    }

    public void UpdateEdge(Guid edgeId)
    {
        var edge = _edges.Get(edgeId);

        if (edge is null)
            return;

        var vertexA = _vertices.Get(edge.VertexAId);

        var vertexB = _vertices.Get(edge.VertexBId);

        if (vertexA is null || vertexB is null)
        {
            return;
        }

        if (!_index.TryGetEdgeObjectId(edgeId, out var edgeObjectId))
        {
            return;
        }

        var document = NanoApplication.DocumentManager.MdiActiveDocument;

        using var transaction = document.Database.TransactionManager.StartTransaction();

        var entity = transaction.GetObject(edgeObjectId, OpenMode.ForWrite);

        if (entity is not Polyline polyline)
        {
            throw new InvalidOperationException(
                $"Graph edge entity is not a Polyline. "
                    + $"EdgeId: {edgeId}. "
                    + $"Actual type: "
                    + $"{entity?.GetType().FullName ?? "<null>"}."
            );
        }

        _mapper.UpdateEndpoints(polyline, vertexA, vertexB);

        transaction.Commit();
    }
}
