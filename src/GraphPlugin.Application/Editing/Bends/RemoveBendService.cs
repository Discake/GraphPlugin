using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Editing.Bends;

public sealed class RemoveBendService
{
    private readonly IEdgeRepository _edges;

    public RemoveBendService(IEdgeRepository edges)
    {
        _edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }

    public RemoveBendResult Remove(Guid edgeId, int bendIndex)
    {
        var edge = _edges.Get(edgeId) ?? throw new InvalidOperationException($"Edge '{edgeId}' does not exist.");

        if (bendIndex < 0 || bendIndex >= edge.Route.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(bendIndex));
        }

        var removedPoint = edge.Route.IntermediatePoints[bendIndex];

        var originalRoute = edge.Route;

        var updatedRoute = originalRoute.RemovePoint(bendIndex);

        edge.ChangeRoute(updatedRoute);

        try
        {
            _edges.Update(edge);
        }
        catch
        {
            edge.ChangeRoute(originalRoute);

            throw;
        }

        return new RemoveBendResult(edge, bendIndex, removedPoint);
    }
}
