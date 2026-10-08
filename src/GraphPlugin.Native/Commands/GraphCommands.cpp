#include "GraphCommands.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Services/NativeVertexDeletionService.h"
#include "../Services/NativeVertexStyleService.h"

using namespace System;

using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;

using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;
using namespace Teigha::Runtime;

using namespace GraphPlugin::Native::Persistence;
using namespace GraphPlugin::Native::Services;

namespace
{
int GetAttachmentCount(Entity ^ entity, Transaction ^ transaction)
{
    if (entity->ExtensionDictionary.IsNull)
    {
        return 0;
    }

    DBDictionary ^ dictionary =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(entity->ExtensionDictionary, OpenMode::ForRead));

    if (dictionary == nullptr || !dictionary->Contains(GraphDwgSchema::VertexAttachmentsRecord))
    {
        return 0;
    }

    Xrecord ^ record = dynamic_cast<Xrecord ^>(
        transaction->GetObject(dictionary->GetAt(GraphDwgSchema::VertexAttachmentsRecord), OpenMode::ForRead));

    if (record == nullptr || record->Data == nullptr)
    {
        return 0;
    }

    array<TypedValue> ^ values = record->Data->AsArray();

    if (values->Length < 2)
    {
        return 0;
    }

    int version = Convert::ToInt32(values[0].Value);

    if (version != GraphDwgSchema::Version)
    {
        throw gcnew InvalidOperationException(String::Format("Unsupported attachment record version: {0}.", version));
    }

    int count = Convert::ToInt32(values[1].Value);

    if (count < 0)
    {
        throw gcnew InvalidOperationException("Invalid attachment count.");
    }

    return count;
}
} // namespace

namespace GraphPlugin::Native::Commands
{
[CommandMethod("GRAPHCPPINFO")] void GraphCommands::GraphCppInfo()
{
    Document ^ document = Application::DocumentManager->MdiActiveDocument;

    if (document == nullptr)
        return;

    document->Editor->WriteMessage("\n=== GraphPlugin.Native ===");

    document->Editor->WriteMessage("\nLanguage: C++/CLI");

    document->Editor->WriteMessage("\nDWG schema version: {0}", GraphDwgSchema::Version);

    document->Editor->WriteMessage("\nVertex record: {0}", GraphDwgSchema::VertexRecord);

    document->Editor->WriteMessage("\n==========================");
}

    [CommandMethod("GRAPHCPPVERTEX")] void GraphCommands::GraphCppVertex()
{
    Document ^ document = Application::DocumentManager->MdiActiveDocument;

    if (document == nullptr)
        return;

    Editor ^ editor = document->Editor;

    try
    {
        PromptPointOptions ^ options = gcnew PromptPointOptions("\nSpecify graph vertex position: ");

        PromptPointResult ^ pointResult = editor->GetPoint(options);

        if (pointResult->Status != PromptStatus::OK)
            return;

        Database ^ database = document->Database;
        Transaction ^ transaction = database->TransactionManager->StartTransaction();

        try
        {
            BlockTable ^ blockTable =
                dynamic_cast<BlockTable ^>(transaction->GetObject(database->BlockTableId, OpenMode::ForRead));

            if (blockTable == nullptr)
            {
                throw gcnew InvalidOperationException("Block table could not be opened.");
            }

            BlockTableRecord ^ modelSpace = dynamic_cast<BlockTableRecord ^>(
                transaction->GetObject(blockTable[BlockTableRecord::ModelSpace], OpenMode::ForWrite));

            if (modelSpace == nullptr)
            {
                throw gcnew InvalidOperationException("Model space could not be opened.");
            }

            constexpr double DefaultSize = 10.0;

            Circle ^ circle = gcnew Circle(pointResult->Value, Vector3d::ZAxis, DefaultSize);

            // AutoCAD Color Index / ACI: 5 = blue.
            circle->ColorIndex = 5;

            ObjectId objectId = modelSpace->AppendEntity(circle);

            transaction->AddNewlyCreatedDBObject(circle, true);

            NativeVertexMetadata ^ metadata = gcnew NativeVertexMetadata();

            metadata->VertexId = Guid::NewGuid();
            metadata->Shape = NativeVertexShape::Circle;
            metadata->Color = NativeGraphColor::Blue;
            metadata->Size = DefaultSize;

            VertexMetadataStore ^ store = gcnew VertexMetadataStore();

            store->Write(circle, transaction, metadata);

            transaction->Commit();

            editor->WriteMessage("\nC++ vertex created.");

            editor->WriteMessage("\nVertexId: {0}", metadata->VertexId);

            editor->WriteMessage("\nObjectId: {0}", objectId);
        }
        finally
        {
            delete transaction;
        }
    }
    catch (System::Exception ^ ex)
    {
        editor->WriteMessage("\nGRAPHCPPVERTEX failed: {0}", ex->ToString());
    }
}

[CommandMethod("GRAPHCPPVERTEXINFO")] void GraphCommands::GraphCppVertexInfo()
{
    Document ^ document = Application::DocumentManager->MdiActiveDocument;

    if (document == nullptr)
        return;

    Editor ^ editor = document->Editor;

    try
    {
        PromptEntityOptions ^ options = gcnew PromptEntityOptions("\nSelect graph vertex: ");

        PromptEntityResult ^ result = editor->GetEntity(options);

        if (result->Status != PromptStatus::OK)
            return;

        Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

        try
        {
            Entity ^ entity = dynamic_cast<Entity ^>(transaction->GetObject(result->ObjectId, OpenMode::ForRead));

            if (entity == nullptr)
            {
                throw gcnew InvalidOperationException("Selected object is not an Entity.");
            }

            VertexMetadataStore ^ metadataStore = gcnew VertexMetadataStore();

            NativeVertexMetadata ^ metadata = metadataStore->Read(entity, transaction);

            if (metadata == nullptr)
            {
                editor->WriteMessage("\nSelected object is not a graph vertex.");
                return;
            }

            int attachmentCount = GetAttachmentCount(entity, transaction);

            editor->WriteMessage("\n=== C++ Vertex Info ===");

            editor->WriteMessage("\nVertexId: {0}", metadata->VertexId);

            editor->WriteMessage("\nShape: {0}", metadata->Shape);

            editor->WriteMessage("\nColor: {0}", metadata->Color);

            editor->WriteMessage("\nSize: {0}", metadata->Size);

            editor->WriteMessage("\nAttachments: {0}", attachmentCount);

            editor->WriteMessage("\nObjectId: {0}", result->ObjectId);

            editor->WriteMessage("\nEntity type: {0}", entity->GetType()->Name);

            editor->WriteMessage("\n=======================");

            transaction->Commit();
        }
        finally
        {
            delete transaction;
        }
    }
    catch (System::Exception ^ ex)
    {
        editor->WriteMessage("\nGRAPHCPPVERTEXINFO failed: {0}", ex->ToString());
    }
}

    [CommandMethod("GRAPHCPPVERTEXSTYLE")] void GraphCommands::GraphCppVertexStyle()
{
    Document ^ document = Application::DocumentManager->MdiActiveDocument;

    if (document == nullptr)
        return;

    Editor ^ editor = document->Editor;

    PromptEntityOptions ^ selectOptions = gcnew PromptEntityOptions("\nSelect graph vertex: ");

    PromptEntityResult ^ selectResult = editor->GetEntity(selectOptions);

    if (selectResult->Status != PromptStatus::OK)
        return;

    Database ^ database = document->Database;
    Transaction ^ transaction = database->TransactionManager->StartTransaction();

    try
    {
        Entity ^ oldEntity = dynamic_cast<Entity ^>(transaction->GetObject(selectResult->ObjectId, OpenMode::ForWrite));

        if (oldEntity == nullptr)
        {
            throw gcnew InvalidOperationException("Selected object is not an Entity.");
        }

        VertexMetadataStore ^ metadataStore = gcnew VertexMetadataStore();

        NativeVertexMetadata ^ metadata = metadataStore->Read(oldEntity, transaction);

        if (metadata == nullptr)
        {
            throw gcnew InvalidOperationException("Selected object is not a graph vertex.");
        }

        PromptKeywordOptions ^ keywordOptions = gcnew PromptKeywordOptions("\nSelect vertex style");

        keywordOptions->Keywords->Add("Circle");
        keywordOptions->Keywords->Add("Triangle");
        keywordOptions->AllowNone = true;
        keywordOptions->Keywords->Default = metadata->Shape == NativeVertexShape::Circle ? "Circle" : "Triangle";

        PromptResult ^ keywordResult = editor->GetKeywords(keywordOptions);

        if (keywordResult->Status != PromptStatus::OK && keywordResult->Status != PromptStatus::None)
        {
            return;
        }

        String ^ selectedStyle = keywordResult->Status == PromptStatus::None ? keywordOptions->Keywords->Default
                                                                             : keywordResult->StringResult;

        NativeVertexShape targetShape = String::Equals(selectedStyle, "Circle", StringComparison::OrdinalIgnoreCase)
                                            ? NativeVertexShape::Circle
                                            : NativeVertexShape::Triangle;

        if (targetShape == metadata->Shape)
        {
            editor->WriteMessage("\nVertex already has this style.");
            return;
        }

        NativeVertexStyleService ^ service = gcnew NativeVertexStyleService();

        service->ChangeStyle(database, transaction, selectResult->ObjectId, targetShape);

        transaction->Commit();
    }
    finally
    {
        delete transaction;
    }
}

[CommandMethod("GRAPHCPPDELETEVERTEX")] void GraphCommands::GraphCppDeleteVertex()
{
    Document ^ document = Application::DocumentManager->MdiActiveDocument;

    if (document == nullptr)
        return;

    Editor ^ editor = document->Editor;

    try
    {
        PromptEntityOptions ^ options = gcnew PromptEntityOptions("\nSelect graph vertex to delete: ");

        PromptEntityResult ^ result = editor->GetEntity(options);

        if (result->Status != PromptStatus::OK)
            return;

        Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

        try
        {
            NativeVertexDeletionService ^ service = gcnew NativeVertexDeletionService();

            int deletedEdges = service->Delete(document->Database, transaction, result->ObjectId);

            transaction->Commit();

            editor->WriteMessage("\nC++ vertex deleted.");

            editor->WriteMessage("\nIncident edges deleted: {0}", deletedEdges);
        }
        finally
        {
            delete transaction;
        }
    }
    catch (System::Exception ^ ex)
    {
        editor->WriteMessage("\nGRAPHCPPDELETEVERTEX failed: {0}", ex->ToString());
    }
}
} // namespace GraphPlugin::Native::Commands
