using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class MoveBendService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public MoveBendService(
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

    public MoveBendResult Move(
        Guid edgeId,
        int bendIndex,
        Point2 newPosition,
        double tolerance =
            EdgeRouteGeometry.DefaultTolerance)
    {
        ValidatePoint(
            newPosition);

        ValidateTolerance(
            tolerance);

        var edge =
            _edges.Get(edgeId)
            ?? throw new InvalidOperationException(
                $"Edge '{edgeId}' does not exist.");

        ValidateBendIndex(
            edge,
            bendIndex);

        var vertexA =
            _vertices.Get(
                edge.VertexAId)
            ?? throw new InvalidOperationException(
                $"Vertex '{edge.VertexAId}' does not exist.");

        var vertexB =
            _vertices.Get(
                edge.VertexBId)
            ?? throw new InvalidOperationException(
                $"Vertex '{edge.VertexBId}' does not exist.");

        var oldPosition =
            edge.Route
                .IntermediatePoints[bendIndex];

        EnsureNoDegenerateSegment(
            edge,
            vertexA,
            vertexB,
            bendIndex,
            newPosition,
            tolerance);

        var originalRoute =
            edge.Route;

        var updatedRoute =
            originalRoute.MovePoint(
                bendIndex,
                newPosition);

        edge.ChangeRoute(
            updatedRoute);

        try
        {
            _edges.Update(
                edge);
        }
        catch
        {
            edge.ChangeRoute(
                originalRoute);

            throw;
        }

        return new MoveBendResult(
            edge,
            bendIndex,
            oldPosition,
            newPosition);
    }

    private static void EnsureNoDegenerateSegment(
        GraphEdge edge,
        GraphVertex vertexA,
        GraphVertex vertexB,
        int bendIndex,
        Point2 newPosition,
        double tolerance)
    {
        var previous =
            bendIndex == 0
                ? vertexA.Position
                : edge.Route
                    .IntermediatePoints[
                        bendIndex - 1];

        var next =
            bendIndex ==
            edge.Route.Count - 1
                ? vertexB.Position
                : edge.Route
                    .IntermediatePoints[
                        bendIndex + 1];

        if (AreEqual(
                newPosition,
                previous,
                tolerance))
        {
            throw new ArgumentException(
                "Bend cannot coincide with the previous route point.",
                nameof(newPosition));
        }

        if (AreEqual(
                newPosition,
                next,
                tolerance))
        {
            throw new ArgumentException(
                "Bend cannot coincide with the next route point.",
                nameof(newPosition));
        }
    }

    private static void ValidateBendIndex(
        GraphEdge edge,
        int bendIndex)
    {
        if (bendIndex < 0 ||
            bendIndex >= edge.Route.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bendIndex));
        }
    }

    private static bool AreEqual(
        Point2 a,
        Point2 b,
        double tolerance)
    {
        return
            a.DistanceTo(b) <=
            tolerance;
    }

    private static void ValidatePoint(
        Point2 point)
    {
        if (!double.IsFinite(point.X) ||
            !double.IsFinite(point.Y))
        {
            throw new ArgumentException(
                "Bend position must contain finite coordinates.",
                nameof(point));
        }
    }

    private static void ValidateTolerance(
        double tolerance)
    {
        if (!double.IsFinite(tolerance) ||
            tolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance));
        }
    }
}