#pragma once

using namespace System;

namespace GraphPlugin::Native::Persistence
{
    public enum class NativeVertexShape
    {
        Circle = 0,
        Triangle = 1
    };

    public enum class NativeGraphColor
    {
        Blue = 0,
        Red = 1,
        Green = 2,
        White = 3,
        Black = 4
    };

    public ref class NativeVertexMetadata sealed
    {
    public:
        property Guid VertexId;

        property NativeVertexShape Shape;

        property NativeGraphColor Color;

        property double Size;
    };
}