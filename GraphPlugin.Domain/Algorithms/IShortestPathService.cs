using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Algorithms;

public interface IShortestPathService
{
    ShortestPathResult Find(
        Graph graph,
        Guid startVertexId,
        Guid endVertexId);
}