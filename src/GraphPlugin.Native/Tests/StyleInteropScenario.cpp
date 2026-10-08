#include "StyleInteropScenario.h"

#include "../Services/NativeVertexStyleService.h"
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
void WriteManifest(
    Database ^ database, Transaction ^ transaction, Guid vertexId, String ^ oldHandle, String ^ attachmentPath)
{
    DBDictionary ^ nod =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(database->NamedObjectsDictionaryId, OpenMode::ForWrite));

    if (nod == nullptr)
    {
        throw gcnew InvalidOperationException("Named Objects Dictionary could not be opened.");
    }

    array<TypedValue> ^ values = gcnew array<TypedValue>(4);

    values[0] =
        TypedValue(static_cast<int>(DxfCode::Int32), GraphPlugin::Native::Tests::NativeStagedTestSchema::Version);

    values[1] = TypedValue(static_cast<int>(DxfCode::Text), vertexId.ToString("D"));

    values[2] = TypedValue(static_cast<int>(DxfCode::Text), oldHandle);

    values[3] = TypedValue(static_cast<int>(DxfCode::Text), attachmentPath);

    Xrecord ^ record = gcnew Xrecord();
    record->Data = gcnew ResultBuffer(values);

    nod->SetAt(GraphPlugin::Native::Tests::NativeStagedTestSchema::StyleRecord, record);

    transaction->AddNewlyCreatedDBObject(record, true);
}

Guid ReadVertexId(Database ^ database, Transaction ^ transaction)
{
    DBDictionary ^ nod =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(database->NamedObjectsDictionaryId, OpenMode::ForRead));

    if (nod == nullptr)
    {
        throw gcnew InvalidOperationException("Named Objects Dictionary could not be opened.");
    }

    if (!nod->Contains(GraphPlugin::Native::Tests::NativeStagedTestSchema::StyleRecord))
    {
        throw gcnew InvalidOperationException("Style interop test was not prepared.");
    }

    Xrecord ^ record = dynamic_cast<Xrecord ^>(transaction->GetObject(
        nod->GetAt(GraphPlugin::Native::Tests::NativeStagedTestSchema::StyleRecord), OpenMode::ForRead));

    if (record == nullptr || record->Data == nullptr)
    {
        throw gcnew InvalidOperationException("Style interop test manifest is invalid.");
    }

    array<TypedValue> ^ values = record->Data->AsArray();

    if (values->Length < 4)
    {
        throw gcnew InvalidOperationException("Style interop test manifest contains too few values.");
    }

    int version = Convert::ToInt32(values[0].Value);

    if (version != GraphPlugin::Native::Tests::NativeStagedTestSchema::Version)
    {
        throw gcnew InvalidOperationException("Unsupported style interop test manifest version.");
    }

    Guid vertexId;

    if (!Guid::TryParse(safe_cast<String ^>(values[1].Value), vertexId))
    {
        throw gcnew InvalidOperationException("Style interop test contains invalid VertexId.");
    }

    return vertexId;
}
} // namespace

namespace GraphPlugin::Native::Tests
{
void StyleInteropScenario::Prepare(Document ^ document)
{
    ArgumentNullException::ThrowIfNull(document);

    Editor ^ editor = document->Editor;
    Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

    try
    {
        BlockTableRecord ^ modelSpace =
            NativeTestDwgHelpers::GetModelSpace(document->Database, transaction, OpenMode::ForWrite);

        Guid vertexId = Guid::NewGuid();

        Circle ^ vertex =
            NativeTestDwgHelpers::CreateVertex(transaction, modelSpace, vertexId, Point3d(110000.0, 110000.0, 0.0));

        String ^ attachmentPath = "Files\\cpp-style-interop.pdf";

        NativeTestDwgHelpers::WriteAttachment(vertex, transaction, attachmentPath);

        WriteManifest(document->Database, transaction, vertexId, vertex->Handle.ToString(), attachmentPath);

        transaction->Commit();

        editor->WriteMessage("\n[PREPARED] C++ style interop test.");

        editor->WriteMessage("\nVertexId: {0}", vertexId);

        editor->WriteMessage("\nNext: GRAPHCPP_EXECUTE_STYLE_INTEROP_TEST "
                             "(normally driven by GRAPHTESTS).");
    }
    finally
    {
        delete transaction;
    }
}

void StyleInteropScenario::Execute(Document ^ document)
{
    ArgumentNullException::ThrowIfNull(document);

    Editor ^ editor = document->Editor;
    Transaction ^ transaction = document->Database->TransactionManager->StartTransaction();

    try
    {
        Guid vertexId = ReadVertexId(document->Database, transaction);

        ObjectId vertexObjectId = NativeTestDwgHelpers::FindVertex(document->Database, transaction, vertexId);

        if (vertexObjectId.IsNull)
        {
            throw gcnew InvalidOperationException("Test vertex was not found.");
        }

        NativeVertexStyleService ^ service = gcnew NativeVertexStyleService();

        service->ChangeStyle(document->Database, transaction, vertexObjectId, Persistence::NativeVertexShape::Triangle);

        transaction->Commit();

        editor->WriteMessage("\n[EXECUTED] Circle -> Triangle.");

        editor->WriteMessage("\nC# verification is performed by GRAPHTESTS.");
    }
    finally
    {
        delete transaction;
    }
}
} // namespace GraphPlugin::Native::Tests
