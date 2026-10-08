using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Drawing;

public sealed class EdgeEntityMapper
{
    private const double BulgeTolerance = 1e-12;

    public EdgeRoute ReadRoute(Polyline polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        ValidatePolyline(polyline);

        if (polyline.NumberOfVertices == 2)
        {
            return EdgeRoute.Straight;
        }

        var intermediatePoints = new List<Point2>(polyline.NumberOfVertices - 2);

        for (var i = 1; i < polyline.NumberOfVertices - 1; i++)
        {
            var point = polyline.GetPoint2dAt(i);

            intermediatePoints.Add(new Point2(point.X, point.Y));
        }

        return new EdgeRoute(intermediatePoints);
    }

    public void WriteGeometry(Polyline polyline, GraphEdge edge, GraphVertex vertexA, GraphVertex vertexB)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        ArgumentNullException.ThrowIfNull(edge);

        ArgumentNullException.ThrowIfNull(vertexA);

        ArgumentNullException.ThrowIfNull(vertexB);

        ValidateEndpoints(edge, vertexA, vertexB);

        var points = new List<Point2> { vertexA.Position };

        points.AddRange(edge.Route.IntermediatePoints);

        points.Add(vertexB.Position);

        polyline.Closed = false;

        var existingCount = polyline.NumberOfVertices;

        var targetCount = points.Count;

        var commonCount = Math.Min(existingCount, targetCount);

        // Сначала обновляем уже существующие вершины.
        for (var i = 0; i < commonCount; i++)
        {
            polyline.SetPointAt(i, ToPoint2d(points[i]));

            polyline.SetBulgeAt(i, 0);
        }

        // Если новой геометрии нужно больше точек —
        // добавляем недостающие.
        for (var i = existingCount; i < targetCount; i++)
        {
            polyline.AddVertexAt(i, ToPoint2d(points[i]), bulge: 0, startWidth: 0, endWidth: 0);
        }

        // Если точек стало меньше —
        // удаляем только лишний хвост.
        //
        // targetCount всегда >= 2,
        // поэтому Polyline никогда не становится
        // вырожденной.
        for (var i = polyline.NumberOfVertices - 1; i >= targetCount; i--)
        {
            polyline.RemoveVertexAt(i);
        }
    }

    public void UpdateEndpoints(Polyline polyline, GraphVertex vertexA, GraphVertex vertexB)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        ArgumentNullException.ThrowIfNull(vertexA);

        ArgumentNullException.ThrowIfNull(vertexB);

        ValidatePolyline(polyline);

        polyline.SetPointAt(0, ToPoint2d(vertexA.Position));

        polyline.SetPointAt(polyline.NumberOfVertices - 1, ToPoint2d(vertexB.Position));
    }

    private static void AddPoint(Polyline polyline, int index, Point2 point)
    {
        polyline.AddVertexAt(index, ToPoint2d(point), bulge: 0, startWidth: 0, endWidth: 0);
    }

    private static Point2d ToPoint2d(Point2 point)
    {
        return new Point2d(point.X, point.Y);
    }

    private static void ValidateEndpoints(GraphEdge edge, GraphVertex vertexA, GraphVertex vertexB)
    {
        if (vertexA.Id != edge.VertexAId)
        {
            throw new ArgumentException("Provided vertex A does not match edge VertexAId.", nameof(vertexA));
        }

        if (vertexB.Id != edge.VertexBId)
        {
            throw new ArgumentException("Provided vertex B does not match edge VertexBId.", nameof(vertexB));
        }
    }

    private static void ValidatePolyline(Polyline polyline)
    {
        if (polyline.Closed)
        {
            throw new InvalidOperationException("Graph edge polyline cannot be closed.");
        }

        if (polyline.NumberOfVertices < 2)
        {
            throw new InvalidOperationException("Graph edge polyline must contain at least two vertices.");
        }

        // В нашей Domain-модели Edge состоит
        // только из прямых сегментов.
        for (var i = 0; i < polyline.NumberOfVertices - 1; i++)
        {
            if (Math.Abs(polyline.GetBulgeAt(i)) > BulgeTolerance)
            {
                throw new InvalidOperationException("Graph edge polyline cannot contain arc segments.");
            }
        }
    }
}
