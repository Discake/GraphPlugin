using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Runtime;

public sealed class AttachmentPersistenceTestManifestStore
{
    private const string RecordKey =
        "GRAPH_ATTACHMENT_PERSISTENCE_TEST";

    private const int CurrentVersion =
        1;

    public void Write(
        Database database,
        Transaction transaction,
        AttachmentPersistenceTestManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(manifest);

        var nod =
            transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForWrite)
            as DBDictionary
            ?? throw new InvalidOperationException(
                "Named Objects Dictionary could not be opened.");

        var values =
            new List<TypedValue>
            {
                new(
                    (int)DxfCode.Int32,
                    CurrentVersion),

                new(
                    (int)DxfCode.Text,
                    manifest.VertexId.ToString("D")),

                new(
                    (int)DxfCode.Int32,
                    manifest.Paths.Count)
            };

        foreach (var path in manifest.Paths)
        {
            values.Add(
                new TypedValue(
                    (int)DxfCode.Text,
                    path));
        }

        using var buffer =
            new ResultBuffer(
                values.ToArray());

        Xrecord xRecord;

        if (nod.Contains(
                RecordKey))
        {
            var id =
                nod.GetAt(
                    RecordKey);

            xRecord =
                transaction.GetObject(
                    id,
                    OpenMode.ForWrite)
                as Xrecord
                ?? throw new InvalidOperationException(
                    "Attachment test manifest is not an XRecord.");
        }
        else
        {
            xRecord =
                new Xrecord();

            nod.SetAt(
                RecordKey,
                xRecord);

            transaction.AddNewlyCreatedDBObject(
                xRecord,
                true);
        }

        xRecord.Data =
            buffer;
    }

    public AttachmentPersistenceTestManifest? Read(
        Database database,
        Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(transaction);

        var nod =
            transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForRead)
            as DBDictionary
            ?? throw new InvalidOperationException(
                "Named Objects Dictionary could not be opened.");

        if (!nod.Contains(
                RecordKey))
        {
            return null;
        }

        var id =
            nod.GetAt(
                RecordKey);

        var xRecord =
            transaction.GetObject(
                id,
                OpenMode.ForRead)
            as Xrecord
            ?? throw new InvalidOperationException(
                "Attachment test manifest is not an XRecord.");

        using var data =
            xRecord.Data;

        if (data is null)
        {
            throw new InvalidOperationException(
                "Attachment test manifest is empty.");
        }

        var values =
            data.AsArray();

        if (values.Length < 3)
        {
            throw new InvalidOperationException(
                "Attachment test manifest is invalid.");
        }

        var version =
            Convert.ToInt32(
                values[0].Value);

        if (version !=
            CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported attachment persistence " +
                $"test version: {version}.");
        }

        if (values[1].Value is not string vertexText ||
            !Guid.TryParse(
                vertexText,
                out var vertexId))
        {
            throw new InvalidOperationException(
                "Attachment test manifest contains " +
                "an invalid VertexId.");
        }

        var count =
            Convert.ToInt32(
                values[2].Value);

        if (count < 0 ||
            values.Length != count + 3)
        {
            throw new InvalidOperationException(
                "Attachment test manifest contains " +
                "an invalid path count.");
        }

        var paths =
            new List<string>(
                count);

        for (var i = 0;
             i < count;
             i++)
        {
            if (values[i + 3].Value is not string path ||
                string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    $"Attachment test path {i} is invalid.");
            }

            paths.Add(
                path);
        }

        return new AttachmentPersistenceTestManifest(
            vertexId,
            paths);
    }

    public void Delete(
        Database database,
        Transaction transaction)
    {
        var nod =
            transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForWrite)
            as DBDictionary
            ?? throw new InvalidOperationException(
                "Named Objects Dictionary could not be opened.");

        if (!nod.Contains(
                RecordKey))
        {
            return;
        }

        var id =
            nod.GetAt(
                RecordKey);

        nod.Remove(
            RecordKey);

        var obj =
            transaction.GetObject(
                id,
                OpenMode.ForWrite);

        obj.Erase();
    }
}