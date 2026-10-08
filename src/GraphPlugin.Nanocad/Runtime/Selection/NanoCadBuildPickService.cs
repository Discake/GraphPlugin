using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Runtime;

public enum AmbiguousPickBehavior
{
    NativeEntitySelection,
    FirstOsnapEntity,
}

public sealed class NanoCadBuildPickService
{
    private readonly Editor _editor;
    private readonly GraphEntityIndex _index;
    private readonly IVertexRepository _vertices;
    private readonly IEdgeRepository _edges;
    private readonly AmbiguousPickBehavior _ambiguousBehavior;

    public NanoCadBuildPickService(
        Editor editor,
        GraphEntityIndex index,
        IVertexRepository vertices,
        IEdgeRepository edges,
        AmbiguousPickBehavior ambiguousBehavior = AmbiguousPickBehavior.NativeEntitySelection
    )
    {
        _editor = editor;
        _index = index;
        _vertices = vertices;
        _edges = edges;
        _ambiguousBehavior = ambiguousBehavior;
    }

    public BuildPickResult GetNext()
    {
        ObjectId[] keyObjectIds = Array.Empty<ObjectId>();

        Point3d rawPoint = Point3d.Origin;

        bool hasRawPoint = false;

        void OnPointMonitor(object? sender, PointMonitorEventArgs e)
        {
            try
            {
                rawPoint = e.Context.RawPoint;

                hasRawPoint = true;

                if (!e.Context.PointComputed)
                {
                    keyObjectIds = Array.Empty<ObjectId>();

                    return;
                }

                var objectSnapped = (e.Context.History & PointHistoryBits.ObjectSnapped) != 0;

                if (!objectSnapped)
                {
                    keyObjectIds = Array.Empty<ObjectId>();

                    return;
                }

                var paths = e.Context.GetKeyPointEntities();

                keyObjectIds = GetLeafObjectIds(paths);
            }
            catch
            {
                keyObjectIds = Array.Empty<ObjectId>();
            }
        }

        _editor.PointMonitor += OnPointMonitor;

        PromptPointResult result;

        try
        {
            var options = new PromptPointOptions("\nУкажите следующую вершину " + "или [Select/Finish]: ");

            options.Keywords.Add("Select");

            options.Keywords.Add("Finish");

            options.AllowNone = true;

            result = _editor.GetPoint(options);
        }
        finally
        {
            _editor.PointMonitor -= OnPointMonitor;
        }

        var computedPoint = result.Value;

        var actualPickPoint = hasRawPoint ? rawPoint : computedPoint;

        if (result.Status == PromptStatus.Cancel || result.Status == PromptStatus.None)
        {
            return BuildPickResult.Finish();
        }

        if (result.Status == PromptStatus.Keyword)
        {
            return HandleKeyword(result.StringResult);
        }

        if (result.Status != PromptStatus.OK)
        {
            return BuildPickResult.Finish();
        }

        return ResolvePointPick(computedPoint, actualPickPoint, keyObjectIds);
    }

    private static ObjectId[] GetLeafObjectIds(FullSubentityPath[]? paths)
    {
        if (paths is null || paths.Length == 0)
        {
            return Array.Empty<ObjectId>();
        }

        var result = new HashSet<ObjectId>();

        foreach (var path in paths)
        {
            var ids = path.GetObjectIds();

            if (ids is null || ids.Length == 0)
            {
                continue;
            }

            result.Add(ids[^1]);
        }

        return result.ToArray();
    }

    private BuildPickResult ResolvePointPick(Point3d point, Point3d pickPoint, IReadOnlyCollection<ObjectId> objectIds)
    {
        var validIds = objectIds.Where(IsUsableObjectId).Distinct().ToArray();

        if (validIds.Length == 0)
        {
            return BuildPickResult.Empty(point, pickPoint);
        }

        if (validIds.Length == 1)
        {
            return Classify(validIds[0], point, pickPoint);
        }

        if (_ambiguousBehavior == AmbiguousPickBehavior.FirstOsnapEntity)
        {
            return Classify(validIds[0], point, pickPoint);
        }

        return GetEntityExplicitly("\nВ точке несколько объектов. " + "Выберите нужный: ");
    }

    private static bool IsUsableObjectId(ObjectId objectId)
    {
        return !objectId.IsNull && !objectId.IsErased;
    }

    private BuildPickResult Classify(ObjectId objectId, Point3d point, Point3d pickPoint)
    {
        if (_index.TryGetVertexId(objectId, out var vertexId))
        {
            var vertex = _vertices.Get(vertexId);

            if (vertex is not null)
            {
                return BuildPickResult.FromVertex(point, pickPoint, objectId, vertex);
            }
        }

        if (_index.TryGetEdgeId(objectId, out var edgeId))
        {
            var edge = _edges.Get(edgeId);

            if (edge is not null)
            {
                return BuildPickResult.FromEdge(point, pickPoint, objectId, edge);
            }
        }

        return BuildPickResult.Empty(point, pickPoint, objectId);
    }

    private BuildPickResult HandleKeyword(string? keyword)
    {
        return keyword switch
        {
            "Select" => GetEntityExplicitly("\nВыберите объект: "),

            _ => BuildPickResult.Finish(),
        };
    }

    private BuildPickResult GetEntityExplicitly(string message)
    {
        var options = new PromptEntityOptions(message);

        var result = _editor.GetEntity(options);

        if (result.Status == PromptStatus.Cancel || result.Status == PromptStatus.None)
        {
            return BuildPickResult.Finish();
        }

        if (result.Status != PromptStatus.OK)
        {
            return BuildPickResult.Finish();
        }

        var pickedPointWcs = result.PickedPoint.TransformBy(_editor.CurrentUserCoordinateSystem);

        return Classify(
            result.ObjectId,
            // В GetEntity это не точка создания новой
            // Vertex как таковая, но если выбран OTHER,
            // использовать её вполне корректно.
            pickedPointWcs,
            pickedPointWcs
        );
    }
}
