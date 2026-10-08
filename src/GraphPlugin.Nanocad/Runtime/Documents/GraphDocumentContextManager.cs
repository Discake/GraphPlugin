using HostMgd.ApplicationServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphDocumentContextManager
{
    private readonly Dictionary<Document, GraphDocumentContext>
        _contexts = new();

    private readonly GraphDocumentContextFactory
        _factory = new();

    public GraphDocumentContext GetOrCreate(
        Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_contexts.TryGetValue(
                document,
                out var existing))
        {
            return existing;
        }

        var context =
            _factory.Create(document);

        context.Watcher.Start();

        _contexts.Add(
            document,
            context);

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
