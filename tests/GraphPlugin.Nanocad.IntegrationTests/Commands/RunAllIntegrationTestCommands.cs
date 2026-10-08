using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class RunAllIntegrationTestCommands
{
    [CommandMethod("GRAPHRUNTESTS")]
    public void RunTests()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var editor =
            document.Editor;

        var runner =
            new GraphIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        editor.WriteMessage(
            "\n=== GraphPlugin integration tests ===");

        var results =
            runner.RunAll();

        foreach (var result in results)
        {
            if (result.Passed)
            {
                editor.WriteMessage(
                    $"\n{result.Name}");
            }
            else
            {
                editor.WriteMessage(
                    $"\n{result.Name}");

                editor.WriteMessage(
                    $"\n       {result.Error}");
            }
        }

        var passed =
            results.Count(x => x.Passed);

        var failed =
            results.Count - passed;

        editor.WriteMessage(
            $"\n------------------------------" +
            $"\nPassed: {passed}" +
            $"\nFailed: {failed}" +
            $"\nTotal:  {results.Count}");

        editor.WriteMessage(
            "\n=== Tests finished ===");
    }
}
