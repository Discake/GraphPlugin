using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.Nanocad.Persistence.Metadata;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class NanoCadEdgeRepository : IEdgeRepository
{
    private readonly Document _document;
    private readonly IVertexRepository _vertices;
    private readonly EdgeEntityFactory _factory;
    private readonly XRecordMetadataStore _metadata;
    private readonly GraphEntityIndex _index;
    private readonly EdgeStyleApplier _styleApplier;
    private readonly IGraphSettingsRepository _settings;
    private readonly EdgeEntityMapper _entityMapper;

    public NanoCadEdgeRepository(
        Document document,
        IVertexRepository vertices,
        EdgeEntityFactory factory,
        XRecordMetadataStore metadata,
        GraphEntityIndex index,
        IGraphSettingsRepository settings,
        EdgeStyleApplier applier,
        EdgeEntityMapper mapper)
    {
        _document =
            document ??
            throw new ArgumentNullException(
                nameof(document));

        _vertices = vertices;
        _factory = factory;
        _metadata = metadata;
        _index = index;
        _settings = settings;
        _styleApplier = applier;
        _entityMapper = mapper;
    }

    public void Add(GraphEdge edge)
    {
        var vertexA =
            _vertices.Get(edge.VertexAId)
            ?? throw new InvalidOperationException();

        var vertexB =
            _vertices.Get(edge.VertexBId)
            ?? throw new InvalidOperationException();

        var style =
            _settings
                .Load()
                .EdgeStyle;

        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var blockTable =
            (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);

        var modelSpace =
            (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite);

        var entity =
            _factory.Create(
                edge,
                vertexA,
                vertexB);

        _styleApplier.ApplyStyle(
            entity,
            style,
            database,
            transaction);

        modelSpace.AppendEntity(entity);

        transaction.AddNewlyCreatedDBObject(
            entity,
            true);

        _metadata.WriteEdge(
            entity,
            edge,
            transaction);

        var objectId = entity.ObjectId;

        transaction.Commit();

        _index.AddEdge(
            edge.Id,
            objectId,
            edge.VertexAId,
            edge.VertexBId);
    }

    public GraphEdge? Get(Guid id)
    {
        if (!_index.TryGetEdgeObjectId(
                id,
                out var objectId))
        {
            return null;
        }

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Entity;

        if (entity is null ||
            entity.IsErased)
        {
            return null;
        }

        if (entity is not Polyline polyline)
        {
            throw new InvalidOperationException(
                $"Graph edge '{id}' is not a Polyline.");
        }

        var metadata =
            _metadata.ReadEdge(
                entity,
                transaction);

        if (metadata is null)
        {
            return null;
        }

        if (metadata.Id != id)
        {
            throw new InvalidOperationException(
                $"Edge metadata id mismatch. " +
                $"Requested: {id}, " +
                $"metadata: {metadata.Id}.");
        }

        var route =
            _entityMapper.ReadRoute(
                polyline);

        var edge =
            GraphEdge.Restore(
                metadata.Id,
                metadata.VertexAId,
                metadata.VertexBId,
                route);

        return edge;
    }

    public IReadOnlyCollection<GraphEdge> GetAll()
    {
        var edgeIds =
            _index
                .GetEdgeIds()
                .ToArray();

        var result =
            new List<GraphEdge>(
                edgeIds.Length);

        foreach (var edgeId in edgeIds)
        {
            var edge =
                Get(edgeId);

            if (edge is null)
            {
                throw new InvalidOperationException(
                    $"Edge '{edgeId}' exists in graph index, " +
                    "but repository could not restore it.");
            }

            result.Add(edge);
        }

        return result;
    }

    public IReadOnlyCollection<GraphEdge> GetByVertex(
        Guid vertexId)
    {
        var edgeIds =
            _index
                .GetIncidentEdgeIds(vertexId)
                .ToArray();

        var result =
            new List<GraphEdge>(
                edgeIds.Length);

        foreach (var edgeId in edgeIds)
        {
            var edge =
                Get(edgeId);

            if (edge is null)
            {
                throw new InvalidOperationException(
                    $"Edge '{edgeId}' is registered as incident " +
                    $"to vertex '{vertexId}', but repository " +
                    "could not restore it.");
            }

            result.Add(edge);
        }

        return result;
    }

    public void Update(GraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(
            edge);

        if (!_index.TryGetEdgeObjectId(
                edge.Id,
                out var objectId))
        {
            throw new InvalidOperationException(
                $"Edge '{edge.Id}' is not indexed.");
        }

        var vertexA =
            _vertices.Get(
                edge.VertexAId)
            ?? throw new InvalidOperationException(
                $"Vertex '{edge.VertexAId}' not found.");

        var vertexB =
            _vertices.Get(
                edge.VertexBId)
            ?? throw new InvalidOperationException(
                $"Vertex '{edge.VertexBId}' not found.");

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var polyline =
            transaction.GetObject(
                objectId,
                OpenMode.ForWrite)
            as Polyline
            ?? throw new InvalidOperationException(
                $"Edge '{edge.Id}' is not a Polyline.");

        _entityMapper.WriteGeometry(
            polyline,
            edge,
            vertexA,
            vertexB);

        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        if (!_index.TryGetEdgeObjectId(
                id,
                out var objectId))
        {
            return;
        }

        if (!objectId.IsErased)
        {
            using var transaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();

            var entity =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForWrite)
                as Entity;

            entity?.Erase();

            transaction.Commit();
        }

        _index.RemoveEdge(id);
    }
}
