namespace GraphPlugin.Nanocad.Persistence.Metadata;

internal static class GraphMetadataKeys
{
    public const string VertexRecord = "GRAPH_VERTEX";

    public const int CurrentVertexVersion = 1;

    public const string EdgeRecord = "GRAPH_EDGE";

    public const int CurrentEdgeVersion = 1;

    public const string VertexAttachmentsRecord =
        "GRAPH_VERTEX_ATTACHMENTS";

    public const int CurrentVertexAttachmentsVersion = 1;

    public const string SettingsRecord = "GRAPH_PLUGIN_SETTINGS";

    public const int CurrentSettingsVersion = 1;
}