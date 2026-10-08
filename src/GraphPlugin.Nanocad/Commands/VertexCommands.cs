using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.NanoCad.Persistence.Metadata;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class VertexCommands
{
    [CommandMethod("GRAPHNODE")]
    public void CreateVertex()
    {
        var context =
            PluginServices.CurrentContext;

        var editor =
            context.Document.Editor;

        var options =
            new PromptKeywordOptions(
                "\nТип вершины");

        options.Keywords.Add("Circle");
        options.Keywords.Add("Triangle");
        options.Keywords.Default = "Circle";

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

        var service =
            new VertexService(
                context.Vertices);

        service.CreateVertex(
            new Point2(
                point.X,
                point.Y),
            shape);
    }

    [CommandMethod("GRAPHINFO")]
    public void GraphInfo()
    {
        var context =
            PluginServices.CurrentContext;

        var document =
            context.Document;

        var editor =
            document.Editor;

        var selection =
            editor.GetEntity(
                "\nВыберите объект графа: ");

        if (selection.Status != PromptStatus.OK)
            return;

        using var transaction =
            document.Database.TransactionManager
                .StartTransaction();

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
        var context =
            PluginServices.CurrentContext;

        var editor =
            context.Document.Editor;

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
}
