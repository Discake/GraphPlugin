using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using HostMgd.EditorInput;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class EdgeCommands
{
    [CommandMethod("GRAPHEDGE")]
    public void CreateEdge()
    {
        var context =
            PluginServices.CurrentContext;

        var editor =
            context.Document.Editor;

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

        var service =
            new EdgeService(
                context.Vertices,
                context.Edges);

        service.CreateEdge(
            vertexA.Id,
            vertexB.Id);

        editor.WriteMessage(
            "\nРебро создано.");
    }

    [CommandMethod("GRAPHEDGESTYLE")]
    public void ChangeEdgeStyle()
    {
        var context =
            PluginServices.CurrentContext;

        var editor =
            context.Document.Editor;

        var colorOptions =
            new PromptKeywordOptions(
                "\nЦвет ребра");

        colorOptions.Keywords.Add("White");
        colorOptions.Keywords.Add("Red");
        colorOptions.Keywords.Add("Blue");
        colorOptions.Keywords.Add("Green");
        colorOptions.Keywords.Default = "White";

        var colorResult =
            editor.GetKeywords(colorOptions);

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
        typeOptions.Keywords.Default = "Continuous";

        var typeResult =
            editor.GetKeywords(typeOptions);

        if (typeResult.Status != PromptStatus.OK)
            return;

        var lineType =
            typeResult.StringResult switch
            {
                "Dashed" => EdgeLineType.Dashed,
                "Dotted" => EdgeLineType.Dotted,
                _ => EdgeLineType.Continuous
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
            editor.GetDouble(weightOptions);

        if (weightResult.Status != PromptStatus.OK)
            return;

        var style =
            new EdgeStyle(
                color,
                lineType,
                weightResult.Value);

        context.Settings.ChangeEdgeStyle(style);

        editor.WriteMessage(
            "\nСтиль всех рёбер обновлён.");
    }
}
