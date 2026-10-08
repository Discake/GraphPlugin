#include "DeleteUndoScenario.h"

#include "../Services/NativeVertexDeletionService.h"
#include "NativeStagedTestSchema.h"
#include "NativeTestDwgHelpers.h"

using namespace System;
using namespace HostMgd::ApplicationServices;
using namespace HostMgd::EditorInput;
using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;
using namespace GraphPlugin::Native::Services;

namespace
{
void WriteManifest(Database ^ database,
                   Transaction ^ transaction,
                   Guid vertexAId,
                   Guid vertexBId,
                   Guid vertexCId,
                   Guid edgeABId,
                   Guid edgeACId,
                   Guid edgeBCId)
{
    DBDictionary ^ nod =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(database->NamedObjectsDictionaryId, OpenMode::ForWrite));

    if (nod == nullptr)
    {
        throw gcnew InvalidOperationException("Named Objects Dictionary could not be opened.");
    }

    if (nod->Contains(GraphPlugin::Native::Tests::NativeStagedTestSchema::DeleteUndoRecord))
    {
        throw gcnew InvalidOperationException("Delete/undo test manifest already exists. "
                                              "Finish or clean the previous test first.");
    }

    array<TypedValue> ^ values = gcnew array<TypedValue>(7);

    values[0] =
        TypedValue(static_cast<int>(DxfCode::Int32), GraphPlugin::Native::Tests::NativeStagedTestSchema::Version);

    values[1] = TypedValue(static_cast<int>(DxfCode::Text), vertexAId.ToString("D"));

    values[2] = TypedValue(static_cast<int>(DxfCode::Text), vertexBId.ToString("D"));

    values[3] = TypedValue(static_cast<int>(DxfCode::Text), vertexCId.ToString("D"));

    values[4] = TypedValue(static_cast<int>(DxfCode::Text), edgeABId.ToString("D"));

    values[5] = TypedValue(static_cast<int>(DxfCode::Text), edgeACId.ToString("D"));

    values[6] = TypedValue(static_cast<int>(DxfCode::Text), edgeBCId.ToString("D"));

    Xrecord ^ record = gcnew Xrecord();
    record->Data = gcnew ResultBuffer(values);

    nod->SetAt(GraphPlugin::Native::Tests::NativeStagedTestSchema::DeleteUndoRecord, record);

    transaction->AddNewlyCreatedDBObject(record, true);
}

Guid ReadVertexAId(Database ^ database, Transaction ^ transaction)
{
    DBDictionary ^ nod =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(database->NamedObjectsDictionaryId, OpenMode::ForRead));

    if (nod == nullptr)
    {
        throw gcnew InvalidOperationException("Named Objects Dictionary could not be opened.");
    }

    if (!nod->Contains(GraphPlugin::Native::Tests::NativeStagedTestSchema::DeleteUndoRecord))
    {
        throw gcnew InvalidOperationException("Delete/undo test was not prepared.");
    }

    Xrecord ^ record = dynamic_cast<Xrecord ^>(transaction->GetObject(
        nod->GetAt(GraphPlugin::Native::Tests::NativeStagedTestSchema::DeleteUndoRecord), OpenMode::ForRead));

    if (record == nullptr || record->Data == nullptr)
    {
        throw gcnew InvalidOperationException("Delete/undo manifest is invalid.");
    }

    array<TypedValue> ^ values = record->Data->AsArray();

    if (values->Length < 7)
    {
        throw gcnew InvalidOperationException("Delete/undo manifest contains too few values.");
    }

    int version = Convert::ToInt32(values[0].Value);

    if (version != GraphPlugin::Native::Tests::NativeStagedTestSchema::Version)
    {
        throw gcnew InvalidOperationException("Unsupported delete/undo manifest version.");
    }

    Guid vertexAId;

    if (!Guid::TryParse(safe_cast<String ^>(values[1].Value), vertexAId))
    {
        throw gcnew InvalidOperationException("Delete/undo manifest contains invalid VertexAId.");
    }

    return vertexAId;
}
} // namespace

namespace GraphPlugin::Native::Tests
{
void DeleteUndoScenario::Prepare(Document ^ document)
{
    ArgumentNullException::ThrowIfNull(document);

    Editor ^ editor = document->Editor;
    Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

    try
    {
        Database ^ database = document->Database;
        BlockTableRecord ^ modelSpace = NativeTestDwgHelpers::GetModelSpace(database, transaction, OpenMode::ForWrite);

        Guid vertexAId = Guid::NewGuid();
        Guid vertexBId = Guid::NewGuid();
        Guid vertexCId = Guid::NewGuid();
        Guid edgeABId = Guid::NewGuid();
        Guid edgeACId = Guid::NewGuid();
        Guid edgeBCId = Guid::NewGuid();

        NativeTestDwgHelpers::CreateVertex(transaction, modelSpace, vertexAId, Point3d(120000.0, 120000.0, 0.0));

        NativeTestDwgHelpers::CreateVertex(transaction, modelSpace, vertexBId, Point3d(120100.0, 120000.0, 0.0));

        NativeTestDwgHelpers::CreateVertex(transaction, modelSpace, vertexCId, Point3d(120050.0, 120100.0, 0.0));

        NativeTestDwgHelpers::CreateEdge(transaction,
                                         modelSpace,
                                         edgeABId,
                                         vertexAId,
                                         vertexBId,
                                         Point2d(120000.0, 120000.0),
                                         Point2d(120100.0, 120000.0));

        NativeTestDwgHelpers::CreateEdge(transaction,
                                         modelSpace,
                                         edgeACId,
                                         vertexAId,
                                         vertexCId,
                                         Point2d(120000.0, 120000.0),
                                         Point2d(120050.0, 120100.0));

        NativeTestDwgHelpers::CreateEdge(transaction,
                                         modelSpace,
                                         edgeBCId,
                                         vertexBId,
                                         vertexCId,
                                         Point2d(120100.0, 120000.0),
                                         Point2d(120050.0, 120100.0));

        WriteManifest(database, transaction, vertexAId, vertexBId, vertexCId, edgeABId, edgeACId, edgeBCId);

        transaction->Commit();

        editor->WriteMessage("\n[PREPARED] C++ delete/undo test.");

        editor->WriteMessage("\nVertex A: {0}", vertexAId);

        editor->WriteMessage("\nEdges incident to A: 2.");

        editor->WriteMessage("\nNext: GRAPHCPP_EXECUTE_DELETE_UNDO_TEST "
                             "(normally driven by GRAPHTESTS).");
    }
    catch (System::Exception ^ ex)
    {
        editor->WriteMessage("\n[FAIL] Prepare delete/undo test: {0}", ex->ToString());
    }
    finally
    {
        delete transaction;
    }
}

void DeleteUndoScenario::Execute(Document ^ document)
{
    ArgumentNullException::ThrowIfNull(document);

    Editor ^ editor = document->Editor;
    Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

    try
    {
        Database ^ database = document->Database;

        Guid vertexAId = ReadVertexAId(database, transaction);

        ObjectId vertexObjectId = NativeTestDwgHelpers::FindVertex(database, transaction, vertexAId);

        if (vertexObjectId.IsNull)
        {
            throw gcnew InvalidOperationException("Vertex A could not be found in ModelSpace.");
        }

        NativeVertexDeletionService ^ service = gcnew NativeVertexDeletionService();

        int deletedEdges = service->Delete(database, transaction, vertexObjectId);

        if (deletedEdges != 2)
        {
            throw gcnew InvalidOperationException(
                String::Format("Expected 2 deleted incident edges, actual {0}.", deletedEdges));
        }

        transaction->Commit();

        editor->WriteMessage("\n[EXECUTED] C++ cascade delete.");

        editor->WriteMessage("\nVertex A deleted.");

        editor->WriteMessage("\nIncident edges deleted: {0}", deletedEdges);

        editor->WriteMessage("\nUNDO and C# verification are performed by GRAPHTESTS.");
    }
    catch (System::Exception ^ ex)
    {
        editor->WriteMessage("\n[FAIL] Execute delete/undo test: {0}", ex->ToString());
    }
    finally
    {
        delete transaction;
    }
}
} // namespace GraphPlugin::Native::Tests
