using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class EdgeStyleTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var style = new EdgeStyle(GraphColor.Blue, EdgeLineType.Dashed, 0.50);

        Assert.Equal(GraphColor.Blue, style.Color);

        Assert.Equal(EdgeLineType.Dashed, style.LineType);

        Assert.Equal(0.50, style.LineWeightMm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Constructor_Throws_WhenLineWeightIsNotPositive(double lineWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EdgeStyle(GraphColor.White, EdgeLineType.Continuous, lineWeight)
        );
    }

    [Fact]
    public void Default_HasExpectedValues()
    {
        var style = EdgeStyle.Default;

        Assert.Equal(GraphColor.White, style.Color);

        Assert.Equal(EdgeLineType.Continuous, style.LineType);

        Assert.Equal(0.25, style.LineWeightMm);
    }
}
