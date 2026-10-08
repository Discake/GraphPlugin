#include "NativeTestDwgHelpers.h"

#include "../Persistence/GraphDwgSchema.h"
#include "../Persistence/NativeVertexMetadata.h"
#include "../Persistence/VertexMetadataStore.h"

using namespace System;
using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;
using namespace GraphPlugin::Native::Persistence;

namespace
{
    void WriteEdgeMetadata(
        Entity^ entity,
        Transaction^ transaction,
        Guid edgeId,
        Guid vertexAId,
        Guid vertexBId)
    {
        if (entity->ExtensionDictionary.IsNull)
            entity->CreateExtensionDictionary();

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

        values[0] = TypedValue(
            static_cast<int>(DxfCode::Int32),
            GraphDwgSchema::Version);

        values[1] = TypedValue(
            static_cast<int>(DxfCode::Text),
            edgeId.ToString("D"));

        values[2] = TypedValue(
            static_cast<int>(DxfCode::Text),
            vertexAId.ToString("D"));

        values[3] = TypedValue(
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
}

namespace GraphPlugin::Native::Tests
{
    BlockTableRecord^ NativeTestDwgHelpers::GetModelSpace(
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

    Circle^ NativeTestDwgHelpers::CreateVertex(
        Transaction^ transaction,
        BlockTableRecord^ modelSpace,
        Guid vertexId,
        Point3d position)
    {
        constexpr double Size = 10.0;

        Circle^ circle = gcnew Circle(
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

    ObjectId NativeTestDwgHelpers::FindVertex(
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

            if (entity == nullptr || entity->IsErased)
                continue;

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

    void NativeTestDwgHelpers::WriteAttachment(
        Entity^ entity,
        Transaction^ transaction,
        String^ path)
    {
        if (entity->ExtensionDictionary.IsNull)
            entity->CreateExtensionDictionary();

        DBDictionary^ dictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    entity->ExtensionDictionary,
                    OpenMode::ForWrite));

        if (dictionary == nullptr)
        {
            throw gcnew InvalidOperationException(
                "ExtensionDictionary could not be opened.");
        }

        array<TypedValue>^ values =
            gcnew array<TypedValue>(3);

        values[0] = TypedValue(
            static_cast<int>(DxfCode::Int32),
            GraphDwgSchema::Version);

        values[1] = TypedValue(
            static_cast<int>(DxfCode::Int32),
            1);

        values[2] = TypedValue(
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

    Polyline^ NativeTestDwgHelpers::CreateEdge(
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

        WriteEdgeMetadata(
            edge,
            transaction,
            edgeId,
            vertexAId,
            vertexBId);

        return edge;
    }
}
