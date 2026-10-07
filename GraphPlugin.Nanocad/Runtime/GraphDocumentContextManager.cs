using GraphPlugin.Application.Services;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.NanoCad.Drawing;
using GraphPlugin.NanoCad.Persistence;
using GraphPlugin.NanoCad.Persistence.Metadata;
using GraphPlugin.NanoCad.Runtime;
using HostMgd.ApplicationServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphDocumentContextManager
{
    private readonly Dictionary<Document, GraphDocumentContext>
        _contexts = new();

    public GraphDocumentContext GetOrCreate(
        Document document)
    {
        if (_contexts.TryGetValue(
                document,
                out var existing))
        {
            return existing;
        }

        var context = CreateContext(document);
        _contexts.Add(document, context);

        return context;
    }

    private GraphDocumentContext CreateContext(
        Document document)
    {
        var metadata =
            new XRecordMetadataStore();

        var indexBuilder =
            new GraphEntityIndexBuilder(
                metadata);

        var index =
            indexBuilder.Build(
                document.Database);

        var vertexMapper =
            new VertexEntityMapper(
                metadata);

        var edgeMapper = new EdgeEntityMapper();

        var vertexFactory =
            new VertexEntityFactory();

        var vertexRepository =
            new NanoCadVertexRepository(
                document,
                vertexFactory,
                metadata,
                vertexMapper,
                index);

        var edgeFactory =
            new EdgeEntityFactory(edgeMapper);

        var settingsRepository =
            new NanoCadGraphSettingsRepository(
                document);

        var lineTypes = new LinetypeManager();

        var styleApplier = new EdgeStyleApplier(index, lineTypes);

        var edgeRepository =
            new NanoCadEdgeRepository(
                document,
                vertexRepository,
                edgeFactory,
                metadata,
                index,
                settingsRepository,
                styleApplier,
                edgeMapper);

        var graphService =
            new GraphService(
                vertexRepository,
                edgeRepository);

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
                graphService,
                metadata);

        var vertexSelection =
            new VertexSelectionService(vertexMapper);

        var edgeSelection =
            new EdgeSelectionService(edgeRepository, index);

        var edgeStyleApplier =
            new EdgeStyleApplier(index, lineTypes);

        var settingsService =
            new GraphSettingsService(
                settingsRepository,
                edgeStyleApplier);

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

        var context =
            new GraphDocumentContext(
                document,
                index,
                vertexRepository,
                edgeRepository,
                graphService,
                vertexSelection,
                edgeSelection,
                watcher,
                settingsService,
                pathHighlighter,
                buildPick,
                synchronizer);

        watcher.Start();

        return context;
    }

    public void Remove(Document document)
    {
        if (!_contexts.TryGetValue(
                document,
                out var context))
        {
            return;
        }

        context.Watcher.Stop();

        _contexts.Remove(document);
    }

    public void Shutdown()
    {
        foreach (var context
                 in _contexts.Values)
        {
            context.Watcher.Stop();
        }

        _contexts.Clear();
    }
}
