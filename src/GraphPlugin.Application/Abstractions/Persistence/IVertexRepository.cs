using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Abstractions.Persistence;

public interface IVertexRepository
{
    GraphVertex? Get(Guid id);

    IReadOnlyCollection<GraphVertex> GetAll();

    void Add(GraphVertex vertex);

    void Delete(Guid id);
}
