using GraphPlugin.Domain.Models;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GraphPlugin.Nanocad.Runtime;

public enum BuildPickKind
{
    Empty,
    Vertex,
    Edge,
    Finish,
}

public sealed record BuildPickResult(
    BuildPickKind Kind,
    Point3d Point,
    Point3d PickPoint,
    GraphVertex? Vertex = null,
    GraphEdge? Edge = null,
    ObjectId ObjectId = default
)
{
    public static BuildPickResult Empty(Point3d point, Point3d pickPoint, ObjectId objectId = default)
    {
        return new BuildPickResult(BuildPickKind.Empty, point, pickPoint, ObjectId: objectId);
    }

    public static BuildPickResult FromVertex(Point3d point, Point3d pickPoint, ObjectId objectId, GraphVertex vertex)
    {
        return new BuildPickResult(BuildPickKind.Vertex, point, pickPoint, Vertex: vertex, ObjectId: objectId);
    }

    public static BuildPickResult FromEdge(Point3d point, Point3d pickPoint, ObjectId objectId, GraphEdge edge)
    {
        return new BuildPickResult(BuildPickKind.Edge, point, pickPoint, Edge: edge, ObjectId: objectId);
    }

    public static BuildPickResult Finish()
    {
        return new BuildPickResult(BuildPickKind.Finish, Point3d.Origin, Point3d.Origin);
    }
}
