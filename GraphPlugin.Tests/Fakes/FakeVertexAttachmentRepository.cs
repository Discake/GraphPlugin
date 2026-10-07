using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

public sealed class FakeVertexAttachmentRepository
    : IVertexAttachmentRepository
{
    private readonly Dictionary<Guid, List<VertexAttachment>>
        _attachments = new();

    public IReadOnlyCollection<VertexAttachment> GetAll(
        Guid vertexId)
    {
        if (!_attachments.TryGetValue(
                vertexId,
                out var attachments))
        {
            return Array.Empty<VertexAttachment>();
        }

        return attachments.ToArray();
    }

    public void Add(
        Guid vertexId,
        VertexAttachment attachment)
    {
        if (!_attachments.TryGetValue(
                vertexId,
                out var attachments))
        {
            attachments =
                new List<VertexAttachment>();

            _attachments.Add(
                vertexId,
                attachments);
        }

        attachments.Add(
            attachment);
    }

    public void Remove(
        Guid vertexId,
        string storedPath)
    {
        if (!_attachments.TryGetValue(
                vertexId,
                out var attachments))
        {
            return;
        }

        attachments.RemoveAll(
            attachment =>
                string.Equals(
                    attachment.Path,
                    storedPath,
                    StringComparison.OrdinalIgnoreCase));

        if (attachments.Count == 0)
        {
            _attachments.Remove(
                vertexId);
        }
    }
}