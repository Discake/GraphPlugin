using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Domain.Algorithms;

public sealed class EdgeLengthCalculator
{
    public double Calculate(GraphEdge edge, GraphVertex first, GraphVertex second)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Id == edge.VertexAId && second.Id == edge.VertexBId)
        {
            return EdgeRouteGeometry.CalculateLength(first.Position, second.Position, edge.Route);
        }

        if (first.Id == edge.VertexBId && second.Id == edge.VertexAId)
        {
            var reversedRoute = new EdgeRoute(edge.Route.IntermediatePoints.Reverse());

            return EdgeRouteGeometry.CalculateLength(first.Position, second.Position, reversedRoute);
        }

        throw new ArgumentException("Provided vertices are not endpoints of the edge.");
    }
}
