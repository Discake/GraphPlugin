using GraphPlugin.Domain.Models;

namespace GraphPlugin.Nanocad.Persistence.Metadata;

public sealed record VertexMetadata(int Version, Guid Id, VertexShape Shape, GraphColor Color, double Size);
