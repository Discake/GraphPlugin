#pragma once

#include "../Persistence/NativeVertexMetadata.h"

using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Services
{
public
ref class NativeVertexStyleService sealed
{
  public:
    ObjectId ChangeStyle(Database ^ database,
                         Transaction ^ transaction,
                         ObjectId vertexObjectId,
                         Persistence::NativeVertexShape targetShape);
};
} // namespace GraphPlugin::Native::Services