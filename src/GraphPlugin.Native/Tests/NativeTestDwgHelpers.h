#pragma once

using namespace System;
using namespace Teigha::DatabaseServices;
using namespace Teigha::Geometry;

namespace GraphPlugin::Native::Tests
{
public
ref class NativeTestDwgHelpers abstract sealed
{
  public:
    static BlockTableRecord ^ GetModelSpace(Database ^ database, Transaction ^ transaction, OpenMode mode);

    static Circle ^
        CreateVertex(Transaction ^ transaction, BlockTableRecord ^ modelSpace, Guid vertexId, Point3d position);

    static ObjectId FindVertex(Database ^ database, Transaction ^ transaction, Guid vertexId);

    static void WriteAttachment(Entity ^ entity, Transaction ^ transaction, String ^ path);

    static Polyline ^ CreateEdge(Transaction ^ transaction,
                                 BlockTableRecord ^ modelSpace,
                                 Guid edgeId,
                                 Guid vertexAId,
                                 Guid vertexBId,
                                 Point2d start,
                                 Point2d end);
};
} // namespace GraphPlugin::Native::Tests
