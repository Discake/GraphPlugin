using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Algorithms;

public sealed class DijkstraShortestPathService
    : IShortestPathService
{
    private readonly EdgeLengthCalculator _lengthCalculator;

    public DijkstraShortestPathService(
        EdgeLengthCalculator lengthCalculator)
    {
        _lengthCalculator = lengthCalculator;
    }

    public ShortestPathResult Find(
        Graph graph,
        Guid startVertexId,
        Guid endVertexId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var start =
            graph.GetVertex(startVertexId);

        if (start is null)
        {
            throw new ArgumentException(
                $"Start vertex {startVertexId} does not exist.",
                nameof(startVertexId));
        }

        var end =
            graph.GetVertex(endVertexId);

        if (end is null)
        {
            throw new ArgumentException(
                $"End vertex {endVertexId} does not exist.",
                nameof(endVertexId));
        }

        // Путь из вершины в неё саму.
        if (startVertexId == endVertexId)
        {
            return ShortestPathResult.Create(
                new[] { startVertexId },
                Array.Empty<Guid>(),
                0);
        }

        var distances =
            graph.Vertices.ToDictionary(
                vertex => vertex.Id,
                _ => double.PositiveInfinity);

        var verticesDict = graph.Vertices.ToDictionary(
            vertex => vertex.Id,
            vertex => vertex);

        var previousVertex =
            new Dictionary<Guid, Guid>();

        var previousEdge =
            new Dictionary<Guid, Guid>();

        var queue =
            new PriorityQueue<Guid, double>();

        distances[startVertexId] = 0;

        queue.Enqueue(
            startVertexId,
            0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(
                out var currentVertexId,
                out var queuedDistance);

            // В очереди могут остаться старые записи
            // после улучшения расстояния.
            if (queuedDistance >
                distances[currentVertexId])
            {
                continue;
            }

            if (currentVertexId ==
                endVertexId)
            {
                break;
            }

            var incidentEdges =
                graph.GetIncidentEdges(
                    currentVertexId);

            foreach (var edge in incidentEdges)
            {
                var neighbourId =
                    edge.GetOtherVertexId(
                        currentVertexId);

                var edgeLength =
                    _lengthCalculator.Calculate(
                        edge,
                        verticesDict[edge.VertexAId],
                        verticesDict[edge.VertexBId]);

                var candidateDistance =
                    distances[currentVertexId] +
                    edgeLength;

                if (candidateDistance >=
                    distances[neighbourId])
                {
                    continue;
                }

                distances[neighbourId] =
                    candidateDistance;

                previousVertex[neighbourId] =
                    currentVertexId;

                previousEdge[neighbourId] =
                    edge.Id;

                queue.Enqueue(
                    neighbourId,
                    candidateDistance);
            }
        }

        if (double.IsPositiveInfinity(
                distances[endVertexId]))
        {
            return ShortestPathResult.NoPath();
        }

        return BuildResult(
            startVertexId,
            endVertexId,
            distances[endVertexId],
            previousVertex,
            previousEdge);
    }

    private static ShortestPathResult BuildResult(
        Guid startVertexId,
        Guid endVertexId,
        double totalLength,
        IReadOnlyDictionary<Guid, Guid> previousVertex,
        IReadOnlyDictionary<Guid, Guid> previousEdge)
    {
        var vertexIds =
            new List<Guid>();

        var edgeIds =
            new List<Guid>();

        var current =
            endVertexId;

        vertexIds.Add(current);

        while (current != startVertexId)
        {
            if (!previousVertex.TryGetValue(
                    current,
                    out var previous))
            {
                return ShortestPathResult.NoPath();
            }

            if (!previousEdge.TryGetValue(
                    current,
                    out var edgeId))
            {
                return ShortestPathResult.NoPath();
            }

            edgeIds.Add(edgeId);

            current = previous;

            vertexIds.Add(current);
        }

        vertexIds.Reverse();
        edgeIds.Reverse();

        return ShortestPathResult.Create(
            vertexIds,
            edgeIds,
            totalLength);
    }
}