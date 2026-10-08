using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class GraphBuildCommands
{
    [CommandMethod("GRAPHBUILD")]
    public void GraphBuild()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        editor.WriteMessage(
            "\nПостроение графа."
                + "\nКлик по пустой области — новая вершина."
                + "\nКлик по вершине — использовать существующую."
                + "\nКлик по ребру — разделить ребро."
                + "\nEnter или Esc — завершить."
        );

        try
        {
            while (true)
            {
                var pick = context.BuildPick.GetNext();

                if (pick.Kind == BuildPickKind.Finish)
                    break;

                try
                {
                    context.BuildStepExecutor.Execute(pick);
                }
                catch (System.Exception exception)
                {
                    editor.WriteMessage("\nНе удалось выполнить шаг построения: " + exception.Message);
                }
            }
        }
        finally
        {
            context.GraphBuild.Finish();
        }

        editor.WriteMessage("\nПостроение графа завершено.");
    }
}
