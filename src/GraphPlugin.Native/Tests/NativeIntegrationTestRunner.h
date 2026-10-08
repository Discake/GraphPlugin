#pragma once

using namespace System;

using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;

using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;

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

        Circle^ CreateVertex(
            Transaction^ transaction,
            BlockTableRecord^ modelSpace,
            Guid vertexId,
            Point3d position);

        Polyline^ CreateEdge(
            Transaction^ transaction,
            BlockTableRecord^ modelSpace,
            Guid edgeId,
            Guid vertexAId,
            Guid vertexBId,
            Point2d start,
            Point2d end);

        void WriteEdgeMetadata(
            Entity^ entity,
            Transaction^ transaction,
            Guid edgeId,
            Guid vertexAId,
            Guid vertexBId);

        void WriteTestAttachment(
            Entity^ entity,
            Transaction^ transaction,
            String^ path);

        BlockTableRecord^ GetModelSpace(
            Transaction^ transaction);
    };
}