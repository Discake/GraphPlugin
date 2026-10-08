namespace GraphPlugin.Nanocad.Persistence;

public sealed record CppDeleteUndoTestManifest(
    Guid VertexAId,
    Guid VertexBId,
    Guid VertexCId,
    Guid EdgeABId,
    Guid EdgeACId,
    Guid EdgeBCId
);
