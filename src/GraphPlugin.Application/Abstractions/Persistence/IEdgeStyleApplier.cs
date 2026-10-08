using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Abstractions;

public interface IEdgeStyleApplier
{
    void ApplyToAll(EdgeStyle style);
}
