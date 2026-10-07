using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Application.Services;
using GraphPlugin.Nanocad.Runtime;
using GraphPlugin.NanoCad.Drawing;
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
        NanoCadBuildPickService buildPick,
        AddBendService addBend,
        MoveBendService moveBend,
        RemoveBendService removeBend,
        EdgePickGeometry edgePickGeometry,
        EdgeGeometrySynchronizer edgeGeometrySynchronizer,
        IVertexAttachmentRepository attachments,
        VertexAttachmentService attachmentService)
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
        BuildPick = buildPick;
        AddBend = addBend;
        MoveBend = moveBend;
        RemoveBend = removeBend;
        EdgePickGeometry = edgePickGeometry;
        EdgeGeometrySynchronizer = edgeGeometrySynchronizer;
        Attachments = attachments;
        AttachmentService = attachmentService;
    }
}
