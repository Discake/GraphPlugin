#pragma once

using namespace System;

namespace GraphPlugin::Native::Persistence
{
    public ref class NativeEdgeMetadata sealed
    {
    public:
        property Guid EdgeId;

        property Guid VertexAId;

        property Guid VertexBId;
    };
}