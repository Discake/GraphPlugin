namespace GraphPlugin.Domain.Geometry;

public readonly record struct Point2(double X, double Y)
{
    public double DistanceTo(Point2 other)
    {
        double dx = other.X - X;
        double dy = other.Y - Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}
