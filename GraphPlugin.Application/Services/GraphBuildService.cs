using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class GraphBuildService
{
    private readonly EdgeService _edgeService;
    private readonly IEdgeRepository _edges;

    public GraphVertex? CurrentVertex
    {
        get;
        private set;
    }

    public bool IsActive =>
        CurrentVertex is not null;

    public GraphBuildService(
        EdgeService edgeService,
        IEdgeRepository edges)
    {
        _edgeService =
            edgeService ??
            throw new ArgumentNullException(
                nameof(edgeService));

        _edges =
            edges ??
            throw new ArgumentNullException(
                nameof(edges));
    }

    public GraphEdge? AdvanceTo(
        GraphVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(
            vertex);

        // Первый элемент цепочки.
        if (CurrentVertex is null)
        {
            CurrentVertex = vertex;

            return null;
        }

        // Пользователь снова выбрал ту же Vertex.
        // Ничего не создаём.
        if (CurrentVertex.Id == vertex.Id)
        {
            return null;
        }

        // Например, после SplitEdge:
        //
        // Current = A
        //
        // A ---- C ---- B
        //
        // Edge A-C уже был создан SplitEdgeService.
        // Поэтому второй A-C создавать нельзя.
        if (AreConnected(
                CurrentVertex.Id,
                vertex.Id))
        {
            CurrentVertex = vertex;

            return null;
        }

        var edge =
            _edgeService.CreateEdge(
                CurrentVertex.Id,
                vertex.Id);

        CurrentVertex = vertex;

        return edge;
    }

    public void Finish()
    {
        CurrentVertex = null;
    }

    private bool AreConnected(
        Guid vertexAId,
        Guid vertexBId)
    {
        var incidentEdges =
            _edges.GetByVertex(
                vertexAId);

        return incidentEdges.Any(
            edge =>
                edge.IsIncidentTo(
                    vertexBId));
    }
}