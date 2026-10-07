#include "NativeIntegrationTestRunner.h"
#include "NativeIntegrationTestException.h"
#include "NativeStagedTestSchema.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Services/NativeVertexDeletionService.h"

using namespace System;

using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;

using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;

using namespace GraphPlugin::Native::Persistence;
using namespace GraphPlugin::Native::Services;

namespace GraphPlugin::Native::Tests
{
    void NativeIntegrationTestRunner::RunAll()
    {
        int passed =
            0;

        int failed =
            0;

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

    NativeIntegrationTestRunner::
        NativeIntegrationTestRunner(
            Document^ document)
    {
        if (document == nullptr)
        {
            throw gcnew ArgumentNullException(
                "document");
        }

        _document =
            document;

        _editor =
            document->Editor;
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


    BlockTableRecord^
        NativeIntegrationTestRunner::GetModelSpace(
            Transaction^ transaction)
    {
        Database^ database =
            _document->Database;

        BlockTable^ blockTable =
            dynamic_cast<BlockTable^>(
                transaction->GetObject(
                    database->BlockTableId,
                    OpenMode::ForRead));

        if (blockTable == nullptr)
        {
            throw gcnew NativeIntegrationTestException(
                "BlockTable could not be opened.");
        }

        BlockTableRecord^ modelSpace =
            dynamic_cast<BlockTableRecord^>(
                transaction->GetObject(
                    blockTable[
                        BlockTableRecord::ModelSpace],
                        OpenMode::ForWrite));

        if (modelSpace == nullptr)
        {
            throw gcnew NativeIntegrationTestException(
                "ModelSpace could not be opened.");
        }

        return modelSpace;
    }

    Circle^ NativeIntegrationTestRunner::CreateVertex(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid vertexId,
        Point3d position)
    {
        constexpr double Size =
            10.0;

        Circle^ circle =
            gcnew Circle(
                position,
                Vector3d::ZAxis,
                Size);

        circle->ColorIndex =
            5;

        modelSpace->AppendEntity(
            circle);

        transaction
            ->AddNewlyCreatedDBObject(
                circle,
                true);

        NativeVertexMetadata^ metadata =
            gcnew NativeVertexMetadata();

        metadata->VertexId =
            vertexId;

        metadata->Shape =
            NativeVertexShape::Circle;

        metadata->Color =
            NativeGraphColor::Blue;

        metadata->Size =
            Size;

        VertexMetadataStore^ store =
            gcnew VertexMetadataStore();

        store->Write(
            circle,
            transaction,
            metadata);

        return circle;
    }

    Polyline^ NativeIntegrationTestRunner::CreateEdge(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid edgeId,
        Guid vertexAId,
        Guid vertexBId,
        Point2d start,
        Point2d end)
    {
        Polyline^ edge =
            gcnew Polyline(2);

        edge->AddVertexAt(
            0,
            start,
            0.0,
            0.0,
            0.0);

        edge->AddVertexAt(
            1,
            end,
            0.0,
            0.0,
            0.0);

        edge->Closed =
            false;

        modelSpace->AppendEntity(
            edge);

        transaction
            ->AddNewlyCreatedDBObject(
                edge,
                true);

        WriteEdgeMetadata(
            edge,
            transaction,
            edgeId,
            vertexAId,
            vertexBId);

        return edge;
    }

    void NativeIntegrationTestRunner::WriteEdgeMetadata(
        Entity^ entity,
        Transaction^ transaction,
        Guid edgeId,
        Guid vertexAId,
        Guid vertexBId)
    {
        if (entity->ExtensionDictionary.IsNull)
        {
            entity->CreateExtensionDictionary();
        }

        DBDictionary^ dictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    entity->ExtensionDictionary,
                    OpenMode::ForWrite));

        Ensure(
            dictionary != nullptr,
            "Edge extension dictionary could not be opened.");

        array<TypedValue>^ values =
            gcnew array<TypedValue>(4);

        values[0] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Int32),
                GraphDwgSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Text),
                edgeId.ToString("D"));

        values[2] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Text),
                vertexAId.ToString("D"));

        values[3] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Text),
                vertexBId.ToString("D"));

        Xrecord^ record =
            gcnew Xrecord();

        record->Data =
            gcnew ResultBuffer(
                values);

        dictionary->SetAt(
            GraphDwgSchema::EdgeRecord,
            record);

        transaction
            ->AddNewlyCreatedDBObject(
                record,
                true);
    }

    void NativeIntegrationTestRunner::
        TestVertexMetadataRoundTrip()
    {
        Database^ database =
            _document->Database;

        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    transaction);

            Guid expectedId =
                Guid::NewGuid();

            Circle^ circle =
                CreateVertex(
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
                actual->VertexId ==
                expectedId,
                "VertexId changed after round-trip.");

            Ensure(
                actual->Shape ==
                NativeVertexShape::Circle,
                "Vertex shape changed after round-trip.");

            Ensure(
                actual->Color ==
                NativeGraphColor::Blue,
                "Vertex color changed after round-trip.");

            Ensure(
                actual->Size == 10.0,
                "Vertex size changed after round-trip.");

            //
            // Test fixture никогда не попадает
            // в окончательный DWG.
            //
            transaction->Abort();
        }
        finally
        {
            delete transaction;
        }
    }

    void NativeIntegrationTestRunner::
        WriteTestAttachment(
            Entity^ entity,
            Transaction^ transaction,
            String^ path)
    {
        if (entity->ExtensionDictionary.IsNull)
        {
            entity->CreateExtensionDictionary();
        }

        DBDictionary^ dictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    entity->ExtensionDictionary,
                    OpenMode::ForWrite));

        Ensure(
            dictionary != nullptr,
            "ExtensionDictionary could not be opened.");

        array<TypedValue>^ values =
            gcnew array<TypedValue>(3);

        values[0] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Int32),
                GraphDwgSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Int32),
                1);

        values[2] =
            TypedValue(
                static_cast<int>(
                    DxfCode::Text),
                path);

        Xrecord^ record =
            gcnew Xrecord();

        record->Data =
            gcnew ResultBuffer(
                values);

        dictionary->SetAt(
            GraphDwgSchema::
            VertexAttachmentsRecord,
            record);

        transaction
            ->AddNewlyCreatedDBObject(
                record,
                true);
    }

    void NativeIntegrationTestRunner::
        TestVertexWriterPreservesAttachments()
    {
        Database^ database =
            _document->Database;

        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    transaction);

            Guid vertexId =
                Guid::NewGuid();

            Circle^ circle =
                CreateVertex(
                    transaction,
                    modelSpace,
                    vertexId,
                    Point3d(
                        100100.0,
                        100000.0,
                        0.0));

            WriteTestAttachment(
                circle,
                transaction,
                "Files\\native-test.pdf");

            //
            // Повторная запись GRAPH_VERTEX.
            //
            NativeVertexMetadata^ metadata =
                gcnew NativeVertexMetadata();

            metadata->VertexId =
                vertexId;

            metadata->Shape =
                NativeVertexShape::Circle;

            metadata->Color =
                NativeGraphColor::Blue;

            metadata->Size =
                20.0;

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
                    GraphDwgSchema::
                    VertexRecord),
                "GRAPH_VERTEX disappeared.");

            Ensure(
                dictionary->Contains(
                    GraphDwgSchema::
                    VertexAttachmentsRecord),
                "GRAPH_VERTEX_ATTACHMENTS was destroyed " +
                "by VertexMetadataStore.Write.");

            transaction->Abort();
        }
        finally
        {
            delete transaction;
        }
    }

    void NativeIntegrationTestRunner::
        TestCascadeDelete()
    {
        Database^ database =
            _document->Database;

        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    transaction);

            Guid aId = Guid::NewGuid();
            Guid bId = Guid::NewGuid();
            Guid cId = Guid::NewGuid();
            Guid dId = Guid::NewGuid();

            Circle^ a =
                CreateVertex(
                    transaction,
                    modelSpace,
                    aId,
                    Point3d(101000, 100000, 0));

            Circle^ b =
                CreateVertex(
                    transaction,
                    modelSpace,
                    bId,
                    Point3d(101100, 100000, 0));

            Circle^ c =
                CreateVertex(
                    transaction,
                    modelSpace,
                    cId,
                    Point3d(101000, 100100, 0));

            Circle^ d =
                CreateVertex(
                    transaction,
                    modelSpace,
                    dId,
                    Point3d(101100, 100100, 0));

            Polyline^ ab =
                CreateEdge(
                    transaction,
                    modelSpace,
                    Guid::NewGuid(),
                    aId,
                    bId,
                    Point2d(101000, 100000),
                    Point2d(101100, 100000));

            Polyline^ ac =
                CreateEdge(
                    transaction,
                    modelSpace,
                    Guid::NewGuid(),
                    aId,
                    cId,
                    Point2d(101000, 100000),
                    Point2d(101000, 100100));

            Polyline^ cd =
                CreateEdge(
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