using System.Diagnostics;
using System.Windows.Forms;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.UI.GraphControl;
using HostMgd.ApplicationServices;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class GraphControlCommands
{
    private sealed class WindowHandle : IWin32Window
    {
        public IntPtr Handle { get; }

        public WindowHandle(IntPtr handle)
        {
            Handle = handle;
        }
    }

    private static GraphControlForm? _form;
    private static Document? _queuedCommandDocument;

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

            StopWaitingForQueuedCommand();
        };

        _form = form;

        using var process = Process.GetCurrentProcess();
        process.Refresh();

        var nanoCadWindow = process.MainWindowHandle;

        if (nanoCadWindow != IntPtr.Zero)
            form.Show(new WindowHandle(nanoCadWindow));
        else
            form.Show();

        form.Activate();
        form.BringToFront();
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
            GraphControlAction.OpenFile => "GRAPHOPENFILE",
            GraphControlAction.ListFiles => "GRAPHVERTEXFILES",
            GraphControlAction.None => null,
            _ => null,
        };

        if (command is null)
            return;

        var context = PluginServices.CurrentContext;
        var document = context.Document;

        StartWaitingForQueuedCommand(document);

        try
        {
            _form?.Hide();
            document.SendStringToExecute(command + " ", true, false, false);
        }
        catch
        {
            StopWaitingForQueuedCommand();
            RestoreControlCenter();
            throw;
        }
    }

    private static void StartWaitingForQueuedCommand(Document document)
    {
        StopWaitingForQueuedCommand();

        _queuedCommandDocument = document;
        document.CommandEnded += OnQueuedCommandFinished;
        document.CommandCancelled += OnQueuedCommandFinished;
        document.CommandFailed += OnQueuedCommandFinished;
    }

    private static void StopWaitingForQueuedCommand()
    {
        if (_queuedCommandDocument is null)
            return;

        _queuedCommandDocument.CommandEnded -= OnQueuedCommandFinished;
        _queuedCommandDocument.CommandCancelled -= OnQueuedCommandFinished;
        _queuedCommandDocument.CommandFailed -= OnQueuedCommandFinished;
        _queuedCommandDocument = null;
    }

    private static void OnQueuedCommandFinished(object sender, CommandEventArgs e)
    {
        StopWaitingForQueuedCommand();

        if (_form is not { IsDisposed: false })
            return;

        if (_form.IsHandleCreated)
            _form.BeginInvoke((Action)RestoreControlCenter);
        else
            RestoreControlCenter();
    }

    private static void RestoreControlCenter()
    {
        if (_form is not { IsDisposed: false })
            return;

        if (!_form.Visible)
            _form.Show();

        _form.Activate();
        _form.BringToFront();
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
