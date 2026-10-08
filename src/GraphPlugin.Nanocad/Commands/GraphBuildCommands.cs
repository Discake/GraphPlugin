using GraphPlugin.Nanocad.Bootstrap;
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

        editor.WriteMessage(
            "\nПостроение графа." +
            "\nКлик по пустой области — новая вершина." +
            "\nКлик по вершине — использовать существующую." +
            "\nКлик по ребру — разделить ребро." +
            "\nEnter или Esc — завершить.");

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
                    context.BuildStepExecutor.Execute(pick);
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
            context.GraphBuild.Finish();
        }

        editor.WriteMessage(
            "\nПостроение графа завершено.");
    }
}
