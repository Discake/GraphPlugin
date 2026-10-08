using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Commands;

public sealed class ShortestPathCommands
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

        ShortestPathResult result;

        try
        {
            result =
                context.ShortestPath.Find(
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

            if (result.Status == PromptStatus.Cancel)
                return null;

            if (result.Status != PromptStatus.OK)
                return null;

            var vertex =
                context.VertexSelection.ReadVertex(
                    result.ObjectId);

            if (vertex is not null)
                return vertex;

            editor.WriteMessage(
                "\nВыбранный объект не является " +
                "вершиной GraphPlugin.");
        }
    }
}
