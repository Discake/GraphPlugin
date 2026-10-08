using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Runtime;
using Teigha.DatabaseServices;
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
                "\n1. GRAPH_ERASE_EDGE_UNDO_TEST" +
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

    [CommandMethod("GRAPH_ERASE_EDGE_UNDO_TEST")]
    public void ErasePreparedEdge()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        try
        {
            var manifest =
                RequireManifest(
                    document,
                    UndoTestScenario.Edge);

            var context =
                PluginServices.CurrentContext;

            if (!context.Index.TryGetEdgeObjectId(
                    manifest.EdgeABId,
                    out var objectId))
            {
                throw new InvalidOperationException(
                    "Prepared edge is missing from GraphEntityIndex.");
            }

            EraseEntity(
                document.Database,
                objectId,
                "prepared edge");

            document.Editor.WriteMessage(
                "\nPrepared edge erased." +
                "\nNow run GRAPH_VERIFY_EDGE_ERASED.");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Edge erase: {exception.Message}");
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
                "\n1. GRAPH_ERASE_VERTEX_UNDO_TEST" +
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

    [CommandMethod("GRAPH_ERASE_VERTEX_UNDO_TEST")]
    public void ErasePreparedVertex()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        try
        {
            var manifest =
                RequireManifest(
                    document,
                    UndoTestScenario.Vertex);

            var context =
                PluginServices.CurrentContext;

            if (!context.Index.TryGetVertexObjectId(
                    manifest.VertexBId,
                    out var objectId))
            {
                throw new InvalidOperationException(
                    "Prepared middle vertex is missing from GraphEntityIndex.");
            }

            EraseEntity(
                document.Database,
                objectId,
                "prepared middle vertex");

            document.Editor.WriteMessage(
                "\nPrepared middle vertex erased." +
                "\nCascade cleanup will be processed when this command ends." +
                "\nNow run GRAPH_VERIFY_VERTEX_ERASED.");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[FAIL] Vertex erase: {exception.Message}");
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

    private static UndoTestManifest RequireManifest(
        HostMgd.ApplicationServices.Document document,
        UndoTestScenario expectedScenario)
    {
        var manifest =
            new UndoTestManifestStore()
                .Load(document.Database)
            ?? throw new InvalidOperationException(
                "Undo integration test is not prepared.");

        if (manifest.Scenario != expectedScenario)
        {
            throw new InvalidOperationException(
                $"Prepared scenario is {manifest.Scenario}, " +
                $"but {expectedScenario} was expected.");
        }

        return manifest;
    }

    private static void EraseEntity(
        Database database,
        ObjectId objectId,
        string description)
    {
        if (objectId.IsNull || objectId.IsErased)
        {
            throw new InvalidOperationException(
                $"The {description} is already erased or invalid.");
        }

        using var transaction =
            database.TransactionManager.StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForWrite)
            as Entity
            ?? throw new InvalidOperationException(
                $"The {description} ObjectId is not an Entity.");

        entity.Erase();
        transaction.Commit();
    }
}
