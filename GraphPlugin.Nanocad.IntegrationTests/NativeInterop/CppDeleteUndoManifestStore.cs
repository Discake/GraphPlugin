using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Persistence;

internal sealed class CppDeleteUndoTestManifestStore
{
    public const string RecordKey =
        "GRAPH_CPP_DELETE_UNDO_TEST";

    private const int CurrentVersion =
        1;

    public CppDeleteUndoTestManifest? Read(
        Database database,
        Transaction transaction)
    {
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

        var record =
            transaction.GetObject(
                nod.GetAt(
                    RecordKey),
                OpenMode.ForRead)
            as Xrecord
            ?? throw new InvalidOperationException(
                "C++ delete/undo manifest is not an XRecord.");

        var values =
            record.Data?.AsArray()
            ?? throw new InvalidOperationException(
                "C++ delete/undo manifest contains no data.");

        if (values.Length != 7)
        {
            throw new InvalidOperationException(
                $"Invalid C++ delete/undo manifest. " +
                $"Expected 7 values, actual {values.Length}.");
        }

        var version =
            Convert.ToInt32(
                values[0].Value);

        if (version !=
            CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported C++ delete/undo " +
                $"manifest version: {version}.");
        }

        return new CppDeleteUndoTestManifest(
            ParseGuid(values[1], "VertexAId"),
            ParseGuid(values[2], "VertexBId"),
            ParseGuid(values[3], "VertexCId"),
            ParseGuid(values[4], "EdgeABId"),
            ParseGuid(values[5], "EdgeACId"),
            ParseGuid(values[6], "EdgeBCId"));
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

        var recordId =
            nod.GetAt(
                RecordKey);

        nod.Remove(
            RecordKey);

        var record =
            transaction.GetObject(
                recordId,
                OpenMode.ForWrite);

        record.Erase();
    }

    private static Guid ParseGuid(
        TypedValue value,
        string fieldName)
    {
        if (value.Value is not string text ||
            !Guid.TryParse(
                text,
                out var result))
        {
            throw new InvalidOperationException(
                $"Invalid {fieldName} in " +
                $"C++ delete/undo manifest.");
        }

        return result;
    }
}