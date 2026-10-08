using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Runtime;

using HostMgd.ApplicationServices;

using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphBuildStepExecutor
{
    private readonly Document _document;
    private readonly VertexService _vertexService;
    private readonly SplitEdgeService _splitEdgeService;
    private readonly GraphBuildService _buildService;

    public GraphBuildStepExecutor(
        Document document,
        VertexService vertexService,
        SplitEdgeService splitEdgeService,
        GraphBuildService buildService)
    {
        _document = document;
        _vertexService = vertexService;
        _splitEdgeService = splitEdgeService;
        _buildService = buildService;
    }

    public GraphVertex Execute(
        BuildPickResult pick)
    {
        GraphVertex targetVertex =
            pick.Kind switch
            {
                BuildPickKind.Empty =>
                    CreateVertex(pick),

                BuildPickKind.Vertex =>
                    pick.Vertex
                    ?? throw new InvalidOperationException(
                        "Vertex pick does not contain a vertex."),

                BuildPickKind.Edge =>
                    SplitEdge(pick),

                _ =>
                    throw new InvalidOperationException(
                        $"Cannot execute build step '{pick.Kind}'.")
            };

        _buildService.AdvanceTo(
            targetVertex);

        return targetVertex;
    }

    private GraphVertex CreateVertex(
        BuildPickResult pick)
    {
        return _vertexService.CreateVertex(
            new Point2(
                pick.Point.X,
                pick.Point.Y),
            VertexShape.Circle);
    }

    private GraphVertex SplitEdge(
        BuildPickResult pick)
    {
        var edge =
            pick.Edge
            ?? throw new InvalidOperationException(
                "Edge pick does not contain an edge.");

        if (pick.ObjectId.IsNull)
        {
            throw new InvalidOperationException(
                "Edge pick does not contain ObjectId.");
        }

        var splitPosition =
            ProjectOntoEdge(
                pick.ObjectId,
                pick.PickPoint);

        var result =
            _splitEdgeService.Split(
                edge.Id,
                splitPosition,
                VertexShape.Circle);

        return result.NewVertex;
    }

    private Point2 ProjectOntoEdge(
        ObjectId objectId,
        Point3d pointWcs)
    {
        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var polyline =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Polyline
            ?? throw new InvalidOperationException(
                "Selected graph edge is not a Polyline.");

        using var view =
            _document.Editor
                .GetCurrentView();

        var pointOnPolyline =
            polyline.GetClosestPointTo(
                pointWcs,
                view.ViewDirection,
                false);

        return new Point2(
            pointOnPolyline.X,
            pointOnPolyline.Y);
    }
}
