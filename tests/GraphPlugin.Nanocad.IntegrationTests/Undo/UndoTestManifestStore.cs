using GraphPlugin.Domain.Geometry;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class UndoTestManifestStore
{
    private const string RecordKey = "GRAPH_PLUGIN_UNDO_TEST";

    private const int CurrentVersion = 1;

    public bool Exists(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        return dictionary.Contains(RecordKey);
    }

    public void Save(Database database, UndoTestManifest manifest)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        Xrecord record;

        if (dictionary.Contains(RecordKey))
        {
            record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordKey), OpenMode.ForWrite);
        }
        else
        {
            dictionary.UpgradeOpen();

            record = new Xrecord();

            dictionary.SetAt(RecordKey, record);

            transaction.AddNewlyCreatedDBObject(record, true);
        }

        var vertexCId = manifest.VertexCId ?? Guid.Empty;

        var edgeBCId = manifest.EdgeBCId ?? Guid.Empty;

        var positionC = manifest.PositionC ?? new Point2(0, 0);

        record.Data = new ResultBuffer(
            new TypedValue((int)DxfCode.Int32, CurrentVersion),
            new TypedValue((int)DxfCode.Text, manifest.TestId.ToString()),
            new TypedValue((int)DxfCode.Int32, (int)manifest.Scenario),
            new TypedValue((int)DxfCode.Text, manifest.VertexAId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.VertexBId.ToString()),
            new TypedValue((int)DxfCode.Text, vertexCId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.EdgeABId.ToString()),
            new TypedValue((int)DxfCode.Text, edgeBCId.ToString()),
            new TypedValue((int)DxfCode.Real, manifest.PositionA.X),
            new TypedValue((int)DxfCode.Real, manifest.PositionA.Y),
            new TypedValue((int)DxfCode.Real, manifest.PositionB.X),
            new TypedValue((int)DxfCode.Real, manifest.PositionB.Y),
            new TypedValue((int)DxfCode.Real, positionC.X),
            new TypedValue((int)DxfCode.Real, positionC.Y)
        );

        transaction.Commit();
    }

    public UndoTestManifest? Load(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        if (!dictionary.Contains(RecordKey))
            return null;

        var record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordKey), OpenMode.ForRead);

        var values = record.Data?.AsArray();

        if (values is null || values.Length < 14)
        {
            throw new InvalidOperationException("Undo test manifest is invalid.");
        }

        var version = Convert.ToInt32(values[0].Value);

        if (version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported Undo test version: {version}.");
        }

        var scenario = (UndoTestScenario)Convert.ToInt32(values[2].Value);

        var vertexCId = Guid.Parse((string)values[5].Value);

        var edgeBCId = Guid.Parse((string)values[7].Value);

        return new UndoTestManifest(
            Version: version,
            TestId: Guid.Parse((string)values[1].Value),
            Scenario: scenario,
            VertexAId: Guid.Parse((string)values[3].Value),
            VertexBId: Guid.Parse((string)values[4].Value),
            VertexCId: scenario == UndoTestScenario.Vertex ? vertexCId : null,
            EdgeABId: Guid.Parse((string)values[6].Value),
            EdgeBCId: scenario == UndoTestScenario.Vertex ? edgeBCId : null,
            PositionA: new Point2(Convert.ToDouble(values[8].Value), Convert.ToDouble(values[9].Value)),
            PositionB: new Point2(Convert.ToDouble(values[10].Value), Convert.ToDouble(values[11].Value)),
            PositionC: scenario == UndoTestScenario.Vertex
                ? new Point2(Convert.ToDouble(values[12].Value), Convert.ToDouble(values[13].Value))
                : null
        );
    }

    public void Delete(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        if (!dictionary.Contains(RecordKey))
            return;

        dictionary.UpgradeOpen();
        dictionary.Remove(RecordKey);

        transaction.Commit();
    }
}
