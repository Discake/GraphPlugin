using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class SplitEdgeService
{
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;

    public SplitEdgeService(
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

    public SplitEdgeResult Split(
        Guid edgeId,
        Point2 splitPoint,
        VertexShape shape =
            VertexShape.Circle)
    {
        ValidatePoint(
            splitPoint);

        var originalEdge =
            _edges.Get(edgeId)
            ?? throw new InvalidOperationException(
                $"Edge '{edgeId}' does not exist.");

        var vertexA =
            _vertices.Get(
                originalEdge.VertexAId)
            ?? throw new InvalidOperationException(
                $"Vertex '{originalEdge.VertexAId}' does not exist.");

        var vertexB =
            _vertices.Get(
                originalEdge.VertexBId)
            ?? throw new InvalidOperationException(
                $"Vertex '{originalEdge.VertexBId}' does not exist.");

        var routeSplit =
            EdgeRouteGeometry.Split(
                vertexA.Position,
                vertexB.Position,
                originalEdge.Route,
                splitPoint);

        //
        // Важно:
        // используем именно нормализованную точку,
        // которую вернул EdgeRouteGeometry.
        //
        var newVertex =
            new GraphVertex(
                Guid.NewGuid(),
                routeSplit.SplitPoint,
                VertexStyle.DefaultFor(shape));

        var edgeA =
            GraphEdge.Create(
                vertexA.Id,
                newVertex.Id,
                routeSplit.LeftRoute);

        var edgeB =
            GraphEdge.Create(
                newVertex.Id,
                vertexB.Id,
                routeSplit.RightRoute);

        var vertexAdded =
            false;

        var edgeAAdded =
            false;

        var edgeBAdded =
            false;

        try
        {
            //
            // Сначала создаём всю новую структуру.
            //
            _vertices.Add(
                newVertex);

            vertexAdded =
                true;

            _edges.Add(
                edgeA);

            edgeAAdded =
                true;

            _edges.Add(
                edgeB);

            edgeBAdded =
                true;

            //
            // Исходное ребро удаляем последним.
            //
            // Пока всё выше не прошло успешно,
            // старое ребро остаётся нетронутым.
            //
            _edges.Delete(
                originalEdge.Id);

            return new SplitEdgeResult(
                originalEdge,
                newVertex,
                edgeA,
                edgeB);
        }
        catch
        {
            //
            // Best-effort rollback.
            //
            // Если исходное ребро ещё существует,
            // удаляем только то, что успели добавить.
            //

            if (edgeBAdded)
            {
                TryDeleteEdge(
                    edgeB.Id);
            }

            if (edgeAAdded)
            {
                TryDeleteEdge(
                    edgeA.Id);
            }

            if (vertexAdded)
            {
                TryDeleteVertex(
                    newVertex.Id);
            }

            throw;
        }
    }

    private static void ValidatePoint(
        Point2 point)
    {
        if (!double.IsFinite(point.X) ||
            !double.IsFinite(point.Y))
        {
            throw new ArgumentException(
                "Split point must contain finite coordinates.",
                nameof(point));
        }
    }

    private void TryDeleteEdge(
        Guid edgeId)
    {
        try
        {
            _edges.Delete(
                edgeId);
        }
        catch
        {
            // Best-effort rollback.
        }
    }

    private void TryDeleteVertex(
        Guid vertexId)
    {
        try
        {
            _vertices.Delete(
                vertexId);
        }
        catch
        {
            // Best-effort rollback.
        }
    }
}