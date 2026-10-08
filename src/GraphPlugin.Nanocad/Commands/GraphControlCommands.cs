using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.UI.GraphControl;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class GraphControlCommands
{
    private static GraphControlForm? _form;

    [CommandMethod("GRAPHCONTROL")]
    public void ShowControlCenter()
    {
        if (_form is { IsDisposed: false })
        {
            if (!_form.Visible)
                _form.Show();

            _form.Activate();
            _form.BringToFront();

            return;
        }

        var context = PluginServices.CurrentContext;
        var settings = context.Settings.GetSettings();

        var form = new GraphControlForm(
            settings.EdgeStyle,
            ApplyEdgeStyle,
            QueueAction,
            GetStatistics
        );

        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_form, form))
                _form = null;
        };

        _form = form;
        form.Show();
    }

    private static void QueueAction(GraphControlAction action)
    {
        var command = action switch
        {
            GraphControlAction.CreateVertex => "GRAPHNODE",
            GraphControlAction.CreateEdge => "GRAPHEDGE",
            GraphControlAction.BuildGraph => "GRAPHBUILD",
            GraphControlAction.SplitEdge => "GRAPHSPLITEDGE",
            GraphControlAction.AddBend => "GRAPHADDBEND",
            GraphControlAction.ShortestPath => "GRAPHSHORTESTPATH",
            GraphControlAction.ClearShortestPath => "GRAPHCLEARPATH",
            GraphControlAction.AttachFile => "GRAPHATTACHFILE",
            GraphControlAction.DetachFile => "GRAPHDETACHFILE",
            GraphControlAction.None => null,
            _ => null,
        };

        if (command is null)
            return;

        var context = PluginServices.CurrentContext;
        context.Document.SendStringToExecute(command + " ", true, false, false);
    }

    private static void ApplyEdgeStyle(EdgeStyle style)
    {
        var context = PluginServices.CurrentContext;

        using var documentLock = context.Document.LockDocument();

        context.Settings.ChangeEdgeStyle(style);
    }

    private static (int VertexCount, int EdgeCount) GetStatistics()
    {
        var context = PluginServices.CurrentContext;

        using var documentLock = context.Document.LockDocument();

        return (context.Vertices.GetAll().Count, context.Edges.GetAll().Count);
    }
}
