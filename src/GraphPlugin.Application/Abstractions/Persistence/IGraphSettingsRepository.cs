using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Abstractions.Persistence;

public interface IGraphSettingsRepository
{
    GraphSettings Load();

    void Save(GraphSettings settings);
}
