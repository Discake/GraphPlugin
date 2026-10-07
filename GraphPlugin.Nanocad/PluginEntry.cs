using GraphPlugin.NanoCad.Bootstrap;
using HostMgd.ApplicationServices;
using Teigha.Runtime;

using NanoApplication =
    HostMgd.ApplicationServices.Application;

public sealed class PluginEntry : IExtensionApplication
{
    public void Initialize()
    {
        var manager =
            NanoApplication.DocumentManager;

        manager.DocumentCreated +=
            OnDocumentCreated;

        manager.DocumentToBeDestroyed +=
            OnDocumentToBeDestroyed;

        var document =
            manager.MdiActiveDocument;

        if (document is not null)
        {
            PluginServices.InitializeDocument(
                document);
        }
    }

    public void Terminate()
    {
        var manager =
            NanoApplication.DocumentManager;

        manager.DocumentCreated -=
            OnDocumentCreated;

        manager.DocumentToBeDestroyed -=
            OnDocumentToBeDestroyed;

        PluginServices.Shutdown();
    }

    private static void OnDocumentCreated(
        object sender,
        DocumentCollectionEventArgs e)
    {
        PluginServices.InitializeDocument(
            e.Document);
    }

    private static void OnDocumentToBeDestroyed(
        object sender,
        DocumentCollectionEventArgs e)
    {
        PluginServices.RemoveDocument(
            e.Document);
    }
}