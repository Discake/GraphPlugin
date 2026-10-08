namespace GraphPlugin.Domain.Models;

public sealed class EdgeStyle
{
    public GraphColor Color { get; }

    public EdgeLineType LineType { get; }

    public double LineWeightMm { get; }

    public EdgeStyle(GraphColor color, EdgeLineType lineType, double lineWeightMm)
    {
        if (lineWeightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineWeightMm));

        Color = color;
        LineType = lineType;
        LineWeightMm = lineWeightMm;
    }

    public static EdgeStyle Default => new(GraphColor.White, EdgeLineType.Continuous, 0.25);
}
