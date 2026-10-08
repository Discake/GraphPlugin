#include "NativeVertexDeletionService.h"

#include "../Persistence/VertexMetadataStore.h"
#include "../Persistence/EdgeMetadataStore.h"

using namespace System;
using namespace Teigha::DatabaseServices;

using namespace GraphPlugin::Native::Persistence;

namespace GraphPlugin::Native::Services
{
    int NativeVertexDeletionService::Delete(
        Database^ database,
        Transaction^ transaction,
        ObjectId vertexObjectId)
    {
        if (database == nullptr)
            throw gcnew ArgumentNullException("database");

        if (transaction == nullptr)
            throw gcnew ArgumentNullException("transaction");

        Entity^ vertexEntity =
            dynamic_cast<Entity^>(
                transaction->GetObject(
                    vertexObjectId,
                    OpenMode::ForWrite));

        if (vertexEntity == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Selected object is not an Entity.");
        }

        VertexMetadataStore^ vertexStore =
            gcnew VertexMetadataStore();

        NativeVertexMetadata^ vertexMetadata =
            vertexStore->Read(
                vertexEntity,
                transaction);

        if (vertexMetadata == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Selected object is not a graph vertex.");
        }

        Guid vertexId =
            vertexMetadata->VertexId;

        EdgeMetadataStore^ edgeStore =
            gcnew EdgeMetadataStore();

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
                        OpenMode::ForRead));

        if (modelSpace == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Model space could not be opened.");
        }

        //
        // Сначала собираем ObjectId.
        // Не стираем прямо во время enumeration.
        //
        System::Collections::Generic::
            List<ObjectId>^ incidentEdges =
            gcnew System::Collections::Generic::
            List<ObjectId>();

        for each (
            ObjectId objectId
            in modelSpace)
        {
            if (objectId ==
                vertexObjectId)
            {
                continue;
            }

            Entity^ entity =
                dynamic_cast<Entity^>(
                    transaction->GetObject(
                        objectId,
                        OpenMode::ForRead));

            if (entity == nullptr)
                continue;

            NativeEdgeMetadata^ edge =
                edgeStore->Read(
                    entity,
                    transaction);

            if (edge == nullptr)
                continue;

            if (edge->VertexAId == vertexId ||
                edge->VertexBId == vertexId)
            {
                incidentEdges->Add(
                    objectId);
            }
        }

        //
        // Сначала Edge.
        //
        for each (
            ObjectId edgeObjectId
            in incidentEdges)
        {
            Entity^ edgeEntity =
                dynamic_cast<Entity^>(
                    transaction->GetObject(
                        edgeObjectId,
                        OpenMode::ForWrite));

            if (edgeEntity == nullptr ||
                edgeEntity->IsErased)
            {
                continue;
            }

            edgeEntity->Erase();
        }

        //
        // Потом Vertex.
        //
        vertexEntity->Erase();

        return incidentEdges->Count;
    }
}