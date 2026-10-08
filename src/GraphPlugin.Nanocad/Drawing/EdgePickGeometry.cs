using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Drawing;

public sealed class EdgePickGeometry
{
    private readonly Document _document;

    public EdgePickGeometry(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public Point2 ProjectOntoEdge(ObjectId edgeObjectId, Point3d pointWcs)
    {
        using var transaction = _document.Database.TransactionManager.StartTransaction();

        var polyline =
            transaction.GetObject(edgeObjectId, OpenMode.ForRead) as Polyline
            ?? throw new InvalidOperationException("Graph edge entity is not a Polyline.");

        using var view = _document.Editor.GetCurrentView();

        var projected = polyline.GetClosestPointTo(pointWcs, view.ViewDirection, false);

        return new Point2(projected.X, projected.Y);
    }

    public int FindNearestBend(GraphEdge edge, Point3d pointWcs)
    {
        ArgumentNullException.ThrowIfNull(edge);

        if (edge.Route.Count == 0)
        {
            throw new InvalidOperationException("Edge does not contain bends.");
        }

        var point = new Point2(pointWcs.X, pointWcs.Y);

        var bestIndex = 0;

        var bestDistance = edge.Route.IntermediatePoints[0].DistanceTo(point);

        for (var i = 1; i < edge.Route.Count; i++)
        {
            var distance = edge.Route.IntermediatePoints[i].DistanceTo(point);

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;

            bestIndex = i;
        }

        return bestIndex;
    }
}
