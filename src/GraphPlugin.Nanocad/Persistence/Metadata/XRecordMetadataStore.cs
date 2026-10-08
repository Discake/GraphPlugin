using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence.Metadata;

public sealed class XRecordMetadataStore
{
    public void WriteVertex(Entity entity, GraphVertex vertex, Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(vertex);
        ArgumentNullException.ThrowIfNull(transaction);

        EnsureExtensionDictionary(entity);

        var dictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForWrite);

        var data = new ResultBuffer(
            new TypedValue((int)DxfCode.Int32, GraphMetadataKeys.CurrentVertexVersion),
            new TypedValue((int)DxfCode.Text, vertex.Id.ToString("D")),
            new TypedValue((int)DxfCode.Int32, (int)vertex.Style.Shape),
            new TypedValue((int)DxfCode.Int32, (int)vertex.Style.Color),
            new TypedValue((int)DxfCode.Real, vertex.Style.Size)
        );

        var xrecord = new Xrecord { Data = data };

        dictionary.SetAt(GraphMetadataKeys.VertexRecord, xrecord);

        transaction.AddNewlyCreatedDBObject(xrecord, true);
    }

    public VertexMetadata? ReadVertex(Entity entity, Transaction transaction)
    {
        if (entity.ExtensionDictionary.IsNull)
            return null;

        var dictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead);

        if (!dictionary.Contains(GraphMetadataKeys.VertexRecord))
        {
            return null;
        }

        var recordId = dictionary.GetAt(GraphMetadataKeys.VertexRecord);

        var xrecord = (Xrecord)transaction.GetObject(recordId, OpenMode.ForRead);

        if (xrecord.Data is null)
            return null;

        var values = xrecord.Data.AsArray();

        if (values.Length < 5)
            return null;

        int version = Convert.ToInt32(values[0].Value);

        EnsureSupportedVersion(GraphMetadataKeys.VertexRecord, version, GraphMetadataKeys.CurrentVertexVersion);

        if (!Guid.TryParse(values[1].Value?.ToString(), out Guid id))
        {
            return null;
        }

        var shape = (VertexShape)Convert.ToInt32(values[2].Value);

        var color = (GraphColor)Convert.ToInt32(values[3].Value);

        double size = Convert.ToDouble(values[4].Value);

        return new VertexMetadata(version, id, shape, color, size);
    }

    public void WriteEdge(Entity entity, GraphEdge edge, Transaction transaction)
    {
        EnsureExtensionDictionary(entity);

        var dictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForWrite);

        var data = new ResultBuffer(
            new TypedValue((int)DxfCode.Int32, GraphMetadataKeys.CurrentEdgeVersion),
            new TypedValue((int)DxfCode.Text, edge.Id.ToString("D")),
            new TypedValue((int)DxfCode.Text, edge.VertexAId.ToString("D")),
            new TypedValue((int)DxfCode.Text, edge.VertexBId.ToString("D"))
        );

        var xrecord = new Xrecord { Data = data };

        dictionary.SetAt(GraphMetadataKeys.EdgeRecord, xrecord);

        transaction.AddNewlyCreatedDBObject(xrecord, true);
    }

    public EdgeMetadata? ReadEdge(Entity entity, Transaction transaction)
    {
        if (entity.ExtensionDictionary.IsNull)
            return null;

        var dictionary = (DBDictionary)transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead);

        if (!dictionary.Contains(GraphMetadataKeys.EdgeRecord))
        {
            return null;
        }

        var recordId = dictionary.GetAt(GraphMetadataKeys.EdgeRecord);

        var xrecord = (Xrecord)transaction.GetObject(recordId, OpenMode.ForRead);

        if (xrecord.Data is null)
            return null;

        var values = xrecord.Data.AsArray();

        if (values.Length < 4)
            return null;

        int version = Convert.ToInt32(values[0].Value);

        EnsureSupportedVersion(GraphMetadataKeys.EdgeRecord, version, GraphMetadataKeys.CurrentEdgeVersion);

        if (!Guid.TryParse(values[1].Value?.ToString(), out var id))
        {
            return null;
        }

        if (!Guid.TryParse(values[2].Value?.ToString(), out var vertexAId))
        {
            return null;
        }

        if (!Guid.TryParse(values[3].Value?.ToString(), out var vertexBId))
        {
            return null;
        }

        return new EdgeMetadata(version, id, vertexAId, vertexBId);
    }

    private static void EnsureSupportedVersion(string recordName, int actualVersion, int supportedVersion)
    {
        if (actualVersion == supportedVersion)
            return;

        throw new InvalidOperationException(
            $"Unsupported {recordName} version: " + $"{actualVersion}. Supported version: " + $"{supportedVersion}."
        );
    }

    private static void EnsureExtensionDictionary(Entity entity)
    {
        if (entity.ExtensionDictionary.IsNull)
        {
            entity.CreateExtensionDictionary();
        }
    }
}
