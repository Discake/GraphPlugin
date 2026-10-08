using GraphPlugin.Application.Abstractions;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Tests.Fakes;

public sealed class FakeEdgeStyleApplier : IEdgeStyleApplier
{
    public EdgeStyle? LastAppliedStyle { get; private set; }

    public int ApplyCallCount { get; private set; }

    public void ApplyToAll(EdgeStyle style)
    {
        LastAppliedStyle = style;
        ApplyCallCount++;
    }
}
