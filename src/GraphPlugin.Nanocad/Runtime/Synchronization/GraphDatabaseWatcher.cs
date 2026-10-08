using GraphPlugin.Application.Graph;
using GraphPlugin.Nanocad.Persistence.Metadata;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphDatabaseWatcher
{
    private enum PendingEraseKind
    {
        Vertex,
        Edge,
    }

    private sealed record PendingErase(ObjectId ObjectId, Guid GraphObjectId, PendingEraseKind Kind);

    private readonly Document _document;
    private readonly GraphEntityIndex _index;
    private readonly EdgeGeometrySynchronizer _synchronizer;

    private readonly HashSet<Guid> _dirtyVertices = new();

    private readonly HashSet<Guid> _dirtyEdges = new();

    private readonly HashSet<ObjectId> _restoredObjects = new();

    private readonly HashSet<ObjectId> _appendedObjects = new();

    private readonly List<PendingErase> _pendingErases = new();

    private readonly HashSet<ObjectId> _pendingErasedObjectIds = new();

    private readonly EdgeService _edgeService;
    private readonly XRecordMetadataStore _metadata;

    private bool _started;
    private bool _internalDeletion;
    private int _suspendCount;

    public bool IsSuspended => _suspendCount > 0;

    public GraphDatabaseWatcher(
        Document document,
        GraphEntityIndex index,
        EdgeGeometrySynchronizer synchronizer,
        EdgeService edgeService,
        XRecordMetadataStore metadata
    )
    {
        _document = document;
        _index = index;
        _synchronizer = synchronizer;
        _edgeService = edgeService;
        _metadata = metadata;
    }

    public void Start()
    {
        if (_started)
            return;

        _document.Database.ObjectModified += OnObjectModified;

        _document.CommandEnded += OnCommandEnded;

        _document.CommandCancelled += OnCommandCancelled;

        _document.CommandFailed += OnCommandFailed;

        _document.Database.ObjectErased += OnObjectErased;

        _document.Database.ObjectAppended += OnObjectAppended;

        _started = true;
    }

    public void Stop()
    {
        if (!_started)
            return;

        _document.Database.ObjectModified -= OnObjectModified;

        _document.CommandEnded -= OnCommandEnded;

        _document.CommandCancelled -= OnCommandCancelled;

        _document.CommandFailed -= OnCommandFailed;

        _document.Database.ObjectErased -= OnObjectErased;

        _document.Database.ObjectAppended -= OnObjectAppended;

        ClearPending();

        _started = false;
    }

    private void OnObjectModified(object sender, ObjectEventArgs e)
    {
        var objectId = e.DBObject.ObjectId;

        if (_index.TryGetVertexId(objectId, out var vertexId))
        {
            _dirtyVertices.Add(vertexId);

            return;
        }

        if (_index.TryGetEdgeId(objectId, out var edgeId))
        {
            _dirtyEdges.Add(edgeId);
        }
    }

    private void OnCommandEnded(object sender, CommandEventArgs e)
    {
        try
        {
            FlushRestoredObjects();
            FlushAppendedObjects();
            FlushErasedObjects();
            FlushDirtyVertices();
            FlushDirtyEdges();
        }
        finally
        {
            ClearPending();
        }
    }

    private void OnCommandCancelled(object sender, CommandEventArgs e)
    {
        ClearPending();
    }

    private void OnCommandFailed(object sender, CommandEventArgs e)
    {
        ClearPending();
    }

    private void OnObjectErased(object sender, ObjectErasedEventArgs e)
    {
        var objectId = e.DBObject.ObjectId;

        if (objectId.IsNull)
            return;

        if (e.Erased)
        {
            _pendingErasedObjectIds.Add(objectId);

            if (_index.TryGetVertexId(objectId, out var vertexId))
            {
                _pendingErases.Add(new PendingErase(objectId, vertexId, PendingEraseKind.Vertex));

                return;
            }

            if (_index.TryGetEdgeId(objectId, out var edgeId))
            {
                _pendingErases.Add(new PendingErase(objectId, edgeId, PendingEraseKind.Edge));
            }

            return;
        }

        _pendingErasedObjectIds.Remove(objectId);

        _pendingErases.RemoveAll(x => x.ObjectId == objectId);

        _restoredObjects.Add(objectId);
    }

    private void OnObjectAppended(object sender, ObjectEventArgs e)
    {
        if (e.DBObject is not Entity entity)
            return;

        if (entity.ObjectId.IsNull)
            return;

        _appendedObjects.Add(entity.ObjectId);
    }

    private bool IsPendingErase(ObjectId objectId)
    {
        if (objectId.IsNull)
            return false;

        return _pendingErasedObjectIds.Contains(objectId) || objectId.IsErased;
    }

    private void ClearPending()
    {
        _appendedObjects.Clear();
        _pendingErasedObjectIds.Clear();
        _pendingErases.Clear();
        _restoredObjects.Clear();
        _dirtyVertices.Clear();
        _dirtyEdges.Clear();
    }

    private void FlushDirtyEdges()
    {
        if (_dirtyEdges.Count == 0)
            return;

        var edgeIds = _dirtyEdges.ToArray();

        _dirtyEdges.Clear();

        foreach (var edgeId in edgeIds)
        {
            try
            {
                _synchronizer.UpdateEdge(edgeId);
            }
            catch
            {
                // Edge may have been deleted by the same command.
            }
        }
    }

    private void FlushErasedObjects()
    {
        if (_pendingErases.Count == 0)
            return;

        var pending = _pendingErases.ToArray();

        _pendingErases.Clear();

        foreach (var erased in pending)
        {
            if (erased.Kind != PendingEraseKind.Edge)
                continue;

            FlushErasedEdge(erased);
        }

        foreach (var erased in pending)
        {
            if (erased.Kind != PendingEraseKind.Vertex)
                continue;

            FlushErasedVertex(erased);
        }
    }

    private void FlushErasedVertex(PendingErase erased)
    {
        if (!_index.TryGetVertexObjectId(erased.GraphObjectId, out var currentObjectId))
        {
            return;
        }

        if (currentObjectId != erased.ObjectId)
            return;

        DeleteVertexAfterExternalErase(erased.GraphObjectId, erased.ObjectId);
    }

    private void DeleteVertexAfterExternalErase(Guid vertexId, ObjectId erasedObjectId)
    {
        var incidentEdges = _index.GetIncidentEdgeIds(vertexId).ToArray();

        foreach (var edgeId in incidentEdges)
        {
            _edgeService.DeleteEdge(edgeId);
        }

        _index.RemoveVertex(vertexId);
    }

    private void FlushErasedEdge(PendingErase erased)
    {
        if (!_index.TryGetEdgeObjectId(erased.GraphObjectId, out var currentObjectId))
        {
            return;
        }

        if (currentObjectId != erased.ObjectId)
            return;

        _index.RemoveEdge(erased.GraphObjectId);
    }

    private void FlushDirtyVertices()
    {
        if (_dirtyVertices.Count == 0)
            return;

        var vertices = _dirtyVertices.ToArray();

        _dirtyVertices.Clear();

        foreach (var vertexId in vertices)
        {
            _synchronizer.UpdateIncidentEdges(vertexId);
        }
    }

    private void FlushRestoredObjects()
    {
        if (_restoredObjects.Count == 0)
            return;

        var objectIds = _restoredObjects.ToArray();

        _restoredObjects.Clear();

        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        var entities = new List<(ObjectId ObjectId, Entity Entity)>();

        foreach (var objectId in objectIds)
        {
            if (objectId.IsNull || objectId.IsErased)
            {
                continue;
            }

            var entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;

            if (entity is null)
                continue;

            entities.Add((objectId, entity));
        }

        foreach (var item in entities)
        {
            RestoreVertexIfNeeded(item.Entity, item.ObjectId, transaction);
        }

        foreach (var item in entities)
        {
            RestoreEdgeIfNeeded(item.Entity, item.ObjectId, transaction);
        }

        transaction.Commit();
    }

    private bool RestoreVertexIfNeeded(Entity entity, ObjectId objectId, Transaction transaction)
    {
        var metadata = _metadata.ReadVertex(entity, transaction);

        if (metadata is null)
            return false;

        if (_index.TryGetVertexObjectId(metadata.Id, out var existingObjectId))
        {
            if (existingObjectId == objectId)
                return true;
        }

        _index.AddVertex(metadata.Id, objectId);

        return true;
    }

    private bool RestoreEdgeIfNeeded(Entity entity, ObjectId objectId, Transaction transaction)
    {
        var metadata = _metadata.ReadEdge(entity, transaction);

        if (metadata is null)
            return false;

        if (_index.TryGetEdgeObjectId(metadata.Id, out var existingObjectId))
        {
            if (existingObjectId == objectId)
                return true;
        }

        _index.AddEdge(metadata.Id, objectId, metadata.VertexAId, metadata.VertexBId);

        return true;
    }

    private void FlushAppendedObjects()
    {
        if (_appendedObjects.Count == 0)
            return;

        var objectIds = _appendedObjects.ToArray();

        _appendedObjects.Clear();

        using var transaction = _document.Database.TransactionManager.StartTransaction();

        foreach (var objectId in objectIds)
        {
            if (objectId.IsNull || objectId.IsErased)
            {
                continue;
            }

            if (_index.TryGetVertexId(objectId, out _))
            {
                continue;
            }

            if (_index.TryGetEdgeId(objectId, out _))
            {
                continue;
            }

            var entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;

            if (entity is null || entity.IsErased)
            {
                continue;
            }

            var vertexMetadata = _metadata.ReadVertex(entity, transaction);

            if (vertexMetadata is not null)
            {
                if (_index.TryGetVertexObjectId(vertexMetadata.Id, out var existingObjectId))
                {
                    if (existingObjectId == objectId)
                        continue;

                    if (_pendingErasedObjectIds.Contains(existingObjectId) || existingObjectId.IsErased)
                    {
                        _index.ReplaceVertexObject(vertexMetadata.Id, objectId);

                        continue;
                    }

                    throw new InvalidOperationException($"Duplicate graph vertex id " + $"{vertexMetadata.Id}.");
                }

                _index.AddVertex(vertexMetadata.Id, objectId);

                continue;
            }

            var edge = _metadata.ReadEdge(entity, transaction);

            if (edge is not null)
            {
                _index.AddEdge(edge.Id, objectId, edge.VertexAId, edge.VertexBId);
            }
        }

        transaction.Commit();
    }

    public void Suspend()
    {
        _suspendCount++;
    }

    public void Resume()
    {
        if (_suspendCount == 0)
            return;

        _suspendCount--;
    }
}
