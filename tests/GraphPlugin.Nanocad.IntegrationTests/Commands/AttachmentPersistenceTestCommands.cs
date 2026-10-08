using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using GraphPlugin.NanoCad.Persistence;
using GraphPlugin.NanoCad.Runtime;
using Teigha.DatabaseServices;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class AttachmentPersistenceTestCommands
{
    [CommandMethod("GRAPH_PREPARE_ATTACHMENT_PERSISTENCE_TEST")]
    public void PrepareAttachmentPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        var editor =
            document.Editor;

        var manifestStore =
            new AttachmentPersistenceTestManifestStore();

        using (var transaction =
               document.Database
                   .TransactionManager
                   .StartTransaction())
        {
            var existing =
                manifestStore.Read(
                    document.Database,
                    transaction);

            if (existing is not null)
            {
                throw new InvalidOperationException(
                    "An attachment persistence test is already prepared. " +
                    "Run GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST " +
                    "or clear the previous test first.");
            }
        }

        try
        {
            var vertexService =
                new VertexService(
                    context.Vertices);

            var vertex =
                vertexService.CreateVertex(
                    new Point2(
                        3200,
                        3200));

            var paths =
                new[]
                {
                    @"Documents\attachment-persist-a.pdf",
                    @"Images\attachment-persist-b.jpg"
                };

            foreach (var path in paths)
            {
                context.Attachments.Add(
                    vertex.Id,
                    new VertexAttachment(
                        path));
            }

            var beforeSave =
                context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            EnsureAttachmentPaths(
                beforeSave,
                paths,
                "Prepare");

            var manifest =
                new AttachmentPersistenceTestManifest(
                    vertex.Id,
                    paths);

            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifestStore.Write(
                    document.Database,
                    transaction,
                    manifest);

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nAttachment persistence test prepared.");

            editor.WriteMessage(
                $"\nVertexId: {vertex.Id}");

            editor.WriteMessage(
                "\nNow SAVE the drawing, CLOSE it, OPEN it again, " +
                "then run GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST.");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\nAttachment persistence prepare FAILED:\n{exception}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST")]
    public void VerifyAttachmentPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        var editor =
            document.Editor;

        var manifestStore =
            new AttachmentPersistenceTestManifestStore();

        try
        {
            AttachmentPersistenceTestManifest manifest;

            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifest =
                    manifestStore.Read(
                        document.Database,
                        transaction)
                    ?? throw new InvalidOperationException(
                        "Attachment persistence test manifest " +
                        "was not found.");

                transaction.Commit();
            }

            var vertex =
                context.Vertices.Get(
                    manifest.VertexId);

            if (vertex is null)
            {
                throw new IntegrationTestException(
                    $"Vertex '{manifest.VertexId}' " +
                    "was not restored after reopening DWG.");
            }

            if (!context.Index.TryGetVertexObjectId(
                    manifest.VertexId,
                    out var objectId))
            {
                throw new IntegrationTestException(
                    "Restored attachment test vertex " +
                    "is missing from GraphEntityIndex.");
            }

            var restoredAttachments =
                context.Attachments
                    .GetAll(
                        manifest.VertexId)
                    .ToArray();

            EnsureAttachmentPaths(
                restoredAttachments,
                manifest.Paths,
                "Verify");

            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var entity =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Entity
                    ?? throw new IntegrationTestException(
                        "Restored vertex ObjectId does not point " +
                        "to an Entity.");

                if (entity.ExtensionDictionary.IsNull)
                {
                    throw new IntegrationTestException(
                        "Restored vertex has no ExtensionDictionary.");
                }

                var dictionary =
                    transaction.GetObject(
                        entity.ExtensionDictionary,
                        OpenMode.ForRead)
                    as DBDictionary
                    ?? throw new IntegrationTestException(
                        "Restored vertex ExtensionDictionary " +
                        "could not be opened.");

                if (!dictionary.Contains(
                        VertexAttachmentXRecordStore.RecordKey))
                {
                    throw new IntegrationTestException(
                        $"'{VertexAttachmentXRecordStore.RecordKey}' " +
                        "was not persisted in DWG.");
                }
            }

            editor.WriteMessage(
                "\n[PASS] Attachment persistence after SAVE/CLOSE/OPEN.");

            editor.WriteMessage(
                $"\nVertexId preserved: {manifest.VertexId}");

            editor.WriteMessage(
                $"\nAttachments restored: " +
                $"{restoredAttachments.Length}");

            Cleanup(
                context,
                document,
                manifest,
                manifestStore);

            editor.WriteMessage(
                "\nAttachment persistence test data cleared.");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\n[FAIL] Attachment persistence test:\n{exception}");
        }
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
                    $"{stage}: attachment '{expectedPath}' " +
                    "was not found.");
            }
        }
    }

    private static void Cleanup(
        GraphDocumentContext context,
        HostMgd.ApplicationServices.Document document,
        AttachmentPersistenceTestManifest manifest,
        AttachmentPersistenceTestManifestStore manifestStore)
    {
        var graphService =
            new GraphService(
                context.Vertices,
                context.Edges);

        if (context.Vertices.Get(
                manifest.VertexId) is not null)
        {
            graphService.DeleteVertex(
                manifest.VertexId);
        }

        using var transaction =
            document.Database
                .TransactionManager
                .StartTransaction();

        manifestStore.Delete(
            document.Database,
            transaction);

        transaction.Commit();
    }
}
