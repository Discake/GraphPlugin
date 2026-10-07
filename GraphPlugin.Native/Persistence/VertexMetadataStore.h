#pragma once

#include "NativeVertexMetadata.h"

using namespace System;
using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Persistence
{
    public ref class VertexMetadataStore sealed
    {
    public:
        void Write(
            Entity^ entity,
            Transaction^ transaction,
            NativeVertexMetadata^ metadata);

        NativeVertexMetadata^ Read(
            Entity^ entity,
            Transaction^ transaction);
    };
}