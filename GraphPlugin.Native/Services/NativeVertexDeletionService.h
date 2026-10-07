#pragma once

using namespace System;
using namespace Teigha::DatabaseServices;

namespace GraphPlugin::Native::Services
{
    public ref class NativeVertexDeletionService sealed
    {
    public:
        int Delete(
            Database^ database,
            Transaction^ transaction,
            ObjectId vertexObjectId);
    };
}