using GraphPlugin.Nanocad.Bootstrap;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class DiagnosticProbeCommands
{
    [CommandMethod("GRAPH_OSNAP_PROBE")]
    public void GraphOsnapProbe()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        ObjectId[] keyObjectIds = Array.Empty<ObjectId>();

        Point3d snappedPoint = Point3d.Origin;

        var wasSnapped = false;
        string? monitorError = null;

        void OnPointMonitor(object? sender, PointMonitorEventArgs e)
        {
            try
            {
                if (!e.Context.PointComputed)
                    return;

                var snapped = (e.Context.History & PointHistoryBits.ObjectSnapped) != 0;

                if (!snapped)
                {
                    wasSnapped = false;
                    keyObjectIds = Array.Empty<ObjectId>();

                    return;
                }

                wasSnapped = true;
                snappedPoint = e.Context.ObjectSnappedPoint;

                var paths = e.Context.GetKeyPointEntities();

                if (paths is null)
                {
                    keyObjectIds = Array.Empty<ObjectId>();

                    return;
                }

                keyObjectIds = paths.SelectMany(path => path.GetObjectIds()).Distinct().ToArray();
            }
            catch (System.Exception exception)
            {
                monitorError = exception.Message;
            }
        }

        editor.PointMonitor += OnPointMonitor;

        try
        {
            var result = editor.GetPoint("\nУкажите точку с объектной привязкой: ");

            if (result.Status != PromptStatus.OK)
            {
                editor.WriteMessage($"\nStatus: {result.Status}");

                return;
            }
        }
        finally
        {
            editor.PointMonitor -= OnPointMonitor;
        }

        editor.WriteMessage("\n=== GRAPH OSNAP PROBE ===");

        if (monitorError is not null)
        {
            editor.WriteMessage($"\nPointMonitor error: " + monitorError);

            return;
        }

        editor.WriteMessage($"\nObject snapped: {wasSnapped}");

        if (!wasSnapped)
        {
            editor.WriteMessage("\nKey entities: 0");

            return;
        }

        editor.WriteMessage(
            $"\nSnapped point: " + $"({snappedPoint.X:F6}, " + $"{snappedPoint.Y:F6}, " + $"{snappedPoint.Z:F6})"
        );

        editor.WriteMessage($"\nKey entities: " + keyObjectIds.Length);

        foreach (var objectId in keyObjectIds)
        {
            editor.WriteMessage($"\n\nObjectId: {objectId}");

            if (context.Index.TryGetVertexId(objectId, out var vertexId))
            {
                editor.WriteMessage("\nGraph kind: VERTEX" + $"\nVertexId: {vertexId}");

                continue;
            }

            if (context.Index.TryGetEdgeId(objectId, out var edgeId))
            {
                editor.WriteMessage("\nGraph kind: EDGE" + $"\nEdgeId: {edgeId}");

                continue;
            }

            editor.WriteMessage("\nGraph kind: OTHER");
        }
    }

    [CommandMethod("GRAPH_PICK_PROBE")]
    public void GraphPickProbe()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        var options = new PromptEntityOptions("\nКликните по объекту или в пустую область: ");

        options.AllowNone = true;

        try
        {
            var result = editor.GetEntity(options);

            editor.WriteMessage("\n=== GETENTITY PROBE ===");

            editor.WriteMessage($"\nStatus: {result.Status}");

            try
            {
                var point = result.PickedPoint;

                editor.WriteMessage($"\nPickedPoint: " + $"({point.X:F6}, " + $"{point.Y:F6}, " + $"{point.Z:F6})");
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage($"\nPickedPoint unavailable: " + exception.Message);
            }

            if (result.Status == PromptStatus.OK)
            {
                editor.WriteMessage($"\nObjectId: {result.ObjectId}");

                if (context.Index.TryGetVertexId(result.ObjectId, out var vertexId))
                {
                    editor.WriteMessage("\nGraph kind: VERTEX" + $"\nVertexId: {vertexId}");

                    return;
                }

                if (context.Index.TryGetEdgeId(result.ObjectId, out var edgeId))
                {
                    editor.WriteMessage("\nGraph kind: EDGE" + $"\nEdgeId: {edgeId}");

                    return;
                }

                editor.WriteMessage("\nGraph kind: OTHER");
            }
            else
            {
                editor.WriteMessage("\nNo entity selected.");
            }
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                "\n=== GETENTITY PROBE ==="
                    + $"\nException type: "
                    + exception.GetType().FullName
                    + $"\nMessage: {exception.Message}"
            );
        }
    }

    [CommandMethod("GRAPH_BUILD_PICK_PROBE")]
    public void GraphBuildPickProbe()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var result = context.BuildPick.GetNext();

        document.Editor.WriteMessage(
            "\n=== BUILD PICK ==="
                + $"\nKind: {result.Kind}"
                + $"\nPoint: "
                + $"({result.Point.X:F4}, "
                + $"{result.Point.Y:F4}, "
                + $"{result.Point.Z:F4})"
        );

        if (result.Vertex is not null)
        {
            document.Editor.WriteMessage($"\nVertexId: " + result.Vertex.Id);
        }

        if (result.Edge is not null)
        {
            document.Editor.WriteMessage($"\nEdgeId: " + result.Edge.Id);
        }

        if (!result.ObjectId.IsNull)
        {
            document.Editor.WriteMessage($"\nObjectId: " + result.ObjectId);
        }
    }
}
