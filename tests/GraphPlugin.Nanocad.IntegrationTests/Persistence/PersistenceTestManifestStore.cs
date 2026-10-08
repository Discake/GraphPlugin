using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class PersistenceTestManifestStore
{
    private const string RecordKey = "GRAPH_PLUGIN_PERSISTENCE_TEST";

    private const int CurrentVersion = 1;

    public bool Exists(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        return dictionary.Contains(RecordKey);
    }

    public void Save(Database database, PersistenceTestManifest manifest)
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

        record.Data = new ResultBuffer(
            new TypedValue((int)DxfCode.Int32, CurrentVersion),
            new TypedValue((int)DxfCode.Text, manifest.TestId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.VertexAId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.VertexBId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.VertexCId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.EdgeABId.ToString()),
            new TypedValue((int)DxfCode.Text, manifest.EdgeBCId.ToString()),
            new TypedValue((int)DxfCode.Real, manifest.PositionA.X),
            new TypedValue((int)DxfCode.Real, manifest.PositionA.Y),
            new TypedValue((int)DxfCode.Real, manifest.PositionB.X),
            new TypedValue((int)DxfCode.Real, manifest.PositionB.Y),
            new TypedValue((int)DxfCode.Real, manifest.PositionC.X),
            new TypedValue((int)DxfCode.Real, manifest.PositionC.Y),
            new TypedValue((int)DxfCode.Int32, (int)manifest.TestEdgeColor),
            new TypedValue((int)DxfCode.Int32, (int)manifest.TestEdgeLineType),
            new TypedValue((int)DxfCode.Real, manifest.TestEdgeLineWeightMm),
            new TypedValue((int)DxfCode.Int32, (int)manifest.OriginalEdgeColor),
            new TypedValue((int)DxfCode.Int32, (int)manifest.OriginalEdgeLineType),
            new TypedValue((int)DxfCode.Real, manifest.OriginalEdgeLineWeightMm)
        );

        transaction.Commit();
    }

    public PersistenceTestManifest? Load(Database database)
    {
        using var transaction = database.TransactionManager.StartTransaction();

        var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

        if (!dictionary.Contains(RecordKey))
            return null;

        var record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordKey), OpenMode.ForRead);

        var values = record.Data?.AsArray();

        if (values is null || values.Length < 19)
        {
            throw new InvalidOperationException("Persistence test manifest is invalid.");
        }

        var version = Convert.ToInt32(values[0].Value);

        if (version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported persistence test version: {version}.");
        }

        return new PersistenceTestManifest(
            Version: version,
            TestId: Guid.Parse((string)values[1].Value),
            VertexAId: Guid.Parse((string)values[2].Value),
            VertexBId: Guid.Parse((string)values[3].Value),
            VertexCId: Guid.Parse((string)values[4].Value),
            EdgeABId: Guid.Parse((string)values[5].Value),
            EdgeBCId: Guid.Parse((string)values[6].Value),
            PositionA: new Point2(Convert.ToDouble(values[7].Value), Convert.ToDouble(values[8].Value)),
            PositionB: new Point2(Convert.ToDouble(values[9].Value), Convert.ToDouble(values[10].Value)),
            PositionC: new Point2(Convert.ToDouble(values[11].Value), Convert.ToDouble(values[12].Value)),
            TestEdgeColor: (GraphColor)Convert.ToInt32(values[13].Value),
            TestEdgeLineType: (EdgeLineType)Convert.ToInt32(values[14].Value),
            TestEdgeLineWeightMm: Convert.ToDouble(values[15].Value),
            OriginalEdgeColor: (GraphColor)Convert.ToInt32(values[16].Value),
            OriginalEdgeLineType: (EdgeLineType)Convert.ToInt32(values[17].Value),
            OriginalEdgeLineWeightMm: Convert.ToDouble(values[18].Value)
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
