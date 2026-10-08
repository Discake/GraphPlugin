using GraphPlugin.Nanocad.Persistence.Metadata;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphEntityIndexBuilder
{
    private readonly XRecordMetadataStore _metadata;

    public GraphEntityIndexBuilder(XRecordMetadataStore metadata)
    {
        _metadata = metadata;
    }

    public GraphEntityIndex Build(Database database)
    {
        var index = new GraphEntityIndex();

        using var transaction = database.TransactionManager.StartTransaction();

        var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);

        var modelSpace = (BlockTableRecord)
            transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        foreach (ObjectId objectId in modelSpace)
        {
            if (transaction.GetObject(objectId, OpenMode.ForRead) is not Entity entity)
            {
                continue;
            }

            var vertex = _metadata.ReadVertex(entity, transaction);

            if (vertex is not null)
            {
                index.AddVertex(vertex.Id, objectId);

                continue;
            }

            var edge = _metadata.ReadEdge(entity, transaction);

            if (edge is not null)
            {
                index.AddEdge(edge.Id, objectId, edge.VertexAId, edge.VertexBId);
            }
        }

        transaction.Commit();

        return index;
    }
}
