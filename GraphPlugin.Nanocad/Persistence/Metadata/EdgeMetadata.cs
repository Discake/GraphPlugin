namespace GraphPlugin.NanoCad.Persistence.Metadata;

public sealed record EdgeMetadata(
    int Version,
    Guid Id,
    Guid VertexAId,
    Guid VertexBId
);