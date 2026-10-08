#pragma once

using namespace System;

namespace GraphPlugin::Native::Persistence
{
public
ref class GraphDwgSchema abstract sealed
{
  public:
    literal int Version = 1;

    literal String ^ VertexRecord = "GRAPH_VERTEX";

    literal String ^ EdgeRecord = "GRAPH_EDGE";

    literal String ^ VertexAttachmentsRecord = "GRAPH_VERTEX_ATTACHMENTS";

    literal String ^ SettingsRecord = "GRAPH_PLUGIN_SETTINGS";
};
} // namespace GraphPlugin::Native::Persistence