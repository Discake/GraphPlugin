namespace GraphPlugin.Domain.Models;

public sealed class GraphEdge
{
    public Guid Id { get; }

    public Guid VertexAId { get; }

    public Guid VertexBId { get; }

    public EdgeRoute Route { get; private set; }

    public GraphEdge(Guid id, Guid vertexAId, Guid vertexBId, EdgeRoute route = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Edge id cannot be empty.", nameof(id));

        if (vertexAId == Guid.Empty)
            throw new ArgumentException("Vertex A id cannot be empty.", nameof(vertexAId));

        if (vertexBId == Guid.Empty)
            throw new ArgumentException("Vertex B id cannot be empty.", nameof(vertexBId));

        if (vertexAId == vertexBId)
            throw new ArgumentException("An edge cannot connect a vertex to itself.");

        Id = id;
        VertexAId = vertexAId;
        VertexBId = vertexBId;
        Route = route ?? EdgeRoute.Straight;
    }

    public static GraphEdge Create(Guid vertexAId, Guid vertexBId, EdgeRoute route = null)
    {
        return new GraphEdge(Guid.NewGuid(), vertexAId, vertexBId, route);
    }

    public static GraphEdge Restore(Guid id, Guid vertexAId, Guid vertexBId, EdgeRoute? route = null)
    {
        return new GraphEdge(id, vertexAId, vertexBId, route ?? EdgeRoute.Straight);
    }

    public bool IsIncidentTo(Guid vertexId)
    {
        return VertexAId == vertexId || VertexBId == vertexId;
    }

    public Guid GetOtherVertexId(Guid vertexId)
    {
        if (VertexAId == vertexId)
            return VertexBId;

        if (VertexBId == vertexId)
            return VertexAId;

        throw new ArgumentException($"Vertex {vertexId} is not incident to edge {Id}.", nameof(vertexId));
    }

    public void ChangeRoute(EdgeRoute route)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
    }
}
