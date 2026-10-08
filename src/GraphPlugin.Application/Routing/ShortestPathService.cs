using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Algorithms;

namespace GraphPlugin.Application.Routing;

public sealed class ShortestPathService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;
    private readonly DijkstraShortestPath _shortestPath;

    public ShortestPathService(
        IVertexRepository vertices,
        IEdgeRepository edges,
        DijkstraShortestPath shortestPath)
    {
        _vertices =
            vertices ??
            throw new ArgumentNullException(
                nameof(vertices));

        _edges =
            edges ??
            throw new ArgumentNullException(
                nameof(edges));

        _shortestPath =
            shortestPath ??
            throw new ArgumentNullException(
                nameof(shortestPath));
    }

    public ShortestPathResult Find(
        Guid startVertexId,
        Guid endVertexId)
    {
        return _shortestPath.Find(
            _vertices.GetAll(),
            _edges.GetAll(),
            startVertexId,
            endVertexId);
    }
}
