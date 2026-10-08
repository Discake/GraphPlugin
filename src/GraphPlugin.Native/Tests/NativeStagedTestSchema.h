#pragma once
using namespace System;

namespace GraphPlugin::Native::Tests
{
    public ref class NativeStagedTestSchema abstract sealed
    {
    public:
        literal String^ StyleRecord =
            "GRAPH_CPP_STYLE_INTEROP_TEST";

        literal String^ DeleteUndoRecord =
            "GRAPH_CPP_DELETE_UNDO_TEST";

        literal int Version = 1;
    };
}
