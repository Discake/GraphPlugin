using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Bootstrap;
using GraphPlugin.NanoCad.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using System.Diagnostics;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

using NanoApplication =
    HostMgd.ApplicationServices.Application;

public sealed class GraphCommands
{
    [CommandMethod("GRAPHCLEARPATH")]
    public void ClearShortestPath()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        if (!context.PathHighlighter.HasHighlight)
        {
            document.Editor.WriteMessage(
                "\nПодсвеченного пути нет.");

            return;
        }

        context.PathHighlighter.Clear();

        document.Editor.WriteMessage(
            "\nПодсветка кратчайшего пути снята.");
    }

    [CommandMethod("GRAPHSHORTESTPATH")]
    public void FindShortestPath()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var context =
            PluginServices.CurrentContext;

        context.PathHighlighter.Clear();

        // Убираем выделение от предыдущего запуска команды.
        editor.SetImpliedSelection(
            Array.Empty<ObjectId>());

        editor.WriteMessage(
            "\nПоиск кратчайшего пути.");

        var startVertex =
            SelectPathVertex(
                editor,
                context,
                "\nВыберите начальную вершину: ");

        if (startVertex is null)
        {
            editor.WriteMessage(
                "\nПоиск отменён.");

            return;
        }

        var endVertex =
            SelectPathVertex(
                editor,
                context,
                "\nВыберите конечную вершину: ");

        if (endVertex is null)
        {
            editor.WriteMessage(
                "\nПоиск отменён.");

            return;
        }

        var service =
            PluginServices
                .CreateShortestPathService();

        ShortestPathResult result;

        try
        {
            result =
                service.Find(
                    startVertex.Id,
                    endVertex.Id);
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\nНе удалось найти путь: " +
                exception.Message);

            return;
        }

        if (!result.Found)
        {
            editor.WriteMessage(
                "\nПуть между выбранными вершинами не найден.");

            return;
        }

        context.PathHighlighter.Show(
            result);

        editor.WriteMessage(
            $"\nКратчайший путь найден." +
            $"\nДлина: {result.TotalLength:0.###}" +
            $" ед. чертежа" +
            $"\nКоличество рёбер: {result.EdgeIds.Count}");
    }

    private static GraphVertex? SelectPathVertex(
        Editor editor,
        GraphDocumentContext context,
        string prompt)
    {
        while (true)
        {
            var result =
                editor.GetEntity(prompt);

            if (result.Status ==
                PromptStatus.Cancel)
            {
                return null;
            }

            if (result.Status !=
                PromptStatus.OK)
            {
                return null;
            }

            var vertex =
                context.VertexSelection.ReadVertex(
                    result.ObjectId);

            if (vertex is not null)
            {
                return vertex;
            }

            editor.WriteMessage(
                "\nВыбранный объект не является " +
                "вершиной GraphPlugin.");
        }
    }

    private static void ShowShortestPath(
    Editor editor,
    GraphDocumentContext context,
    ShortestPathResult result)
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var database =
            document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        foreach (var vertexId
                 in result.VertexIds)
        {
            if (!context.Index.TryGetVertexObjectId(
                    vertexId,
                    out var objectId))
            {
                continue;
            }

            if (objectId.IsErased)
                continue;

            if (transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                is Entity entity)
            {
                entity.Highlight();
            }
        }

        foreach (var edgeId
                 in result.EdgeIds)
        {
            if (!context.Index.TryGetEdgeObjectId(
                    edgeId,
                    out var objectId))
            {
                continue;
            }

            if (objectId.IsErased)
                continue;

            if (transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                is Entity entity)
            {
                entity.Highlight();
            }
        }

        transaction.Commit();

        editor.Regen();
    }

    private enum BuildAction
    {
        Existing,
        New,
        Finish,
        Cancel
    }

    [CommandMethod("GRAPHBUILD")]
    public void GraphBuild()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var context =
            PluginServices.CurrentContext;

        var vertexService =
            new VertexService(
                context.Vertices);

        var edgeService =
            new EdgeService(
                context.Vertices,
                context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                context.Edges);

        var splitService =
            new SplitEdgeService(
                context.Vertices,
                context.Edges);

        editor.WriteMessage(
            "\nПостроение графа." +
            "\nКлик по пустой области — новая вершина." +
            "\nКлик по вершине — использовать существующую." +
            "\nКлик по ребру — разделить ребро." +
            "\nEnter или Esc — завершить.");

        var executor =
            new GraphBuildStepExecutor(
                document,
                vertexService,
                splitService,
                buildService);

        try
        {
            while (true)
            {
                var pick =
                    context.BuildPick.GetNext();

                if (pick.Kind ==
                    BuildPickKind.Finish)
                {
                    break;
                }

                try
                {
                    executor.Execute(pick);
                }
                catch (System.Exception exception)
                {
                    editor.WriteMessage(
                        "\nНе удалось выполнить шаг построения: " +
                        exception.Message);
                }
            }
        }
        finally
        {
            buildService.Finish();
        }

        editor.WriteMessage(
            "\nПостроение графа завершено.");
    }
    private sealed record SelectedEdge(
        GraphEdge Edge,
        ObjectId ObjectId,
        Point3d PickedPoint);

    [CommandMethod("GRAPHSPLITEDGE")]
    public void SplitEdge()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var context =
            PluginServices.CurrentContext;

        editor.WriteMessage(
            "\nРазбиение ребра.");

        var selected =
            SelectEdge(
                editor,
                context);

        if (selected is null)
        {
            editor.WriteMessage(
                "\nКоманда отменена.");

            return;
        }

        Point2 splitPosition;

        try
        {
            splitPosition =
                ProjectPointOntoEdge(
                    editor,
                    document.Database,
                    selected);
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\nНе удалось определить точку разбиения: " +
                exception.Message);

            return;
        }

        var shape =
            AskVertexShape(editor);

        if (shape is null)
        {
            editor.WriteMessage(
                "\nКоманда отменена.");

            return;
        }

        var service =
            new SplitEdgeService(
                context.Vertices,
                context.Edges);

        var vertexA =
            context.Vertices.Get(
            selected.Edge.VertexAId)!;

        var vertexB =
            context.Vertices.Get(
                selected.Edge.VertexBId)!;

        var distanceToA =
            vertexA.Position.DistanceTo(
                splitPosition);

        var distanceToB =
            vertexB.Position.DistanceTo(
                splitPosition);

        editor.WriteMessage(
            $"\nA: " +
            $"({vertexA.Position.X}, " +
            $"{vertexA.Position.Y})" +

            $"\nSplit: " +
            $"({splitPosition.X}, " +
            $"{splitPosition.Y})" +

            $"\nB: " +
            $"({vertexB.Position.X}, " +
            $"{vertexB.Position.Y})" +

            $"\nDistance to A: {distanceToA}" +
            $"\nDistance to B: {distanceToB}");

        try
        {
            var result =
                service.Split(
                    selected.Edge.Id,
                    splitPosition,
                    shape.Value);

            editor.WriteMessage(
                "\nРебро успешно разделено." +
                $"\nНовая вершина: " +
                $"{result.NewVertex.Id}");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\nНе удалось разделить ребро: " +
                exception.Message);
        }
    }

    [CommandMethod("GRAPHADDBEND")]
    public void GraphAddBend()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var context = PluginServices.CurrentContext;

        try
        {
            var selection = SelectEdge(
                context.Document.Editor,
                context);

            if (selection is null)
                return;

            var bendPoint =
                context.EdgePickGeometry
                    .ProjectOntoEdge(
                        selection.ObjectId,
                        selection.PickedPoint);

            var result =
                context.AddBend.Add(
                    selection.Edge.Id,
                    bendPoint);

            document.Editor.WriteMessage(
                $"\nBend added. " +
                $"Index: {result.BendIndex}.");
        }
        catch (System.Exception ex)
        {
            document.Editor.WriteMessage(
                $"\nCannot add bend: " +
                $"{ex.Message}");
        }
    }

    private static SelectedEdge? SelectEdge(
        Editor editor,
        GraphDocumentContext context)
    {
        while (true)
        {
            var options =
                new PromptEntityOptions(
                    "\nУкажите место на ребре, где создать вершину: ");

            var result =
                editor.GetEntity(options);

            if (result.Status == PromptStatus.Cancel)
            {
                return null;
            }

            if (result.Status != PromptStatus.OK)
            {
                return null;
            }

            var edge =
                context.EdgeSelection.ReadEdge(
                    result.ObjectId);

            if (edge is null)
            {
                editor.WriteMessage(
                    "\nВыбранный объект не является " +
                    "ребром GraphPlugin.");

                continue;
            }

            return new SelectedEdge(
                edge,
                result.ObjectId,
                result.PickedPoint);
        }
    }

    private static Point2 ProjectPointOntoEdge(
        Editor editor,
        Database database,
        SelectedEdge selected)
    {
        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var line =
            transaction.GetObject(
                selected.ObjectId,
                OpenMode.ForRead)
            as Polyline;

        if (line is null)
        {
            throw new InvalidOperationException(
                "Selected GraphPlugin edge is not a Line.");
        }

        // PickedPoint интерактивного Editor находится
        // в координатах текущей UCS.
        var pointWcs =
            selected.PickedPoint.TransformBy(
                editor.CurrentUserCoordinateSystem);

        using var view =
            editor.GetCurrentView();

        var pointOnLine =
            line.GetClosestPointTo(
                pointWcs,
                view.ViewDirection,
                false);

        return new Point2(
            pointOnLine.X,
            pointOnLine.Y);
    }

    private static VertexShape? AskVertexShape(
        Editor editor)
    {
        var options =
            new PromptKeywordOptions(
                "\nФорма новой вершины");

        options.Keywords.Add(
            "Circle");

        options.Keywords.Add(
            "Triangle");

        options.Keywords.Default =
            "Circle";

        options.AllowNone = true;

        var result =
            editor.GetKeywords(
                options);

        if (result.Status ==
            PromptStatus.Cancel)
        {
            return null;
        }

        if (result.Status ==
            PromptStatus.None)
        {
            return VertexShape.Circle;
        }

        if (result.Status !=
            PromptStatus.OK)
        {
            return null;
        }

        return result.StringResult switch
        {
            "Triangle" =>
                VertexShape.Triangle,

            _ =>
                VertexShape.Circle
        };
    }

    [CommandMethod("GRAPH_OSNAP_PROBE")]
    public void GraphOsnapProbe()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var context =
            PluginServices.CurrentContext;

        ObjectId[] keyObjectIds =
            Array.Empty<ObjectId>();

        Point3d snappedPoint =
            Point3d.Origin;

        bool wasSnapped =
            false;

        string? monitorError =
            null;

        void OnPointMonitor(
            object? sender,
            PointMonitorEventArgs e)
        {
            try
            {
                if (!e.Context.PointComputed)
                    return;

                var snapped =
                    (e.Context.History &
                     PointHistoryBits.ObjectSnapped) != 0;

                if (!snapped)
                {
                    wasSnapped = false;
                    keyObjectIds =
                        Array.Empty<ObjectId>();

                    return;
                }

                wasSnapped = true;

                snappedPoint =
                    e.Context.ObjectSnappedPoint;

                var paths =
                    e.Context.GetKeyPointEntities();

                if (paths is null)
                {
                    keyObjectIds =
                        Array.Empty<ObjectId>();

                    return;
                }

                keyObjectIds =
                    paths
                        .SelectMany(
                            path =>
                                path.GetObjectIds())
                        .Distinct()
                        .ToArray();
            }
            catch (System.Exception exception)
            {
                monitorError =
                    exception.Message;
            }
        }

        editor.PointMonitor +=
            OnPointMonitor;

        try
        {
            var result =
                editor.GetPoint(
                    "\nУкажите точку с объектной привязкой: ");

            if (result.Status !=
                PromptStatus.OK)
            {
                editor.WriteMessage(
                    $"\nStatus: {result.Status}");

                return;
            }
        }
        finally
        {
            editor.PointMonitor -=
                OnPointMonitor;
        }

        editor.WriteMessage(
            "\n=== GRAPH OSNAP PROBE ===");

        if (monitorError is not null)
        {
            editor.WriteMessage(
                $"\nPointMonitor error: " +
                monitorError);

            return;
        }

        editor.WriteMessage(
            $"\nObject snapped: {wasSnapped}");

        if (!wasSnapped)
        {
            editor.WriteMessage(
                "\nKey entities: 0");

            return;
        }

        editor.WriteMessage(
            $"\nSnapped point: " +
            $"({snappedPoint.X:F6}, " +
            $"{snappedPoint.Y:F6}, " +
            $"{snappedPoint.Z:F6})");

        editor.WriteMessage(
            $"\nKey entities: " +
            keyObjectIds.Length);

        foreach (var objectId in keyObjectIds)
        {
            editor.WriteMessage(
                $"\n\nObjectId: {objectId}");

            if (context.Index.TryGetVertexId(
                    objectId,
                    out var vertexId))
            {
                editor.WriteMessage(
                    "\nGraph kind: VERTEX" +
                    $"\nVertexId: {vertexId}");

                continue;
            }

            if (context.Index.TryGetEdgeId(
                    objectId,
                    out var edgeId))
            {
                editor.WriteMessage(
                    "\nGraph kind: EDGE" +
                    $"\nEdgeId: {edgeId}");

                continue;
            }

            editor.WriteMessage(
                "\nGraph kind: OTHER");
        }
    }

    [CommandMethod("GRAPH_PICK_PROBE")]
    public void GraphPickProbe()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var options =
            new PromptEntityOptions(
                "\nКликните по объекту или в пустую область: ");

        options.AllowNone = true;

        try
        {
            var result =
                editor.GetEntity(options);

            editor.WriteMessage(
                "\n=== GETENTITY PROBE ===");

            editor.WriteMessage(
                $"\nStatus: {result.Status}");

            try
            {
                var point =
                    result.PickedPoint;

                editor.WriteMessage(
                    $"\nPickedPoint: " +
                    $"({point.X:F6}, " +
                    $"{point.Y:F6}, " +
                    $"{point.Z:F6})");
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage(
                    $"\nPickedPoint unavailable: " +
                    exception.Message);
            }

            if (result.Status ==
                PromptStatus.OK)
            {
                editor.WriteMessage(
                    $"\nObjectId: {result.ObjectId}");

                var context =
                    PluginServices.CurrentContext;

                if (context.Index.TryGetVertexId(
                        result.ObjectId,
                        out var vertexId))
                {
                    editor.WriteMessage(
                        "\nGraph kind: VERTEX" +
                        $"\nVertexId: {vertexId}");

                    return;
                }

                if (context.Index.TryGetEdgeId(
                        result.ObjectId,
                        out var edgeId))
                {
                    editor.WriteMessage(
                        "\nGraph kind: EDGE" +
                        $"\nEdgeId: {edgeId}");

                    return;
                }

                editor.WriteMessage(
                    "\nGraph kind: OTHER");
            }
            else
            {
                editor.WriteMessage(
                    "\nNo entity selected.");
            }
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                "\n=== GETENTITY PROBE ===" +
                $"\nException type: " +
                exception.GetType().FullName +
                $"\nMessage: {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_BUILD_PICK_PROBE")]
    public void GraphBuildPickProbe()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        var result =
            context.BuildPick.GetNext();

        document.Editor.WriteMessage(
            "\n=== BUILD PICK ===" +
            $"\nKind: {result.Kind}" +
            $"\nPoint: " +
            $"({result.Point.X:F4}, " +
            $"{result.Point.Y:F4}, " +
            $"{result.Point.Z:F4})");

        if (result.Vertex is not null)
        {
            document.Editor.WriteMessage(
                $"\nVertexId: " +
                result.Vertex.Id);
        }

        if (result.Edge is not null)
        {
            document.Editor.WriteMessage(
                $"\nEdgeId: " +
                result.Edge.Id);
        }

        if (!result.ObjectId.IsNull)
        {
            document.Editor.WriteMessage(
                $"\nObjectId: " +
                result.ObjectId);
        }
    }

    private static GraphVertex? SelectVertex(
        Document document,
        GraphDocumentContext context,
        string message)
    {
        var editor =
            document.Editor;

        while (true)
        {
            var options =
                new PromptEntityOptions(
                    message);

            var result =
                editor.GetEntity(
                    options);

            if (result.Status !=
                PromptStatus.OK)
            {
                return null;
            }

            if (!context.Index.TryGetVertexId(
                    result.ObjectId,
                    out var vertexId))
            {
                editor.WriteMessage(
                    "\nSelected object is not a graph vertex.");

                continue;
            }

            var vertex =
                context.Vertices.Get(
                    vertexId);

            if (vertex is null)
            {
                editor.WriteMessage(
                    "\nGraph vertex could not be restored.");

                continue;
            }

            return vertex;
        }
    }

    private static string? GetDrawingPath(
    Document document)
    {
        var filename =
            document.Database.Filename;

        if (string.IsNullOrWhiteSpace(
                filename))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(
                filename);
        }
        catch
        {
            return null;
        }
    }

    private static void PrintAttachments(
        Document document,
        IReadOnlyList<VertexAttachment> attachments)
    {
        var editor =
            document.Editor;

        if (attachments.Count == 0)
        {
            editor.WriteMessage(
                "\nVertex has no attached files.");

            return;
        }

        editor.WriteMessage(
            $"\nAttached files: {attachments.Count}");

        for (var i = 0;
             i < attachments.Count;
             i++)
        {
            editor.WriteMessage(
                $"\n  {i + 1}. " +
                $"{attachments[i].Path}");
        }
    }

    private static VertexAttachment? ChooseAttachment(
        Document document,
        IReadOnlyList<VertexAttachment> attachments,
        string prompt)
    {
        if (attachments.Count == 0)
        {
            document.Editor.WriteMessage(
                "\nVertex has no attached files.");

            return null;
        }

        if (attachments.Count == 1)
        {
            return attachments[0];
        }

        PrintAttachments(
            document,
            attachments);

        var options =
            new PromptIntegerOptions(
                prompt)
            {
                LowerLimit = 1,
                UpperLimit = attachments.Count
            };

        var result =
            document.Editor.GetInteger(
                options);

        if (result.Status !=
            PromptStatus.OK)
        {
            return null;
        }

        return attachments[
            result.Value - 1];
    }

    [CommandMethod("GRAPHATTACHFILE")]
    public void GraphAttachFile()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try
        {
            var vertex =
                SelectVertex(
                    document,
                    context,
                    "\nSelect vertex to attach file: ");

            if (vertex is null)
                return;

            var fileOptions =
                new PromptOpenFileOptions(
                    "\nSelect file to attach");

            var fileResult =
                editor.GetFileNameForOpen(
                    fileOptions);

            if (fileResult.Status !=
                PromptStatus.OK)
            {
                return;
            }

            var drawingPath = GetDrawingPath(document);

            var filePath =
                fileResult.StringResult;

            var attachment =
                context.AttachmentService.Attach(
                    vertex.Id,
                    fileResult.StringResult,
                    drawingPath);

            editor.WriteMessage(
                "\nFile attached.");

            editor.WriteMessage(
                $"\nStored path: " +
                $"{attachment.Path}");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\nCannot attach file: " +
                $"{ex.Message}");
        }
    }

    [CommandMethod("GRAPHVERTEXFILES")]
    public void GraphVertexFiles()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try
        {
            var vertex =
                SelectVertex(
                    document,
                    context,
                    "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments =
                context.AttachmentService
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            editor.WriteMessage(
                $"\nVertex: {vertex.Id}");

            PrintAttachments(
                document,
                attachments);
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\nCannot read attached files: " +
                $"{ex.Message}");
        }
    }

    [CommandMethod("GRAPHOPENFILE")]
    public void GraphOpenFile()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try
        {
            var vertex =
                SelectVertex(
                    document,
                    context,
                    "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments =
                context.AttachmentService
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            var attachment =
                ChooseAttachment(
                    document,
                    attachments,
                    "\nEnter file number to open: ");

            if (attachment is null)
                return;

            var path =
                context.AttachmentService
                    .ResolvePath(
                        vertex.Id,
                        attachment.Path,
                        GetDrawingPath(
                            document));

            if (!File.Exists(
                    path))
            {
                editor.WriteMessage(
                    "\nAttached file was not found.");

                editor.WriteMessage(
                    $"\nResolved path: {path}");

                return;
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });

            editor.WriteMessage(
                $"\nOpened: {path}");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\nCannot open attached file: " +
                $"{ex.Message}");
        }
    }

    [CommandMethod("GRAPHDETACHFILE")]
    public void GraphDetachFile()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try
        {
            var vertex =
                SelectVertex(
                    document,
                    context,
                    "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments =
                context.AttachmentService
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            var attachment =
                ChooseAttachment(
                    document,
                    attachments,
                    "\nEnter file number to detach: ");

            if (attachment is null)
                return;

            context.AttachmentService.Detach(
                vertex.Id,
                attachment.Path);

            editor.WriteMessage(
                "\nFile detached from vertex.");

            editor.WriteMessage(
                $"\nRemoved reference: " +
                $"{attachment.Path}");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\nCannot detach file: " +
                $"{ex.Message}");
        }
    }
}