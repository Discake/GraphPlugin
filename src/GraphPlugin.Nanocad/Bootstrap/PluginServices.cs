using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;

using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Bootstrap;

public static class PluginServices
{
    private static readonly GraphDocumentContextManager Contexts = new();

    public static GraphDocumentContext CurrentContext
    {
        get
        {
            var document =
                NanoApplication
                    .DocumentManager
                    .MdiActiveDocument;

            return Contexts.GetOrCreate(document);
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
}
