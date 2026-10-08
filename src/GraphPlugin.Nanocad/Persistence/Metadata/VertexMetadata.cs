using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Persistence.Metadata;

public sealed record VertexMetadata(
    int Version,
    Guid Id,
    VertexShape Shape,
    GraphColor Color,
    double Size
);