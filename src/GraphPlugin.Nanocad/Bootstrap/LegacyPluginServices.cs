using GraphPlugin.Application.Services;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;

namespace GraphPlugin.NanoCad.Bootstrap;

public static class PluginServices
{
    public static GraphDocumentContext CurrentContext =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.CurrentContext;

    public static void InitializeDocument(Document document) =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.InitializeDocument(document);

    public static void RemoveDocument(Document document) =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.RemoveDocument(document);

    public static void Shutdown() =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.Shutdown();

    public static VertexService CreateVertexService() =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.CreateVertexService();

    public static EdgeService CreateEdgeService() =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.CreateEdgeService();

    public static GraphBuildService CreateGraphBuildService() =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.CreateGraphBuildService();

    public static ShortestPathApplicationService CreateShortestPathService() =>
        GraphPlugin.Nanocad.Bootstrap.PluginServices.CreateShortestPathService();
}
