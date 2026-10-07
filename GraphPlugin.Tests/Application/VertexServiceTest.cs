using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class VertexServiceTests
{
    [Fact]
    public void CreateVertex_AddsVertexToRepository()
    {
        var repository =
            new FakeVertexRepository();

        var service =
            new VertexService(repository);

        var vertex =
            service.CreateVertex(
                new Point2(100, 200));

        Assert.Equal(
            1,
            repository.Count);

        Assert.Same(
            vertex,
            repository.Get(vertex.Id));
    }

    [Fact]
    public void CreateVertex_DefaultShape_IsCircle()
    {
        var repository =
            new FakeVertexRepository();

        var service =
            new VertexService(repository);

        var vertex =
            service.CreateVertex(
                new Point2(0, 0));

        Assert.Equal(
            VertexShape.Circle,
            vertex.Style.Shape);
    }

    [Fact]
    public void CreateVertex_Circle_IsBlue()
    {
        var repository =
            new FakeVertexRepository();

        var service =
            new VertexService(repository);

        var vertex =
            service.CreateVertex(
                new Point2(0, 0),
                VertexShape.Circle);

        Assert.Equal(
            GraphColor.Blue,
            vertex.Style.Color);
    }

    [Fact]
    public void CreateVertex_Triangle_IsRed()
    {
        var repository =
            new FakeVertexRepository();

        var service =
            new VertexService(repository);

        var vertex =
            service.CreateVertex(
                new Point2(0, 0),
                VertexShape.Triangle);

        Assert.Equal(
            GraphColor.Red,
            vertex.Style.Color);
    }
}