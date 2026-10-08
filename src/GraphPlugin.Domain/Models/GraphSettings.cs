namespace GraphPlugin.Domain.Models;

public sealed class GraphSettings
{
    public EdgeStyle EdgeStyle { get; private set; }

    public GraphSettings(EdgeStyle edgeStyle)
    {
        EdgeStyle = edgeStyle ?? throw new ArgumentNullException(nameof(edgeStyle));
    }

    public static GraphSettings Default => new(EdgeStyle.Default);

    public void ChangeEdgeStyle(EdgeStyle style)
    {
        EdgeStyle = style ?? throw new ArgumentNullException(nameof(style));
    }
}
