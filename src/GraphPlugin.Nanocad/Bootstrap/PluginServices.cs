using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.NanoCad.Bootstrap;

public static class PluginServices
{
    private static readonly GraphDocumentContextManager Contexts = new();

    public static GraphDocumentContext CurrentContext {
        get
        {
            var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

            return Contexts.GetOrCreate(
                document);
        }
    }

    public static void InitializeDocument(Document document)
    {
        Contexts.GetOrCreate(document);
    }

    public static void RemoveDocument(Document document)
    {
        Contexts.Remove(document);
    }

    public static void Shutdown()
    {
        Contexts.Shutdown();
    }

    public static VertexService CreateVertexService()
    {
        var context =
            CurrentContext;

        return new VertexService(
            context.Vertices);
    }

    public static EdgeService CreateEdgeService()
    {
        var context =
            CurrentContext;

        return new EdgeService(
            context.Vertices,
            context.Edges);
    }

    public static GraphBuildService CreateGraphBuildService()
    {
        var context =
            CurrentContext;

        var edgeService =
            new EdgeService(
                context.Vertices,
                context.Edges);

        return new GraphBuildService(
            edgeService, context.Edges);
    }

    public static ShortestPathApplicationService CreateShortestPathService()
    {
        var context =
            CurrentContext;

        var lengthCalculator =
            new EdgeLengthCalculator();

        var algorithm =
            new DijkstraShortestPathService(
                lengthCalculator);

        return new ShortestPathApplicationService(
            context.Vertices,
            context.Edges,
            algorithm);
    }
}