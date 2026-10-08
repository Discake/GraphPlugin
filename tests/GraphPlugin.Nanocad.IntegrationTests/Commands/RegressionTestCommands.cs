using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Runtime;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.IntegrationTests.Commands;

public sealed class RegressionTestCommands
{
    private const string ContinueCommand =
        "GRAPH_TEST_CONTINUE_INTERNAL";

    private static CurrentDocumentTestSession? _session;
    private static Document? _persistencePreparedDocument;

    private enum CurrentDocumentTestStage
    {
        EdgeErase,
        EdgeVerifyErased,
        EdgeVerifyUndo,
        VertexErase,
        VertexVerifyErased,
        VertexVerifyUndo,
        AttachmentErase,
        AttachmentVerifyErased,
        AttachmentVerifyUndo,
        AfterNativeBasic,
        NativeStyleVerify,
        NativeDeleteAfterErase,
        NativeDeleteVerify
    }

    private sealed class CurrentDocumentTestSession
    {
        public Document Document { get; }

        public List<IntegrationTestResult> Results { get; } = new();

        public CurrentDocumentTestStage Stage { get; set; }

        public CurrentDocumentTestSession(Document document)
        {
            Document = document;
        }
    }

    [CommandMethod("GRAPHTESTS")]
    public void RunCurrentDocumentTests()
    {
        var document = GetActiveDocument();
        if (document is null)
            return;

        var context = PluginServices.CurrentContext;
        var editor = document.Editor;

        try
        {
            if (HasPreparedPersistenceSuite(
                    document,
                    context))
            {
                editor.WriteMessage(
                    "\n[FAIL] Persistence regression is prepared in this DWG." +
                    "\nFinish GRAPHTESTS_PERSISTENCE before running GRAPHTESTS.");
                return;
            }

            AbortPreviousCurrentSession(
                document,
                context);

            CleanupCurrentDocumentArtifacts(
                document,
                context);

            var session =
                new CurrentDocumentTestSession(
                    document);

            _session = session;

            editor.WriteMessage(
                "\n=== GraphPlugin current-document regression ===" +
                "\nRunning core C# integration tests...");

            var coreRunner =
                new GraphIntegrationTestRunner(
                    document,
                    context);

            AddResults(
                session,
                "Core",
                coreRunner.RunAll());

            PrepareEdgeScenario(
                document,
                session);
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage(
                $"\n[FAIL] Could not start regression suite:\n{exception}");

            _session = null;
        }
    }

    [CommandMethod(ContinueCommand)]
    public void ContinueCurrentDocumentTests()
    {
        var document = GetActiveDocument();
        if (document is null)
            return;

        var session = _session;

        if (session is null ||
            !ReferenceEquals(
                session.Document,
                document))
        {
            document.Editor.WriteMessage(
                "\n[FAIL] No active GraphPlugin regression session for this document.");
            return;
        }

        try
        {
            switch (session.Stage)
            {
                case CurrentDocumentTestStage.EdgeErase:
                    RunEdgeErase(document, session);
                    break;

                case CurrentDocumentTestStage.EdgeVerifyErased:
                    RunEdgeErasedVerification(document, session);
                    break;

                case CurrentDocumentTestStage.EdgeVerifyUndo:
                    RunEdgeUndoVerification(document, session);
                    break;

                case CurrentDocumentTestStage.VertexErase:
                    RunVertexErase(document, session);
                    break;

                case CurrentDocumentTestStage.VertexVerifyErased:
                    RunVertexErasedVerification(document, session);
                    break;

                case CurrentDocumentTestStage.VertexVerifyUndo:
                    RunVertexUndoVerification(document, session);
                    break;

                case CurrentDocumentTestStage.AttachmentErase:
                    RunAttachmentErase(document, session);
                    break;

                case CurrentDocumentTestStage.AttachmentVerifyErased:
                    RunAttachmentErasedVerification(document, session);
                    break;

                case CurrentDocumentTestStage.AttachmentVerifyUndo:
                    RunAttachmentUndoVerification(document, session);
                    break;

                case CurrentDocumentTestStage.AfterNativeBasic:
                    StartNativeStyleInterop(document, session);
                    break;

                case CurrentDocumentTestStage.NativeStyleVerify:
                    VerifyNativeStyleInterop(document, session);
                    break;

                case CurrentDocumentTestStage.NativeDeleteAfterErase:
                    ContinueNativeDeleteUndo(document, session);
                    break;

                case CurrentDocumentTestStage.NativeDeleteVerify:
                    VerifyNativeDeleteUndo(document, session);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported regression stage: {session.Stage}.");
            }
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Harness",
                session.Stage.ToString(),
                exception);

            FinishCurrentDocumentSuite(
                document,
                session);
        }
    }

    [CommandMethod("GRAPHTESTS_PERSISTENCE")]
    public void RunPersistenceTests()
    {
        var document = GetActiveDocument();
        if (document is null)
            return;

        var context = PluginServices.CurrentContext;
        var editor = document.Editor;

        if (_session is not null)
        {
            editor.WriteMessage(
                "\n[FAIL] Current-document regression is still running." +
                "\nWait until GRAPHTESTS finishes.");
            return;
        }

        var graphStore =
            new PersistenceTestManifestStore();

        var graphPrepared =
            graphStore.Exists(
                document.Database);

        var attachmentRunner =
            new AttachmentPersistenceScenarioRunner(
                document,
                context);

        var attachmentPrepared =
            attachmentRunner.IsPrepared();

        if (!graphPrepared &&
            !attachmentPrepared)
        {
            PreparePersistenceSuite(
                document,
                context,
                attachmentRunner);
            return;
        }

        if (graphPrepared !=
            attachmentPrepared)
        {
            editor.WriteMessage(
                "\n[FAIL] Persistence regression state is incomplete." +
                $"\nGraph manifest: {(graphPrepared ? "present" : "missing")}." +
                $"\nAttachment manifest: {(attachmentPrepared ? "present" : "missing")}." +
                "\nThe partial test state will be cleaned. Run GRAPHTESTS_PERSISTENCE again.");

            CleanupPersistenceSuite(
                document,
                context);

            _persistencePreparedDocument = null;
            return;
        }

        if (ReferenceEquals(
                _persistencePreparedDocument,
                document))
        {
            editor.WriteMessage(
                "\n[WAIT] Persistence regression is prepared, but this is still " +
                "the same nanoCAD Document instance." +
                "\nSAVE the DWG, CLOSE it, OPEN it again, then run " +
                "GRAPHTESTS_PERSISTENCE once more.");
            return;
        }

        VerifyPersistenceSuite(
            document,
            context,
            attachmentRunner);
    }

    private static void PrepareEdgeScenario(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.PrepareEdge();

            AddPass(
                session,
                "Edge ERASE/UNDO",
                "Prepare");

            session.Stage =
                CurrentDocumentTestStage.EdgeErase;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Edge ERASE/UNDO",
                "Prepare",
                exception);

            SafeClearUndoScenario(
                document,
                session,
                "Edge ERASE/UNDO");

            PrepareVertexScenario(
                document,
                session);
        }
    }

    private static void RunEdgeErase(
        Document document,
        CurrentDocumentTestSession session)
    {
        try
        {
            ErasePreparedUndoTarget(
                document,
                UndoTestScenario.Edge);

            AddPass(
                session,
                "Edge ERASE/UNDO",
                "Erase prepared edge");

            session.Stage =
                CurrentDocumentTestStage.EdgeVerifyErased;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Edge ERASE/UNDO",
                "Erase prepared edge",
                exception);

            SafeClearUndoScenario(
                document,
                session,
                "Edge ERASE/UNDO");

            PrepareVertexScenario(
                document,
                session);
        }
    }

    private static void RunEdgeErasedVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Edge erased",
                runner.VerifyEdgeErased());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Edge erased",
                "Verification",
                exception);
        }

        session.Stage =
            CurrentDocumentTestStage.EdgeVerifyUndo;

        QueueUndoAndContinue(document);
    }

    private static void RunEdgeUndoVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Edge UNDO",
                runner.VerifyEdgeUndo());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Edge UNDO",
                "Verification",
                exception);
        }

        SafeClearUndoScenario(
            document,
            session,
            "Edge ERASE/UNDO");

        PrepareVertexScenario(
            document,
            session);
    }

    private static void PrepareVertexScenario(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.PrepareVertex();

            AddPass(
                session,
                "Vertex cascade ERASE/UNDO",
                "Prepare");

            session.Stage =
                CurrentDocumentTestStage.VertexErase;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Vertex cascade ERASE/UNDO",
                "Prepare",
                exception);

            SafeClearUndoScenario(
                document,
                session,
                "Vertex cascade ERASE/UNDO");

            PrepareAttachmentScenario(
                document,
                session);
        }
    }

    private static void RunVertexErase(
        Document document,
        CurrentDocumentTestSession session)
    {
        try
        {
            ErasePreparedUndoTarget(
                document,
                UndoTestScenario.Vertex);

            AddPass(
                session,
                "Vertex cascade ERASE/UNDO",
                "Erase middle vertex");

            session.Stage =
                CurrentDocumentTestStage.VertexVerifyErased;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Vertex cascade ERASE/UNDO",
                "Erase middle vertex",
                exception);

            SafeClearUndoScenario(
                document,
                session,
                "Vertex cascade ERASE/UNDO");

            PrepareAttachmentScenario(
                document,
                session);
        }
    }

    private static void RunVertexErasedVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Vertex cascade erased",
                runner.VerifyVertexErased());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Vertex cascade erased",
                "Verification",
                exception);
        }

        session.Stage =
            CurrentDocumentTestStage.VertexVerifyUndo;

        QueueUndoAndContinue(document);
    }

    private static void RunVertexUndoVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new GraphUndoIntegrationTestRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Vertex cascade UNDO",
                runner.VerifyVertexUndo());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Vertex cascade UNDO",
                "Verification",
                exception);
        }

        SafeClearUndoScenario(
            document,
            session,
            "Vertex cascade ERASE/UNDO");

        PrepareAttachmentScenario(
            document,
            session);
    }

    private static void PrepareAttachmentScenario(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new AttachmentUndoScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.Prepare();

            AddPass(
                session,
                "Attachment ERASE/UNDO",
                "Prepare");

            session.Stage =
                CurrentDocumentTestStage.AttachmentErase;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Attachment ERASE/UNDO",
                "Prepare",
                exception);

            SafeClearAttachmentUndo(
                document,
                session);

            StartNativeSuiteOrFinish(
                document,
                session);
        }
    }

    private static void RunAttachmentErase(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new AttachmentUndoScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            runner.ErasePreparedVertex();

            AddPass(
                session,
                "Attachment ERASE/UNDO",
                "Erase prepared vertex");

            session.Stage =
                CurrentDocumentTestStage.AttachmentVerifyErased;

            QueueContinue(document);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Attachment ERASE/UNDO",
                "Erase prepared vertex",
                exception);

            SafeClearAttachmentUndo(
                document,
                session);

            StartNativeSuiteOrFinish(
                document,
                session);
        }
    }

    private static void RunAttachmentErasedVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new AttachmentUndoScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Attachment erased",
                runner.VerifyErased());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Attachment erased",
                "Verification",
                exception);
        }

        session.Stage =
            CurrentDocumentTestStage.AttachmentVerifyUndo;

        QueueUndoAndContinue(document);
    }

    private static void RunAttachmentUndoVerification(
        Document document,
        CurrentDocumentTestSession session)
    {
        var runner =
            new AttachmentUndoScenarioRunner(
                document,
                PluginServices.CurrentContext);

        try
        {
            AddResults(
                session,
                "Attachment UNDO",
                runner.VerifyUndo());
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Attachment UNDO",
                "Verification",
                exception);
        }

        SafeClearAttachmentUndo(
            document,
            session);

        StartNativeSuiteOrFinish(
            document,
            session);
    }

    private static void StartNativeSuiteOrFinish(
        Document document,
        CurrentDocumentTestSession session)
    {
        if (!IsNativePluginLoaded())
        {
            AddFailure(
                session,
                "Native",
                "Plugin load",
                new InvalidOperationException(
                    "GraphPlugin.Native.dll is not loaded. C++ tests were not run."));

            FinishCurrentDocumentSuite(
                document,
                session);
            return;
        }

        try
        {
            CleanupNativeInteropArtifacts(
                document,
                PluginServices.CurrentContext);
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Native interop",
                "Cleanup stale state",
                exception);
        }

        session.Stage =
            CurrentDocumentTestStage.AfterNativeBasic;

        document.Editor.WriteMessage(
            "\n[RUN] Native C++ integration tests...");

        Queue(
            document,
            $"GRAPHCPPRUNTESTS {ContinueCommand}");
    }

    private static void StartNativeStyleInterop(
        Document document,
        CurrentDocumentTestSession session)
    {
        document.Editor.WriteMessage(
            "\n[INFO] GRAPHCPPRUNTESTS writes its own C++ summary above." +
            "\n[RUN] C++ -> C# style interop...");

        session.Stage =
            CurrentDocumentTestStage.NativeStyleVerify;

        Queue(
            document,
            "GRAPHCPP_PREPARE_STYLE_INTEROP_TEST " +
            "GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST " +
            ContinueCommand);
    }

    private static void VerifyNativeStyleInterop(
        Document document,
        CurrentDocumentTestSession session)
    {
        try
        {
            new GraphIntegrationTestRunner(
                    document,
                    PluginServices.CurrentContext)
                .VerifyCppStyleInteropTest();

            AddPass(
                session,
                "Native interop",
                "C++ style replacement -> C# verification");
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Native interop",
                "C++ style replacement -> C# verification",
                exception);
        }

        session.Stage =
            CurrentDocumentTestStage.NativeDeleteAfterErase;

        document.Editor.WriteMessage(
            "\n[RUN] C++ cascade delete -> nanoCAD UNDO -> C# restore...");

        Queue(
            document,
            "GRAPHCPP_PREPARE_DELETE_UNDO_TEST " +
            "GRAPHCPP_EXECUTE_DELETE_UNDO_TEST " +
            ContinueCommand);
    }

    private static void ContinueNativeDeleteUndo(
        Document document,
        CurrentDocumentTestSession session)
    {
        var manifest =
            ReadCppDeleteUndoManifest(
                document.Database);

        if (manifest is null)
        {
            AddFailure(
                session,
                "Native interop",
                "C++ cascade delete",
                new InvalidOperationException(
                    "C++ delete/undo manifest was not created."));

            FinishCurrentDocumentSuite(
                document,
                session);
            return;
        }

        var context = PluginServices.CurrentContext;

        if (context.Vertices.Get(
                manifest.VertexAId) is not null)
        {
            AddFailure(
                session,
                "Native interop",
                "C++ cascade delete",
                new InvalidOperationException(
                    "C++ delete command did not remove Vertex A."));

            CleanupNativeInteropArtifacts(
                document,
                context);

            FinishCurrentDocumentSuite(
                document,
                session);
            return;
        }

        AddPass(
            session,
            "Native interop",
            "C++ cascade delete");

        session.Stage =
            CurrentDocumentTestStage.NativeDeleteVerify;

        QueueUndoAndContinue(document);
    }

    private static void VerifyNativeDeleteUndo(
        Document document,
        CurrentDocumentTestSession session)
    {
        new GraphIntegrationTestRunner(
                document,
                PluginServices.CurrentContext)
            .VerifyCppDeleteUndoTest();

        var manifest =
            ReadCppDeleteUndoManifest(
                document.Database);

        if (manifest is null)
        {
            AddPass(
                session,
                "Native interop",
                "C++ delete -> UNDO -> C# restore");
        }
        else
        {
            AddFailure(
                session,
                "Native interop",
                "C++ delete -> UNDO -> C# restore",
                new InvalidOperationException(
                    "C++ delete/undo manifest remains after verification. " +
                    "See the preceding C++/C# test output for details."));
        }

        CleanupNativeInteropArtifacts(
            document,
            PluginServices.CurrentContext);

        FinishCurrentDocumentSuite(
            document,
            session);
    }

    private static void PreparePersistenceSuite(
        Document document,
        GraphDocumentContext context,
        AttachmentPersistenceScenarioRunner attachmentRunner)
    {
        var editor = document.Editor;
        var graphRunner =
            new GraphPersistenceScenarioRunner(
                document,
                context);

        try
        {
            var graphManifest =
                graphRunner.Prepare();

            try
            {
                var attachmentManifest =
                    attachmentRunner.Prepare();

                _persistencePreparedDocument =
                    document;

                editor.WriteMessage(
                    "\n=== GraphPlugin persistence regression prepared ===" +
                    $"\nGraph TestId: {graphManifest.TestId}" +
                    $"\nAttachment VertexId: {attachmentManifest.VertexId}" +
                    "\n" +
                    "\nNext:" +
                    "\n1. SAVE the DWG" +
                    "\n2. CLOSE the DWG" +
                    "\n3. OPEN the same DWG" +
                    "\n4. Run GRAPHTESTS_PERSISTENCE again");
            }
            catch
            {
                graphRunner.Clear();
                throw;
            }
        }
        catch (System.Exception exception)
        {
            try
            {
                attachmentRunner.Clear();
            }
            catch
            {
                // Keep the original prepare exception.
            }

            editor.WriteMessage(
                $"\n[FAIL] Persistence prepare:\n{exception}");
        }
    }

    private static void VerifyPersistenceSuite(
        Document document,
        GraphDocumentContext context,
        AttachmentPersistenceScenarioRunner attachmentRunner)
    {
        var results =
            new List<IntegrationTestResult>();

        var graphRunner =
            new GraphPersistenceScenarioRunner(
                document,
                context);

        try
        {
            AddResults(
                results,
                "DWG graph",
                graphRunner.Verify());
        }
        catch (System.Exception exception)
        {
            results.Add(
                new IntegrationTestResult(
                    "[DWG graph] Verification",
                    false,
                    exception.Message));
        }

        try
        {
            AddResults(
                results,
                "DWG attachments",
                attachmentRunner.Verify());
        }
        catch (System.Exception exception)
        {
            results.Add(
                new IntegrationTestResult(
                    "[DWG attachments] Verification",
                    false,
                    exception.Message));
        }

        IntegrationTestCommandOutput.WriteResults(
            document.Editor,
            "GraphPlugin persistence regression",
            results);

        try
        {
            graphRunner.Clear();
            attachmentRunner.Clear();

            document.Editor.WriteMessage(
                "\nPersistence test objects were removed from the current document.");
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage(
                $"\n[WARN] Persistence cleanup failed: {exception.Message}");
        }
        finally
        {
            _persistencePreparedDocument = null;
        }
    }

    private static void FinishCurrentDocumentSuite(
        Document document,
        CurrentDocumentTestSession session)
    {
        if (!ReferenceEquals(
                _session,
                session))
        {
            return;
        }

        IntegrationTestCommandOutput.WriteResults(
            document.Editor,
            "GraphPlugin current-document regression",
            session.Results);

        if (IsNativePluginLoaded())
        {
            document.Editor.WriteMessage(
                "\nNote: GRAPHCPPRUNTESTS has its own detailed summary above; " +
                "its internal count is not duplicated in the C# total.");
        }

        _session = null;
    }

    private static void AbortPreviousCurrentSession(
        Document document,
        GraphDocumentContext context)
    {
        var previous = _session;
        _session = null;

        if (previous is null ||
            !ReferenceEquals(
                previous.Document,
                document))
        {
            return;
        }

        CleanupCurrentDocumentArtifacts(
            document,
            context);

        document.Editor.WriteMessage(
            "\n[INFO] Previous incomplete regression session was reset.");
    }

    private static void CleanupCurrentDocumentArtifacts(
        Document document,
        GraphDocumentContext context)
    {
        new GraphUndoIntegrationTestRunner(
                document,
                context)
            .Clear();

        new AttachmentUndoScenarioRunner(
                document,
                context)
            .Clear();

        CleanupNativeInteropArtifacts(
            document,
            context);
    }

    private static void CleanupPersistenceSuite(
        Document document,
        GraphDocumentContext context)
    {
        try
        {
            new GraphPersistenceScenarioRunner(
                    document,
                    context)
                .Clear();
        }
        catch
        {
            // Continue with the attachment cleanup.
        }

        try
        {
            new AttachmentPersistenceScenarioRunner(
                    document,
                    context)
                .Clear();
        }
        catch
        {
            // The caller already reports the inconsistent state.
        }
    }

    private static void SafeClearUndoScenario(
        Document document,
        CurrentDocumentTestSession session,
        string section)
    {
        try
        {
            new GraphUndoIntegrationTestRunner(
                    document,
                    PluginServices.CurrentContext)
                .Clear();
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                section,
                "Cleanup",
                exception);
        }
    }

    private static void SafeClearAttachmentUndo(
        Document document,
        CurrentDocumentTestSession session)
    {
        try
        {
            new AttachmentUndoScenarioRunner(
                    document,
                    PluginServices.CurrentContext)
                .Clear();
        }
        catch (System.Exception exception)
        {
            AddFailure(
                session,
                "Attachment ERASE/UNDO",
                "Cleanup",
                exception);
        }
    }

    private static void ErasePreparedUndoTarget(
        Document document,
        UndoTestScenario scenario)
    {
        var manifest =
            new UndoTestManifestStore()
                .Load(document.Database)
            ?? throw new InvalidOperationException(
                "Undo integration test manifest was not found.");

        if (manifest.Scenario != scenario)
        {
            throw new InvalidOperationException(
                $"Prepared undo scenario is {manifest.Scenario}, " +
                $"but {scenario} was expected.");
        }

        var context = PluginServices.CurrentContext;

        ObjectId objectId;
        string description;

        if (scenario == UndoTestScenario.Edge)
        {
            if (!context.Index.TryGetEdgeObjectId(
                    manifest.EdgeABId,
                    out objectId))
            {
                throw new InvalidOperationException(
                    "Prepared edge is missing from GraphEntityIndex.");
            }

            description = "prepared edge";
        }
        else
        {
            if (!context.Index.TryGetVertexObjectId(
                    manifest.VertexBId,
                    out objectId))
            {
                throw new InvalidOperationException(
                    "Prepared middle vertex is missing from GraphEntityIndex.");
            }

            description = "prepared middle vertex";
        }

        if (objectId.IsNull ||
            objectId.IsErased)
        {
            throw new InvalidOperationException(
                $"The {description} is already erased or invalid.");
        }

        using var transaction =
            document.Database
                .TransactionManager
                .StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForWrite) as Entity
            ?? throw new InvalidOperationException(
                $"The {description} ObjectId is not an Entity.");

        entity.Erase();
        transaction.Commit();
    }

    private static bool HasPreparedPersistenceSuite(
        Document document,
        GraphDocumentContext context)
    {
        var graphPrepared =
            new PersistenceTestManifestStore()
                .Exists(document.Database);

        var attachmentPrepared =
            new AttachmentPersistenceScenarioRunner(
                    document,
                    context)
                .IsPrepared();

        return graphPrepared ||
               attachmentPrepared;
    }

    private static void CleanupNativeInteropArtifacts(
        Document document,
        GraphDocumentContext context)
    {
        var styleStore =
            new CppStyleInteropManifestStore();

        try
        {
            CppStyleInteropManifest? manifest = null;

            using (var transaction =
                   document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                try
                {
                    manifest =
                        styleStore.ReadStyleManifest(
                            document.Database,
                            transaction);
                }
                catch (IntegrationTestException)
                {
                    // No style manifest is a clean state.
                }
            }

            if (manifest is not null &&
                context.Vertices.Get(
                    manifest.VertexId) is not null)
            {
                context.Graph.DeleteVertex(
                    manifest.VertexId);
            }
        }
        finally
        {
            styleStore.Delete(
                document.Database);
        }

        var deleteStore =
            new CppDeleteUndoTestManifestStore();

        var deleteManifest =
            ReadCppDeleteUndoManifest(
                document.Database);

        if (deleteManifest is not null)
        {
            DeleteVertexIfExists(
                context,
                deleteManifest.VertexAId);

            DeleteVertexIfExists(
                context,
                deleteManifest.VertexBId);

            DeleteVertexIfExists(
                context,
                deleteManifest.VertexCId);
        }

        using var deleteTransaction =
            document.Database
                .TransactionManager
                .StartTransaction();

        deleteStore.Delete(
            document.Database,
            deleteTransaction);

        deleteTransaction.Commit();
    }

    private static CppDeleteUndoTestManifest? ReadCppDeleteUndoManifest(
        Database database)
    {
        using var transaction =
            database.TransactionManager
                .StartTransaction();

        return new CppDeleteUndoTestManifestStore()
            .Read(
                database,
                transaction);
    }

    private static void DeleteVertexIfExists(
        GraphDocumentContext context,
        Guid vertexId)
    {
        if (context.Vertices.Get(vertexId) is not null)
        {
            context.Graph.DeleteVertex(vertexId);
        }
    }

    private static bool IsNativePluginLoaded() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(
                assembly =>
                    string.Equals(
                        assembly.GetName().Name,
                        "GraphPlugin.Native",
                        StringComparison.OrdinalIgnoreCase));

    private static Document? GetActiveDocument() =>
        NanoApplication
            .DocumentManager
            .MdiActiveDocument;

    private static void QueueContinue(
        Document document)
    {
        Queue(
            document,
            ContinueCommand);
    }

    private static void QueueUndoAndContinue(
        Document document)
    {
        Queue(
            document,
            $"UNDO 1 {ContinueCommand}");
    }

    private static void Queue(
        Document document,
        string commands)
    {
        document.SendStringToExecute(
            commands.TrimEnd() + " ",
            true,
            false,
            false);
    }

    private static void AddPass(
        CurrentDocumentTestSession session,
        string section,
        string name)
    {
        session.Results.Add(
            new IntegrationTestResult(
                $"[{section}] {name}",
                true));
    }

    private static void AddFailure(
        CurrentDocumentTestSession session,
        string section,
        string name,
        System.Exception exception)
    {
        session.Results.Add(
            new IntegrationTestResult(
                $"[{section}] {name}",
                false,
                exception.Message));
    }

    private static void AddResults(
        CurrentDocumentTestSession session,
        string section,
        IReadOnlyList<IntegrationTestResult> results)
    {
        AddResults(
            session.Results,
            section,
            results);
    }

    private static void AddResults(
        ICollection<IntegrationTestResult> target,
        string section,
        IReadOnlyList<IntegrationTestResult> results)
    {
        foreach (var result in results)
        {
            var name = result.Name;
            var error = result.Error;

            if (name.StartsWith(
                    "[PASS] ",
                    StringComparison.Ordinal))
            {
                name = name[7..];
            }
            else if (name.StartsWith(
                         "[FAIL] ",
                         StringComparison.Ordinal))
            {
                name = name[7..];

                var separator =
                    name.IndexOf(
                        Environment.NewLine,
                        StringComparison.Ordinal);

                if (separator >= 0)
                {
                    error ??=
                        name[(separator + Environment.NewLine.Length)..];

                    name = name[..separator];
                }
            }

            target.Add(
                new IntegrationTestResult(
                    $"[{section}] {name}",
                    result.Passed,
                    error));
        }
    }
}
