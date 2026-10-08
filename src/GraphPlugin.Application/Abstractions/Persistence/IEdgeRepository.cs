using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Abstractions.Persistence;

public interface IEdgeRepository
{
    GraphEdge? Get(Guid id);

    IReadOnlyCollection<GraphEdge> GetAll();

    IReadOnlyCollection<GraphEdge> GetByVertex(
        Guid vertexId);

    void Add(GraphEdge edge);

    void Update(GraphEdge edge);

    void Delete(Guid id);
}
