using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

internal sealed class AttachmentUndoScenarioRunner
{
    private readonly Document _document;
    private readonly GraphDocumentContext _context;
    private readonly AttachmentUndoTestManifestStore _store;

    public AttachmentUndoScenarioRunner(Document document, GraphDocumentContext context)
    {
        _document = document;
        _context = context;
        _store = new AttachmentUndoTestManifestStore();
    }

    public bool IsPrepared() => _store.Exists(_document.Database);

    public AttachmentUndoTestManifest Prepare()
    {
        if (IsPrepared())
        {
            throw new InvalidOperationException("An attachment undo test is already prepared.");
        }

        GraphVertex? vertex = null;

        try
        {
            vertex = _context.VertexService.CreateVertex(new Point2(3500, 3500));

            const string attachmentPath = @"UndoTest\attachment.pdf";

            _context.Attachments.Add(vertex.Id, new VertexAttachment(attachmentPath));

            var attachments = _context.Attachments.GetAll(vertex.Id).ToArray();

            Ensure(attachments.Length == 1, $"Prepare: expected one attachment, " + $"actual {attachments.Length}.");

            Ensure(
                string.Equals(attachments[0].Path, attachmentPath, StringComparison.Ordinal),
                "Prepare: attachment path is incorrect."
            );

            var manifest = new AttachmentUndoTestManifest(vertex.Id, attachmentPath);

            _store.Write(_document.Database, manifest);

            return manifest;
        }
        catch
        {
            if (vertex is not null && _context.Vertices.Get(vertex.Id) is not null)
            {
                _context.Graph.DeleteVertex(vertex.Id);
            }

            _store.Delete(_document.Database);
            throw;
        }
    }

    public void ErasePreparedVertex()
    {
        var manifest = RequireManifest();

        if (!_context.Index.TryGetVertexObjectId(manifest.VertexId, out var objectId))
        {
            throw new InvalidOperationException("Prepared attachment vertex is missing from GraphEntityIndex.");
        }

        if (objectId.IsNull || objectId.IsErased)
        {
            throw new InvalidOperationException("Prepared attachment vertex is already erased or invalid.");
        }

        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var entity =
            transaction.GetObject(objectId, OpenMode.ForWrite) as Entity
            ?? throw new InvalidOperationException("Prepared attachment vertex ObjectId is not an Entity.");

        entity.Erase();
        transaction.Commit();
    }

    public IReadOnlyList<IntegrationTestResult> VerifyErased()
    {
        var manifest = RequireManifest();
        var results = new List<IntegrationTestResult>();

        Run(
            results,
            "Attachment vertex removed",
            () => Ensure(_context.Vertices.Get(manifest.VertexId) is null, "Attachment test vertex still exists.")
        );

        Run(
            results,
            "Attachment vertex removed from index",
            () =>
                Ensure(
                    !_context.Index.TryGetVertexObjectId(manifest.VertexId, out _),
                    "Attachment test vertex is still present in index."
                )
        );

        return results;
    }

    public IReadOnlyList<IntegrationTestResult> VerifyUndo()
    {
        var manifest = RequireManifest();
        var results = new List<IntegrationTestResult>();

        Run(
            results,
            "Attachment vertex restored",
            () =>
                Ensure(
                    _context.Vertices.Get(manifest.VertexId) is not null,
                    "Attachment test vertex was not restored by UNDO."
                )
        );

        Run(
            results,
            "Attachment vertex restored in index",
            () =>
                Ensure(
                    _context.Index.TryGetVertexObjectId(manifest.VertexId, out var objectId)
                        && !objectId.IsNull
                        && !objectId.IsErased,
                    "Restored attachment vertex is missing from GraphEntityIndex."
                )
        );

        Run(
            results,
            "Attachment metadata restored",
            () =>
            {
                var attachments = _context.Attachments.GetAll(manifest.VertexId).ToArray();

                Ensure(
                    attachments.Length == 1,
                    $"Expected one restored attachment, " + $"actual {attachments.Length}."
                );

                Ensure(
                    string.Equals(attachments[0].Path, manifest.AttachmentPath, StringComparison.Ordinal),
                    $"Restored attachment path is incorrect. "
                        + $"Actual: '{attachments[0].Path}', "
                        + $"expected: '{manifest.AttachmentPath}'."
                );
            }
        );

        Run(results, "Attachment XRecord restored", () => VerifyAttachmentXRecord(manifest));

        return results;
    }

    public void Clear()
    {
        var manifest = _store.Read(_document.Database);

        if (manifest is not null && _context.Vertices.Get(manifest.VertexId) is not null)
        {
            _context.Graph.DeleteVertex(manifest.VertexId);
        }

        _store.Delete(_document.Database);
    }

    private AttachmentUndoTestManifest RequireManifest() =>
        _store.Read(_document.Database)
        ?? throw new InvalidOperationException("Attachment undo test manifest was not found.");

    private void VerifyAttachmentXRecord(AttachmentUndoTestManifest manifest)
    {
        Ensure(
            _context.Index.TryGetVertexObjectId(manifest.VertexId, out var objectId),
            "Restored attachment vertex is missing from index."
        );

        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var entity =
            transaction.GetObject(objectId, OpenMode.ForRead) as Entity
            ?? throw new IntegrationTestException("Restored attachment ObjectId is not an Entity.");

        Ensure(!entity.ExtensionDictionary.IsNull, "Restored attachment vertex has no ExtensionDictionary.");

        var dictionary =
            transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead) as DBDictionary
            ?? throw new IntegrationTestException("Restored attachment ExtensionDictionary could not be opened.");

        Ensure(
            dictionary.Contains(VertexAttachmentXRecordStore.RecordKey),
            $"'{VertexAttachmentXRecordStore.RecordKey}' " + "was not restored by UNDO."
        );
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
            throw new IntegrationTestException(message);
    }

    private static void Run(ICollection<IntegrationTestResult> results, string name, Action action)
    {
        try
        {
            action();
            results.Add(new IntegrationTestResult(name, true));
        }
        catch (Exception exception)
        {
            results.Add(new IntegrationTestResult(name, false, exception.Message));
        }
    }
}
