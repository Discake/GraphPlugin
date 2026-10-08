using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Application.Services;
using GraphPlugin.NanoCad.Drawing;
using GraphPlugin.NanoCad.Runtime;
using HostMgd.ApplicationServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphDocumentContext
{
    public Document Document { get; }

    public GraphEntityIndex Index { get; }

    public IVertexRepository Vertices { get; }

    public IEdgeRepository Edges { get; }

    public VertexService VertexService { get; }

    public EdgeService EdgeService { get; }

    public GraphService Graph { get; }

    public GraphBuildService GraphBuild { get; }

    public SplitEdgeService SplitEdge { get; }

    public ShortestPathApplicationService ShortestPath { get; }

    public GraphBuildStepExecutor BuildStepExecutor { get; }

    public GraphDatabaseWatcher Watcher { get; }

    public VertexSelectionService VertexSelection { get; }

    public EdgeSelectionService EdgeSelection { get; }

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
        VertexService vertexService,
        EdgeService edgeService,
        GraphService graph,
        GraphBuildService graphBuild,
        SplitEdgeService splitEdge,
        ShortestPathApplicationService shortestPath,
        GraphBuildStepExecutor buildStepExecutor,
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
        VertexService = vertexService;
        EdgeService = edgeService;
        Graph = graph;
        GraphBuild = graphBuild;
        SplitEdge = splitEdge;
        ShortestPath = shortestPath;
        BuildStepExecutor = buildStepExecutor;
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
