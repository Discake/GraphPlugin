using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Bootstrap;
using GraphPlugin.NanoCad.Persistence.Metadata;
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.NanoCad.Commands;

public sealed class VertexCommands
{
    [CommandMethod("GRAPHNODE")]
    public void CreateVertex()
    {
        var context = PluginServices.CurrentContext;
        var editor = context.Document.Editor;

        var options =
            new PromptKeywordOptions(
                "\nТип вершины");

        options.Keywords.Add("Circle");
        options.Keywords.Add("Triangle");

        options.Keywords.Default =
            "Circle";

        var shapeResult =
            editor.GetKeywords(options);

        if (shapeResult.Status != PromptStatus.OK)
            return;

        var shape =
            shapeResult.StringResult == "Triangle"
                ? VertexShape.Triangle
                : VertexShape.Circle;

        var pointResult =
            editor.GetPoint(
                "\nУкажите положение вершины графа: ");

        if (pointResult.Status != PromptStatus.OK)
            return;

        var point =
            pointResult.Value;

        var service = new VertexService(context.Vertices);

        service.CreateVertex(
            new Point2(
                point.X,
                point.Y),
            shape);
    }

    [CommandMethod("GRAPHINFO")]
    public void GraphInfo()
    {
        var document =
            NanoApplication.DocumentManager.MdiActiveDocument;

        var editor =
            document.Editor;

        var selection =
            editor.GetEntity(
                "\nВыберите объект графа: ");

        if (selection.Status != PromptStatus.OK)
            return;

        var database =
            document.Database;

        using var transaction =
            database.TransactionManager.StartTransaction();

        var entity =
            transaction.GetObject(
                selection.ObjectId,
                OpenMode.ForRead) as Entity;

        if (entity is null)
            return;

        var metadata =
            new XRecordMetadataStore();

        var vertex =
            metadata.ReadVertex(
                entity,
                transaction);

        if (vertex is null)
        {
            editor.WriteMessage(
                "\nОбъект не является вершиной GraphPlugin.");

            return;
        }

        editor.WriteMessage(
            $"\nGraph vertex:" +
            $"\n  Id: {vertex.Id}" +
            $"\n  Version: {vertex.Version}" +
            $"\n  Shape: {vertex.Shape}");

        transaction.Commit();
    }

    [CommandMethod("GRAPHVERTICES")]
    public void ListVertices()
    {
        var context = PluginServices.CurrentContext;
        var editor = context.Document.Editor;

        var vertices =
            context.Vertices.GetAll();

        editor.WriteMessage(
            $"\nНайдено вершин: {vertices.Count}");

        foreach (var vertex in vertices)
        {
            editor.WriteMessage(
                $"\n" +
                $"  {vertex.Id}" +
                $" | X={vertex.Position.X:F2}" +
                $" | Y={vertex.Position.Y:F2}" +
                $" | {vertex.Style.Shape}");
        }
    }

    [CommandMethod("GRAPHEDGE")]
    public void CreateEdge()
    {
        var context = PluginServices.CurrentContext;
        var editor = context.Document.Editor;

        var firstResult =
            editor.GetEntity(
                "\nВыберите первую вершину графа: ");

        if (firstResult.Status != PromptStatus.OK)
            return;

        var vertexA =
            context.VertexSelection.ReadVertex(
                firstResult.ObjectId);

        if (vertexA is null)
        {
            editor.WriteMessage(
                "\nВыбранный объект не является вершиной GraphPlugin.");

            return;
        }

        var secondResult =
            editor.GetEntity(
                "\nВыберите вторую вершину графа: ");

        if (secondResult.Status != PromptStatus.OK)
            return;

        var vertexB =
            context.VertexSelection.ReadVertex(
                secondResult.ObjectId);

        if (vertexB is null)
        {
            editor.WriteMessage(
                "\nВыбранный объект не является вершиной GraphPlugin.");

            return;
        }

        if (vertexA.Id == vertexB.Id)
        {
            editor.WriteMessage(
                "\nНельзя соединить вершину саму с собой.");

            return;
        }

        var service = new EdgeService(context.Vertices, context.Edges);

        service.CreateEdge(
            vertexA.Id,
            vertexB.Id);

        editor.WriteMessage(
            "\nРебро создано.");
    }

    [CommandMethod("GRAPHEDGESTYLE")]
    public void ChangeEdgeStyle()
    {
        var context = PluginServices.CurrentContext;
        var editor = context.Document.Editor;

        var colorOptions =
            new PromptKeywordOptions(
                "\nЦвет ребра");

        colorOptions.Keywords.Add("White");
        colorOptions.Keywords.Add("Red");
        colorOptions.Keywords.Add("Blue");
        colorOptions.Keywords.Add("Green");

        colorOptions.Keywords.Default =
            "White";

        var colorResult =
            editor.GetKeywords(
                colorOptions);

        if (colorResult.Status != PromptStatus.OK)
            return;

        var color =
            colorResult.StringResult switch
            {
                "Red" => GraphColor.Red,
                "Blue" => GraphColor.Blue,
                "Green" => GraphColor.Green,
                _ => GraphColor.White
            };

        var typeOptions =
            new PromptKeywordOptions(
                "\nТип линии");

        typeOptions.Keywords.Add("Continuous");
        typeOptions.Keywords.Add("Dashed");
        typeOptions.Keywords.Add("Dotted");

        typeOptions.Keywords.Default =
            "Continuous";

        var typeResult =
            editor.GetKeywords(
                typeOptions);

        if (typeResult.Status != PromptStatus.OK)
            return;

        var lineType =
            typeResult.StringResult switch
            {
                "Dashed" =>
                    EdgeLineType.Dashed,

                "Dotted" =>
                    EdgeLineType.Dotted,

                _ =>
                    EdgeLineType.Continuous
            };

        var weightOptions =
            new PromptDoubleOptions(
                "\nТолщина линии в мм")
            {
                DefaultValue = 0.25,
                UseDefaultValue = true,
                AllowNegative = false,
                AllowZero = false
            };

        var weightResult =
            editor.GetDouble(
                weightOptions);

        if (weightResult.Status != PromptStatus.OK)
            return;

        double lineWeightMm =
            weightResult.Value;

        var style =
            new EdgeStyle(
                color,
                lineType,
                lineWeightMm);

        context.Settings.ChangeEdgeStyle(
            style);

        editor.WriteMessage(
            "\nСтиль всех рёбер обновлён.");
    }


}