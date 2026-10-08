using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class PersistenceTestCommands
{
    [CommandMethod("GRAPH_PREPARE_PERSISTENCE_TEST")]
    public void PreparePersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            var manifest =
                runner.Prepare();

            document.Editor.WriteMessage(
                "\nPersistence test prepared successfully." +
                $"\nTestId: {manifest.TestId}" +
                "\n" +
                "\nТеперь:" +
                "\n1. SAVE" +
                "\n2. Закройте DWG" +
                "\n3. Откройте DWG заново" +
                "\n4. Загрузите плагин, если он не загружается автоматически" +
                "\n5. Выполните GRAPH_VERIFY_PERSISTENCE_TEST");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Prepare persistence test: " +
                exception.Message);
        }
    }

    [CommandMethod("GRAPH_VERIFY_PERSISTENCE_TEST")]
    public void VerifyPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            var results =
                runner.Verify();

            IntegrationTestCommandOutput.WriteResults(
                document.Editor,
                "Persistence verification",
                results);
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Persistence verification: " +
                exception.Message);
        }
    }

    [CommandMethod("GRAPH_CLEAR_PERSISTENCE_TEST")]
    public void ClearPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.Clear();

            document.Editor.WriteMessage(
                "\nPersistence test data removed.");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Cleanup: {exception.Message}");
        }
    }
}
