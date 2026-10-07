namespace GraphPlugin.NanoCad.Runtime;

public sealed record AttachmentPersistenceTestManifest(
    Guid VertexId,
    IReadOnlyList<string> Paths);