#include "EdgeMetadataStore.h"
#include "GraphDwgSchema.h"

using namespace System;
using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Persistence
{
NativeEdgeMetadata ^ EdgeMetadataStore::Read(Entity ^ entity, Transaction ^ transaction)
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

    if (!dictionary->Contains(GraphDwgSchema::EdgeRecord))
    {
        return nullptr;
    }

    ObjectId recordId = dictionary->GetAt(GraphDwgSchema::EdgeRecord);

    Xrecord ^ record = dynamic_cast<Xrecord ^>(transaction->GetObject(recordId, OpenMode::ForRead));

    if (record == nullptr || record->Data == nullptr)
    {
        return nullptr;
    }

    array<TypedValue> ^ values = record->Data->AsArray();

    //
    // GRAPH_EDGE:
    //
    // 0 Version
    // 1 EdgeId
    // 2 VertexAId
    // 3 VertexBId
    //
    if (values->Length < 4)
    {
        throw gcnew InvalidOperationException("GRAPH_EDGE contains invalid metadata.");
    }

    int version = Convert::ToInt32(values[0].Value);

    if (version != GraphDwgSchema::Version)
    {
        throw gcnew InvalidOperationException(String::Format("Unsupported GRAPH_EDGE version: {0}.", version));
    }

    Guid edgeId;
    Guid vertexAId;
    Guid vertexBId;

    if (!Guid::TryParse(safe_cast<String ^>(values[1].Value), edgeId))
    {
        throw gcnew InvalidOperationException("Invalid EdgeId.");
    }

    if (!Guid::TryParse(safe_cast<String ^>(values[2].Value), vertexAId))
    {
        throw gcnew InvalidOperationException("Invalid VertexAId.");
    }

    if (!Guid::TryParse(safe_cast<String ^>(values[3].Value), vertexBId))
    {
        throw gcnew InvalidOperationException("Invalid VertexBId.");
    }

    NativeEdgeMetadata ^ metadata = gcnew NativeEdgeMetadata();

    metadata->EdgeId = edgeId;

    metadata->VertexAId = vertexAId;

    metadata->VertexBId = vertexBId;

    return metadata;
}
} // namespace GraphPlugin::Native::Persistence