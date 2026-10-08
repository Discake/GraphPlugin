using GraphPlugin.Domain.Models;

public sealed class VertexStyle
{
    public VertexShape Shape { get; set; }
    public GraphColor Color { get; set; }
    public double Size { get; set; }

    public static VertexStyle DefaultFor(VertexShape shape)
    {
        return new VertexStyle
        {
            Shape = shape,

            Color = shape switch
            {
                VertexShape.Circle => GraphColor.Blue,

                VertexShape.Triangle => GraphColor.Red,

                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            },

            Size = 10.0,
        };
    }
}
