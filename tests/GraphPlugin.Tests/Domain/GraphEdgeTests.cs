using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Domain;

public sealed class GraphEdgeTests
{
    [Fact]
    public void Create_CreatesEdgeWithNonEmptyId()
    {
        var vertexAId = Guid.NewGuid();

        var vertexBId = Guid.NewGuid();

        var edge = GraphEdge.Create(vertexAId, vertexBId);

        Assert.NotEqual(Guid.Empty, edge.Id);

        Assert.Equal(vertexAId, edge.VertexAId);

        Assert.Equal(vertexBId, edge.VertexBId);
    }

    [Fact]
    public void Create_Throws_WhenVerticesAreSame()
    {
        var vertexId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => GraphEdge.Create(vertexId, vertexId));
    }

    [Fact]
    public void IsIncidentTo_ReturnsTrue_ForFirstVertex()
    {
        var vertexAId = Guid.NewGuid();

        var edge = GraphEdge.Create(vertexAId, Guid.NewGuid());

        Assert.True(edge.IsIncidentTo(vertexAId));
    }

    [Fact]
    public void IsIncidentTo_ReturnsTrue_ForSecondVertex()
    {
        var vertexBId = Guid.NewGuid();

        var edge = GraphEdge.Create(Guid.NewGuid(), vertexBId);

        Assert.True(edge.IsIncidentTo(vertexBId));
    }

    [Fact]
    public void IsIncidentTo_ReturnsFalse_ForUnrelatedVertex()
    {
        var edge = GraphEdge.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(edge.IsIncidentTo(Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_Throws_WhenEdgeIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new GraphEdge(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_Throws_WhenVertexAIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new GraphEdge(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_Throws_WhenVertexBIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new GraphEdge(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void GetOtherVertexId_ReturnsSecondVertex_WhenGivenFirst()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var edge = GraphEdge.Create(a, b);

        Assert.Equal(b, edge.GetOtherVertexId(a));
    }

    [Fact]
    public void GetOtherVertexId_ReturnsFirstVertex_WhenGivenSecond()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var edge = GraphEdge.Create(a, b);

        Assert.Equal(a, edge.GetOtherVertexId(b));
    }

    [Fact]
    public void GetOtherVertexId_Throws_ForUnrelatedVertex()
    {
        var edge = GraphEdge.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => edge.GetOtherVertexId(Guid.NewGuid()));
    }
}
