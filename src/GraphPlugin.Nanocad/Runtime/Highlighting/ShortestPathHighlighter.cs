using GraphPlugin.Domain.Algorithms;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class ShortestPathHighlighter
{
    private readonly Document _document;
    private readonly GraphEntityIndex _index;

    private readonly HashSet<ObjectId> _highlighted = new();

    public ShortestPathHighlighter(Document document, GraphEntityIndex index)
    {
        _document = document;
        _index = index;
    }

    public bool HasHighlight => _highlighted.Count > 0;

    public void Show(ShortestPathResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Clear();

        if (!result.Found)
            return;

        var objectIds = CollectObjectIds(result);

        if (objectIds.Count == 0)
            return;

        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        foreach (var objectId in objectIds)
        {
            if (objectId.IsNull || objectId.IsErased)
            {
                continue;
            }

            if (transaction.GetObject(objectId, OpenMode.ForRead) is not Entity entity)
            {
                continue;
            }

            entity.Highlight();

            _highlighted.Add(objectId);
        }

        transaction.Commit();

        _document.Editor.Regen();
    }

    public void Clear()
    {
        if (_highlighted.Count == 0)
            return;

        var database = _document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        foreach (var objectId in _highlighted)
        {
            if (objectId.IsNull || objectId.IsErased)
            {
                continue;
            }

            if (transaction.GetObject(objectId, OpenMode.ForRead) is not Entity entity)
            {
                continue;
            }

            entity.Unhighlight();
        }

        transaction.Commit();

        _highlighted.Clear();

        _document.Editor.Regen();
    }

    private List<ObjectId> CollectObjectIds(ShortestPathResult result)
    {
        var objectIds = new HashSet<ObjectId>();

        foreach (var vertexId in result.VertexIds)
        {
            if (_index.TryGetVertexObjectId(vertexId, out var objectId))
            {
                objectIds.Add(objectId);
            }
        }

        foreach (var edgeId in result.EdgeIds)
        {
            if (_index.TryGetEdgeObjectId(edgeId, out var objectId))
            {
                objectIds.Add(objectId);
            }
        }

        return objectIds.ToList();
    }
}
