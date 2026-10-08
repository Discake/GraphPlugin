#include "NativeTestCommands.h"

#include "DeleteUndoScenario.h"
#include "NativeIntegrationTestRunner.h"
#include "StyleInteropScenario.h"

using namespace HostMgd::ApplicationServices;
using namespace Teigha::Runtime;

namespace GraphPlugin::Native::Tests
{
    namespace
    {
        Document^ CurrentDocument()
        {
            return Application::DocumentManager
                ->MdiActiveDocument;
        }
    }

    [CommandMethod("GRAPHCPPRUNTESTS")]
    void NativeTestCommands::GraphCppRunTests()
    {
        Document^ document = CurrentDocument();

        if (document == nullptr)
            return;

        NativeIntegrationTestRunner^ runner =
            gcnew NativeIntegrationTestRunner(document);

        runner->RunAll();
    }

    [CommandMethod("GRAPHCPP_PREPARE_STYLE_INTEROP_TEST")]
    void NativeTestCommands::GraphCppPrepareStyleInteropTest()
    {
        Document^ document = CurrentDocument();

        if (document == nullptr)
            return;

        StyleInteropScenario::Prepare(document);
    }

    [CommandMethod("GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST")]
    void NativeTestCommands::GraphCppExecuteStyleInteropTest()
    {
        Document^ document = CurrentDocument();

        if (document == nullptr)
            return;

        StyleInteropScenario::Execute(document);
    }

    [CommandMethod("GRAPHCPP_PREPARE_DELETE_UNDO_TEST")]
    void NativeTestCommands::GraphCppPrepareDeleteUndoTest()
    {
        Document^ document = CurrentDocument();

        if (document == nullptr)
            return;

        DeleteUndoScenario::Prepare(document);
    }

    [CommandMethod("GRAPHCPP_EXECUTE_DELETE_UNDO_TEST")]
    void NativeTestCommands::GraphCppExecuteDeleteUndoTest()
    {
        Document^ document = CurrentDocument();

        if (document == nullptr)
            return;

        DeleteUndoScenario::Execute(document);
    }
}
