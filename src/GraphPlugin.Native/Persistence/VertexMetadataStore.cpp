#include "VertexMetadataStore.h"
#include "GraphDwgSchema.h"

using namespace System;
using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Persistence
{
void VertexMetadataStore::Write(Entity ^ entity, Transaction ^ transaction, NativeVertexMetadata ^ metadata)
{
    if (entity == nullptr)
        throw gcnew ArgumentNullException("entity");

    if (transaction == nullptr)
        throw gcnew ArgumentNullException("transaction");

    if (metadata == nullptr)
        throw gcnew ArgumentNullException("metadata");

    if (entity->ExtensionDictionary.IsNull)
    {
        entity->CreateExtensionDictionary();
    }

    DBDictionary ^ dictionary =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(entity->ExtensionDictionary, OpenMode::ForWrite));

    if (dictionary == nullptr)
    {
        throw gcnew InvalidOperationException("Vertex extension dictionary could not be opened.");
    }

    array<TypedValue> ^ values = gcnew array<TypedValue>(5);

    values[0] = TypedValue(static_cast<int>(DxfCode::Int32), GraphDwgSchema::Version);

    values[1] = TypedValue(static_cast<int>(DxfCode::Text), metadata->VertexId.ToString("D"));

    values[2] = TypedValue(static_cast<int>(DxfCode::Int32), static_cast<int>(metadata->Shape));

    values[3] = TypedValue(static_cast<int>(DxfCode::Int32), static_cast<int>(metadata->Color));

    values[4] = TypedValue(static_cast<int>(DxfCode::Real), metadata->Size);

    ResultBuffer ^ buffer = gcnew ResultBuffer(values);

    Xrecord ^ record = nullptr;

    if (dictionary->Contains(GraphDwgSchema::VertexRecord))
    {
        ObjectId recordId = dictionary->GetAt(GraphDwgSchema::VertexRecord);

        record = dynamic_cast<Xrecord ^>(transaction->GetObject(recordId, OpenMode::ForWrite));

        if (record == nullptr)
        {
            throw gcnew InvalidOperationException("'GRAPH_VERTEX' is not an XRecord.");
        }
    }
    else
    {
        record = gcnew Xrecord();

        dictionary->SetAt(GraphDwgSchema::VertexRecord, record);

        transaction->AddNewlyCreatedDBObject(record, true);
    }

    record->Data = buffer;
}

NativeVertexMetadata ^ VertexMetadataStore::Read(Entity ^ entity, Transaction ^ transaction)
{
    if (entity == nullptr)
        throw gcnew ArgumentNullException("entity");

    if (transaction == nullptr)
        throw gcnew ArgumentNullException("transaction");

    if (entity->ExtensionDictionary.IsNull)
    {
        return nullptr;
    }

    DBDictionary ^ dictionary =
        dynamic_cast<DBDictionary ^>(transaction->GetObject(entity->ExtensionDictionary, OpenMode::ForRead));

    if (dictionary == nullptr)
        return nullptr;

    if (!dictionary->Contains(GraphDwgSchema::VertexRecord))
    {
        return nullptr;
    }

    ObjectId recordId = dictionary->GetAt(GraphDwgSchema::VertexRecord);

    Xrecord ^ record = dynamic_cast<Xrecord ^>(transaction->GetObject(recordId, OpenMode::ForRead));

    if (record == nullptr || record->Data == nullptr)
    {
        return nullptr;
    }

    array<TypedValue> ^ values = record->Data->AsArray();

    if (values->Length < 5)
    {
        throw gcnew InvalidOperationException("GRAPH_VERTEX contains invalid metadata.");
    }

    int version = Convert::ToInt32(values[0].Value);

    if (version != GraphDwgSchema::Version)
    {
        throw gcnew InvalidOperationException(String::Format("Unsupported GRAPH_VERTEX version: {0}.", version));
    }

    Guid vertexId;

    if (!Guid::TryParse(safe_cast<String ^>(values[1].Value), vertexId))
    {
        throw gcnew InvalidOperationException("GRAPH_VERTEX contains invalid VertexId.");
    }

    NativeVertexMetadata ^ metadata = gcnew NativeVertexMetadata();

    metadata->VertexId = vertexId;

    metadata->Shape = static_cast<NativeVertexShape>(Convert::ToInt32(values[2].Value));

    metadata->Color = static_cast<NativeGraphColor>(Convert::ToInt32(values[3].Value));

    metadata->Size = Convert::ToDouble(values[4].Value);

    return metadata;
}
} // namespace GraphPlugin::Native::Persistence