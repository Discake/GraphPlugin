using GraphPlugin.Application.Attachments;
using GraphPlugin.Application.Editing.Bends;
using GraphPlugin.Application.Editing.Splitting;
using GraphPlugin.Application.Graph;
using GraphPlugin.Application.Routing;
using GraphPlugin.Application.Styling;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Persistence.Metadata;
using HostMgd.ApplicationServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphDocumentContextFactory
{
    public GraphDocumentContext Create(
        Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var metadata =
            new XRecordMetadataStore();

        var index =
            new GraphEntityIndexBuilder(metadata)
                .Build(document.Database);

        var vertexMapper =
            new VertexEntityMapper(metadata);

        var edgeMapper =
            new EdgeEntityMapper();

        var vertexRepository =
            new NanoCadVertexRepository(
                document,
                new VertexEntityFactory(),
                metadata,
                vertexMapper,
                index);

        var settingsRepository =
            new NanoCadGraphSettingsRepository(
                document);

        var edgeStyleApplier =
            new EdgeStyleApplier(
                index,
                new LinetypeManager());

        var edgeRepository =
            new NanoCadEdgeRepository(
                document,
                vertexRepository,
                new EdgeEntityFactory(edgeMapper),
                metadata,
                index,
                settingsRepository,
                edgeStyleApplier,
                edgeMapper);

        var vertexService =
            new VertexService(
                vertexRepository,
                edgeRepository);

        var edgeService =
            new EdgeService(
                vertexRepository,
                edgeRepository);

        var graphBuildService =
            new GraphBuildService(
                edgeService,
                edgeRepository);

        var splitEdgeService =
            new SplitEdgeService(
                vertexRepository,
                edgeRepository);

        var shortestPathService =
            new ShortestPathService(
                vertexRepository,
                edgeRepository,
                new DijkstraShortestPath(
                    new EdgeLengthCalculator()));

        var synchronizer =
            new EdgeGeometrySynchronizer(
                vertexRepository,
                edgeRepository,
                index,
                edgeMapper);

        var watcher =
            new GraphDatabaseWatcher(
                document,
                index,
                synchronizer,
                edgeService,
                metadata);

        var settingsService =
            new GraphSettingsService(
                settingsRepository,
                edgeStyleApplier);

        var vertexSelection =
            new VertexSelectionService(
                vertexMapper);

        var edgeSelection =
            new EdgeSelectionService(
                edgeRepository,
                index);

        var pathHighlighter =
            new ShortestPathHighlighter(
                document,
                index);

        var buildPick =
            new NanoCadBuildPickService(
                document.Editor,
                index,
                vertexRepository,
                edgeRepository);

        var buildStepExecutor =
            new GraphBuildStepExecutor(
                document,
                vertexService,
                splitEdgeService,
                graphBuildService);

        var addBend =
            new AddBendService(
                vertexRepository,
                edgeRepository);

        var moveBend =
            new MoveBendService(
                vertexRepository,
                edgeRepository);

        var removeBend =
            new RemoveBendService(
                edgeRepository);

        var edgePickGeometry =
            new EdgePickGeometry(
                document);

        var attachmentStore =
            new VertexAttachmentXRecordStore();

        var attachments =
            new NanoCadVertexAttachmentRepository(
                document,
                index,
                attachmentStore);

        var attachmentService =
            new VertexAttachmentService(
                vertexRepository,
                attachments,
                new AttachmentPathResolver());

        return new GraphDocumentContext(
            document,
            index,
            vertexRepository,
            edgeRepository,
            vertexService,
            edgeService,
            graphBuildService,
            splitEdgeService,
            shortestPathService,
            buildStepExecutor,
            vertexSelection,
            edgeSelection,
            watcher,
            settingsService,
            pathHighlighter,
            buildPick,
            addBend,
            moveBend,
            removeBend,
            edgePickGeometry,
            synchronizer,
            attachments,
            attachmentService);
    }
}
