using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.NanoCad.Runtime;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class NativeInteropTestCommands
{
    [CommandMethod("GRAPH_VERIFY_CPP_STYLE_INTEROP_TEST")]
    public void VerifyCppStyleInteropTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor = document.Editor;

        try
        {
            var runner =
                new GraphIntegrationTestRunner(
                    document,
                    PluginServices.CurrentContext);

            runner.VerifyCppStyleInteropTest();
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\n[FAIL] C++ style interop test:\n{exception}");
        }
    }

    [CommandMethod("GRAPH_VERIFY_CPP_DELETE_UNDO_TEST")]
    public void VerifyCppDeleteUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor = document.Editor;

        try
        {
            var runner =
                new GraphIntegrationTestRunner(
                    document,
                    PluginServices.CurrentContext);

            runner.VerifyCppDeleteUndoTest();
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\n[FAIL] C++ delete undo test:\n{exception}");
        }
    }
}
