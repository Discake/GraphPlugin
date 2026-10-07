using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Persistence.Metadata;

using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Persistence;

public sealed class VertexEntityMapper
{
    private readonly XRecordMetadataStore _metadataStore;

    public VertexEntityMapper(
        XRecordMetadataStore metadataStore)
    {
        _metadataStore = metadataStore;
    }

    public GraphVertex? ToDomain(
        Entity entity,
        Transaction transaction)
    {
        var metadata =
            _metadataStore.ReadVertex(
                entity,
                transaction);

        if (metadata is null)
            return null;

        var position =
            GetPosition(
                entity,
                metadata.Shape);

        if (position is null)
            return null;

        var style = new VertexStyle
        {
            Shape = metadata.Shape,
            Color = metadata.Color,
            Size = metadata.Size
        };

        return new GraphVertex(
            metadata.Id,
            position.Value,
            style);
    }

    private static Point2? GetPosition(
        Entity entity,
        VertexShape shape)
    {
        return shape switch
        {
            VertexShape.Circle
                when entity is Circle circle
                => new Point2(
                    circle.Center.X,
                    circle.Center.Y),

            VertexShape.Triangle
                when entity is Polyline polyline
                => GetTriangleCenter(polyline),

            _ => null
        };
    }

    private static Point2 GetTriangleCenter(
    Polyline polyline)
    {
        if (polyline.NumberOfVertices < 3)
        {
            throw new InvalidOperationException(
                "Triangle vertex must contain at least 3 points.");
        }

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;

        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;

        for (int i = 0;
             i < polyline.NumberOfVertices;
             i++)
        {
            var point =
                polyline.GetPoint2dAt(i);

            minX =
                Math.Min(
                    minX,
                    point.X);

            minY =
                Math.Min(
                    minY,
                    point.Y);

            maxX =
                Math.Max(
                    maxX,
                    point.X);

            maxY =
                Math.Max(
                    maxY,
                    point.Y);
        }

        return new Point2(
            (minX + maxX) / 2.0,
            (minY + maxY) / 2.0);
    }
}