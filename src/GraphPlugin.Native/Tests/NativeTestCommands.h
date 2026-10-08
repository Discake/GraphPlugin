#pragma once

namespace GraphPlugin::Native::Tests
{
    public ref class NativeTestCommands
    {
    public:
        static void GraphCppRunTests();

        static void GraphCppPrepareStyleInteropTest();

        static void GraphCppExecuteStyleInteropTest();

        static void GraphCppPrepareDeleteUndoTest();

        static void GraphCppExecuteDeleteUndoTest();
    };
}
