#include "NativeTestCommands.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Services/NativeVertexDeletionService.h"
#include "../Services/NativeVertexStyleService.h"
#include "NativeIntegrationTestException.h"
#include "NativeIntegrationTestRunner.h"
#include "NativeStagedTestSchema.h"

using namespace System;

using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;

using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;
using namespace Teigha::Runtime;

using namespace GraphPlugin::Native::Persistence;
using namespace GraphPlugin::Native::Services;
using namespace GraphPlugin::Native::Tests;

namespace
{
    void Ensure(
        bool condition,
        String^ message)
    {
        if (!condition)
        {
            throw gcnew NativeIntegrationTestException(
                message);
        }
    }

    BlockTableRecord^ GetModelSpace(
        Database^ database,
        Transaction^ transaction,
        OpenMode mode)
    {
        BlockTable^ blockTable =
            dynamic_cast<BlockTable^>(
                transaction->GetObject(
                    database->BlockTableId,
                    OpenMode::ForRead));

        if (blockTable == nullptr)
        {
            throw gcnew InvalidOperationException(
                "BlockTable could not be opened.");
        }

        BlockTableRecord^ modelSpace =
            dynamic_cast<BlockTableRecord^>(
                transaction->GetObject(
                    blockTable[BlockTableRecord::ModelSpace],
                    mode));

        if (modelSpace == nullptr)
        {
            throw gcnew InvalidOperationException(
                "ModelSpace could not be opened.");
        }

        return modelSpace;
    }

    Circle^ CreateVertex(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid vertexId,
        Point3d position)
    {
        constexpr double Size = 10.0;

        Circle^ circle =
            gcnew Circle(
                position,
                Vector3d::ZAxis,
                Size);

        circle->ColorIndex = 5;

        modelSpace->AppendEntity(circle);
        transaction->AddNewlyCreatedDBObject(
            circle,
            true);

        NativeVertexMetadata^ metadata =
            gcnew NativeVertexMetadata();

        metadata->VertexId = vertexId;
        metadata->Shape = NativeVertexShape::Circle;
        metadata->Color = NativeGraphColor::Blue;
        metadata->Size = Size;

        VertexMetadataStore^ store =
            gcnew VertexMetadataStore();

        store->Write(
            circle,
            transaction,
            metadata);

        return circle;
    }

    ObjectId FindVertex(
        Database^ database,
        Transaction^ transaction,
        Guid vertexId)
    {
        VertexMetadataStore^ store =
            gcnew VertexMetadataStore();

        BlockTableRecord^ modelSpace =
            GetModelSpace(
                database,
                transaction,
                OpenMode::ForRead);

        for each (ObjectId objectId in modelSpace)
        {
            Entity^ entity =
                dynamic_cast<Entity^>(
                    transaction->GetObject(
                        objectId,
                        OpenMode::ForRead));

            if (entity == nullptr ||
                entity->IsErased)
            {
                continue;
            }

            NativeVertexMetadata^ metadata =
                store->Read(
                    entity,
                    transaction);

            if (metadata != nullptr &&
                metadata->VertexId == vertexId)
            {
                return objectId;
            }
        }

        return ObjectId::Null;
    }

    void WriteTestAttachment(
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
                static_cast<int>(DxfCode::Int32),
                GraphDwgSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(DxfCode::Int32),
                1);

        values[2] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                path);

        Xrecord^ record = gcnew Xrecord();
        record->Data = gcnew ResultBuffer(values);

        dictionary->SetAt(
            GraphDwgSchema::VertexAttachmentsRecord,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    void WriteStyleManifest(
        Database^ database,
        Transaction^ transaction,
        Guid vertexId,
        String^ oldHandle,
        String^ attachmentPath)
    {
        DBDictionary^ nod =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    database->NamedObjectsDictionaryId,
                    OpenMode::ForWrite));

        if (nod == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Named Objects Dictionary could not be opened.");
        }

        array<TypedValue>^ values =
            gcnew array<TypedValue>(4);

        values[0] =
            TypedValue(
                static_cast<int>(DxfCode::Int32),
                NativeStagedTestSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexId.ToString("D"));

        values[2] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                oldHandle);

        values[3] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                attachmentPath);

        Xrecord^ record = gcnew Xrecord();
        record->Data = gcnew ResultBuffer(values);

        nod->SetAt(
            NativeStagedTestSchema::StyleRecord,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    Guid ReadStyleTestVertexId(
        Database^ database,
        Transaction^ transaction)
    {
        DBDictionary^ nod =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    database->NamedObjectsDictionaryId,
                    OpenMode::ForRead));

        if (nod == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Named Objects Dictionary could not be opened.");
        }

        if (!nod->Contains(
                NativeStagedTestSchema::StyleRecord))
        {
            throw gcnew InvalidOperationException(
                "Style interop test was not prepared.");
        }

        Xrecord^ record =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    nod->GetAt(
                        NativeStagedTestSchema::StyleRecord),
                    OpenMode::ForRead));

        if (record == nullptr ||
            record->Data == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Style interop test manifest is invalid.");
        }

        array<TypedValue>^ values =
            record->Data->AsArray();

        if (values->Length < 4)
        {
            throw gcnew InvalidOperationException(
                "Style interop test manifest contains too few values.");
        }

        int version = Convert::ToInt32(values[0].Value);

        if (version != NativeStagedTestSchema::Version)
        {
            throw gcnew InvalidOperationException(
                "Unsupported style interop test manifest version.");
        }

        Guid vertexId;

        if (!Guid::TryParse(
                safe_cast<String^>(values[1].Value),
                vertexId))
        {
            throw gcnew InvalidOperationException(
                "Style interop test contains invalid VertexId.");
        }

        return vertexId;
    }

    void WriteDeleteUndoManifest(
        Database^ database,
        Transaction^ transaction,
        Guid vertexAId,
        Guid vertexBId,
        Guid vertexCId,
        Guid edgeABId,
        Guid edgeACId,
        Guid edgeBCId)
    {
        DBDictionary^ nod =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    database->NamedObjectsDictionaryId,
                    OpenMode::ForWrite));

        if (nod == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Named Objects Dictionary could not be opened.");
        }

        if (nod->Contains(
                NativeStagedTestSchema::DeleteUndoRecord))
        {
            throw gcnew InvalidOperationException(
                "Delete/undo test manifest already exists. " +
                "Finish or clean the previous test first.");
        }

        array<TypedValue>^ values =
            gcnew array<TypedValue>(7);

        values[0] =
            TypedValue(
                static_cast<int>(DxfCode::Int32),
                NativeStagedTestSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexAId.ToString("D"));

        values[2] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexBId.ToString("D"));

        values[3] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexCId.ToString("D"));

        values[4] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                edgeABId.ToString("D"));

        values[5] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                edgeACId.ToString("D"));

        values[6] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                edgeBCId.ToString("D"));

        Xrecord^ record = gcnew Xrecord();
        record->Data = gcnew ResultBuffer(values);

        nod->SetAt(
            NativeStagedTestSchema::DeleteUndoRecord,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    void WriteDeleteTestEdgeMetadata(
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

        if (dictionary == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Edge ExtensionDictionary could not be opened.");
        }

        array<TypedValue>^ values =
            gcnew array<TypedValue>(4);

        values[0] =
            TypedValue(
                static_cast<int>(DxfCode::Int32),
                GraphDwgSchema::Version);

        values[1] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                edgeId.ToString("D"));

        values[2] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexAId.ToString("D"));

        values[3] =
            TypedValue(
                static_cast<int>(DxfCode::Text),
                vertexBId.ToString("D"));

        Xrecord^ record = gcnew Xrecord();
        record->Data = gcnew ResultBuffer(values);

        dictionary->SetAt(
            GraphDwgSchema::EdgeRecord,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    Polyline^ CreateDeleteTestEdge(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid edgeId,
        Guid vertexAId,
        Guid vertexBId,
        Point2d start,
        Point2d end)
    {
        Polyline^ edge = gcnew Polyline(2);

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

        edge->Closed = false;

        modelSpace->AppendEntity(edge);
        transaction->AddNewlyCreatedDBObject(
            edge,
            true);

        WriteDeleteTestEdgeMetadata(
            edge,
            transaction,
            edgeId,
            vertexAId,
            vertexBId);

        return edge;
    }

    Guid ReadDeleteTestVertexAId(
        Database^ database,
        Transaction^ transaction)
    {
        DBDictionary^ nod =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    database->NamedObjectsDictionaryId,
                    OpenMode::ForRead));

        if (nod == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Named Objects Dictionary could not be opened.");
        }

        if (!nod->Contains(
                NativeStagedTestSchema::DeleteUndoRecord))
        {
            throw gcnew InvalidOperationException(
                "Delete/undo test was not prepared.");
        }

        Xrecord^ record =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    nod->GetAt(
                        NativeStagedTestSchema::DeleteUndoRecord),
                    OpenMode::ForRead));

        if (record == nullptr ||
            record->Data == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Delete/undo manifest is invalid.");
        }

        array<TypedValue>^ values =
            record->Data->AsArray();

        if (values->Length < 7)
        {
            throw gcnew InvalidOperationException(
                "Delete/undo manifest contains too few values.");
        }

        int version = Convert::ToInt32(values[0].Value);

        if (version != NativeStagedTestSchema::Version)
        {
            throw gcnew InvalidOperationException(
                "Unsupported delete/undo manifest version.");
        }

        Guid vertexAId;

        if (!Guid::TryParse(
                safe_cast<String^>(values[1].Value),
                vertexAId))
        {
            throw gcnew InvalidOperationException(
                "Delete/undo manifest contains invalid VertexAId.");
        }

        return vertexAId;
    }
}

namespace GraphPlugin::Native::Tests
{
    [CommandMethod("GRAPHCPPRUNTESTS")]
    void NativeTestCommands::GraphCppRunTests()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        NativeIntegrationTestRunner^ runner =
            gcnew NativeIntegrationTestRunner(document);

        runner->RunAll();
    }

    [CommandMethod("GRAPHCPP_PREPARE_STYLE_INTEROP_TEST")]
    void NativeTestCommands::GraphCppPrepareStyleInteropTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor = document->Editor;
        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    document->Database,
                    transaction,
                    OpenMode::ForWrite);

            Guid vertexId = Guid::NewGuid();

            Circle^ vertex =
                CreateVertex(
                    transaction,
                    modelSpace,
                    vertexId,
                    Point3d(
                        110000.0,
                        110000.0,
                        0.0));

            String^ attachmentPath =
                "Files\\cpp-style-interop.pdf";

            WriteTestAttachment(
                vertex,
                transaction,
                attachmentPath);

            WriteStyleManifest(
                document->Database,
                transaction,
                vertexId,
                vertex->Handle.ToString(),
                attachmentPath);

            transaction->Commit();

            editor->WriteMessage(
                "\n[PREPARED] C++ style interop test.");

            editor->WriteMessage(
                "\nVertexId: {0}",
                vertexId);

            editor->WriteMessage(
                "\nNext: GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST "
                "(normally driven by GRAPHTESTS).");
        }
        finally
        {
            delete transaction;
        }
    }

    [CommandMethod("GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST")]
    void NativeTestCommands::GraphCppExecuteStyleInteropTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor = document->Editor;
        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Guid vertexId =
                ReadStyleTestVertexId(
                    document->Database,
                    transaction);

            ObjectId vertexObjectId =
                FindVertex(
                    document->Database,
                    transaction,
                    vertexId);

            if (vertexObjectId.IsNull)
            {
                throw gcnew InvalidOperationException(
                    "Test vertex was not found.");
            }

            NativeVertexStyleService^ service =
                gcnew NativeVertexStyleService();

            service->ChangeStyle(
                document->Database,
                transaction,
                vertexObjectId,
                NativeVertexShape::Triangle);

            transaction->Commit();

            editor->WriteMessage(
                "\n[EXECUTED] Circle -> Triangle.");

            editor->WriteMessage(
                "\nC# verification is performed by GRAPHTESTS.");
        }
        finally
        {
            delete transaction;
        }
    }

    [CommandMethod("GRAPHCPP_PREPARE_DELETE_UNDO_TEST")]
    void NativeTestCommands::GraphCppPrepareDeleteUndoTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor = document->Editor;
        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Database^ database = document->Database;
            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    database,
                    transaction,
                    OpenMode::ForWrite);

            Guid vertexAId = Guid::NewGuid();
            Guid vertexBId = Guid::NewGuid();
            Guid vertexCId = Guid::NewGuid();
            Guid edgeABId = Guid::NewGuid();
            Guid edgeACId = Guid::NewGuid();
            Guid edgeBCId = Guid::NewGuid();

            CreateVertex(
                transaction,
                modelSpace,
                vertexAId,
                Point3d(
                    120000.0,
                    120000.0,
                    0.0));

            CreateVertex(
                transaction,
                modelSpace,
                vertexBId,
                Point3d(
                    120100.0,
                    120000.0,
                    0.0));

            CreateVertex(
                transaction,
                modelSpace,
                vertexCId,
                Point3d(
                    120050.0,
                    120100.0,
                    0.0));

            CreateDeleteTestEdge(
                transaction,
                modelSpace,
                edgeABId,
                vertexAId,
                vertexBId,
                Point2d(
                    120000.0,
                    120000.0),
                Point2d(
                    120100.0,
                    120000.0));

            CreateDeleteTestEdge(
                transaction,
                modelSpace,
                edgeACId,
                vertexAId,
                vertexCId,
                Point2d(
                    120000.0,
                    120000.0),
                Point2d(
                    120050.0,
                    120100.0));

            CreateDeleteTestEdge(
                transaction,
                modelSpace,
                edgeBCId,
                vertexBId,
                vertexCId,
                Point2d(
                    120100.0,
                    120000.0),
                Point2d(
                    120050.0,
                    120100.0));

            WriteDeleteUndoManifest(
                database,
                transaction,
                vertexAId,
                vertexBId,
                vertexCId,
                edgeABId,
                edgeACId,
                edgeBCId);

            transaction->Commit();

            editor->WriteMessage(
                "\n[PREPARED] C++ delete/undo test.");

            editor->WriteMessage(
                "\nVertex A: {0}",
                vertexAId);

            editor->WriteMessage(
                "\nEdges incident to A: 2.");

            editor->WriteMessage(
                "\nNext: GRAPHCPP_EXECUTE_DELETE_UNDO_TEST "
                "(normally driven by GRAPHTESTS).");
        }
        catch (System::Exception^ ex)
        {
            editor->WriteMessage(
                "\n[FAIL] Prepare delete/undo test: {0}",
                ex->ToString());
        }
        finally
        {
            delete transaction;
        }
    }

    [CommandMethod("GRAPHCPP_EXECUTE_DELETE_UNDO_TEST")]
    void NativeTestCommands::GraphCppExecuteDeleteUndoTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor = document->Editor;
        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Database^ database = document->Database;

            Guid vertexAId =
                ReadDeleteTestVertexAId(
                    database,
                    transaction);

            ObjectId vertexObjectId =
                FindVertex(
                    database,
                    transaction,
                    vertexAId);

            if (vertexObjectId.IsNull)
            {
                throw gcnew InvalidOperationException(
                    "Vertex A could not be found in ModelSpace.");
            }

            NativeVertexDeletionService^ service =
                gcnew NativeVertexDeletionService();

            int deletedEdges =
                service->Delete(
                    database,
                    transaction,
                    vertexObjectId);

            if (deletedEdges != 2)
            {
                throw gcnew InvalidOperationException(
                    String::Format(
                        "Expected 2 deleted incident edges, actual {0}.",
                        deletedEdges));
            }

            transaction->Commit();

            editor->WriteMessage(
                "\n[EXECUTED] C++ cascade delete.");

            editor->WriteMessage(
                "\nVertex A deleted.");

            editor->WriteMessage(
                "\nIncident edges deleted: {0}",
                deletedEdges);

            editor->WriteMessage(
                "\nUNDO and C# verification are performed by GRAPHTESTS.");
        }
        catch (System::Exception^ ex)
        {
            editor->WriteMessage(
                "\n[FAIL] Execute delete/undo test: {0}",
                ex->ToString());
        }
        finally
        {
            delete transaction;
        }
    }
}
