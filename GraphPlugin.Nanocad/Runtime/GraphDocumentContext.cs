using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Application.Services;
using GraphPlugin.Nanocad.Runtime;
using GraphPlugin.NanoCad.Drawing;
using GraphPlugin.NanoCad.Persistence;
using GraphPlugin.NanoCad.Runtime;
using HostMgd.ApplicationServices;

public sealed class GraphDocumentContext
{
    public Document Document { get; }

    public GraphEntityIndex Index { get; }

    public IVertexRepository Vertices { get; }

    public IEdgeRepository Edges { get; }

    public GraphDatabaseWatcher Watcher { get; }

    public VertexSelectionService VertexSelection { get; }

    public EdgeSelectionService EdgeSelection { get; }

    public GraphService Graph { get; }

    public GraphSettingsService Settings { get; }

    public ShortestPathHighlighter PathHighlighter { get; }

    public NanoCadBuildPickService BuildPick { get; }

    public AddBendService AddBend { get; }

    public MoveBendService MoveBend { get; }

    public RemoveBendService RemoveBend { get; }

    public EdgePickGeometry EdgePickGeometry { get; }

    public EdgeGeometrySynchronizer EdgeGeometrySynchronizer { get; }

    public IVertexAttachmentRepository Attachments { get; }

    public VertexAttachmentService AttachmentService { get; }

    public GraphDocumentContext(
        Document document,
        GraphEntityIndex index,
        IVertexRepository vertices,
        IEdgeRepository edges,
        GraphService graph,
        VertexSelectionService vertexSelection,
        EdgeSelectionService edgeSelection,
        GraphDatabaseWatcher watcher,
        GraphSettingsService settings,
        ShortestPathHighlighter pathHighlighter,
        NanoCadBuildPickService pickService,
        EdgeGeometrySynchronizer edgeGeometrySynchronizer)
    {
        Document = document;
        Index = index;
        Vertices = vertices;
        Edges = edges;
        Graph = graph;
        VertexSelection = vertexSelection;
        EdgeSelection = edgeSelection;
        Watcher = watcher;
        Settings = settings;
        PathHighlighter = pathHighlighter;
        BuildPick = pickService;
        EdgeGeometrySynchronizer = edgeGeometrySynchronizer;

        AddBend =
            new AddBendService(
                Vertices,
                Edges);

        MoveBend =
            new MoveBendService(
                Vertices,
                Edges);

        RemoveBend =
            new RemoveBendService(
                Edges);

        EdgePickGeometry =
            new EdgePickGeometry(
                document);

        var attachmentStore =
            new VertexAttachmentXRecordStore();

        Attachments =
            new NanoCadVertexAttachmentRepository(
                document,
                Index,
                attachmentStore);

        AttachmentService =
            new VertexAttachmentService(
                Vertices,
                Attachments,
                new AttachmentPathResolver());
    }
}