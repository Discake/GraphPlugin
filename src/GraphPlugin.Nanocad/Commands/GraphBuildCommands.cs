using GraphPlugin.Application.Services;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.NanoCad.Runtime;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Commands;

public sealed class GraphBuildCommands
{
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

                if (pick.Kind == BuildPickKind.Finish)
                    break;

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
}
