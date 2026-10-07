using GraphPlugin.Application.Abstractions;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Runtime;
using Teigha.DatabaseServices;
using NanoApplication =
    HostMgd.ApplicationServices.Application;

namespace GraphPlugin.NanoCad.Drawing;

public sealed class EdgeStyleApplier
    : IEdgeStyleApplier
{
    private readonly GraphEntityIndex _index;
    private readonly LinetypeManager _linetypes;

    public EdgeStyleApplier(
        GraphEntityIndex index,
        LinetypeManager linetypes)
    {
        _index = index;
        _linetypes = linetypes;
    }

    public void ApplyToAll(
        EdgeStyle style)
    {
        var document =
            NanoApplication
                .DocumentManager
                .MdiActiveDocument;

        var database =
            document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        foreach (var objectId
                 in _index.GetEdgeObjectIds())
        {
            if (objectId.IsErased)
                continue;

            if (transaction.GetObject(
                    objectId,
                    OpenMode.ForWrite)
                is not Entity entity)
            {
                continue;
            }

            ApplyStyle(
                entity,
                style,
                database,
                transaction);
        }

        transaction.Commit();
    }

    public void ApplyStyle(
        Entity entity,
        EdgeStyle style,
        Database database,
        Transaction transaction)
    {
        entity.Color =
            CadColorMapper.ToCadColor(
                style.Color);

        entity.LineWeight =
            LineWeightMapper.ToCadLineWeight(
                style.LineWeightMm);

        entity.LinetypeId =
            _linetypes.GetOrCreate(
                style.LineType,
                database,
                transaction);
    }
}