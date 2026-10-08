using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.Nanocad.Persistence.Metadata;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class NanoCadVertexRepository : IVertexRepository
{
    private readonly Document _document;
    private readonly VertexEntityFactory _factory;
    private readonly XRecordMetadataStore _metadata;
    private readonly VertexEntityMapper _mapper;
    private readonly GraphEntityIndex _index;

    public NanoCadVertexRepository(
        Document document,
        VertexEntityFactory factory,
        XRecordMetadataStore metadata,
        VertexEntityMapper mapper,
        GraphEntityIndex index
    )
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        _factory = factory;
        _metadata = metadata;
        _mapper = mapper;
        _index = index;
    }

    public void Add(GraphVertex vertex)
    {
        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);

        var modelSpace = (BlockTableRecord)
            transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        var entity = _factory.Create(vertex);

        modelSpace.AppendEntity(entity);

        transaction.AddNewlyCreatedDBObject(entity, true);

        _metadata.WriteVertex(entity, vertex, transaction);

        var objectId = entity.ObjectId;

        transaction.Commit();

        _index.AddVertex(vertex.Id, objectId);
    }

    public GraphVertex? Get(Guid id)
    {
        if (!_index.TryGetVertexObjectId(id, out var objectId))
        {
            return null;
        }

        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        var entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;

        if (entity is null || entity.IsErased)
        {
            return null;
        }

        var vertex = _mapper.ToDomain(entity, transaction);

        transaction.Commit();

        return vertex;
    }

    public IReadOnlyCollection<GraphVertex> GetAll()
    {
        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);

        var modelSpace = (BlockTableRecord)
            transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var result = new List<GraphVertex>();

        foreach (ObjectId objectId in modelSpace)
        {
            var dbObject = transaction.GetObject(objectId, OpenMode.ForRead);

            if (dbObject is not Entity entity)
                continue;

            var vertex = _mapper.ToDomain(entity, transaction);

            if (vertex is not null)
                result.Add(vertex);
        }

        transaction.Commit();

        return result;
    }

    public void Delete(Guid id)
    {
        if (!_index.TryGetVertexObjectId(id, out var objectId))
        {
            return;
        }

        if (!objectId.IsErased)
        {
            using var transaction = _document.Database.TransactionManager.StartTransaction();

            var entity = transaction.GetObject(objectId, OpenMode.ForWrite) as Entity;

            entity?.Erase();

            transaction.Commit();
        }

        _index.RemoveVertex(id);
    }
}
