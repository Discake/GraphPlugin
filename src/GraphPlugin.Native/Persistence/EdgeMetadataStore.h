#pragma once

#include "NativeEdgeMetadata.h"

using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Persistence
{
    public ref class EdgeMetadataStore sealed
    {
    public:
        NativeEdgeMetadata^ Read(
            Entity^ entity,
            Transaction^ transaction);
    };
}