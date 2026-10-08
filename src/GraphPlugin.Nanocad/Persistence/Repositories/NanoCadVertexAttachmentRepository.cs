using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class NanoCadVertexAttachmentRepository : IVertexAttachmentRepository
{
    private readonly Document _document;

    private readonly GraphEntityIndex _index;

    private readonly VertexAttachmentXRecordStore _store;

    public NanoCadVertexAttachmentRepository(
        Document document,
        GraphEntityIndex index,
        VertexAttachmentXRecordStore store
    )
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        _index = index ?? throw new ArgumentNullException(nameof(index));

        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public IReadOnlyCollection<VertexAttachment> GetAll(Guid vertexId)
    {
        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var entity = GetVertexEntity(vertexId, transaction, OpenMode.ForRead);

        var attachments = _store.Read(entity, transaction).ToArray();

        transaction.Commit();

        return attachments;
    }

    public void Add(Guid vertexId, VertexAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var entity = GetVertexEntity(vertexId, transaction, OpenMode.ForWrite);

        var attachments = _store.Read(entity, transaction).ToList();

        attachments.Add(attachment);

        _store.Write(entity, transaction, attachments);

        transaction.Commit();
    }

    public void Remove(Guid vertexId, string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Attachment path cannot be empty.", nameof(storedPath));
        }

        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var entity = GetVertexEntity(vertexId, transaction, OpenMode.ForWrite);

        var attachments = _store.Read(entity, transaction).ToList();

        var removed = attachments.RemoveAll(attachment =>
            string.Equals(attachment.Path, storedPath, StringComparison.OrdinalIgnoreCase)
        );

        if (removed == 0)
        {
            //
            // Repository остаётся идемпотентным.
            //
            transaction.Commit();
            return;
        }

        _store.Write(entity, transaction, attachments);

        transaction.Commit();
    }

    private Entity GetVertexEntity(Guid vertexId, Transaction transaction, OpenMode openMode)
    {
        if (!_index.TryGetVertexObjectId(vertexId, out var objectId))
        {
            throw new InvalidOperationException($"Vertex '{vertexId}' is missing from index.");
        }

        var entity = transaction.GetObject(objectId, openMode) as Entity;

        if (entity is null || entity.IsErased)
        {
            throw new InvalidOperationException($"Vertex '{vertexId}' DWG entity does not exist.");
        }

        return entity;
    }
}
