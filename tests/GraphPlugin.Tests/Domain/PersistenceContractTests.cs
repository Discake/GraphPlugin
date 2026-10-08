using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class PersistenceContractTests
{
    [Fact]
    public void VertexShape_ValuesMatchDwgSchema()
    {
        Assert.Equal(0, (int)VertexShape.Circle);
        Assert.Equal(1, (int)VertexShape.Triangle);
    }

    [Fact]
    public void GraphColor_ValuesMatchDwgSchema()
    {
        Assert.Equal(0, (int)GraphColor.Blue);
        Assert.Equal(1, (int)GraphColor.Red);
        Assert.Equal(2, (int)GraphColor.Green);
        Assert.Equal(3, (int)GraphColor.White);
        Assert.Equal(4, (int)GraphColor.Black);
    }

    [Fact]
    public void EdgeLineType_ValuesMatchDwgSchema()
    {
        Assert.Equal(0, (int)EdgeLineType.Continuous);
        Assert.Equal(1, (int)EdgeLineType.Dashed);
        Assert.Equal(2, (int)EdgeLineType.Dotted);
    }
}
