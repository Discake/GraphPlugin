using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Drawing;

public sealed class VertexEntityFactory
{
    public Entity Create(GraphVertex vertex)
    {
        var position = new Point3d(vertex.Position.X, vertex.Position.Y, 0);

        Entity entity = vertex.Style.Shape switch
        {
            VertexShape.Circle => CreateCircle(vertex, position),

            VertexShape.Triangle => CreateTriangle(vertex, position),

            _ => throw new NotSupportedException($"Unsupported vertex shape: {vertex.Style.Shape}"),
        };

        entity.Color = CadColorMapper.ToCadColor(vertex.Style.Color);

        return entity;
    }

    private static Circle CreateCircle(GraphVertex vertex, Point3d position)
    {
        return new Circle(position, Vector3d.ZAxis, vertex.Style.Size);
    }

    private static Polyline CreateTriangle(GraphVertex vertex, Point3d position)
    {
        double size = vertex.Style.Size;

        var polyline = new Polyline();

        // Верхняя вершина
        polyline.AddVertexAt(0, new Point2d(position.X, position.Y + size), 0, 0, 0);

        // Левая нижняя
        polyline.AddVertexAt(1, new Point2d(position.X - size, position.Y - size), 0, 0, 0);

        // Правая нижняя
        polyline.AddVertexAt(2, new Point2d(position.X + size, position.Y - size), 0, 0, 0);

        polyline.Closed = true;

        return polyline;
    }
}
