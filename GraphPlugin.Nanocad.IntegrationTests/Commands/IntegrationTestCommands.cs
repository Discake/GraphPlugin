using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.NanoCad.Bootstrap;
using GraphPlugin.NanoCad.Persistence;
using GraphPlugin.NanoCad.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using NanoApplication =
    HostMgd.ApplicationServices.Application;

namespace GraphPlugin.NanoCad.Commands;

public sealed class IntegrationTestCommands
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

        var context =
            PluginServices.CurrentContext;

        var runner =
            new GraphIntegrationTestRunner(
                document,
                context);

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

    [CommandMethod("GRAPH_PREPARE_PERSISTENCE_TEST")]
    public void PreparePersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        if (document is null)
            return;

        var context =
            PluginServices.CurrentContext;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                context);

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

        var context =
            PluginServices.CurrentContext;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                context);

        document.Editor.WriteMessage(
            "\n=== Persistence verification ===");

        try
        {
            var results =
                runner.Verify();

            foreach (var result in results)
            {
                document.Editor.WriteMessage(
                    result.Passed
                        ? $"\n[PASS] {result.Name}"
                        : $"\n[FAIL] {result.Name}: {result.Error}");
            }

            var passed =
                results.Count(x => x.Passed);

            var failed =
                results.Count - passed;

            document.Editor.WriteMessage(
                $"\n------------------------------" +
                $"\nPassed: {passed}" +
                $"\nFailed: {failed}" +
                $"\nTotal:  {results.Count}");
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

        var context =
            PluginServices.CurrentContext;

        var runner =
            new GraphPersistenceScenarioRunner(
                document,
                context);

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

    private static void WriteResults(
        Editor editor,
        string title,
        IReadOnlyList<IntegrationTestResult> results)
    {
        editor.WriteMessage(
            $"\n=== {title} ===");

        foreach (var result in results)
        {
            editor.WriteMessage(
                result.Passed
                    ? $"\n[PASS] {result.Name}"
                    : $"\n[FAIL] {result.Name}: {result.Error}");
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
    }

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
            WriteResults(
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
            WriteResults(
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
            WriteResults(
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
            WriteResults(
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

    [CommandMethod("GRAPH_PREPARE_ATTACHMENT_PERSISTENCE_TEST")]
    public void PrepareAttachmentPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var context = PluginServices.CurrentContext;

        var editor =
            document.Editor;

        var manifestStore = new AttachmentPersistenceTestManifestStore();

        using (var transaction =
               document.Database
                   .TransactionManager
                   .StartTransaction())
        {
            var existing =
                manifestStore.Read(
                    document.Database,
                    transaction);

            if (existing is not null)
            {
                throw new InvalidOperationException(
                    "An attachment persistence test is already prepared. " +
                    "Run GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST " +
                    "or clear the previous test first.");
            }
        }

        try
        {
            var vertexService =
                new VertexService(
                    context.Vertices);

            var vertex =
                vertexService.CreateVertex(
                    new Point2(
                        3200,
                        3200));

            var paths =
                new[]
                {
                @"Documents\attachment-persist-a.pdf",
                @"Images\attachment-persist-b.jpg"
                };

            foreach (var path in paths)
            {
                context.Attachments.Add(
                    vertex.Id,
                    new VertexAttachment(
                        path));
            }

            //
            // Проверяем состояние ДО SAVE,
            // чтобы не записывать заведомо
            // некорректный тестовый fixture.
            //
            var beforeSave =
                context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            EnsureAttachmentPaths(
                beforeSave,
                paths,
                "Prepare");

            var manifest =
                new AttachmentPersistenceTestManifest(
                    vertex.Id,
                    paths);

            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifestStore.Write(
                    document.Database,
                    transaction,
                    manifest);

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nAttachment persistence test prepared.");

            editor.WriteMessage(
                $"\nVertexId: {vertex.Id}");

            editor.WriteMessage(
                "\nNow SAVE the drawing, CLOSE it, OPEN it again, " +
                "then run GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST.");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\nAttachment persistence prepare FAILED:\n{ex}");
        }
    }

    [CommandMethod(
    "GRAPH_VERIFY_ATTACHMENT_PERSISTENCE_TEST")]
    public void VerifyAttachmentPersistenceTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var context = PluginServices.CurrentContext;

        var editor =
            document.Editor;

        var manifestStore =
            new AttachmentPersistenceTestManifestStore();

        AttachmentPersistenceTestManifest manifest;

        try
        {
            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifest =
                    manifestStore.Read(
                        document.Database,
                        transaction)
                    ?? throw new InvalidOperationException(
                        "Attachment persistence test manifest " +
                        "was not found.");

                transaction.Commit();
            }

            //
            // 1. Vertex должен восстановиться
            // через обычный context/index.
            //
            var vertex =
                context.Vertices.Get(
                    manifest.VertexId);

            if (vertex is null)
            {
                throw new IntegrationTestException(
                    $"Vertex '{manifest.VertexId}' " +
                    "was not restored after reopening DWG.");
            }

            //
            // 2. Index тоже обязан знать его.
            //
            if (!context.Index.TryGetVertexObjectId(
                    manifest.VertexId,
                    out var objectId))
            {
                throw new IntegrationTestException(
                    "Restored attachment test vertex " +
                    "is missing from GraphEntityIndex.");
            }

            //
            // 3. Читаем attachment XRecord через
            // настоящий repository.
            //
            var restoredAttachments =
                context.Attachments
                    .GetAll(
                        manifest.VertexId)
                    .ToArray();

            EnsureAttachmentPaths(
                restoredAttachments,
                manifest.Paths,
                "Verify");

            //
            // 4. Дополнительно проверяем,
            // что XRecord физически находится
            // на восстановленной Vertex entity.
            //
            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var entity =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Entity
                    ?? throw new IntegrationTestException(
                        "Restored vertex ObjectId does not point " +
                        "to an Entity.");

                if (entity.ExtensionDictionary.IsNull)
                {
                    throw new IntegrationTestException(
                        "Restored vertex has no ExtensionDictionary.");
                }

                var dictionary =
                    transaction.GetObject(
                        entity.ExtensionDictionary,
                        OpenMode.ForRead)
                    as DBDictionary
                    ?? throw new IntegrationTestException(
                        "Restored vertex ExtensionDictionary " +
                        "could not be opened.");

                if (!dictionary.Contains(
                        VertexAttachmentXRecordStore.RecordKey))
                {
                    throw new IntegrationTestException(
                        $"'{VertexAttachmentXRecordStore.RecordKey}' " +
                        "was not persisted in DWG.");
                }
            }

            editor.WriteMessage(
                "\n[PASS] Attachment persistence after SAVE/CLOSE/OPEN.");

            editor.WriteMessage(
                $"\nVertexId preserved: {manifest.VertexId}");

            editor.WriteMessage(
                $"\nAttachments restored: " +
                $"{restoredAttachments.Length}");

            //
            // Тест успешен — очищаем fixture.
            //
            CleanupAttachmentPersistenceTest(
                context,
                document,
                manifest,
                manifestStore);

            editor.WriteMessage(
                "\nAttachment persistence test data cleared.");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] Attachment persistence test:\n{ex}");
        }
    }

    private static void EnsureAttachmentPaths(
        IReadOnlyCollection<VertexAttachment> actual,
        IReadOnlyCollection<string> expected,
        string stage)
    {
        if (actual.Count !=
            expected.Count)
        {
            throw new IntegrationTestException(
                $"{stage}: expected {expected.Count} attachments, " +
                $"actual {actual.Count}.");
        }

        var actualPaths =
            actual
                .Select(
                    x => x.Path)
                .ToArray();

        foreach (var expectedPath in
                 expected)
        {
            var exists =
                actualPaths.Any(
                    actualPath =>
                        string.Equals(
                            actualPath,
                            expectedPath,
                            StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                throw new IntegrationTestException(
                    $"{stage}: attachment '{expectedPath}' " +
                    "was not found.");
            }
        }
    }

    private static void CleanupAttachmentPersistenceTest(
        GraphDocumentContext context,
        Document document,
        AttachmentPersistenceTestManifest manifest,
        AttachmentPersistenceTestManifestStore manifestStore)
    {
        //
        // Сначала удаляем тестовый Vertex.
        //
        // Его ExtensionDictionary вместе с
        // GRAPH_VERTEX_ATTACHMENTS уйдёт
        // вместе с entity.
        //
        var graphService =
            new GraphService(
                context.Vertices,
                context.Edges);

        if (context.Vertices.Get(
                manifest.VertexId) is not null)
        {
            graphService.DeleteVertex(
                manifest.VertexId);
        }

        //
        // Затем убираем NOD manifest.
        //
        using var transaction =
            document.Database
                .TransactionManager
                .StartTransaction();

        manifestStore.Delete(
            document.Database,
            transaction);

        transaction.Commit();
    }

    private sealed record AttachmentUndoTestState(
        Document Document,
        Guid VertexId,
        string AttachmentPath);

    private static AttachmentUndoTestState? _attachmentUndoTestState;

    [CommandMethod(
    "GRAPH_PREPARE_ATTACHMENT_UNDO_TEST")]
    public void PrepareAttachmentUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        try
        {
            if (_attachmentUndoTestState is not null)
            {
                throw new InvalidOperationException(
                    "Attachment undo test is already active.");
            }

            var context = PluginServices.CurrentContext;

            var vertexService =
                new VertexService(
                    context.Vertices);

            var vertex =
                vertexService.CreateVertex(
                    new Point2(
                        3500,
                        3500));

            const string attachmentPath =
                @"UndoTest\attachment.pdf";

            context.Attachments.Add(
                vertex.Id,
                new VertexAttachment(
                    attachmentPath));

            //
            // Проверяем fixture перед переходом
            // к следующей команде.
            //
            var attachments =
                context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            if (attachments.Length != 1)
            {
                throw new IntegrationTestException(
                    $"Prepare: expected one attachment, " +
                    $"actual {attachments.Length}.");
            }

            if (!string.Equals(
                    attachments[0].Path,
                    attachmentPath,
                    StringComparison.Ordinal))
            {
                throw new IntegrationTestException(
                    "Prepare: attachment path is incorrect.");
            }

            _attachmentUndoTestState =
                new AttachmentUndoTestState(
                    document,
                    vertex.Id,
                    attachmentPath);

            editor.WriteMessage(
                "\nAttachment UNDO test prepared.");

            editor.WriteMessage(
                $"\nVertexId: {vertex.Id}");

            editor.WriteMessage(
                "\nNow run GRAPH_ERASE_ATTACHMENT_UNDO_TEST.");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] Attachment UNDO prepare:\n{ex}");
        }
    }

    [CommandMethod("GRAPH_ERASE_ATTACHMENT_UNDO_TEST")]
    public void EraseAttachmentUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        try
        {
            var state =
                _attachmentUndoTestState
                ?? throw new InvalidOperationException(
                    "Attachment UNDO test was not prepared.");

            if (!ReferenceEquals(
                    state.Document,
                    document))
            {
                throw new InvalidOperationException(
                    "Attachment UNDO test belongs to another document.");
            }

            var context = PluginServices.CurrentContext;

            var vertex =
                context.Vertices.Get(
                    state.VertexId);

            if (vertex is null)
            {
                throw new IntegrationTestException(
                    "Test vertex does not exist before erase.");
            }

            var graphService =
                new GraphService(
                    context.Vertices,
                    context.Edges);

            graphService.DeleteVertex(
                state.VertexId);

            //
            // Repository/index должны обновиться
            // синхронно, не ожидая CommandEnded.
            //
            if (context.Vertices.Get(
                    state.VertexId) is not null)
            {
                throw new IntegrationTestException(
                    "Vertex still exists after delete.");
            }

            if (context.Index.TryGetVertexObjectId(
                    state.VertexId,
                    out _))
            {
                throw new IntegrationTestException(
                    "Deleted vertex still exists in index.");
            }

            editor.WriteMessage(
                "\nTest vertex erased successfully.");

            editor.WriteMessage(
                "\nNow press Ctrl+Z ONCE.");

            editor.WriteMessage(
                "\nAfter UNDO finishes, run " +
                "GRAPH_VERIFY_ATTACHMENT_UNDO_TEST.");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] Attachment UNDO erase:\n{ex}");
        }
    }

    [CommandMethod(
    "GRAPH_VERIFY_ATTACHMENT_UNDO_TEST")]
    public void VerifyAttachmentUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        try
        {
            var state =
                _attachmentUndoTestState
                ?? throw new InvalidOperationException(
                    "Attachment UNDO test was not prepared.");

            if (!ReferenceEquals(
                    state.Document,
                    document))
            {
                throw new InvalidOperationException(
                    "Attachment UNDO test belongs to another document.");
            }

            var context = PluginServices.CurrentContext;

            //
            // 1. Vertex восстановлен.
            //
            var vertex =
                context.Vertices.Get(
                    state.VertexId);

            if (vertex is null)
            {
                throw new IntegrationTestException(
                    "Vertex was not restored by UNDO.");
            }

            //
            // 2. Index восстановлен.
            //
            if (!context.Index.TryGetVertexObjectId(
                    state.VertexId,
                    out var objectId))
            {
                throw new IntegrationTestException(
                    "Restored vertex is missing from GraphEntityIndex.");
            }

            //
            // 3. Attachment repository должен увидеть
            // восстановленный XRecord.
            //
            var attachments =
                context.Attachments
                    .GetAll(
                        state.VertexId)
                    .ToArray();

            if (attachments.Length != 1)
            {
                throw new IntegrationTestException(
                    $"Expected one restored attachment, " +
                    $"actual {attachments.Length}.");
            }

            if (!string.Equals(
                    attachments[0].Path,
                    state.AttachmentPath,
                    StringComparison.Ordinal))
            {
                throw new IntegrationTestException(
                    $"Restored attachment path is incorrect. " +
                    $"Actual: '{attachments[0].Path}', " +
                    $"expected: '{state.AttachmentPath}'.");
            }

            //
            // 4. Проверяем физическую структуру DWG.
            //
            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var entity =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Entity
                    ?? throw new IntegrationTestException(
                        "Restored ObjectId is not an Entity.");

                if (entity.ExtensionDictionary.IsNull)
                {
                    throw new IntegrationTestException(
                        "Restored vertex has no ExtensionDictionary.");
                }

                var dictionary =
                    transaction.GetObject(
                        entity.ExtensionDictionary,
                        OpenMode.ForRead)
                    as DBDictionary
                    ?? throw new IntegrationTestException(
                        "Restored ExtensionDictionary could not be opened.");

                if (!dictionary.Contains(
                        VertexAttachmentXRecordStore.RecordKey))
                {
                    throw new IntegrationTestException(
                        $"'{VertexAttachmentXRecordStore.RecordKey}' " +
                        "was not restored by UNDO.");
                }
            }

            editor.WriteMessage(
                "\n[PASS] Vertex ERASE -> UNDO restored attachment.");

            editor.WriteMessage(
                $"\nVertexId: {state.VertexId}");

            editor.WriteMessage(
                $"\nAttachment: {attachments[0].Path}");

            //
            // Cleanup.
            //
            var graphService =
                new GraphService(
                    context.Vertices,
                    context.Edges);

            graphService.DeleteVertex(
                state.VertexId);

            _attachmentUndoTestState =
                null;

            editor.WriteMessage(
                "\nAttachment UNDO test data cleared.");
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] Attachment UNDO verify:\n{ex}");
        }
    }

    [CommandMethod(
    "GRAPH_VERIFY_CPP_STYLE_INTEROP_TEST")]
    public void VerifyCppStyleInteropTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try 
        {
            var runtime = 
                new GraphIntegrationTestRunner
                    (document,
                    context);

            runtime.VerifyCppStyleInteropTest();
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] C++ style interop test:\n{ex}");
        }
    }

    [CommandMethod(
    "GRAPH_VERIFY_CPP_DELETE_UNDO_TEST")]
    public void VerifyCppDeleteUndoTest()
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var editor =
            document.Editor;

        var context = PluginServices.CurrentContext;

        try
        {
            var runtime =
                new GraphIntegrationTestRunner
                    (document,
                    context);

            runtime.VerifyCppDeleteUndoTest();
        }
        catch (System.Exception ex)
        {
            editor.WriteMessage(
                $"\n[FAIL] C++ delete undo test:\n{ex}");
        }
    }
}