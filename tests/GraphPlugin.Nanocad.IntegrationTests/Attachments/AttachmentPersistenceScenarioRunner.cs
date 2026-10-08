using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

internal sealed class AttachmentPersistenceScenarioRunner
{
    private readonly Document _document;
    private readonly GraphDocumentContext _context;
    private readonly AttachmentPersistenceTestManifestStore _store;

    public AttachmentPersistenceScenarioRunner(
        Document document,
        GraphDocumentContext context)
    {
        _document = document;
        _context = context;
        _store = new AttachmentPersistenceTestManifestStore();
    }

    public bool IsPrepared()
    {
        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        return _store.Read(
            _document.Database,
            transaction) is not null;
    }

    public AttachmentPersistenceTestManifest Prepare()
    {
        if (IsPrepared())
        {
            throw new InvalidOperationException(
                "An attachment persistence test is already prepared.");
        }

        GraphVertex? vertex = null;

        try
        {
            vertex = _context.VertexService.CreateVertex(
                new Point2(3200, 3200));

            var paths =
                new[]
                {
                    @"Documents\attachment-persist-a.pdf",
                    @"Images\attachment-persist-b.jpg"
                };

            foreach (var path in paths)
            {
                _context.Attachments.Add(
                    vertex.Id,
                    new VertexAttachment(path));
            }

            EnsureAttachmentPaths(
                _context.Attachments
                    .GetAll(vertex.Id),
                paths,
                "Prepare");

            var manifest =
                new AttachmentPersistenceTestManifest(
                    vertex.Id,
                    paths);

            using var transaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();

            _store.Write(
                _document.Database,
                transaction,
                manifest);

            transaction.Commit();
            return manifest;
        }
        catch
        {
            if (vertex is not null &&
                _context.Vertices.Get(vertex.Id) is not null)
            {
                _context.Graph.DeleteVertex(vertex.Id);
            }

            DeleteManifestIfPresent();
            throw;
        }
    }

    public IReadOnlyList<IntegrationTestResult> Verify()
    {
        var manifest = RequireManifest();
        var results = new List<IntegrationTestResult>();

        Run(
            results,
            "Attachment vertex restored",
            () => Ensure(
                _context.Vertices.Get(manifest.VertexId) is not null,
                $"Vertex '{manifest.VertexId}' was not restored after reopening DWG."));

        Run(
            results,
            "Attachment vertex restored in index",
            () => Ensure(
                _context.Index.TryGetVertexObjectId(
                    manifest.VertexId,
                    out var objectId) &&
                !objectId.IsNull &&
                !objectId.IsErased,
                "Restored attachment vertex is missing from GraphEntityIndex."));

        Run(
            results,
            "Attachment paths restored",
            () => EnsureAttachmentPaths(
                _context.Attachments
                    .GetAll(manifest.VertexId),
                manifest.Paths,
                "Verify"));

        Run(
            results,
            "Attachment XRecord persisted",
            () => VerifyAttachmentXRecord(manifest));

        return results;
    }

    public void Clear()
    {
        var manifest = TryReadManifest();

        if (manifest is not null &&
            _context.Vertices.Get(manifest.VertexId) is not null)
        {
            _context.Graph.DeleteVertex(manifest.VertexId);
        }

        DeleteManifestIfPresent();
    }

    private AttachmentPersistenceTestManifest RequireManifest() =>
        TryReadManifest()
        ?? throw new InvalidOperationException(
            "Attachment persistence test manifest was not found.");

    private AttachmentPersistenceTestManifest? TryReadManifest()
    {
        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        return _store.Read(
            _document.Database,
            transaction);
    }

    private void DeleteManifestIfPresent()
    {
        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        _store.Delete(
            _document.Database,
            transaction);

        transaction.Commit();
    }

    private void VerifyAttachmentXRecord(
        AttachmentPersistenceTestManifest manifest)
    {
        Ensure(
            _context.Index.TryGetVertexObjectId(
                manifest.VertexId,
                out var objectId),
            "Restored attachment vertex is missing from GraphEntityIndex.");

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead) as Entity
            ?? throw new IntegrationTestException(
                "Restored attachment ObjectId does not point to an Entity.");

        Ensure(
            !entity.ExtensionDictionary.IsNull,
            "Restored attachment vertex has no ExtensionDictionary.");

        var dictionary =
            transaction.GetObject(
                entity.ExtensionDictionary,
                OpenMode.ForRead) as DBDictionary
            ?? throw new IntegrationTestException(
                "Restored attachment ExtensionDictionary could not be opened.");

        Ensure(
            dictionary.Contains(
                VertexAttachmentXRecordStore.RecordKey),
            $"'{VertexAttachmentXRecordStore.RecordKey}' was not persisted in DWG.");
    }

    private static void EnsureAttachmentPaths(
        IReadOnlyCollection<VertexAttachment> actual,
        IReadOnlyCollection<string> expected,
        string stage)
    {
        if (actual.Count != expected.Count)
        {
            throw new IntegrationTestException(
                $"{stage}: expected {expected.Count} attachments, " +
                $"actual {actual.Count}.");
        }

        var actualPaths =
            actual
                .Select(x => x.Path)
                .ToArray();

        foreach (var expectedPath in expected)
        {
            var exists =
                actualPaths.Any(
                    actualPath =>
                        string.Equals(
                            actualPath,
                            expectedPath,
                            StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                throw new IntegrationTestException(
                    $"{stage}: attachment '{expectedPath}' was not found.");
            }
        }
    }

    private static void Ensure(
        bool condition,
        string message)
    {
        if (!condition)
            throw new IntegrationTestException(message);
    }

    private static void Run(
        ICollection<IntegrationTestResult> results,
        string name,
        Action action)
    {
        try
        {
            action();
            results.Add(
                new IntegrationTestResult(
                    name,
                    true));
        }
        catch (Exception exception)
        {
            results.Add(
                new IntegrationTestResult(
                    name,
                    false,
                    exception.Message));
        }
    }
}
