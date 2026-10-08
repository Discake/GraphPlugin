using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class GraphVertexTests
{
    [Fact]
    public void Create_CreatesVertexWithNonEmptyId()
    {
        var vertex = GraphVertex.Create(new Point2(10, 20));

        Assert.NotEqual(Guid.Empty, vertex.Id);
    }

    [Fact]
    public void Constructor_Throws_WhenIdIsEmpty()
    {
        var style = new VertexStyle();

        Assert.Throws<ArgumentException>(() => new GraphVertex(Guid.Empty, new Point2(0, 0), style));
    }

    [Fact]
    public void ChangeStyle_ChangesVertexStyle()
    {
        var vertex = GraphVertex.Create(new Point2(10, 20));

        var style = new VertexStyle
        {
            Shape = VertexShape.Triangle,
            Color = GraphColor.Red,
            Size = 15,
        };

        vertex.ChangeStyle(style);

        Assert.Same(style, vertex.Style);

        Assert.Equal(VertexShape.Triangle, vertex.Style.Shape);

        Assert.Equal(GraphColor.Red, vertex.Style.Color);
    }

    [Fact]
    public void ChangeStyle_Throws_WhenStyleIsNull()
    {
        var vertex = GraphVertex.Create(new Point2(0, 0));

        Assert.Throws<ArgumentNullException>(() => vertex.ChangeStyle(null!));
    }
}
