#include "GraphCommands.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Services/NativeVertexDeletionService.h"
#include "../Tests/NativeIntegrationTestRunner.h"
#include "../Services/NativeVertexStyleService.h"
#include "../Tests/NativeIntegrationTestException.h"
#include "../Tests/NativeStagedTestSchema.h"

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

        String^ recordKey =
            "GRAPH_CPP_STYLE_INTEROP_TEST";

        if (!nod->Contains(recordKey))
        {
            throw gcnew InvalidOperationException(
                "Style interop test was not prepared.");
        }

        ObjectId recordId =
            nod->GetAt(recordKey);

        Xrecord^ record =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    recordId,
                    OpenMode::ForRead));

        if (record == nullptr ||
            record->Data == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Style interop test manifest is invalid.");
        }

        array<TypedValue>^ values =
            record->Data->AsArray();

        //
        // 0 Version
        // 1 VertexId
        // 2 OldHandle
        // 3 AttachmentPath
        //
        if (values->Length < 4)
        {
            throw gcnew InvalidOperationException(
                "Style interop test manifest contains too few values.");
        }

        int version =
            Convert::ToInt32(
                values[0].Value);

        if (version != 1)
        {
            throw gcnew InvalidOperationException(
                "Unsupported style interop test manifest version.");
        }

        Guid vertexId;

        if (!Guid::TryParse(
            safe_cast<String^>(
                values[1].Value),
            vertexId))
        {
            throw gcnew InvalidOperationException(
                "Style interop test contains invalid VertexId.");
        }

        return vertexId;
    }

    Circle^ CreateVertex(
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

    BlockTableRecord^ GetModelSpace(
        Transaction^ transaction)
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        Database^ database =
            document->Database;

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

    ObjectId FindVertex(
        Database^ database,
        Transaction^ transaction,
        Guid vertexId)
    {
        VertexMetadataStore^ store =
            gcnew VertexMetadataStore();

        BlockTableRecord^ modelSpace =
            GetModelSpace(transaction);

        for each (ObjectId id in modelSpace)
        {
            Entity^ entity =
                dynamic_cast<Entity^>(
                    transaction->GetObject(
                        id,
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
                return id;
            }
        }

        return ObjectId::Null;
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
                "NOD could not be opened.");
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

        Xrecord^ record =
            gcnew Xrecord();

        record->Data =
            gcnew ResultBuffer(values);

        nod->SetAt(
            NativeStagedTestSchema::StyleRecord,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    int GetAttachmentCount(
        Teigha::DatabaseServices::Entity^ entity,
        Teigha::DatabaseServices::Transaction^ transaction)
    {
        using namespace Teigha::DatabaseServices;
        using namespace GraphPlugin::Native::Persistence;

        if (entity->ExtensionDictionary.IsNull)
        {
            return 0;
        }

        DBDictionary^ dictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    entity->ExtensionDictionary,
                    OpenMode::ForRead));

        if (dictionary == nullptr)
        {
            return 0;
        }

        if (!dictionary->Contains(
            GraphDwgSchema::VertexAttachmentsRecord))
        {
            return 0;
        }

        ObjectId recordId =
            dictionary->GetAt(
                GraphDwgSchema::VertexAttachmentsRecord);

        Xrecord^ record =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    recordId,
                    OpenMode::ForRead));

        if (record == nullptr ||
            record->Data == nullptr)
        {
            return 0;
        }

        array<TypedValue>^ values =
            record->Data->AsArray();

        //
        // GRAPH_VERTEX_ATTACHMENTS:
        //
        // 0 - Version
        // 1 - Count
        // 2.. - paths
        //
        if (values->Length < 2)
        {
            return 0;
        }

        int version =
            Convert::ToInt32(
                values[0].Value);

        if (version !=
            GraphDwgSchema::Version)
        {
            throw gcnew InvalidOperationException(
                String::Format(
                    "Unsupported attachment record version: {0}.",
                    version));
        }

        int count =
            Convert::ToInt32(
                values[1].Value);

        if (count < 0)
        {
            throw gcnew InvalidOperationException(
                "Invalid attachment count.");
        }

        return count;
    }
}

namespace GraphPlugin::Native::Commands
{
    [CommandMethod("GRAPHCPPINFO")]
    void GraphCommands::GraphCppInfo()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        document->Editor->WriteMessage(
            "\n=== GraphPlugin.Native ===");

        document->Editor->WriteMessage(
            "\nLanguage: C++/CLI");

        document->Editor->WriteMessage(
            "\nDWG schema version: {0}",
            GraphDwgSchema::Version);

        document->Editor->WriteMessage(
            "\nVertex record: {0}",
            GraphDwgSchema::VertexRecord);

        document->Editor->WriteMessage(
            "\n==========================");
    }

    [CommandMethod("GRAPHCPPVERTEX")]
    void GraphCommands::GraphCppVertex()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor =
            document->Editor;

        try
        {
            PromptPointOptions^ options =
                gcnew PromptPointOptions(
                    "\nSpecify graph vertex position: ");

            PromptPointResult^ pointResult =
                editor->GetPoint(
                    options);

            if (pointResult->Status !=
                PromptStatus::OK)
            {
                return;
            }

            Point3d point =
                pointResult->Value;

            Database^ database =
                document->Database;

            Transaction^ transaction =
                database
                ->TransactionManager
                ->StartTransaction();

            try
            {
                BlockTable^ blockTable =
                    dynamic_cast<BlockTable^>(
                        transaction->GetObject(
                            database->BlockTableId,
                            OpenMode::ForRead));

                if (blockTable == nullptr)
                {
                    throw gcnew InvalidOperationException(
                        "Block table could not be opened.");
                }

                BlockTableRecord^ modelSpace =
                    dynamic_cast<BlockTableRecord^>(
                        transaction->GetObject(
                            blockTable[
                                BlockTableRecord::ModelSpace],
                                OpenMode::ForWrite));

                if (modelSpace == nullptr)
                {
                    throw gcnew InvalidOperationException(
                        "Model space could not be opened.");
                }

                constexpr double DefaultSize =
                    10.0;

                Circle^ circle =
                    gcnew Circle(
                        point,
                        Vector3d::ZAxis,
                        DefaultSize);

                //
                // AutoCAD Color Index / ACI:
                // 5 = blue.
                //
                circle->ColorIndex =
                    5;

                ObjectId objectId =
                    modelSpace->AppendEntity(
                        circle);

                transaction
                    ->AddNewlyCreatedDBObject(
                        circle,
                        true);

                NativeVertexMetadata^ metadata =
                    gcnew NativeVertexMetadata();

                metadata->VertexId =
                    Guid::NewGuid();

                metadata->Shape =
                    NativeVertexShape::Circle;

                metadata->Color =
                    NativeGraphColor::Blue;

                metadata->Size =
                    DefaultSize;

                VertexMetadataStore^ store =
                    gcnew VertexMetadataStore();

                store->Write(
                    circle,
                    transaction,
                    metadata);

                transaction->Commit();

                editor->WriteMessage(
                    "\nC++ vertex created.");

                editor->WriteMessage(
                    "\nVertexId: {0}",
                    metadata->VertexId);

                editor->WriteMessage(
                    "\nObjectId: {0}",
                    objectId);
            }
            finally
            {
                delete transaction;
            }
        }
        catch (System::Exception^ ex)
        {
            editor->WriteMessage(
                "\nGRAPHCPPVERTEX failed: {0}",
                ex->ToString());
        }
    }

    [CommandMethod("GRAPHCPPVERTEXINFO")]
    void GraphPlugin::Native::Commands::
        GraphCommands::GraphCppVertexInfo()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
        {
            return;
        }

        Editor^ editor =
            document->Editor;

        try
        {
            PromptEntityOptions^ options =
                gcnew PromptEntityOptions(
                    "\nSelect graph vertex: ");

            PromptEntityResult^ result =
                editor->GetEntity(
                    options);

            if (result->Status !=
                PromptStatus::OK)
            {
                return;
            }

            Database^ database =
                document->Database;

            Transaction^ transaction =
                database
                ->TransactionManager
                ->StartTransaction();

            try
            {
                Entity^ entity =
                    dynamic_cast<Entity^>(
                        transaction->GetObject(
                            result->ObjectId,
                            OpenMode::ForRead));

                if (entity == nullptr)
                {
                    throw gcnew InvalidOperationException(
                        "Selected object is not an Entity.");
                }

                VertexMetadataStore^ metadataStore =
                    gcnew VertexMetadataStore();

                NativeVertexMetadata^ metadata =
                    metadataStore->Read(
                        entity,
                        transaction);

                if (metadata == nullptr)
                {
                    editor->WriteMessage(
                        "\nSelected object is not a graph vertex.");

                    return;
                }

                int attachmentCount =
                    GetAttachmentCount(
                        entity,
                        transaction);

                editor->WriteMessage(
                    "\n=== C++ Vertex Info ===");

                editor->WriteMessage(
                    "\nVertexId: {0}",
                    metadata->VertexId);

                editor->WriteMessage(
                    "\nShape: {0}",
                    metadata->Shape);

                editor->WriteMessage(
                    "\nColor: {0}",
                    metadata->Color);

                editor->WriteMessage(
                    "\nSize: {0}",
                    metadata->Size);

                editor->WriteMessage(
                    "\nAttachments: {0}",
                    attachmentCount);

                editor->WriteMessage(
                    "\nObjectId: {0}",
                    result->ObjectId);

                editor->WriteMessage(
                    "\nEntity type: {0}",
                    entity->GetType()->Name);

                editor->WriteMessage(
                    "\n=======================");

                transaction->Commit();
            }
            finally
            {
                delete transaction;
            }
        }
        catch (System::Exception^ ex)
        {
            editor->WriteMessage(
                "\nGRAPHCPPVERTEXINFO failed: {0}",
                ex->ToString());
        }
    }

    [CommandMethod("GRAPHCPPVERTEXSTYLE")]
    void GraphPlugin::Native::Commands::
        GraphCommands::GraphCppVertexStyle()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
        {
            return;
        }

        Editor^ editor =
            document->Editor;

        PromptEntityOptions^ selectOptions =
            gcnew PromptEntityOptions(
                "\nSelect graph vertex: ");

        PromptEntityResult^ selectResult =
            editor->GetEntity(
                selectOptions);

        if (selectResult->Status !=
            PromptStatus::OK)
        {
            return;
        }

        Database^ database =
            document->Database;

        Transaction^ transaction =
            database
            ->TransactionManager
            ->StartTransaction();

        try {
            Entity^ oldEntity =
                dynamic_cast<Entity^>(
                    transaction->GetObject(
                        selectResult->ObjectId,
                        OpenMode::ForWrite));

            if (oldEntity == nullptr)
            {
                throw gcnew InvalidOperationException(
                    "Selected object is not an Entity.");
            }

            VertexMetadataStore^ metadataStore =
                gcnew VertexMetadataStore();

            NativeVertexMetadata^ metadata =
                metadataStore->Read(
                    oldEntity,
                    transaction);

            if (metadata == nullptr)
            {
                throw gcnew InvalidOperationException(
                    "Selected object is not a graph vertex.");
            }

            PromptKeywordOptions^ keywordOptions =
                gcnew PromptKeywordOptions(
                    "\nSelect vertex style");

            keywordOptions
                ->Keywords
                ->Add("Circle");

            keywordOptions
                ->Keywords
                ->Add("Triangle");

            keywordOptions->AllowNone =
                true;

            keywordOptions->Keywords->Default =
                metadata->Shape ==
                NativeVertexShape::Circle
                ? "Circle"
                : "Triangle";

            PromptResult^ keywordResult =
                editor->GetKeywords(
                    keywordOptions);

            if (keywordResult->Status !=
                PromptStatus::OK &&
                keywordResult->Status !=
                PromptStatus::None)
            {
                return;
            }

            String^ selectedStyle =
                keywordResult->Status ==
                PromptStatus::None
                ? keywordOptions
                ->Keywords
                ->Default
                : keywordResult
                ->StringResult;

            NativeVertexShape targetShape;

            if (String::Equals(
                selectedStyle,
                "Circle",
                StringComparison::OrdinalIgnoreCase))
            {
                targetShape =
                    NativeVertexShape::Circle;
            }
            else
            {
                targetShape =
                    NativeVertexShape::Triangle;
            }

            if (targetShape ==
                metadata->Shape)
            {
                editor->WriteMessage(
                    "\nVertex already has this style.");

                return;
            }

            NativeVertexStyleService^ service =
                gcnew NativeVertexStyleService();

            ObjectId newObjectId =
                service->ChangeStyle(
                    database,
                    transaction,
                    selectResult->ObjectId,
                    targetShape);

            transaction->Commit();
        }
        finally
        {
            delete transaction;
        }
    }

    [CommandMethod("GRAPHCPPDELETEVERTEX")]
    void GraphPlugin::Native::Commands::
        GraphCommands::GraphCppDeleteVertex()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor =
            document->Editor;

        try
        {
            PromptEntityOptions^ options =
                gcnew PromptEntityOptions(
                    "\nSelect graph vertex to delete: ");

            PromptEntityResult^ result =
                editor->GetEntity(
                    options);

            if (result->Status !=
                PromptStatus::OK)
            {
                return;
            }

            Database^ database =
                document->Database;

            Transaction^ transaction =
                database
                ->TransactionManager
                ->StartTransaction();

            try
            {
                NativeVertexDeletionService^ service =
                    gcnew NativeVertexDeletionService();

                int deletedEdges =
                    service->Delete(
                        database,
                        transaction,
                        result->ObjectId);

                transaction->Commit();

                editor->WriteMessage(
                    "\nC++ vertex deleted.");

                editor->WriteMessage(
                    "\nIncident edges deleted: {0}",
                    deletedEdges);
            }
            finally
            {
                delete transaction;
            }
        }
        catch (System::Exception^ ex)
        {
            editor->WriteMessage(
                "\nGRAPHCPPDELETEVERTEX failed: {0}",
                ex->ToString());
        }
    }

    [CommandMethod("GRAPHCPPRUNTESTS")]
    void GraphPlugin::Native::Commands::
        GraphCommands::GraphCppRunTests()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        NativeIntegrationTestRunner^ runner =
            gcnew NativeIntegrationTestRunner(
                document);

        runner->RunAll();
    }

    [CommandMethod("GRAPHCPP_PREPARE_STYLE_INTEROP_TEST")]
    void GraphCommands::GraphCppPrepareStyleInteropTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        Editor^ editor =
            document->Editor;

        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Document^ document =
                Application::DocumentManager
                ->MdiActiveDocument;

            BlockTableRecord^ modelSpace =
                GetModelSpace(
                    transaction);

            Guid vertexId =
                Guid::NewGuid();

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
                "\nNext: GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST");
        }
        finally
        {
            delete transaction;
        }
    }

    [CommandMethod("GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST")]
    void GraphCommands::GraphCppExecuteStyleInteropTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        Editor^ editor =
            document->Editor;

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
                "\nNext: GRAPH_VERIFY_CPP_STYLE_INTEROP_TEST");
        }
        finally
        {
            delete transaction;
        }
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

        String^ recordKey =
            NativeStagedTestSchema::DeleteUndoRecord;

        if (nod->Contains(recordKey))
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

        Xrecord^ record =
            gcnew Xrecord();

        record->Data =
            gcnew ResultBuffer(
                values);

        nod->SetAt(
            recordKey,
            record);

        transaction->AddNewlyCreatedDBObject(
            record,
            true);
    }

    Circle^ CreateDeleteTestVertex(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid vertexId,
        Point3d position)
    {
        const double size =
            10.0;

        Circle^ circle =
            gcnew Circle(
                position,
                Vector3d::ZAxis,
                size);

        circle->ColorIndex =
            5;

        modelSpace->AppendEntity(
            circle);

        transaction->AddNewlyCreatedDBObject(
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
            size;

        VertexMetadataStore^ metadataStore =
            gcnew VertexMetadataStore();

        metadataStore->Write(
            circle,
            transaction,
            metadata);

        return circle;
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

        //
        // Должно совпадать с C# GRAPH_EDGE:
        //
        // 0 Version
        // 1 EdgeId
        // 2 VertexAId
        // 3 VertexBId
        //
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

        Xrecord^ record =
            gcnew Xrecord();

        record->Data =
            gcnew ResultBuffer(
                values);

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

    BlockTableRecord^ GetWritableModelSpace(
        Database^ database,
        Transaction^ transaction)
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
                    blockTable[
                        BlockTableRecord::ModelSpace],
                        OpenMode::ForWrite));

        if (modelSpace == nullptr)
        {
            throw gcnew InvalidOperationException(
                "ModelSpace could not be opened.");
        }

        return modelSpace;
    }

    [CommandMethod("GRAPHCPP_PREPARE_DELETE_UNDO_TEST")]
    void GraphCommands::
        GraphCppPrepareDeleteUndoTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor =
            document->Editor;

        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Database^ database =
                document->Database;

            BlockTableRecord^ modelSpace =
                GetWritableModelSpace(
                    database,
                    transaction);

            Guid vertexAId =
                Guid::NewGuid();

            Guid vertexBId =
                Guid::NewGuid();

            Guid vertexCId =
                Guid::NewGuid();

            Guid edgeABId =
                Guid::NewGuid();

            Guid edgeACId =
                Guid::NewGuid();

            Guid edgeBCId =
                Guid::NewGuid();

            //
            //       B
            //      / \
            //     /   \
            //    A --- C
            //
            //

            CreateDeleteTestVertex(
                transaction,
                modelSpace,
                vertexAId,
                Point3d(
                    120000.0,
                    120000.0,
                    0.0));

            CreateDeleteTestVertex(
                transaction,
                modelSpace,
                vertexBId,
                Point3d(
                    120100.0,
                    120000.0,
                    0.0));

            CreateDeleteTestVertex(
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
                "\nNext run: GRAPHCPP_EXECUTE_DELETE_UNDO_TEST");
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

        String^ recordKey =
            NativeStagedTestSchema::DeleteUndoRecord;

        if (!nod->Contains(recordKey))
        {
            throw gcnew InvalidOperationException(
                "Delete/undo test was not prepared.");
        }

        Xrecord^ record =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    nod->GetAt(recordKey),
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

        int version =
            Convert::ToInt32(
                values[0].Value);

        if (version !=
            NativeStagedTestSchema::Version)
        {
            throw gcnew InvalidOperationException(
                "Unsupported delete/undo manifest version.");
        }

        Guid vertexAId;

        if (!Guid::TryParse(
            safe_cast<String^>(
                values[1].Value),
            vertexAId))
        {
            throw gcnew InvalidOperationException(
                "Delete/undo manifest contains invalid VertexAId.");
        }

        return vertexAId;
    }

    ObjectId FindVertexObjectId(
        Database^ database,
        Transaction^ transaction,
        Guid vertexId)
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
                    blockTable[
                        BlockTableRecord::ModelSpace],
                        OpenMode::ForRead));

        if (modelSpace == nullptr)
        {
            throw gcnew InvalidOperationException(
                "ModelSpace could not be opened.");
        }

        VertexMetadataStore^ metadataStore =
            gcnew VertexMetadataStore();

        for each (
            ObjectId objectId
            in modelSpace)
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
                metadataStore->Read(
                    entity,
                    transaction);

            if (metadata == nullptr)
                continue;

            if (metadata->VertexId ==
                vertexId)
            {
                return objectId;
            }
        }

        return ObjectId::Null;
    }

    [CommandMethod("GRAPHCPP_EXECUTE_DELETE_UNDO_TEST")]
    void GraphCommands::
        GraphCppExecuteDeleteUndoTest()
    {
        Document^ document =
            Application::DocumentManager
            ->MdiActiveDocument;

        if (document == nullptr)
            return;

        Editor^ editor =
            document->Editor;

        Transaction^ transaction =
            document->Database
            ->TransactionManager
            ->StartTransaction();

        try
        {
            Database^ database =
                document->Database;

            Guid vertexAId =
                ReadDeleteTestVertexAId(
                    database,
                    transaction);

            ObjectId vertexObjectId =
                FindVertexObjectId(
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
                "\nNow press Ctrl+Z exactly once.");

            editor->WriteMessage(
                "\nThen run GRAPH_VERIFY_CPP_DELETE_UNDO_TEST.");
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