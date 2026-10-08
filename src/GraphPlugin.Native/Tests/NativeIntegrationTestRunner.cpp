#include "NativeIntegrationTestRunner.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Services/NativeVertexDeletionService.h"
#include "NativeIntegrationTestException.h"
#include "NativeTestDwgHelpers.h"

using namespace System;
using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;
using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;
using namespace GraphPlugin::Native::Persistence;
using namespace GraphPlugin::Native::Services;

namespace GraphPlugin::Native::Tests
{
    NativeIntegrationTestRunner::NativeIntegrationTestRunner(
        Document^ document)
    {
        if (document == nullptr)
        {
            throw gcnew ArgumentNullException(
                "document");
        }

        _document = document;
        _editor = document->Editor;
    }

    void NativeIntegrationTestRunner::RunAll()
    {
        int passed = 0;
        int failed = 0;

        _editor->WriteMessage(
            "\n=== GraphPlugin.Native tests ===");

        try
        {
            TestVertexMetadataRoundTrip();
            ++passed;

            _editor->WriteMessage(
                "\n[PASS] Vertex metadata round-trip");
        }
        catch (Exception^ ex)
        {
            ++failed;

            _editor->WriteMessage(
                "\n[FAIL] Vertex metadata round-trip");

            _editor->WriteMessage(
                "\n{0}",
                ex->Message);
        }

        try
        {
            TestVertexWriterPreservesAttachments();
            ++passed;

            _editor->WriteMessage(
                "\n[PASS] Vertex writer preserves attachments");
        }
        catch (Exception^ ex)
        {
            ++failed;

            _editor->WriteMessage(
                "\n[FAIL] Vertex writer preserves attachments");

            _editor->WriteMessage(
                "\n{0}",
                ex->Message);
        }

        try
        {
            TestCascadeDelete();
            ++passed;

            _editor->WriteMessage(
                "\n[PASS] Cascade delete removes incident edges only");
        }
        catch (Exception^ ex)
        {
            ++failed;

            _editor->WriteMessage(
                "\n[FAIL] Cascade delete removes incident edges only");

            _editor->WriteMessage(
                "\n{0}",
                ex->Message);
        }

        _editor->WriteMessage(
            "\n-------------------------------");

        _editor->WriteMessage(
            "\nNative tests: {0} passed, {1} failed.",
            passed,
            failed);

        _editor->WriteMessage(
            "\n===============================");
    }

    void NativeIntegrationTestRunner::Ensure(
        bool condition,
        String^ message)
    {
        if (!condition)
        {
            throw gcnew NativeIntegrationTestException(
                message);
        }
    }

    void NativeIntegrationTestRunner::TestVertexMetadataRoundTrip()
    {
        Database^ database = _document->Database;
        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                NativeTestDwgHelpers::GetModelSpace(
                    database,
                    transaction,
                    OpenMode::ForWrite);

            Guid expectedId = Guid::NewGuid();

            Circle^ circle =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    expectedId,
                    Point3d(
                        100000.0,
                        100000.0,
                        0.0));

            VertexMetadataStore^ store =
                gcnew VertexMetadataStore();

            NativeVertexMetadata^ actual =
                store->Read(
                    circle,
                    transaction);

            Ensure(
                actual != nullptr,
                "Vertex metadata was not restored.");

            Ensure(
                actual->VertexId == expectedId,
                "VertexId changed after round-trip.");

            Ensure(
                actual->Shape == NativeVertexShape::Circle,
                "Vertex shape changed after round-trip.");

            Ensure(
                actual->Color == NativeGraphColor::Blue,
                "Vertex color changed after round-trip.");

            Ensure(
                actual->Size == 10.0,
                "Vertex size changed after round-trip.");

            transaction->Abort();
        }
        finally
        {
            delete transaction;
        }
    }

    void NativeIntegrationTestRunner::TestVertexWriterPreservesAttachments()
    {
        Database^ database = _document->Database;
        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                NativeTestDwgHelpers::GetModelSpace(
                    database,
                    transaction,
                    OpenMode::ForWrite);

            Guid vertexId = Guid::NewGuid();

            Circle^ circle =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    vertexId,
                    Point3d(
                        100100.0,
                        100000.0,
                        0.0));

            NativeTestDwgHelpers::WriteAttachment(
                circle,
                transaction,
                "Files\\native-test.pdf");

            NativeVertexMetadata^ metadata =
                gcnew NativeVertexMetadata();

            metadata->VertexId = vertexId;
            metadata->Shape = NativeVertexShape::Circle;
            metadata->Color = NativeGraphColor::Blue;
            metadata->Size = 20.0;

            VertexMetadataStore^ store =
                gcnew VertexMetadataStore();

            store->Write(
                circle,
                transaction,
                metadata);

            DBDictionary^ dictionary =
                dynamic_cast<DBDictionary^>(
                    transaction->GetObject(
                        circle->ExtensionDictionary,
                        OpenMode::ForRead));

            Ensure(
                dictionary != nullptr,
                "ExtensionDictionary disappeared.");

            Ensure(
                dictionary->Contains(
                    GraphDwgSchema::VertexRecord),
                "GRAPH_VERTEX disappeared.");

            Ensure(
                dictionary->Contains(
                    GraphDwgSchema::VertexAttachmentsRecord),
                "GRAPH_VERTEX_ATTACHMENTS was destroyed "
                "by VertexMetadataStore.Write.");

            transaction->Abort();
        }
        finally
        {
            delete transaction;
        }
    }

    void NativeIntegrationTestRunner::TestCascadeDelete()
    {
        Database^ database = _document->Database;
        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                NativeTestDwgHelpers::GetModelSpace(
                    database,
                    transaction,
                    OpenMode::ForWrite);

            Guid aId = Guid::NewGuid();
            Guid bId = Guid::NewGuid();
            Guid cId = Guid::NewGuid();
            Guid dId = Guid::NewGuid();

            Circle^ a =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    aId,
                    Point3d(101000, 100000, 0));

            Circle^ b =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    bId,
                    Point3d(101100, 100000, 0));

            Circle^ c =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    cId,
                    Point3d(101000, 100100, 0));

            Circle^ d =
                NativeTestDwgHelpers::CreateVertex(
                    transaction,
                    modelSpace,
                    dId,
                    Point3d(101100, 100100, 0));

            Polyline^ ab =
                NativeTestDwgHelpers::CreateEdge(
                    transaction,
                    modelSpace,
                    Guid::NewGuid(),
                    aId,
                    bId,
                    Point2d(101000, 100000),
                    Point2d(101100, 100000));

            Polyline^ ac =
                NativeTestDwgHelpers::CreateEdge(
                    transaction,
                    modelSpace,
                    Guid::NewGuid(),
                    aId,
                    cId,
                    Point2d(101000, 100000),
                    Point2d(101000, 100100));

            Polyline^ cd =
                NativeTestDwgHelpers::CreateEdge(
                    transaction,
                    modelSpace,
                    Guid::NewGuid(),
                    cId,
                    dId,
                    Point2d(101000, 100100),
                    Point2d(101100, 100100));

            NativeVertexDeletionService^ service =
                gcnew NativeVertexDeletionService();

            int deletedEdges =
                service->Delete(
                    database,
                    transaction,
                    a->ObjectId);

            Ensure(
                deletedEdges == 2,
                String::Format(
                    "Expected 2 deleted incident edges, actual {0}.",
                    deletedEdges));

            Ensure(
                a->IsErased,
                "Vertex A was not erased.");

            Ensure(
                ab->IsErased,
                "Incident edge A-B was not erased.");

            Ensure(
                ac->IsErased,
                "Incident edge A-C was not erased.");

            Ensure(
                !b->IsErased,
                "Unrelated Vertex B was erased.");

            Ensure(
                !c->IsErased,
                "Vertex C was erased.");

            Ensure(
                !d->IsErased,
                "Vertex D was erased.");

            Ensure(
                !cd->IsErased,
                "Unrelated edge C-D was erased.");

            transaction->Abort();
        }
        finally
        {
            delete transaction;
        }
    }
}
