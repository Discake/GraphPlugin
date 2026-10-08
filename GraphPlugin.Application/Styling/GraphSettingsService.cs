using GraphPlugin.Application.Abstractions;
using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class GraphSettingsService
{
    private readonly IGraphSettingsRepository _settings;
    private readonly IEdgeStyleApplier _edgeStyleApplier;

    public GraphSettingsService(
        IGraphSettingsRepository settings,
        IEdgeStyleApplier edgeStyleApplier)
    {
        _settings = settings;
        _edgeStyleApplier = edgeStyleApplier;
    }

    public GraphSettings GetSettings()
    {
        return _settings.Load();
    }

    public void ChangeEdgeStyle(
        EdgeStyle style)
    {
        var settings =
            _settings.Load();

        settings.ChangeEdgeStyle(style);

        _settings.Save(settings);

        _edgeStyleApplier.ApplyToAll(style);
    }
}