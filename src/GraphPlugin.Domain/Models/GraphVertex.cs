using GraphPlugin.Domain.Geometry;

namespace GraphPlugin.Domain.Models;

public sealed class GraphVertex
{
    public Guid Id { get; }

    public Point2 Position { get; private set; }

    public VertexStyle Style { get; private set; }

    public GraphVertex(Guid id, Point2 position, VertexStyle style)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Vertex id cannot be empty.", nameof(id));

        Id = id;
        Position = position;
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    public static GraphVertex Create(Point2 position, VertexStyle? style = null)
    {
        return new GraphVertex(Guid.NewGuid(), position, style ?? new VertexStyle());
    }

    public void MoveTo(Point2 position)
    {
        Position = position;
    }

    public void ChangeStyle(VertexStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }
}
