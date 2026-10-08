using System.Windows.Forms;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.UI.GraphControl;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class GraphControlCommands
{
    [CommandMethod("GRAPHCONTROL")]
    public void ShowControlCenter()
    {
        while (true)
        {
            var context = PluginServices.CurrentContext;
            var settings = context.Settings.GetSettings();

            using var form = new GraphControlForm(
                context.Vertices.GetAll().Count,
                context.Edges.GetAll().Count,
                settings.EdgeStyle,
                context.Settings.ChangeEdgeStyle
            );

            if (form.ShowDialog() != DialogResult.OK)
                return;

            ExecuteAction(form.SelectedAction);
        }
    }

    private static void ExecuteAction(GraphControlAction action)
    {
        switch (action)
        {
            case GraphControlAction.CreateVertex:
                new VertexCommands().CreateVertex();
                break;

            case GraphControlAction.CreateEdge:
                new EdgeCommands().CreateEdge();
                break;

            case GraphControlAction.BuildGraph:
                new GraphBuildCommands().GraphBuild();
                break;

            case GraphControlAction.SplitEdge:
                new EdgeEditingCommands().SplitEdge();
                break;

            case GraphControlAction.AddBend:
                new EdgeEditingCommands().GraphAddBend();
                break;

            case GraphControlAction.ShortestPath:
                new ShortestPathCommands().FindShortestPath();
                break;

            case GraphControlAction.ClearShortestPath:
                new ShortestPathCommands().ClearShortestPath();
                break;

            case GraphControlAction.None:
            default:
                break;
        }
    }
}
