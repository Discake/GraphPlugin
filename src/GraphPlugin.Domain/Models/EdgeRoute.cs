using GraphPlugin.Domain.Geometry;

namespace GraphPlugin.Domain.Models;

public sealed class EdgeRoute
{
    private readonly Point2[] _intermediatePoints;

    public IReadOnlyList<Point2> IntermediatePoints => Array.AsReadOnly(_intermediatePoints);

    public int Count => _intermediatePoints.Length;

    public bool IsStraight => _intermediatePoints.Length == 0;

    public static EdgeRoute Straight { get; } = new();

    public EdgeRoute(IEnumerable<Point2>? intermediatePoints = null)
    {
        _intermediatePoints = intermediatePoints?.ToArray() ?? Array.Empty<Point2>();
    }

    /// <summary>
    /// Возвращает полный маршрут:
    /// A -> P1 -> P2 -> ... -> B.
    /// </summary>
    public IEnumerable<Point2> EnumeratePath(Point2 start, Point2 end)
    {
        yield return start;

        foreach (var point in _intermediatePoints)
        {
            yield return point;
        }

        yield return end;
    }

    /// <summary>
    /// Добавляет bend в указанный сегмент полного маршрута.
    ///
    /// Для:
    /// A -> P1 -> P2 -> B
    ///
    /// segmentIndex:
    /// 0 = A-P1
    /// 1 = P1-P2
    /// 2 = P2-B
    /// </summary>
    public EdgeRoute InsertAtSegment(int segmentIndex, Point2 point)
    {
        if (segmentIndex < 0 || segmentIndex > _intermediatePoints.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));
        }

        var points = _intermediatePoints.ToList();

        points.Insert(segmentIndex, point);

        return new EdgeRoute(points);
    }

    public EdgeRoute MovePoint(int pointIndex, Point2 newPosition)
    {
        ValidatePointIndex(pointIndex);

        var points = _intermediatePoints.ToArray();

        points[pointIndex] = newPosition;

        return new EdgeRoute(points);
    }

    public EdgeRoute RemovePoint(int pointIndex)
    {
        ValidatePointIndex(pointIndex);

        var points = _intermediatePoints.ToList();

        points.RemoveAt(pointIndex);

        return new EdgeRoute(points);
    }

    private void ValidatePointIndex(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= _intermediatePoints.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pointIndex));
        }
    }
}
