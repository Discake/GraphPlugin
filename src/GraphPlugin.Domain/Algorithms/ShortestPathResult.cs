namespace GraphPlugin.Domain.Algorithms;

public sealed class ShortestPathResult
{
    public bool Found { get; }

    public IReadOnlyList<Guid> VertexIds { get; }

    public IReadOnlyList<Guid> EdgeIds { get; }

    public double TotalLength { get; }

    private ShortestPathResult(
        bool found,
        IReadOnlyList<Guid> vertexIds,
        IReadOnlyList<Guid> edgeIds,
        double totalLength)
    {
        Found = found;
        VertexIds = vertexIds;
        EdgeIds = edgeIds;
        TotalLength = totalLength;
    }

    public static ShortestPathResult NoPath()
    {
        return new ShortestPathResult(
            false,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            double.PositiveInfinity);
    }

    public static ShortestPathResult Create(
        IReadOnlyList<Guid> vertexIds,
        IReadOnlyList<Guid> edgeIds,
        double totalLength)
    {
        return new ShortestPathResult(
            true,
            vertexIds,
            edgeIds,
            totalLength);
    }
}