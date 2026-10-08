using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.Nanocad.Persistence;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

internal sealed class GraphPersistenceScenarioRunner
{
    private const double Tolerance =
        0.000001;

    private readonly Document _document;
    private readonly GraphDocumentContext _context;
    private readonly PersistenceTestManifestStore _manifestStore;

    public GraphPersistenceScenarioRunner(
        Document document,
        GraphDocumentContext context)
    {
        _document = document;
        _context = context;

        _manifestStore =
            new PersistenceTestManifestStore();
    }

    private static void Ensure(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new IntegrationTestException(
                message);
        }
    }

    public PersistenceTestManifest Prepare()
    {
        var database =
            _document.Database;

        if (_manifestStore.Exists(database))
        {
            throw new InvalidOperationException(
                "A persistence test is already prepared. " +
                "Verify or clear it first.");
        }

        var vertexService =
            new VertexService(
                _context.Vertices, _context.Edges);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var originalStyle =
            _context.Settings
                .GetSettings()
                .EdgeStyle;

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? c = null;

        try
        {
            var positionA =
                new Point2(
                    1000,
                    1000);

            var positionB =
                new Point2(
                    1020,
                    1010);

            var positionC =
                new Point2(
                    1040,
                    1000);

            a =
                vertexService.CreateVertex(
                    positionA,
                    VertexShape.Circle);

            b =
                vertexService.CreateVertex(
                    positionB,
                    VertexShape.Triangle);

            c =
                vertexService.CreateVertex(
                    positionC,
                    VertexShape.Circle);

            var ab =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var bc =
                edgeService.CreateEdge(
                    b.Id,
                    c.Id);

            var testStyle =
                new EdgeStyle(
                    GraphColor.Blue,
                    EdgeLineType.Dashed,
                    0.50);

            _context.Settings.ChangeEdgeStyle(
                testStyle);

            var manifest =
                new PersistenceTestManifest(
                    Version: 1,
                    TestId: Guid.NewGuid(),

                    VertexAId: a.Id,
                    VertexBId: b.Id,
                    VertexCId: c.Id,

                    EdgeABId: ab.Id,
                    EdgeBCId: bc.Id,

                    PositionA: positionA,
                    PositionB: positionB,
                    PositionC: positionC,

                    TestEdgeColor:
                        testStyle.Color,

                    TestEdgeLineType:
                        testStyle.LineType,

                    TestEdgeLineWeightMm:
                        testStyle.LineWeightMm,

                    OriginalEdgeColor:
                        originalStyle.Color,

                    OriginalEdgeLineType:
                        originalStyle.LineType,

                    OriginalEdgeLineWeightMm:
                        originalStyle.LineWeightMm);

            _manifestStore.Save(
                database,
                manifest);

            return manifest;
        }
        catch
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);

            if (c is not null)
                DeleteVertexIfExists(c.Id);

            _context.Settings.ChangeEdgeStyle(
                originalStyle);

            throw;
        }
    }

    public IReadOnlyList<IntegrationTestResult> Verify()
    {
        var manifest =
            _manifestStore.Load(
                _document.Database)
            ?? throw new InvalidOperationException(
                "Persistence test manifest was not found. " +
                "Run GRAPH_PREPARE_PERSISTENCE_TEST first.");

        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Manifest restored",
            () => TestManifest(manifest));

        Run(
            results,
            "Vertices restored",
            () => TestVertices(manifest));

        Run(
            results,
            "Edges restored",
            () => TestEdges(manifest));

        Run(
            results,
            "Runtime index restored",
            () => TestIndex(manifest));

        Run(
            results,
            "Graph settings restored",
            () => TestSettings(manifest));

        Run(
            results,
            "DWG edge style restored",
            () => TestEdgeStyles(manifest));

        Run(
            results,
            "Shortest path restored",
            () => TestShortestPath(manifest));

        return results;
    }

    private static void Run(
        ICollection<IntegrationTestResult> results,
        string name,
        Action action)
    {
        try
        {
            action();

            results.Add(
                new IntegrationTestResult(
                    name,
                    true));
        }
        catch (Exception exception)
        {
            results.Add(
                new IntegrationTestResult(
                    name,
                    false,
                    exception.Message));
        }
    }

    private void TestManifest(
    PersistenceTestManifest manifest)
    {
        Ensure(
            manifest.TestId != Guid.Empty,
            "TestId was not restored.");

        Ensure(
            manifest.Version == 1,
            $"Unexpected manifest version: {manifest.Version}.");
    }

    private void TestVertices(
    PersistenceTestManifest manifest)
    {
        var a =
            _context.Vertices.Get(
                manifest.VertexAId);

        var b =
            _context.Vertices.Get(
                manifest.VertexBId);

        var c =
            _context.Vertices.Get(
                manifest.VertexCId);

        Ensure(
            a is not null,
            "Vertex A was not restored.");

        Ensure(
            b is not null,
            "Vertex B was not restored.");

        Ensure(
            c is not null,
            "Vertex C was not restored.");

        EnsurePosition(
            a.Position,
            manifest.PositionA,
            "A");

        EnsurePosition(
            b.Position,
            manifest.PositionB,
            "B");

        EnsurePosition(
            c.Position,
            manifest.PositionC,
            "C");

        Ensure(
            a.Style.Shape ==
            VertexShape.Circle,
            "Vertex A shape was not restored.");

        Ensure(
            b.Style.Shape ==
            VertexShape.Triangle,
            "Vertex B shape was not restored.");

        Ensure(
            c.Style.Shape ==
            VertexShape.Circle,
            "Vertex C shape was not restored.");
    }

    private static void EnsurePosition(
        Point2 actual,
        Point2 expected,
        string name)
    {
        Ensure(
            Math.Abs(
                actual.X - expected.X) <
            Tolerance &&
            Math.Abs(
                actual.Y - expected.Y) <
            Tolerance,
            $"Vertex {name} position was not restored. " +
            $"Actual ({actual.X}, {actual.Y}), " +
            $"expected ({expected.X}, {expected.Y}).");
    }

    private void TestEdges(
    PersistenceTestManifest manifest)
    {
        var ab =
            _context.Edges.Get(
                manifest.EdgeABId);

        var bc =
            _context.Edges.Get(
                manifest.EdgeBCId);

        Ensure(
            ab is not null,
            "Edge A-B was not restored.");

        Ensure(
            bc is not null,
            "Edge B-C was not restored.");

        Ensure(
            ab.VertexAId ==
                manifest.VertexAId &&
            ab.VertexBId ==
                manifest.VertexBId,
            "Edge A-B vertex references were not restored.");

        Ensure(
            bc.VertexAId ==
                manifest.VertexBId &&
            bc.VertexBId ==
                manifest.VertexCId,
            "Edge B-C vertex references were not restored.");
    }

    private void TestIndex(
    PersistenceTestManifest manifest)
    {
        Ensure(
            _context.Index.TryGetVertexObjectId(
                manifest.VertexAId,
                out _),
            "Vertex A is missing from rebuilt index.");

        Ensure(
            _context.Index.TryGetVertexObjectId(
                manifest.VertexBId,
                out _),
            "Vertex B is missing from rebuilt index.");

        Ensure(
            _context.Index.TryGetVertexObjectId(
                manifest.VertexCId,
                out _),
            "Vertex C is missing from rebuilt index.");

        Ensure(
            _context.Index.TryGetEdgeObjectId(
                manifest.EdgeABId,
                out _),
            "Edge A-B is missing from rebuilt index.");

        Ensure(
            _context.Index.TryGetEdgeObjectId(
                manifest.EdgeBCId,
                out _),
            "Edge B-C is missing from rebuilt index.");

        var incidentA =
            _context.Index.GetIncidentEdgeIds(
                manifest.VertexAId);

        var incidentB =
            _context.Index.GetIncidentEdgeIds(
                manifest.VertexBId);

        var incidentC =
            _context.Index.GetIncidentEdgeIds(
                manifest.VertexCId);

        Ensure(
            incidentA.Contains(
                manifest.EdgeABId),
            "A -> AB relationship was not rebuilt.");

        Ensure(
            incidentB.Contains(
                manifest.EdgeABId) &&
            incidentB.Contains(
                manifest.EdgeBCId),
            "B incident edges were not rebuilt.");

        Ensure(
            incidentC.Contains(
                manifest.EdgeBCId),
            "C -> BC relationship was not rebuilt.");
    }

    private void TestSettings(
    PersistenceTestManifest manifest)
    {
        var style =
            _context.Settings
                .GetSettings()
                .EdgeStyle;

        Ensure(
            style.Color ==
            manifest.TestEdgeColor,
            "Edge color setting was not restored.");

        Ensure(
            style.LineType ==
            manifest.TestEdgeLineType,
            "Edge line type setting was not restored.");

        Ensure(
            Math.Abs(
                style.LineWeightMm -
                manifest.TestEdgeLineWeightMm) <
            Tolerance,
            "Edge line weight setting was not restored.");
    }

    private void TestEdgeStyles(
    PersistenceTestManifest manifest)
    {
        VerifyEdgeStyle(
            manifest.EdgeABId,
            manifest);

        VerifyEdgeStyle(
            manifest.EdgeBCId,
            manifest);
    }

    private void VerifyEdgeStyle(
    Guid edgeId,
    PersistenceTestManifest manifest)
    {
        Ensure(
            _context.Index.TryGetEdgeObjectId(
                edgeId,
                out var objectId),
            $"Edge {edgeId} is missing from index.");

        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var line =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Polyline;

        Ensure(
            line is not null,
            $"Edge {edgeId} is not a Polyline.");

        var expectedColor =
            CadColorMapper.ToCadColor(
                manifest.TestEdgeColor);

        Ensure(
            line.Color.ColorIndex ==
            expectedColor.ColorIndex,
            "Edge color was not restored.");

        var expectedWeight =
            LineWeightMapper.ToCadLineWeight(
                manifest.TestEdgeLineWeightMm);

        Ensure(
            line.LineWeight ==
            expectedWeight,
            "Edge line weight was not restored.");

        var linetype =
            transaction.GetObject(
                line.LinetypeId,
                OpenMode.ForRead)
            as LinetypeTableRecord;

        Ensure(
            linetype is not null,
            "Edge linetype record is missing.");

        var expectedName =
            manifest.TestEdgeLineType switch
            {
                EdgeLineType.Continuous =>
                    "Continuous",

                EdgeLineType.Dashed =>
                    "GRAPH_DASHED",

                EdgeLineType.Dotted =>
                    "GRAPH_DOTTED",

                _ =>
                    throw new InvalidOperationException()
            };

        Ensure(
            string.Equals(
                linetype.Name,
                expectedName,
                StringComparison.OrdinalIgnoreCase),
            $"Unexpected linetype '{linetype.Name}', " +
            $"expected '{expectedName}'.");
    }

    private void TestShortestPath(
    PersistenceTestManifest manifest)
    {
        var algorithm =
            new DijkstraShortestPathService(
                new EdgeLengthCalculator());

        var service =
            new ShortestPathApplicationService(
                _context.Vertices,
                _context.Edges,
                algorithm);

        var result =
            service.Find(
                manifest.VertexAId,
                manifest.VertexCId);

        Ensure(
            result.Found,
            "Path A -> C was not found after reopen.");

        Ensure(
            result.VertexIds.SequenceEqual(
                new[]
                {
                manifest.VertexAId,
                manifest.VertexBId,
                manifest.VertexCId
                }),
            "Unexpected vertex sequence after reopen.");

        Ensure(
            result.EdgeIds.SequenceEqual(
                new[]
                {
                manifest.EdgeABId,
                manifest.EdgeBCId
                }),
            "Unexpected edge sequence after reopen.");

        var expectedLength =
            manifest.PositionA.DistanceTo(
                manifest.PositionB) +
            manifest.PositionB.DistanceTo(
                manifest.PositionC);

        Ensure(
            Math.Abs(
                result.TotalLength -
                expectedLength) <
            Tolerance,
            $"Unexpected shortest path length: " +
            $"{result.TotalLength}, " +
            $"expected {expectedLength}.");
    }

    public void Clear()
    {
        var manifest =
            _manifestStore.Load(
                _document.Database);

        if (manifest is null)
            return;

        DeleteVertexIfExists(
            manifest.VertexAId);

        DeleteVertexIfExists(
            manifest.VertexBId);

        DeleteVertexIfExists(
            manifest.VertexCId);

        var originalStyle =
            new EdgeStyle(
                manifest.OriginalEdgeColor,
                manifest.OriginalEdgeLineType,
                manifest.OriginalEdgeLineWeightMm);

        _context.Settings.ChangeEdgeStyle(
            originalStyle);

        _manifestStore.Delete(
            _document.Database);
    }

    private void DeleteVertexIfExists(
        Guid vertexId)
    {
        if (_context.Vertices.Get(vertexId)
            is null)
        {
            return;
        }

        _context.Graph.DeleteVertex(
            vertexId);
    }

}