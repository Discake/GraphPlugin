using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class UndoTestCommands
{
    [CommandMethod("GRAPH_PREPARE_EDGE_UNDO_TEST")]
    public void PrepareEdgeUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            var manifest =
                runner.PrepareEdge();

            document.Editor.WriteMessage(
                "\nEdge Undo test prepared." +
                $"\nTestId: {manifest.TestId}" +
                "\n" +
                "\nДалее:" +
                "\n1. ERASE созданного ребра" +
                "\n2. GRAPH_VERIFY_EDGE_ERASED" +
                "\n3. UNDO" +
                "\n4. GRAPH_VERIFY_EDGE_UNDO");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_EDGE_ERASED")]
    public void VerifyEdgeErased()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            IntegrationTestCommandOutput.WriteResults(
                document.Editor,
                "Edge erased verification",
                runner.VerifyEdgeErased());
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_EDGE_UNDO")]
    public void VerifyEdgeUndo()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            IntegrationTestCommandOutput.WriteResults(
                document.Editor,
                "Edge Undo verification",
                runner.VerifyEdgeUndo());
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_PREPARE_VERTEX_UNDO_TEST")]
    public void PrepareVertexUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            var manifest =
                runner.PrepareVertex();

            document.Editor.WriteMessage(
                "\nVertex Undo test prepared." +
                $"\nTestId: {manifest.TestId}" +
                "\n" +
                "\nДалее:" +
                "\n1. ERASE только средней Vertex" +
                "\n2. GRAPH_VERIFY_VERTEX_ERASED" +
                "\n3. UNDO" +
                "\n4. GRAPH_VERIFY_VERTEX_UNDO");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_VERTEX_ERASED")]
    public void VerifyVertexErased()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            IntegrationTestCommandOutput.WriteResults(
                document.Editor,
                "Vertex cascade erased verification",
                runner.VerifyVertexErased());
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_VERTEX_UNDO")]
    public void VerifyVertexUndo()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            IntegrationTestCommandOutput.WriteResults(
                document.Editor,
                "Vertex cascade Undo verification",
                runner.VerifyVertexUndo());
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] {exception.Message}");
        }
    }

    [CommandMethod("GRAPH_CLEAR_UNDO_TEST")]
    public void ClearUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.Clear();

            document.Editor.WriteMessage(
                "\nUndo integration test data removed.");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Cleanup: {exception.Message}");
        }
    }
}
