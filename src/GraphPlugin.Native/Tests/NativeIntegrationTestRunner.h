#pragma once

using namespace System;
using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;

namespace GraphPlugin::Native::Tests
{
    public ref class NativeIntegrationTestRunner sealed
    {
    public:
        explicit NativeIntegrationTestRunner(
            Document^ document);

        void RunAll();

    private:
        Document^ _document;
        Editor^ _editor;

        void TestVertexMetadataRoundTrip();

        void TestVertexWriterPreservesAttachments();

        void TestCascadeDelete();

        void Ensure(
            bool condition,
            String^ message);
    };
}
