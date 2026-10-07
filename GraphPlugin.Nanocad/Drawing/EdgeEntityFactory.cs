using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Drawing;
using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Drawing;

public sealed class EdgeEntityFactory
{
    private readonly EdgeEntityMapper _mapper;

    public EdgeEntityFactory(
        EdgeEntityMapper mapper)
    {
        _mapper =
            mapper ??
            throw new ArgumentNullException(
                nameof(mapper));
    }

    public Polyline Create(
        GraphEdge edge,
        GraphVertex vertexA,
        GraphVertex vertexB)
    {
        var polyline =
            new Polyline();

        _mapper.WriteGeometry(
            polyline,
            edge,
            vertexA,
            vertexB);

        return polyline;
    }
}