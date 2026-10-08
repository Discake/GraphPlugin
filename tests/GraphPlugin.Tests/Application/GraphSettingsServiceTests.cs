using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public sealed class GraphSettingsServiceTests
{
    [Fact]
    public void GetSettings_ReturnsSettingsFromRepository()
    {
        var repository = new FakeGraphSettingsRepository();

        var applier = new FakeEdgeStyleApplier();

        var service = new GraphSettingsService(repository, applier);

        var settings = service.GetSettings();

        Assert.Same(repository.Settings, settings);
    }

    [Fact]
    public void ChangeEdgeStyle_SavesNewStyle()
    {
        var repository = new FakeGraphSettingsRepository();

        var applier = new FakeEdgeStyleApplier();

        var service = new GraphSettingsService(repository, applier);

        var style = new EdgeStyle(GraphColor.Blue, EdgeLineType.Dashed, 0.50);

        service.ChangeEdgeStyle(style);

        Assert.Same(style, repository.Settings.EdgeStyle);

        Assert.Equal(1, repository.SaveCallCount);
    }

    [Fact]
    public void ChangeEdgeStyle_AppliesStyleToAllEdges()
    {
        var repository = new FakeGraphSettingsRepository();

        var applier = new FakeEdgeStyleApplier();

        var service = new GraphSettingsService(repository, applier);

        var style = new EdgeStyle(GraphColor.Red, EdgeLineType.Dotted, 0.70);

        service.ChangeEdgeStyle(style);

        Assert.Same(style, applier.LastAppliedStyle);

        Assert.Equal(1, applier.ApplyCallCount);
    }

    [Fact]
    public void ChangeEdgeStyle_SavesAndAppliesSameStyle()
    {
        var repository = new FakeGraphSettingsRepository();

        var applier = new FakeEdgeStyleApplier();

        var service = new GraphSettingsService(repository, applier);

        var style = new EdgeStyle(GraphColor.Green, EdgeLineType.Continuous, 0.35);

        service.ChangeEdgeStyle(style);

        Assert.Same(repository.Settings.EdgeStyle, applier.LastAppliedStyle);
    }
}
