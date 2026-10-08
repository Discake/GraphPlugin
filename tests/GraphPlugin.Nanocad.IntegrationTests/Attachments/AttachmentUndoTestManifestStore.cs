using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

internal sealed class AttachmentUndoTestManifestStore
{
    private const string RecordKey = "GRAPH_ATTACHMENT_UNDO_TEST";

    private const int CurrentVersion = 1;

    public bool Exists(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var nod =
            transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary
            ?? throw new InvalidOperationException("Named Objects Dictionary could not be opened.");

        return nod.Contains(RecordKey);
    }

    public void Write(Database database, AttachmentUndoTestManifest manifest)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var nod =
            transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary
            ?? throw new InvalidOperationException("Named Objects Dictionary could not be opened.");

        if (nod.Contains(RecordKey))
        {
            throw new InvalidOperationException("An attachment undo test is already prepared.");
        }

        using var buffer = new ResultBuffer(
            new TypedValue((int)DxfCode.Int32, CurrentVersion),
            new TypedValue((int)DxfCode.Text, manifest.VertexId.ToString("D")),
            new TypedValue((int)DxfCode.Text, manifest.AttachmentPath)
        );

        var record = new Xrecord { Data = buffer };

        nod.SetAt(RecordKey, record);

        transaction.AddNewlyCreatedDBObject(record, true);

        transaction.Commit();
    }

    public AttachmentUndoTestManifest? Read(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var nod =
            transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary
            ?? throw new InvalidOperationException("Named Objects Dictionary could not be opened.");

        if (!nod.Contains(RecordKey))
            return null;

        var record =
            transaction.GetObject(nod.GetAt(RecordKey), OpenMode.ForRead) as Xrecord
            ?? throw new InvalidOperationException("Attachment undo manifest is not an XRecord.");

        using var data = record.Data;

        var values =
            data?.AsArray() ?? throw new InvalidOperationException("Attachment undo manifest contains no data.");

        if (values.Length != 3)
        {
            throw new InvalidOperationException(
                $"Attachment undo manifest is invalid. " + $"Expected 3 values, actual {values.Length}."
            );
        }

        var version = Convert.ToInt32(values[0].Value);

        if (version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported attachment undo test version: {version}.");
        }

        if (values[1].Value is not string vertexText || !Guid.TryParse(vertexText, out var vertexId))
        {
            throw new InvalidOperationException("Attachment undo manifest contains an invalid VertexId.");
        }

        if (values[2].Value is not string attachmentPath || string.IsNullOrWhiteSpace(attachmentPath))
        {
            throw new InvalidOperationException("Attachment undo manifest contains an invalid path.");
        }

        return new AttachmentUndoTestManifest(vertexId, attachmentPath);
    }

    public void Delete(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var nod =
            transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary
            ?? throw new InvalidOperationException("Named Objects Dictionary could not be opened.");

        if (!nod.Contains(RecordKey))
            return;

        var recordId = nod.GetAt(RecordKey);

        nod.Remove(RecordKey);

        var record = transaction.GetObject(recordId, OpenMode.ForWrite);

        record.Erase();
        transaction.Commit();
    }
}
