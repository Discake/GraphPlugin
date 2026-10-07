#pragma once

using namespace System;

namespace GraphPlugin::Native::Commands
{
    public ref class GraphCommands
    {
    public:
        static void GraphCppInfo();

        static void GraphCppVertex();

        static void GraphCppVertexInfo();

        static void GraphCppVertexStyle();

        static void GraphCppDeleteVertex();

        static void GraphCppRunTests();

        static void GraphCppPrepareStyleInteropTest();

        static void GraphCppExecuteStyleInteropTest();

        static void GraphCppPrepareDeleteUndoTest();

        static void GraphCppExecuteDeleteUndoTest();
    };
}