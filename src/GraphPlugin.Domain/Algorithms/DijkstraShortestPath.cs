using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Algorithms;

public sealed class DijkstraShortestPath
{
    private readonly EdgeLengthCalculator _lengthCalculator;

    public DijkstraShortestPath(
        EdgeLengthCalculator lengthCalculator)
    {
        _lengthCalculator =
            lengthCalculator ??
            throw new ArgumentNullException(
                nameof(lengthCalculator));
    }

    public ShortestPathResult Find(
        IReadOnlyCollection<GraphVertex> vertices,
        IReadOnlyCollection<GraphEdge> edges,
        Guid startVertexId,
        Guid endVertexId)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(edges);

        var verticesById =
            BuildVertexIndex(vertices);

        if (!verticesById.ContainsKey(startVertexId))
        {
            throw new ArgumentException(
                $"Start vertex {startVertexId} does not exist.",
                nameof(startVertexId));
        }

        if (!verticesById.ContainsKey(endVertexId))
        {
            throw new ArgumentException(
                $"End vertex {endVertexId} does not exist.",
                nameof(endVertexId));
        }

        var adjacency =
            BuildAdjacency(
                verticesById,
                edges);

        if (startVertexId == endVertexId)
        {
            return ShortestPathResult.Create(
                new[] { startVertexId },
                Array.Empty<Guid>(),
                0);
        }

        var distances =
            verticesById.Keys.ToDictionary(
                vertexId => vertexId,
                _ => double.PositiveInfinity);

        var previousVertex =
            new Dictionary<Guid, Guid>();

        var previousEdge =
            new Dictionary<Guid, Guid>();

        var queue =
            new PriorityQueue<Guid, double>();

        distances[startVertexId] = 0;
        queue.Enqueue(startVertexId, 0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(
                out var currentVertexId,
                out var queuedDistance);

            if (queuedDistance >
                distances[currentVertexId])
            {
                continue;
            }

            if (currentVertexId == endVertexId)
                break;

            foreach (var edge in
                     adjacency[currentVertexId])
            {
                var neighbourId =
                    edge.GetOtherVertexId(
                        currentVertexId);

                var edgeLength =
                    _lengthCalculator.Calculate(
                        edge,
                        verticesById[edge.VertexAId],
                        verticesById[edge.VertexBId]);

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

    private static Dictionary<Guid, GraphVertex>
        BuildVertexIndex(
            IReadOnlyCollection<GraphVertex> vertices)
    {
        var result =
            new Dictionary<Guid, GraphVertex>(
                vertices.Count);

        foreach (var vertex in vertices)
        {
            ArgumentNullException.ThrowIfNull(vertex);

            if (!result.TryAdd(
                    vertex.Id,
                    vertex))
            {
                throw new InvalidOperationException(
                    $"Vertex {vertex.Id} already exists.");
            }
        }

        return result;
    }

    private static Dictionary<Guid, List<GraphEdge>>
        BuildAdjacency(
            IReadOnlyDictionary<Guid, GraphVertex> vertices,
            IReadOnlyCollection<GraphEdge> edges)
    {
        var adjacency =
            vertices.Keys.ToDictionary(
                vertexId => vertexId,
                _ => new List<GraphEdge>());

        var edgeIds =
            new HashSet<Guid>();

        foreach (var edge in edges)
        {
            ArgumentNullException.ThrowIfNull(edge);

            if (!edgeIds.Add(edge.Id))
            {
                throw new InvalidOperationException(
                    $"Edge {edge.Id} already exists.");
            }

            if (!vertices.ContainsKey(
                    edge.VertexAId))
            {
                throw new InvalidOperationException(
                    $"Vertex {edge.VertexAId} does not exist.");
            }

            if (!vertices.ContainsKey(
                    edge.VertexBId))
            {
                throw new InvalidOperationException(
                    $"Vertex {edge.VertexBId} does not exist.");
            }

            adjacency[edge.VertexAId].Add(edge);
            adjacency[edge.VertexBId].Add(edge);
        }

        return adjacency;
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
