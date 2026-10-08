using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Runtime;
using Teigha.DatabaseServices;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class AttachmentUndoTestCommands
{
    private sealed record AttachmentUndoTestState(
        HostMgd.ApplicationServices.Document Document,
        Guid VertexId,
        string AttachmentPath);

    private static AttachmentUndoTestState? _state;

    [CommandMethod("GRAPH_PREPARE_ATTACHMENT_UNDO_TEST")]
    public void PrepareAttachmentUndoTest()
    {
        var document = NanoApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
            return;

        var editor = document.Editor;

        try
        {
            if (_state is not null)
                throw new InvalidOperationException("Attachment undo test is already active.");

            var context = PluginServices.CurrentContext;
            var vertex = new VertexService(context.Vertices)
                .CreateVertex(new Point2(3500, 3500));

            const string attachmentPath = @"UndoTest\attachment.pdf";

            context.Attachments.Add(
                vertex.Id,
                new VertexAttachment(attachmentPath));

            var attachments = context.Attachments
                .GetAll(vertex.Id)
                .ToArray();

            if (attachments.Length != 1)
            {
                throw new IntegrationTestException(
                    $"Prepare: expected one attachment, actual {attachments.Length}.");
            }

            if (!string.Equals(
                    attachments[0].Path,
                    attachmentPath,
                    StringComparison.Ordinal))
            {
                throw new IntegrationTestException(
                    "Prepare: attachment path is incorrect.");
            }

            _state = new AttachmentUndoTestState(
                document,
                vertex.Id,
                attachmentPath);

            editor.WriteMessage("\nAttachment UNDO test prepared.");
            editor.WriteMessage($"\nVertexId: {vertex.Id}");
            editor.WriteMessage("\nNow run GRAPH_ERASE_ATTACHMENT_UNDO_TEST.");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\n[FAIL] Attachment UNDO prepare:\n{exception}");
        }
    }

    [CommandMethod("GRAPH_ERASE_ATTACHMENT_UNDO_TEST")]
    public void EraseAttachmentUndoTest()
    {
        var document = NanoApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
            return;

        var editor = document.Editor;

        try
        {
            var state = _state
                ?? throw new InvalidOperationException(
                    "Attachment UNDO test was not prepared.");

            if (!ReferenceEquals(state.Document, document))
            {
                throw new InvalidOperationException(
                    "Attachment UNDO test belongs to another document.");
            }

            var context = PluginServices.CurrentContext;

            if (context.Vertices.Get(state.VertexId) is null)
            {
                throw new IntegrationTestException(
                    "Test vertex does not exist before erase.");
            }

            new GraphService(context.Vertices, context.Edges)
                .DeleteVertex(state.VertexId);

            if (context.Vertices.Get(state.VertexId) is not null)
            {
                throw new IntegrationTestException(
                    "Vertex still exists after delete.");
            }

            if (context.Index.TryGetVertexObjectId(state.VertexId, out _))
            {
                throw new IntegrationTestException(
                    "Deleted vertex still exists in index.");
            }

            editor.WriteMessage("\nTest vertex erased successfully.");
            editor.WriteMessage("\nNow press Ctrl+Z ONCE.");
            editor.WriteMessage(
                "\nAfter UNDO finishes, run GRAPH_VERIFY_ATTACHMENT_UNDO_TEST.");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\n[FAIL] Attachment UNDO erase:\n{exception}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_ATTACHMENT_UNDO_TEST")]
    public void VerifyAttachmentUndoTest()
    {
        var document = NanoApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
            return;

        var editor = document.Editor;

        try
        {
            var state = _state
                ?? throw new InvalidOperationException(
                    "Attachment UNDO test was not prepared.");

            if (!ReferenceEquals(state.Document, document))
            {
                throw new InvalidOperationException(
                    "Attachment UNDO test belongs to another document.");
            }

            var context = PluginServices.CurrentContext;

            if (context.Vertices.Get(state.VertexId) is null)
            {
                throw new IntegrationTestException(
                    "Vertex was not restored by UNDO.");
            }

            if (!context.Index.TryGetVertexObjectId(
                    state.VertexId,
                    out var objectId))
            {
                throw new IntegrationTestException(
                    "Restored vertex is missing from GraphEntityIndex.");
            }

            var attachments = context.Attachments
                .GetAll(state.VertexId)
                .ToArray();

            if (attachments.Length != 1)
            {
                throw new IntegrationTestException(
                    $"Expected one restored attachment, actual {attachments.Length}.");
            }

            if (!string.Equals(
                    attachments[0].Path,
                    state.AttachmentPath,
                    StringComparison.Ordinal))
            {
                throw new IntegrationTestException(
                    $"Restored attachment path is incorrect. Actual: '{attachments[0].Path}', " +
                    $"expected: '{state.AttachmentPath}'.");
            }

            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(
                        objectId,
                        OpenMode.ForRead) as Entity
                    ?? throw new IntegrationTestException(
                        "Restored ObjectId is not an Entity.");

                if (entity.ExtensionDictionary.IsNull)
                {
                    throw new IntegrationTestException(
                        "Restored vertex has no ExtensionDictionary.");
                }

                var dictionary = transaction.GetObject(
                        entity.ExtensionDictionary,
                        OpenMode.ForRead) as DBDictionary
                    ?? throw new IntegrationTestException(
                        "Restored ExtensionDictionary could not be opened.");

                if (!dictionary.Contains(VertexAttachmentXRecordStore.RecordKey))
                {
                    throw new IntegrationTestException(
                        $"'{VertexAttachmentXRecordStore.RecordKey}' was not restored by UNDO.");
                }
            }

            editor.WriteMessage(
                "\n[PASS] Vertex ERASE -> UNDO restored attachment.");
            editor.WriteMessage($"\nVertexId: {state.VertexId}");
            editor.WriteMessage($"\nAttachment: {attachments[0].Path}");

            new GraphService(context.Vertices, context.Edges)
                .DeleteVertex(state.VertexId);

            _state = null;

            editor.WriteMessage("\nAttachment UNDO test data cleared.");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\n[FAIL] Attachment UNDO verify:\n{exception}");
        }
    }
}
