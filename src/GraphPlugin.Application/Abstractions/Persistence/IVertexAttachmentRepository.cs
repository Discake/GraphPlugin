using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Abstractions.Persistence;

public interface IVertexAttachmentRepository
{
    IReadOnlyCollection<VertexAttachment> GetAll(Guid vertexId);

    void Add(Guid vertexId, VertexAttachment attachment);

    void Remove(Guid vertexId, string storedPath);
}
