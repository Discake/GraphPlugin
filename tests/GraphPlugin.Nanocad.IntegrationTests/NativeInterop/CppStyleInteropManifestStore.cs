using GraphPlugin.Nanocad.Runtime;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public class CppStyleInteropManifestStore
{
    private const string StyleRecordKey =
    "GRAPH_CPP_STYLE_INTEROP_TEST";

    public CppStyleInteropManifest ReadStyleManifest(
        Database database,
        Transaction transaction)
    {
        var nod =
            (DBDictionary)transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForRead);

        if (!nod.Contains(
                StyleRecordKey))
        {
            throw new IntegrationTestException(
                "C++ style test manifest was not found.");
        }

        var xrecord =
            (Xrecord)transaction.GetObject(
                nod.GetAt(StyleRecordKey),
                OpenMode.ForRead);

        var values =
            xrecord.Data?.AsArray()
            ?? throw new IntegrationTestException(
                "Style manifest contains no data.");

        if (values.Length < 4)
        {
            throw new IntegrationTestException(
                "Style manifest is invalid.");
        }

        return new CppStyleInteropManifest(
            Guid.Parse(
                (string)values[1].Value),
            (string)values[2].Value,
            (string)values[3].Value);
    }

    public void Delete(Database database)
    {
        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var dictionary =
            (DBDictionary)transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForRead);

        if (!dictionary.Contains(StyleRecordKey))
            return;

        dictionary.UpgradeOpen();
        dictionary.Remove(StyleRecordKey);

        transaction.Commit();
    }
}
