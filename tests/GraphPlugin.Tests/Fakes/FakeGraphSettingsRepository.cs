using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Fakes;

public sealed class FakeGraphSettingsRepository : IGraphSettingsRepository
{
    public GraphSettings Settings { get; private set; } = GraphSettings.Default;

    public int SaveCallCount { get; private set; }

    public GraphSettings Load()
    {
        return Settings;
    }

    public void Save(GraphSettings settings)
    {
        Settings = settings;
        SaveCallCount++;
    }
}
