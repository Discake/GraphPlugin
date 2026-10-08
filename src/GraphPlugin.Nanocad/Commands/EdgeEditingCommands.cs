using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Commands;

public sealed class EdgeEditingCommands
{
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

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        try
        {
            var selection =
                SelectEdge(
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
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\nCannot add bend: " +
                $"{exception.Message}");
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
                return null;

            if (result.Status != PromptStatus.OK)
                return null;

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

        var polyline =
            transaction.GetObject(
                selected.ObjectId,
                OpenMode.ForRead)
            as Polyline;

        if (polyline is null)
        {
            throw new InvalidOperationException(
                "Selected GraphPlugin edge is not a Polyline.");
        }

        var pointWcs =
            selected.PickedPoint.TransformBy(
                editor.CurrentUserCoordinateSystem);

        using var view =
            editor.GetCurrentView();

        var pointOnLine =
            polyline.GetClosestPointTo(
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

        options.Keywords.Add("Circle");
        options.Keywords.Add("Triangle");
        options.Keywords.Default = "Circle";
        options.AllowNone = true;

        var result =
            editor.GetKeywords(options);

        if (result.Status == PromptStatus.Cancel)
            return null;

        if (result.Status == PromptStatus.None)
            return VertexShape.Circle;

        if (result.Status != PromptStatus.OK)
            return null;

        return result.StringResult switch
        {
            "Triangle" => VertexShape.Triangle,
            _ => VertexShape.Circle
        };
    }
}
