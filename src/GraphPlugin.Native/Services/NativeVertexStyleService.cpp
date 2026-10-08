#include "NativeVertexStyleService.h"
#include "../Persistence/VertexMetadataStore.h"
#include "../Persistence/GraphDwgSchema.h"

using namespace Teigha::Geometry;

using namespace GraphPlugin::Native::Persistence;
using namespace GraphPlugin::Native::Services;

namespace {
    Point3d GetVertexCenter(
        Entity^ entity)
    {
        Circle^ circle =
            dynamic_cast<Circle^>(
                entity);

        if (circle != nullptr)
        {
            return circle->Center;
        }

        Polyline^ polyline =
            dynamic_cast<Polyline^>(
                entity);

        if (polyline != nullptr)
        {
            Extents3d extents =
                polyline->GeometricExtents;

            Point3d min =
                extents.MinPoint;

            Point3d max =
                extents.MaxPoint;

            return Point3d(
                (min.X + max.X) / 2.0,
                (min.Y + max.Y) / 2.0,
                (min.Z + max.Z) / 2.0);
        }

        throw gcnew InvalidOperationException(
            "Unsupported graph vertex entity type.");
    }

    Entity^ CreateCircleVertex(
        Point3d center,
        double size)
    {
        Circle^ circle =
            gcnew Circle(
                center,
                Vector3d::ZAxis,
                size);

        //
        // ACI blue.
        //
        circle->ColorIndex =
            5;

        return circle;
    }

    Entity^ CreateTriangleVertex(
        Point3d center,
        double size)
    {
        Polyline^ triangle =
            gcnew Polyline(3);

        //
        // Bounding box специально симметричен
        // относительно center.
        //
        triangle->AddVertexAt(
            0,
            Point2d(
                center.X,
                center.Y + size),
            0.0,
            0.0,
            0.0);

        triangle->AddVertexAt(
            1,
            Point2d(
                center.X - size,
                center.Y - size),
            0.0,
            0.0,
            0.0);

        triangle->AddVertexAt(
            2,
            Point2d(
                center.X + size,
                center.Y - size),
            0.0,
            0.0,
            0.0);

        triangle->Closed =
            true;

        //
        // ACI red.
        //
        triangle->ColorIndex =
            1;

        return triangle;
    }

    void CopyXRecord(
        Entity^ source,
        Entity^ destination,
        Transaction^ transaction,
        String^ recordKey)
    {
        if (source->ExtensionDictionary.IsNull)
        {
            return;
        }

        DBDictionary^ sourceDictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    source->ExtensionDictionary,
                    OpenMode::ForRead));

        if (sourceDictionary == nullptr ||
            !sourceDictionary->Contains(
                recordKey))
        {
            return;
        }

        ObjectId sourceRecordId =
            sourceDictionary->GetAt(
                recordKey);

        Xrecord^ sourceRecord =
            dynamic_cast<Xrecord^>(
                transaction->GetObject(
                    sourceRecordId,
                    OpenMode::ForRead));

        if (sourceRecord == nullptr ||
            sourceRecord->Data == nullptr)
        {
            return;
        }

        array<TypedValue>^ copiedValues =
            sourceRecord
            ->Data
            ->AsArray();

        destination
            ->CreateExtensionDictionary();

        DBDictionary^ destinationDictionary =
            dynamic_cast<DBDictionary^>(
                transaction->GetObject(
                    destination->ExtensionDictionary,
                    OpenMode::ForWrite));

        if (destinationDictionary == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Destination extension dictionary " +
                "could not be opened.");
        }

        Xrecord^ copiedRecord =
            gcnew Xrecord();

        copiedRecord->Data =
            gcnew ResultBuffer(
                copiedValues);

        destinationDictionary->SetAt(
            recordKey,
            copiedRecord);

        transaction->AddNewlyCreatedDBObject(
            copiedRecord,
            true);
    }
}

ObjectId NativeVertexStyleService::ChangeStyle(
    Database^ database,
    Transaction^ transaction,
    ObjectId vertexObjectId,
    Persistence::NativeVertexShape targetShape) {

    try
    {
        Entity^ oldEntity =
            dynamic_cast<Entity^>(
                transaction->GetObject(
                    vertexObjectId,
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

        Point3d center =
            GetVertexCenter(
                oldEntity);

        Entity^ newEntity =
            nullptr;

        if (targetShape ==
            NativeVertexShape::Circle)
        {
            newEntity =
                CreateCircleVertex(
                    center,
                    metadata->Size);
        }
        else
        {
            newEntity =
                CreateTriangleVertex(
                    center,
                    metadata->Size);
        }

        //
        // Сохраняем основные CAD-свойства,
        // не связанные с Graph style.
        //
        newEntity->LayerId =
            oldEntity->LayerId;

        //
        // Добавляем replacement в тот же space,
        // где находилась старая Vertex.
        //
        BlockTableRecord^ owner =
            dynamic_cast<BlockTableRecord^>(
                transaction->GetObject(
                    oldEntity->OwnerId,
                    OpenMode::ForWrite));

        if (owner == nullptr)
        {
            throw gcnew InvalidOperationException(
                "Vertex owner could not be opened.");
        }

        ObjectId newObjectId =
            owner->AppendEntity(
                newEntity);

        transaction
            ->AddNewlyCreatedDBObject(
                newEntity,
                true);

        //
        // Сначала переносим дополнительную
        // пользовательскую информацию.
        //
        CopyXRecord(
            oldEntity,
            newEntity,
            transaction,
            GraphDwgSchema::
            VertexAttachmentsRecord);

        //
        // Логическая вершина сохраняет
        // тот же GUID.
        //
        metadata->Shape =
            targetShape;

        metadata->Color =
            targetShape ==
            NativeVertexShape::Circle
            ? NativeGraphColor::Blue
            : NativeGraphColor::Red;

        metadataStore->Write(
            newEntity,
            transaction,
            metadata);

        //
        // КЛЮЧЕВОЙ ПОРЯДОК:
        //
        // new Entity уже существует
        // и уже имеет тот же VertexId,
        // только теперь стираем старую.
        //
        oldEntity->Erase();

        return newEntity->ObjectId;
    }
    catch (System::Exception^ ex)
    {
        throw gcnew System::Exception(
            "\nGRAPHCPPVERTEXSTYLE failed: " + ex->ToString());
    }
}