namespace GraphPlugin.Nanocad.Persistence.Metadata;

public sealed record EdgeMetadata(
    int Version,
    Guid Id,
    Guid VertexAId,
    Guid VertexBId
);