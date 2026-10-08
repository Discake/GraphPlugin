using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Editing.Bends;

public sealed class AddBendService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public AddBendService(
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

    public AddBendResult Add(
        Guid edgeId,
        Point2 point,
        double tolerance =
            EdgeRouteGeometry.DefaultTolerance)
    {
        ValidatePoint(point);
        ValidateTolerance(tolerance);

        var edge =
            _edges.Get(edgeId)
            ?? throw new InvalidOperationException(
                $"Edge '{edgeId}' does not exist.");

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

        var projection =
            EdgeRouteGeometry.FindClosestSegment(
                vertexA.Position,
                vertexB.Position,
                edge.Route,
                point);

        if (projection.Distance >
            tolerance)
        {
            throw new ArgumentException(
                "Bend point does not lie on the edge route.",
                nameof(point));
        }

        var bendPoint =
            projection.Point;

        EnsureNotDuplicatePoint(
            edge,
            vertexA,
            vertexB,
            bendPoint,
            tolerance);

        var originalRoute = edge.Route;

        var updatedRoute =
            originalRoute.InsertAtSegment(
                projection.SegmentIndex,
                bendPoint);

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

        return new AddBendResult(
            edge,
            bendPoint,
            BendIndex:
                projection.SegmentIndex,
            SegmentIndex:
                projection.SegmentIndex);
    }

    private static void EnsureNotDuplicatePoint(
        GraphEdge edge,
        GraphVertex vertexA,
        GraphVertex vertexB,
        Point2 point,
        double tolerance)
    {
        if (AreEqual(
                point,
                vertexA.Position,
                tolerance) ||
            AreEqual(
                point,
                vertexB.Position,
                tolerance))
        {
            throw new ArgumentException(
                "Bend cannot coincide with an edge endpoint.");
        }

        foreach (var existing in
                 edge.Route.IntermediatePoints)
        {
            if (AreEqual(
                    point,
                    existing,
                    tolerance))
            {
                throw new ArgumentException(
                    "A bend already exists at this position.");
            }
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
                "Bend point must contain finite coordinates.",
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
