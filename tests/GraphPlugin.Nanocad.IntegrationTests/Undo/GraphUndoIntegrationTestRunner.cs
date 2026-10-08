using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Runtime;

using HostMgd.ApplicationServices;

using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphUndoIntegrationTestRunner
{
    private const double Tolerance =
        0.000001;

    private readonly Document _document;
    private readonly GraphDocumentContext _context;
    private readonly UndoTestManifestStore _store;

    public GraphUndoIntegrationTestRunner(
        Document document,
        GraphDocumentContext context)
    {
        _document = document;
        _context = context;
        _store = new UndoTestManifestStore();
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

    public UndoTestManifest PrepareEdge()
    {
        EnsureNoActiveTest();

        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;

        try
        {
            var positionA =
                new Point2(600, 600);

            var positionB =
                new Point2(640, 600);

            a =
                vertexService.CreateVertex(
                    positionA);

            b =
                vertexService.CreateVertex(
                    positionB);

            var edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var manifest =
                new UndoTestManifest(
                    Version: 1,
                    TestId: Guid.NewGuid(),
                    Scenario: UndoTestScenario.Edge,

                    VertexAId: a.Id,
                    VertexBId: b.Id,
                    VertexCId: null,

                    EdgeABId: edge.Id,
                    EdgeBCId: null,

                    PositionA: positionA,
                    PositionB: positionB,
                    PositionC: null);

            _store.Save(
                _document.Database,
                manifest);

            return manifest;
        }
        catch
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);

            throw;
        }
    }

    public UndoTestManifest PrepareVertex()
    {
        EnsureNoActiveTest();

        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? c = null;

        try
        {
            var positionA =
                new Point2(700, 700);

            var positionB =
                new Point2(720, 710);

            var positionC =
                new Point2(740, 700);

            a =
                vertexService.CreateVertex(
                    positionA);

            b =
                vertexService.CreateVertex(
                    positionB,
                    VertexShape.Triangle);

            c =
                vertexService.CreateVertex(
                    positionC);

            var ab =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var bc =
                edgeService.CreateEdge(
                    b.Id,
                    c.Id);

            var manifest =
                new UndoTestManifest(
                    Version: 1,
                    TestId: Guid.NewGuid(),
                    Scenario: UndoTestScenario.Vertex,

                    VertexAId: a.Id,
                    VertexBId: b.Id,
                    VertexCId: c.Id,

                    EdgeABId: ab.Id,
                    EdgeBCId: bc.Id,

                    PositionA: positionA,
                    PositionB: positionB,
                    PositionC: positionC);

            _store.Save(
                _document.Database,
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

            throw;
        }
    }

    private void EnsureNoActiveTest()
    {
        if (_store.Exists(
                _document.Database))
        {
            throw new InvalidOperationException(
                "An Undo integration test is already active. " +
                "Run GRAPH_CLEAR_UNDO_TEST first.");
        }
    }

    public IReadOnlyList<IntegrationTestResult> VerifyEdgeErased()
    {
        var manifest =
            RequireManifest(
                UndoTestScenario.Edge);

        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Edge vertices preserved",
            () =>
            {
                Ensure(
                    _context.Vertices.Get(
                        manifest.VertexAId) is not null,
                    "Vertex A disappeared.");

                Ensure(
                    _context.Vertices.Get(
                        manifest.VertexBId) is not null,
                    "Vertex B disappeared.");
            });

        Run(
            results,
            "Edge removed from repository",
            () =>
            {
                Ensure(
                    _context.Edges.Get(
                        manifest.EdgeABId) is null,
                    "Erased edge is still available from repository.");
            });

        Run(
            results,
            "Edge removed from index",
            () =>
            {
                Ensure(
                    !_context.Index.TryGetEdgeObjectId(
                        manifest.EdgeABId,
                        out _),
                    "Erased edge is still present in index.");
            });

        Run(
            results,
            "Incident relationships removed",
            () =>
            {
                var aEdges =
                    _context.Edges.GetByVertex(
                        manifest.VertexAId);

                var bEdges =
                    _context.Edges.GetByVertex(
                        manifest.VertexBId);

                Ensure(
                    !aEdges.Any(
                        x => x.Id ==
                             manifest.EdgeABId),
                    "Vertex A still references erased edge.");

                Ensure(
                    !bEdges.Any(
                        x => x.Id ==
                             manifest.EdgeABId),
                    "Vertex B still references erased edge.");
            });

        Run(
            results,
            "Path removed",
            () =>
                VerifyNoPath(
                    manifest.VertexAId,
                    manifest.VertexBId));

        return results;
    }

    public IReadOnlyList<IntegrationTestResult> VerifyEdgeUndo()
    {
        var manifest =
            RequireManifest(
                UndoTestScenario.Edge);

        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Edge restored in repository",
            () =>
            {
                var edge =
                    _context.Edges.Get(
                        manifest.EdgeABId);

                Ensure(
                    edge is not null,
                    "Edge was not restored.");

                Ensure(
                    edge.VertexAId ==
                        manifest.VertexAId &&
                    edge.VertexBId ==
                        manifest.VertexBId,
                    "Restored edge has incorrect endpoints.");
            });

        Run(
            results,
            "Edge restored in index",
            () =>
            {
                Ensure(
                    _context.Index.TryGetEdgeObjectId(
                        manifest.EdgeABId,
                        out var objectId),
                    "Restored edge is missing from index.");

                Ensure(
                    !objectId.IsNull &&
                    !objectId.IsErased,
                    "Restored edge ObjectId is invalid.");
            });

        Run(
            results,
            "Incident relationships restored",
            () =>
            {
                EnsureEdgeIncidentTo(
                    manifest.VertexAId,
                    manifest.EdgeABId);

                EnsureEdgeIncidentTo(
                    manifest.VertexBId,
                    manifest.EdgeABId);
            });

        Run(
            results,
            "Edge geometry restored",
            () =>
                VerifyLineEndpoints(
                    manifest.EdgeABId,
                    manifest.PositionA,
                    manifest.PositionB));

        Run(
            results,
            "Path restored",
            () =>
                VerifyDirectPath(
                    manifest.VertexAId,
                    manifest.VertexBId,
                    manifest.EdgeABId));

        return results;
    }

    public IReadOnlyList<IntegrationTestResult>
    VerifyVertexErased()
    {
        var manifest =
            RequireManifest(
                UndoTestScenario.Vertex);

        var vertexCId =
            manifest.VertexCId!.Value;

        var edgeBCId =
            manifest.EdgeBCId!.Value;

        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Outer vertices preserved",
            () =>
            {
                Ensure(
                    _context.Vertices.Get(
                        manifest.VertexAId) is not null,
                    "Vertex A disappeared.");

                Ensure(
                    _context.Vertices.Get(
                        vertexCId) is not null,
                    "Vertex C disappeared.");
            });

        Run(
            results,
            "Middle vertex removed",
            () =>
            {
                Ensure(
                    _context.Vertices.Get(
                        manifest.VertexBId) is null,
                    "Vertex B still exists.");

                Ensure(
                    !_context.Index.TryGetVertexObjectId(
                        manifest.VertexBId,
                        out _),
                    "Vertex B is still present in index.");
            });

        Run(
            results,
            "Incident edges cascade removed",
            () =>
            {
                Ensure(
                    _context.Edges.Get(
                        manifest.EdgeABId) is null,
                    "Edge A-B still exists.");

                Ensure(
                    _context.Edges.Get(
                        edgeBCId) is null,
                    "Edge B-C still exists.");

                Ensure(
                    !_context.Index.TryGetEdgeObjectId(
                        manifest.EdgeABId,
                        out _),
                    "Edge A-B is still in index.");

                Ensure(
                    !_context.Index.TryGetEdgeObjectId(
                        edgeBCId,
                        out _),
                    "Edge B-C is still in index.");
            });

        Run(
            results,
            "Outer incident relationships cleaned",
            () =>
            {
                var aEdges =
                    _context.Edges.GetByVertex(
                        manifest.VertexAId);

                var cEdges =
                    _context.Edges.GetByVertex(
                        vertexCId);

                Ensure(
                    !aEdges.Any(
                        x => x.Id ==
                             manifest.EdgeABId),
                    "A still references A-B.");

                Ensure(
                    !cEdges.Any(
                        x => x.Id ==
                             edgeBCId),
                    "C still references B-C.");
            });

        Run(
            results,
            "Path removed after cascade delete",
            () =>
                VerifyNoPath(
                    manifest.VertexAId,
                    vertexCId));

        return results;
    }

    public IReadOnlyList<IntegrationTestResult> VerifyVertexUndo()
    {
        var manifest =
            RequireManifest(
                UndoTestScenario.Vertex);

        var vertexCId =
            manifest.VertexCId!.Value;

        var edgeBCId =
            manifest.EdgeBCId!.Value;

        var positionC =
            manifest.PositionC!.Value;

        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Middle vertex restored",
            () =>
            {
                var b =
                    _context.Vertices.Get(
                        manifest.VertexBId);

                Ensure(
                    b is not null,
                    "Vertex B was not restored.");

                EnsurePosition(
                    b.Position,
                    manifest.PositionB,
                    "B");

                Ensure(
                    b.Style.Shape ==
                        VertexShape.Triangle,
                    "Vertex B shape was not restored.");
            });

        Run(
            results,
            "Cascade edges restored",
            () =>
            {
                Ensure(
                    _context.Edges.Get(
                        manifest.EdgeABId) is not null,
                    "Edge A-B was not restored.");

                Ensure(
                    _context.Edges.Get(
                        edgeBCId) is not null,
                    "Edge B-C was not restored.");
            });

        Run(
            results,
            "Runtime index restored",
            () =>
            {
                Ensure(
                    _context.Index.TryGetVertexObjectId(
                        manifest.VertexBId,
                        out var bObjectId),
                    "B is missing from index.");

                Ensure(
                    !bObjectId.IsNull &&
                    !bObjectId.IsErased,
                    "B ObjectId is invalid.");

                Ensure(
                    _context.Index.TryGetEdgeObjectId(
                        manifest.EdgeABId,
                        out _),
                    "A-B is missing from index.");

                Ensure(
                    _context.Index.TryGetEdgeObjectId(
                        edgeBCId,
                        out _),
                    "B-C is missing from index.");
            });

        Run(
            results,
            "Incident relationships restored",
            () =>
            {
                EnsureEdgeIncidentTo(
                    manifest.VertexAId,
                    manifest.EdgeABId);

                EnsureEdgeIncidentTo(
                    manifest.VertexBId,
                    manifest.EdgeABId);

                EnsureEdgeIncidentTo(
                    manifest.VertexBId,
                    edgeBCId);

                EnsureEdgeIncidentTo(
                    vertexCId,
                    edgeBCId);

                var bEdges =
                    _context.Edges.GetByVertex(
                        manifest.VertexBId);

                Ensure(
                    bEdges.Count == 2,
                    $"Vertex B has {bEdges.Count} " +
                    "incident edges instead of 2.");
            });

        Run(
            results,
            "Restored edge geometry correct",
            () =>
            {
                VerifyLineEndpoints(
                    manifest.EdgeABId,
                    manifest.PositionA,
                    manifest.PositionB);

                VerifyLineEndpoints(
                    edgeBCId,
                    manifest.PositionB,
                    positionC);
            });

        Run(
            results,
            "Shortest path restored",
            () =>
                VerifyThreeVertexPath(
                    manifest.VertexAId,
                    manifest.VertexBId,
                    vertexCId,
                    manifest.EdgeABId,
                    edgeBCId));

        return results;
    }

    private UndoTestManifest RequireManifest(
        UndoTestScenario expectedScenario)
    {
        var manifest =
            _store.Load(
                _document.Database)
            ?? throw new InvalidOperationException(
                "Undo integration test is not prepared.");

        if (manifest.Scenario !=
            expectedScenario)
        {
            throw new InvalidOperationException(
                $"Prepared scenario is {manifest.Scenario}, " +
                $"but {expectedScenario} was expected.");
        }

        return manifest;
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

    private void EnsureEdgeIncidentTo(
        Guid vertexId,
        Guid edgeId)
    {
        var edges =
            _context.Edges.GetByVertex(
                vertexId);

        Ensure(
            edges.Any(
                edge => edge.Id == edgeId),
            $"Vertex {vertexId} does not reference " +
            $"edge {edgeId}.");
    }

    private static void EnsurePosition(
        Point2 actual,
        Point2 expected,
        string name)
    {
        Ensure(
            Math.Abs(actual.X - expected.X)
                < Tolerance &&
            Math.Abs(actual.Y - expected.Y)
                < Tolerance,
            $"Vertex {name} position is incorrect. " +
            $"Actual ({actual.X}, {actual.Y}), " +
            $"expected ({expected.X}, {expected.Y}).");
    }

    private ShortestPathApplicationService CreateShortestPathService()
    {
        return new ShortestPathApplicationService(
            _context.Vertices,
            _context.Edges,
            new DijkstraShortestPathService(
                new EdgeLengthCalculator()));
    }

    private void VerifyNoPath(
        Guid startId,
        Guid endId)
    {
        var result =
            CreateShortestPathService()
                .Find(
                    startId,
                    endId);

        Ensure(
            !result.Found,
            "Shortest path unexpectedly exists.");
    }

    private void VerifyDirectPath(
        Guid aId,
        Guid bId,
        Guid edgeId)
    {
        var result =
            CreateShortestPathService()
                .Find(
                    aId,
                    bId);

        Ensure(
            result.Found,
            "Shortest path was not restored.");

        Ensure(
            result.VertexIds.SequenceEqual(
                new[]
                {
                aId,
                bId
                }),
            "Unexpected vertex sequence.");

        Ensure(
            result.EdgeIds.SequenceEqual(
                new[]
                {
                edgeId
                }),
            "Unexpected edge sequence.");
    }

    private void VerifyThreeVertexPath(
        Guid aId,
        Guid bId,
        Guid cId,
        Guid edgeABId,
        Guid edgeBCId)
    {
        var result =
            CreateShortestPathService()
                .Find(
                    aId,
                    cId);

        Ensure(
            result.Found,
            "Shortest path A -> C was not restored.");

        Ensure(
            result.VertexIds.SequenceEqual(
                new[]
                {
                aId,
                bId,
                cId
                }),
            "Restored path has incorrect vertex sequence.");

        Ensure(
            result.EdgeIds.SequenceEqual(
                new[]
                {
                edgeABId,
                edgeBCId
                }),
            "Restored path has incorrect edge sequence.");
    }

    private void VerifyLineEndpoints(
        Guid edgeId,
        Point2 expectedStart,
        Point2 expectedEnd)
    {
        Ensure(
            _context.Index.TryGetEdgeObjectId(
                edgeId,
                out var objectId),
            $"Edge {edgeId} is missing from index.");

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var line =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Polyline;

        Ensure(
            line is not null,
            $"Edge {edgeId} is not a Polyline.");

        Ensure(
            Math.Abs(
                line.StartPoint.X -
                expectedStart.X) < Tolerance &&
            Math.Abs(
                line.StartPoint.Y -
                expectedStart.Y) < Tolerance,
            $"Edge {edgeId} has incorrect start point.");

        Ensure(
            Math.Abs(
                line.EndPoint.X -
                expectedEnd.X) < Tolerance &&
            Math.Abs(
                line.EndPoint.Y -
                expectedEnd.Y) < Tolerance,
            $"Edge {edgeId} has incorrect end point.");
    }

    public void Clear()
    {
        var manifest =
            _store.Load(
                _document.Database);

        if (manifest is null)
            return;

        DeleteVertexIfExists(
            manifest.VertexAId);

        DeleteVertexIfExists(
            manifest.VertexBId);

        if (manifest.VertexCId is Guid vertexCId)
        {
            DeleteVertexIfExists(
                vertexCId);
        }

        _store.Delete(
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