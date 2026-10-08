using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence;
using Teigha.DatabaseServices;
using NanoApplication = HostMgd.ApplicationServices.Application;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class VertexSelectionService
{
    private readonly VertexEntityMapper _mapper;

    public VertexSelectionService(VertexEntityMapper mapper)
    {
        _mapper = mapper;
    }

    public GraphVertex? ReadVertex(ObjectId objectId)
    {
        var document = NanoApplication.DocumentManager.MdiActiveDocument;

        var database = document.Database;

        using var transaction = database.TransactionManager.StartTransaction();

        var entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;

        if (entity is null)
            return null;

        var vertex = _mapper.ToDomain(entity, transaction);

        transaction.Commit();

        return vertex;
    }
}
