using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Algorithms;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Drawing;
using GraphPlugin.Nanocad.Persistence;
using GraphPlugin.Nanocad.Runtime;
using GraphPlugin.NanoCad.Drawing;
using GraphPlugin.NanoCad.Persistence;
using GraphPlugin.NanoCad.Persistence.Metadata;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace GraphPlugin.NanoCad.Runtime;

public sealed class GraphIntegrationTestRunner
{
    private readonly Document _document;
    private readonly GraphDocumentContext _context;
    private readonly EdgeGeometrySynchronizer _synchronizer;

    public GraphIntegrationTestRunner(
        Document document,
        GraphDocumentContext context)
    {
        _document = document;
        _context = context;

        _synchronizer = new EdgeGeometrySynchronizer(
            context.Vertices,
            context.Edges,
            context.Index,
            new EdgeEntityMapper());
    }

    public IReadOnlyList<IntegrationTestResult> RunAll()
    {
        var results =
            new List<IntegrationTestResult>();

        Run(
            results,
            "Create vertex",
            TestCreateVertex);

        Run(
            results,
            "Create edge",
            TestCreateEdge);

        Run(
            results,
            "Split edge",
            TestSplitEdge);

        Run(
            results,
            "Cascade delete",
            TestCascadeDelete);

        Run(
            results,
            "Edge style properties",
            TestEdgeStyleProperties);

        Run(
            results,
            "Edge geometry synchronization",
            TestEdgeGeometrySynchronization);

        Run(
            results,
            "Rebuild index from DWG",
            TestRebuildIndex);

        Run(
            results,
            "Shortest path",
            TestShortestPath);

        Run(
            results,
            "Triangle position mapping",
            TestTrianglePositionMapping);

        Run(
            results,
            "Auto build empty points",
            TestAutoBuildEmptyPoints);

        Run(
            results,
            "Auto build existing vertex",
            TestAutoBuildExistingVertex);

        Run(
            results,
            "Auto build edge uses raw pick point",
            TestAutoBuildEdgeUsesRawPickPoint);

        Run(
            results,
            "Auto build incident edge split",
            TestAutoBuildIncidentEdgeSplit);

        Run(
            results,
            "Auto build unrelated edge split",
            TestAutoBuildUnrelatedEdgeSplit);

        Run(
            results,
            "Polyline edge route round-trip",
            TestPolylineEdgeRouteRoundTrip);

        Run(
            results,
            "Polyline edge route update",
            TestPolylineEdgeRouteUpdate);

        Run(
            results,
            "Polyline synchronization preserves bends",
            TestPolylineSynchronizationPreservesBends);

        Run(
            results,
            "Shortest path uses polyline length",
            TestShortestPathUsesPolylineLength);

        Run(
            results,
            "Split bent polyline edge",
            TestSplitBentPolylineEdge);

        Run(
            results,
            "Auto build split bent polyline edge",
            TestAutoBuildSplitBentPolylineEdge);

        Run(
            results,
            "Add bend persists to DWG polyline",
            TestAddBendPersistsToDwgPolyline);

        Run(
            results,
            "Remove last bend makes edge straight",
            TestRemoveLastBendMakesEdgeStraight);

        Run(
            results,
            "Polyline modification preserves moved bend",
            TestPolylineModificationPreservesMovedBend);

        Run(
            results,
            "Polyline endpoint modification is corrected",
            TestPolylineEndpointModificationIsCorrected);

        Run(
            results,
            "Vertex attachment XRecord round-trip",
            TestVertexAttachmentXRecordRoundTrip);

        Run(
            results,
            "Vertex attachment add and detach",
            TestVertexAttachmentAddAndDetach);

        Run(
            results,
            "Attachment detach preserves physical file",
            TestAttachmentDetachPreservesPhysicalFile);

        return results;
    }

    private void Run(
    ICollection<IntegrationTestResult> results,
    string name,
    Action test)
    {
        try
        {
            test();

            results.Add(
                new IntegrationTestResult($"[PASS] {name}", true));
        }


        catch (System.Exception exception)
        {
            results.Add(
                new IntegrationTestResult($"[FAIL] {name}" +
                Environment.NewLine +
                exception, false));
        }
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

    private void TestCreateVertex()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? created = null;

        try
        {
            created =
                vertexService.CreateVertex(
                    new Point2(
                        125.25,
                        340.75));

            var restored =
                _context.Vertices.Get(
                    created.Id);

            Ensure(
                restored is not null,
                "Created vertex cannot be read from repository.");

            Ensure(
                restored.Id == created.Id,
                "Restored vertex has a different Id.");

            Ensure(
                Math.Abs(
                    restored.Position.X - 125.25) < 0.000001,
                "Vertex X coordinate was not persisted correctly.");

            Ensure(
                Math.Abs(
                    restored.Position.Y - 340.75) < 0.000001,
                "Vertex Y coordinate was not persisted correctly.");

            Ensure(
                _context.Index.TryGetVertexObjectId(
                    created.Id,
                    out var objectId),
                "Created vertex is missing from GraphEntityIndex.");

            Ensure(
                !objectId.IsNull,
                "Vertex ObjectId is null.");

            Ensure(
                !objectId.IsErased,
                "Vertex entity is unexpectedly erased.");
        }
        finally
        {
            if (created is not null)
            {
                DeleteVertexIfExists(
                    created.Id);
            }
        }
    }

    private void TestCreateEdge()
    {
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
            a = vertexService.CreateVertex(
                    new Point2(10, 10));

            b = vertexService.CreateVertex(
                    new Point2(20, 10));

            var edge = edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var restored =
                _context.Edges.Get(
                    edge.Id);

            using var transaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();



            Ensure(
                restored is not null,
                "Created edge cannot be read from repository.");

            Ensure(
                restored.VertexAId == a.Id,
                "VertexAId was not persisted correctly.");

            Ensure(
                restored.VertexBId == b.Id,
                "VertexBId was not persisted correctly.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var objectId),
                "Created edge is missing from GraphEntityIndex.");

            Ensure(
                !objectId.IsNull &&
                !objectId.IsErased,
                "Edge entity is invalid.");

            var entity =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead);

            Ensure(
                entity is Polyline,
                "Graph edge entity is not a Polyline.");

            var polyline =
                (Polyline)entity;

            Ensure(
                polyline.NumberOfVertices == 2,
                $"Straight edge has " +
                $"{polyline.NumberOfVertices} polyline vertices, expected 2.");

            Ensure(
                !polyline.Closed,
                "Graph edge polyline is closed.");

            var incidentToA = _context.Edges.GetByVertex(a.Id);

            var incidentToB = _context.Edges.GetByVertex(b.Id);

            Ensure(
                incidentToA.Any(
                    x => x.Id == edge.Id),
                "Edge is not registered as incident to vertex A.");

            Ensure(
                incidentToB.Any(
                    x => x.Id == edge.Id),
                "Edge is not registered as incident to vertex B.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestCascadeDelete()
    {
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
            a = vertexService.CreateVertex(
                    new Point2(0, 0));

            b = vertexService.CreateVertex(
                    new Point2(10, 0));

            c = vertexService.CreateVertex(
                    new Point2(20, 0));

            var ab = edgeService.CreateEdge(a.Id, b.Id);

            var bc = edgeService.CreateEdge(b.Id, c.Id);

            _context.Graph.DeleteVertex(
                b.Id);

            Ensure(
                _context.Vertices.Get(b.Id) is null,
                "Deleted vertex B still exists.");

            Ensure(
                _context.Edges.Get(ab.Id) is null,
                "Incident edge A-B was not deleted.");

            Ensure(
                _context.Edges.Get(bc.Id) is null,
                "Incident edge B-C was not deleted.");

            Ensure(
                _context.Vertices.Get(a.Id) is not null,
                "Unrelated vertex A was deleted.");

            Ensure(
                _context.Vertices.Get(c.Id) is not null,
                "Unrelated vertex C was deleted.");

            Ensure(
                !_context.Index.TryGetVertexObjectId(
                    b.Id,
                    out _),
                "Deleted vertex B is still present in index.");

            Ensure(
                !_context.Index.TryGetEdgeObjectId(
                    ab.Id,
                    out _),
                "Deleted edge A-B is still present in index.");

            Ensure(
                !_context.Index.TryGetEdgeObjectId(
                    bc.Id,
                    out _),
                "Deleted edge B-C is still present in index.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);

            if (c is not null)
                DeleteVertexIfExists(c.Id);
        }
    }

    private void TestRebuildIndex()
    {
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
            a = vertexService.CreateVertex(
                    new Point2(50, 50));

            b = vertexService.CreateVertex(
                    new Point2(60, 50));

            var edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var metadata =
                new XRecordMetadataStore();

            var builder =
                new GraphEntityIndexBuilder(
                    metadata);

            var rebuiltIndex =
                builder.Build(
                    _document.Database);

            Ensure(
                rebuiltIndex.TryGetVertexObjectId(
                    a.Id,
                    out _),
                "Rebuilt index does not contain vertex A.");

            Ensure(
                rebuiltIndex.TryGetVertexObjectId(
                    b.Id,
                    out _),
                "Rebuilt index does not contain vertex B.");

            Ensure(
                rebuiltIndex.TryGetEdgeObjectId(
                    edge.Id,
                    out _),
                "Rebuilt index does not contain edge.");

            var edgesOfA =
                rebuiltIndex.GetIncidentEdgeIds(
                    a.Id);

            var edgesOfB =
                rebuiltIndex.GetIncidentEdgeIds(
                    b.Id);

            Ensure(
                edgesOfA.Contains(edge.Id),
                "Rebuilt index lost A -> Edge relationship.");

            Ensure(
                edgesOfB.Contains(edge.Id),
                "Rebuilt index lost B -> Edge relationship.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestShortestPath()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var algorithm =
            new DijkstraShortestPathService(
                new EdgeLengthCalculator());

        var shortestPath =
            new ShortestPathApplicationService(
                _context.Vertices,
                _context.Edges,
                algorithm);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? c = null;
        GraphVertex? d = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(0, 0));

            b =
                vertexService.CreateVertex(
                    new Point2(5, 0));

            c =
                vertexService.CreateVertex(
                    new Point2(0, 10));

            d =
                vertexService.CreateVertex(
                    new Point2(10, 0));

            var ab =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var bd =
                edgeService.CreateEdge(
                    b.Id,
                    d.Id);

            edgeService.CreateEdge(
                a.Id,
                c.Id);

            edgeService.CreateEdge(
                c.Id,
                d.Id);

            var result =
                shortestPath.Find(
                    a.Id,
                    d.Id);

            Ensure(
                result.Found,
                "Shortest path was not found.");

            Ensure(
                result.EdgeIds.Count == 2,
                "Shortest path contains unexpected number of edges.");

            Ensure(
                result.EdgeIds.SequenceEqual(
                    new[]
                    {
                    ab.Id,
                    bd.Id
                    }),
                "Unexpected shortest path was selected.");

            Ensure(
                Math.Abs(
                    result.TotalLength - 10.0)
                < 0.000001,
                $"Unexpected path length: {result.TotalLength}.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);

            if (c is not null)
                DeleteVertexIfExists(c.Id);

            if (d is not null)
                DeleteVertexIfExists(d.Id);
        }
    }

    private void TestEdgeStyleProperties()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

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

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        200,
                        200));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        220,
                        200));

            var edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            var testStyle =
                new EdgeStyle(
                    GraphColor.Blue,
                    EdgeLineType.Dashed,
                    0.50);

            _context.Settings.ChangeEdgeStyle(
                testStyle);

            VerifySavedEdgeStyle(
                testStyle);

            VerifyEdgeEntityStyle(
                edge.Id,
                testStyle);
        }
        finally
        {
            try
            {
                // Очень важно вернуть настройки DWG
                // в состояние до запуска теста.
                _context.Settings.ChangeEdgeStyle(
                    originalStyle);
            }
            finally
            {
                if (a is not null)
                    DeleteVertexIfExists(a.Id);

                if (b is not null)
                    DeleteVertexIfExists(b.Id);
            }
        }
    }

    private void VerifySavedEdgeStyle(
    EdgeStyle expected)
    {
        var actual =
            _context.Settings
                .GetSettings()
                .EdgeStyle;

        Ensure(
            actual.Color == expected.Color,
            $"Saved edge color is {actual.Color}, " +
            $"expected {expected.Color}.");

        Ensure(
            actual.LineType == expected.LineType,
            $"Saved line type is {actual.LineType}, " +
            $"expected {expected.LineType}.");

        Ensure(
            Math.Abs(
                actual.LineWeightMm -
                expected.LineWeightMm) <
            0.000001,
            $"Saved line weight is " +
            $"{actual.LineWeightMm}, " +
            $"expected {expected.LineWeightMm}.");
    }

    private void VerifyEdgeEntityStyle(
        Guid edgeId,
        EdgeStyle expected)
    {
        Ensure(
            _context.Index.TryGetEdgeObjectId(
                edgeId,
                out var objectId),
            "Edge is missing from GraphEntityIndex.");

        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Entity;

        Ensure(
            entity is not null,
            "Edge ObjectId does not reference an Entity.");

        var expectedColor =
            CadColorMapper.ToCadColor(
                expected.Color);

        Ensure(
            entity.Color.ColorIndex ==
            expectedColor.ColorIndex,
            $"Entity color index is " +
            $"{entity.Color.ColorIndex}, " +
            $"expected {expectedColor.ColorIndex}.");

        var expectedWeight =
            LineWeightMapper.ToCadLineWeight(
                expected.LineWeightMm);

        Ensure(
            entity.LineWeight ==
            expectedWeight,
            $"Entity line weight is " +
            $"{entity.LineWeight}, " +
            $"expected {expectedWeight}.");

        var linetypeManager =
            new LinetypeManager();

        var expectedLinetypeId =
            linetypeManager.GetOrCreate(
                expected.LineType,
                database,
                transaction);

        Ensure(
            entity.LinetypeId ==
            expectedLinetypeId,
            "Entity linetype does not match " +
            $"expected {expected.LineType}.");

        transaction.Commit();
    }

    private void TestEdgeGeometrySynchronization()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;

        var synchronizer = new EdgeGeometrySynchronizer(
            _context.Vertices,
            _context.Edges,
            _context.Index,
            new EdgeEntityMapper());

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        300,
                        300));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        350,
                        300));

            var edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var edgeObjectId),
                "Created edge is missing from index.");

            // Новый invariant:
            // GraphEdge всегда Polyline.
            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var entity =
                    transaction.GetObject(
                        edgeObjectId,
                        OpenMode.ForRead);

                Ensure(
                    entity is Polyline,
                    $"Created GraphEdge is " +
                    $"{entity.GetType().FullName}, " +
                    "expected Polyline.");
            }

            var newPosition =
                new Point2(
                    305,
                    310);

            // Здесь оставь тот код, которым
            // твой старый тест реально двигает
            // DWG-сущность Vertex A.
            MoveVertexEntity(
                a.Id,
                newPosition);

            synchronizer
                .UpdateIncidentEdges(
                    a.Id);

            using var readTransaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();

            var polyline =
                readTransaction.GetObject(
                    edgeObjectId,
                    OpenMode.ForRead)
                as Polyline
                ?? throw new IntegrationTestException(
                    "Graph edge entity is not a Polyline.");

            Ensure(
                polyline.NumberOfVertices == 2,
                $"Straight edge must have 2 vertices, " +
                $"actual: {polyline.NumberOfVertices}.");

            var start =
                polyline.GetPoint2dAt(0);

            var end =
                polyline.GetPoint2dAt(
                    polyline.NumberOfVertices - 1);

            Ensure(
                Math.Abs(
                    start.X -
                    newPosition.X) < 1e-6 &&
                Math.Abs(
                    start.Y -
                    newPosition.Y) < 1e-6,
                $"Unexpected edge start point. " +
                $"Actual: ({start.X}, {start.Y}), " +
                $"Expected: ({newPosition.X}, {newPosition.Y}).");

            Ensure(
                Math.Abs(
                    end.X -
                    b.Position.X) < 1e-6 &&
                Math.Abs(
                    end.Y -
                    b.Position.Y) < 1e-6,
                $"Unexpected edge end point. " +
                $"Actual: ({end.X}, {end.Y}), " +
                $"Expected: ({b.Position.X}, {b.Position.Y}).");
        }
        finally
        {
            if (a is not null)
            {
                DeleteVertexIfExists(
                    a.Id);
            }

            if (b is not null)
            {
                DeleteVertexIfExists(
                    b.Id);
            }
        }
    }

    private void MoveVertexEntity(
    Guid vertexId,
    Point2 newPosition)
    {
        Ensure(
            _context.Index.TryGetVertexObjectId(
                vertexId,
                out var objectId),
            $"Vertex {vertexId} is missing from index.");

        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var entity =
            transaction.GetObject(
                objectId,
                OpenMode.ForWrite);

        Ensure(
            entity is Circle,
            "Test vertex entity is not a Circle.");

        var circle =
            (Circle)entity;

        circle.Center =
            new Point3d(
                newPosition.X,
                newPosition.Y,
                0);

        transaction.Commit();
    }

    private void TestTrianglePositionMapping()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? vertex = null;

        try
        {
            var expected =
                new Point2(
                    450,
                    275);

            vertex =
                vertexService.CreateVertex(
                    expected,
                    VertexShape.Triangle);

            var restored =
                _context.Vertices.Get(
                    vertex.Id);

            Ensure(
                restored is not null,
                "Triangle vertex cannot be read.");

            const double tolerance =
                0.000001;

            Ensure(
                Math.Abs(
                    restored.Position.X -
                    expected.X) <
                tolerance,
                $"Triangle X is {restored.Position.X}, " +
                $"expected {expected.X}.");

            Ensure(
                Math.Abs(
                    restored.Position.Y -
                    expected.Y) <
                tolerance,
                $"Triangle Y is {restored.Position.Y}, " +
                $"expected {expected.Y}.");
        }
        finally
        {
            if (vertex is not null)
            {
                DeleteVertexIfExists(
                    vertex.Id);
            }
        }
    }

    private void TestSplitEdge()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        SplitEdgeResult? splitResult = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        400,
                        400));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        440,
                        400));

            var edgeService =
                new EdgeService(
                    _context.Vertices,
                    _context.Edges);

            var originalEdge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    originalEdge.Id,
                    out var originalObjectId),
                "Original edge is missing from index.");

            var originalLength =
                a.Position.DistanceTo(
                    b.Position);

            var expectedStyle =
                _context.Settings
                    .GetSettings()
                    .EdgeStyle;

            var splitService =
                new SplitEdgeService(
                    _context.Vertices,
                    _context.Edges);

            splitResult =
                splitService.Split(
                    originalEdge.Id,
                    new Point2(
                        415,
                        400),
                    VertexShape.Circle);

            VerifySplitTopology(
                a,
                b,
                originalEdge,
                splitResult);

            VerifySplitIndex(
                originalEdge,
                splitResult);

            VerifySplitGeometry(
                a,
                b,
                splitResult,
                originalLength);

            VerifyShortestPathAfterSplit(
                a.Id,
                b.Id,
                splitResult);

            VerifyEdgeEntityStyle(
                splitResult.EdgeA.Id,
                expectedStyle);

            VerifyEdgeEntityStyle(
                splitResult.EdgeB.Id,
                expectedStyle);

            Ensure(
                originalObjectId.IsErased,
                "Original DWG edge entity was not erased.");
        }
        finally
        {
            // Важно удалить и новую вершину,
            // потому что после split она уже является
            // самостоятельным объектом графа.
            if (splitResult is not null)
            {
                DeleteVertexIfExists(
                    splitResult.NewVertex.Id);
            }

            if (a is not null)
            {
                DeleteVertexIfExists(
                    a.Id);
            }

            if (b is not null)
            {
                DeleteVertexIfExists(
                    b.Id);
            }
        }
    }

    private void VerifySplitTopology(
        GraphVertex a,
        GraphVertex b,
        GraphEdge originalEdge,
        SplitEdgeResult result)
    {
        var newVertex =
            _context.Vertices.Get(
                result.NewVertex.Id);

        Ensure(
            newVertex is not null,
            "New split vertex cannot be read from repository.");

        Ensure(
            _context.Edges.Get(
                originalEdge.Id) is null,
            "Original edge still exists after split.");

        var edgeA =
            _context.Edges.Get(
                result.EdgeA.Id);

        var edgeB =
            _context.Edges.Get(
                result.EdgeB.Id);

        Ensure(
            edgeA is not null,
            "First replacement edge was not created.");

        Ensure(
            edgeB is not null,
            "Second replacement edge was not created.");

        Ensure(
            edgeA.VertexAId == a.Id &&
            edgeA.VertexBId == newVertex.Id,
            "First replacement edge has incorrect topology.");

        Ensure(
            edgeB.VertexAId == newVertex.Id &&
            edgeB.VertexBId == b.Id,
            "Second replacement edge has incorrect topology.");

        var incidentToNewVertex =
            _context.Edges.GetByVertex(
                newVertex.Id);

        Ensure(
            incidentToNewVertex.Count == 2,
            $"New vertex has {incidentToNewVertex.Count} " +
            "incident edges instead of 2.");

        Ensure(
            incidentToNewVertex.Any(
                edge => edge.Id == edgeA.Id),
            "New vertex is not connected to first edge.");

        Ensure(
            incidentToNewVertex.Any(
                edge => edge.Id == edgeB.Id),
            "New vertex is not connected to second edge.");
    }

    private void VerifySplitIndex(
        GraphEdge originalEdge,
        SplitEdgeResult result)
    {
        Ensure(
            !_context.Index.TryGetEdgeObjectId(
                originalEdge.Id,
                out _),
            "Original edge is still present in GraphEntityIndex.");

        Ensure(
            _context.Index.TryGetVertexObjectId(
                result.NewVertex.Id,
                out var vertexObjectId),
            "New vertex is missing from GraphEntityIndex.");

        Ensure(
            !vertexObjectId.IsNull &&
            !vertexObjectId.IsErased,
            "New vertex ObjectId is invalid.");

        Ensure(
            _context.Index.TryGetEdgeObjectId(
                result.EdgeA.Id,
                out var edgeAObjectId),
            "First replacement edge is missing from index.");

        Ensure(
            !edgeAObjectId.IsNull &&
            !edgeAObjectId.IsErased,
            "First replacement edge ObjectId is invalid.");

        Ensure(
            _context.Index.TryGetEdgeObjectId(
                result.EdgeB.Id,
                out var edgeBObjectId),
            "Second replacement edge is missing from index.");

        Ensure(
            !edgeBObjectId.IsNull &&
            !edgeBObjectId.IsErased,
            "Second replacement edge ObjectId is invalid.");

        var incident =
            _context.Index.GetIncidentEdgeIds(
                result.NewVertex.Id);

        Ensure(
            incident.Contains(
                result.EdgeA.Id),
            "Index lost NewVertex -> EdgeA relationship.");

        Ensure(
            incident.Contains(
                result.EdgeB.Id),
            "Index lost NewVertex -> EdgeB relationship.");
    }

    private void VerifySplitGeometry(
        GraphVertex a,
        GraphVertex b,
        SplitEdgeResult result,
        double originalLength)
    {
        var newVertex =
            _context.Vertices.Get(
                result.NewVertex.Id)
            ?? throw new IntegrationTestException(
                "New split vertex was not found.");

        var edgeALength =
            ReadLineLength(
                result.EdgeA.Id);

        var edgeBLength =
            ReadLineLength(
                result.EdgeB.Id);

        const double tolerance =
            0.000001;

        Ensure(
            Math.Abs(
                edgeALength -
                a.Position.DistanceTo(
                    newVertex.Position))
            < tolerance,
            "First replacement Line has incorrect length.");

        Ensure(
            Math.Abs(
                edgeBLength -
                newVertex.Position.DistanceTo(
                    b.Position))
            < tolerance,
            "Second replacement Line has incorrect length.");

        Ensure(
            Math.Abs(
                edgeALength +
                edgeBLength -
                originalLength)
            < tolerance,
            "Split did not preserve total geometric length.");

        VerifyLineEndpoints(
            result.EdgeA.Id,
            a.Position,
            newVertex.Position);

        VerifyLineEndpoints(
            result.EdgeB.Id,
            newVertex.Position,
            b.Position);
    }

    private double ReadLineLength(
        Guid edgeId)
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
            $"Edge {edgeId} is not represented by a Polyline.");

        var dx =
            line.EndPoint.X -
            line.StartPoint.X;

        var dy =
            line.EndPoint.Y -
            line.StartPoint.Y;

        return Math.Sqrt(
            dx * dx +
            dy * dy);
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

        const double tolerance =
            0.000001;

        Ensure(
            Math.Abs(
                line.StartPoint.X -
                expectedStart.X) < tolerance &&
            Math.Abs(
                line.StartPoint.Y -
                expectedStart.Y) < tolerance,
            $"Edge {edgeId} has incorrect start point.");

        Ensure(
            Math.Abs(
                line.EndPoint.X -
                expectedEnd.X) < tolerance &&
            Math.Abs(
                line.EndPoint.Y -
                expectedEnd.Y) < tolerance,
            $"Edge {edgeId} has incorrect end point.");
    }

    private void VerifyShortestPathAfterSplit(
        Guid vertexAId,
        Guid vertexBId,
        SplitEdgeResult result)
    {
        var algorithm =
            new DijkstraShortestPathService(
                new EdgeLengthCalculator());

        var service =
            new ShortestPathApplicationService(
                _context.Vertices,
                _context.Edges,
                algorithm);

        var path =
            service.Find(
                vertexAId,
                vertexBId);

        Ensure(
            path.Found,
            "Shortest path disappeared after edge split.");

        Ensure(
            path.VertexIds.SequenceEqual(
                new[]
                {
                vertexAId,
                result.NewVertex.Id,
                vertexBId
                }),
            "Shortest path does not pass through the new split vertex.");

        Ensure(
            path.EdgeIds.SequenceEqual(
                new[]
                {
                result.EdgeA.Id,
                result.EdgeB.Id
                }),
            "Shortest path uses unexpected edges after split.");
    }

    private void TestAutoBuildEmptyPoints()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? b = null;

        try
        {
            var firstPick =
                BuildPickResult.Empty(
                    new Point3d(800, 800, 0),
                    new Point3d(800, 800, 0));

            a =
                executor.Execute(
                    firstPick);

            var secondPick =
                BuildPickResult.Empty(
                    new Point3d(830, 810, 0),
                    new Point3d(830, 810, 0));

            b =
                executor.Execute(
                    secondPick);

            Ensure(
                _context.Vertices.Get(a.Id)
                    is not null,
                "First auto-build vertex was not created.");

            Ensure(
                _context.Vertices.Get(b.Id)
                    is not null,
                "Second auto-build vertex was not created.");

            var edges =
                _context.Edges.GetByVertex(
                    a.Id);

            Ensure(
                edges.Count == 1,
                $"Expected 1 edge from A, " +
                $"actual {edges.Count}.");

            Ensure(
                edges.Single()
                    .IsIncidentTo(b.Id),
                "Auto-build did not connect A and B.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestAutoBuildExistingVertex()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? existing = null;

        try
        {
            a =
                executor.Execute(
                    BuildPickResult.Empty(
                        new Point3d(850, 800, 0),
                        new Point3d(850, 800, 0)));

            existing =
                vertexService.CreateVertex(
                    new Point2(880, 800));

            Ensure(
                _context.Index.TryGetVertexObjectId(
                    existing.Id,
                    out var existingObjectId),
                "Existing vertex is missing from index.");

            var pick =
                BuildPickResult.FromVertex(
                    new Point3d(880, 800, 0),
                    new Point3d(880, 800, 0),
                    existingObjectId,
                    existing);

            var result =
                executor.Execute(pick);

            Ensure(
                result.Id ==
                existing.Id,
                "Auto-build created a different vertex.");

            var edges =
                _context.Edges.GetByVertex(
                    a.Id);

            Ensure(
                edges.Count == 1 &&
                edges.Single()
                    .IsIncidentTo(existing.Id),
                "Current vertex was not connected " +
                "to selected existing vertex.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (existing is not null)
                DeleteVertexIfExists(existing.Id);
        }
    }

    private void TestAutoBuildEdgeUsesRawPickPoint()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? splitVertex = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(900, 900));

            b =
                vertexService.CreateVertex(
                    new Point2(940, 900));

            var edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var edgeObjectId),
                "Test edge is missing from index.");

            var pick =
                BuildPickResult.FromEdge(
                    // Computed OSNAP point intentionally
                    // equals endpoint A.
                    new Point3d(
                        900,
                        900,
                        0),

                    // Real cursor position is near middle.
                    new Point3d(
                        920,
                        905,
                        0),

                    edgeObjectId,
                    edge);

            splitVertex =
                executor.Execute(
                    pick);

            const double tolerance =
                0.000001;

            Ensure(
                Math.Abs(
                    splitVertex.Position.X -
                    920) < tolerance,
                $"Split X is {splitVertex.Position.X}, " +
                "expected 920.");

            Ensure(
                Math.Abs(
                    splitVertex.Position.Y -
                    900) < tolerance,
                $"Split Y is {splitVertex.Position.Y}, " +
                "expected 900.");

            Ensure(
                splitVertex.Position
                    .DistanceTo(a.Position) >
                tolerance,
                "Split incorrectly used snapped endpoint A.");

            Ensure(
                _context.Edges.Get(
                    edge.Id) is null,
                "Original edge still exists after split.");
        }
        finally
        {
            if (splitVertex is not null)
            {
                DeleteVertexIfExists(
                    splitVertex.Id);
            }

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestAutoBuildIncidentEdgeSplit()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? c = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(1000, 1000));

            b =
                vertexService.CreateVertex(
                    new Point2(1040, 1000));

            var original =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            // A уже CurrentVertex.
            buildService.AdvanceTo(a);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out var objectId),
                "Original edge is missing from index.");

            c =
                executor.Execute(
                    BuildPickResult.FromEdge(
                        new Point3d(
                            1020,
                            1000,
                            0),

                        new Point3d(
                            1020,
                            1000,
                            0),

                        objectId,
                        original));

            var allIncidentToC =
                _context.Edges.GetByVertex(
                    c.Id);

            Ensure(
                allIncidentToC.Count == 2,
                $"Split vertex has " +
                $"{allIncidentToC.Count} edges, expected 2.");

            Ensure(
                allIncidentToC.Any(
                    x => x.IsIncidentTo(a.Id)),
                "A-C edge is missing.");

            Ensure(
                allIncidentToC.Any(
                    x => x.IsIncidentTo(b.Id)),
                "C-B edge is missing.");

            // Если GraphBuildService создал
            // дублирующий A-C, здесь было бы 3.
            Ensure(
                _context.Edges.GetAll().Count(
                    edge =>
                        edge.IsIncidentTo(a.Id) &&
                        edge.IsIncidentTo(c.Id))
                == 1,
                "Duplicate A-C edge was created.");
        }
        finally
        {
            if (c is not null)
                DeleteVertexIfExists(c.Id);

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestAutoBuildUnrelatedEdgeSplit()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? d = null;
        GraphVertex? c = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(1100, 1100));

            b =
                vertexService.CreateVertex(
                    new Point2(1140, 1100));

            d =
                vertexService.CreateVertex(
                    new Point2(1120, 1130));

            var original =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            buildService.AdvanceTo(d);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out var objectId),
                "Original edge is missing from index.");

            c =
                executor.Execute(
                    BuildPickResult.FromEdge(
                        new Point3d(
                            1120,
                            1100,
                            0),

                        new Point3d(
                            1120,
                            1100,
                            0),

                        objectId,
                        original));

            var cEdges =
                _context.Edges.GetByVertex(
                    c.Id);

            Ensure(
                cEdges.Count == 3,
                $"Expected 3 edges incident to split vertex, " +
                $"actual {cEdges.Count}.");

            Ensure(
                cEdges.Any(
                    x => x.IsIncidentTo(a.Id)),
                "A-C edge is missing.");

            Ensure(
                cEdges.Any(
                    x => x.IsIncidentTo(b.Id)),
                "C-B edge is missing.");

            Ensure(
                cEdges.Any(
                    x => x.IsIncidentTo(d.Id)),
                "D-C build edge is missing.");

            Ensure(
                buildService.CurrentVertex?.Id ==
                c.Id,
                "Split vertex did not become CurrentVertex.");
        }
        finally
        {
            if (c is not null)
                DeleteVertexIfExists(c.Id);

            if (d is not null)
                DeleteVertexIfExists(d.Id);

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestPolylineEdgeRoute()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1300,
                        1300));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1340,
                        1300));

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        new Point2(
                            1310,
                            1320),

                        new Point2(
                            1330,
                            1320)
                        }));

            _context.Edges.Add(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var objectId),
                "Polyline edge is missing from index.");

            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var polyline =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Polyline;

                Ensure(
                    polyline is not null,
                    "Edge entity is not a Polyline.");

                Ensure(
                    polyline.NumberOfVertices == 4,
                    $"Expected 4 polyline points, " +
                    $"actual {polyline.NumberOfVertices}.");
            }

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Polyline edge could not be read back.");

            Ensure(
                restored.Route.Count == 2,
                $"Expected 2 bends, " +
                $"actual {restored.Route.Count}.");

            Ensure(
                restored.Route.IntermediatePoints[0] ==
                new Point2(
                    1310,
                    1320),
                "First bend was not restored.");

            Ensure(
                restored.Route.IntermediatePoints[1] ==
                new Point2(
                    1330,
                    1320),
                "Second bend was not restored.");
        }
        finally
        {
            if (a is not null)
            {
                DeleteVertexIfExists(
                    a.Id);
            }

            if (b is not null)
            {
                DeleteVertexIfExists(
                    b.Id);
            }
        }
    }

    private void TestPolylineEdgeRouteRoundTrip()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1300,
                        1300));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1360,
                        1300));

            var p1 =
                new Point2(
                    1320,
                    1330);

            var p2 =
                new Point2(
                    1340,
                    1330);

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        p1,
                        p2
                        }));

            _context.Edges.Add(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var objectId),
                "Polyline edge is missing from index.");

            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var polyline =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Polyline;

                Ensure(
                    polyline is not null,
                    "Graph edge entity is not a Polyline.");

                Ensure(
                    !polyline.Closed,
                    "Graph edge polyline must be open.");

                Ensure(
                    polyline.NumberOfVertices == 4,
                    $"Expected 4 polyline vertices, " +
                    $"actual {polyline.NumberOfVertices}.");

                AssertPolylinePoint(
                    polyline,
                    0,
                    a.Position,
                    "Unexpected Vertex A point.");

                AssertPolylinePoint(
                    polyline,
                    1,
                    p1,
                    "Unexpected first bend.");

                AssertPolylinePoint(
                    polyline,
                    2,
                    p2,
                    "Unexpected second bend.");

                AssertPolylinePoint(
                    polyline,
                    3,
                    b.Position,
                    "Unexpected Vertex B point.");
            }

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Repository could not restore polyline edge.");

            Ensure(
                restored.Id == edge.Id,
                "Restored edge has different Id.");

            Ensure(
                restored.VertexAId == a.Id,
                "Restored VertexAId is incorrect.");

            Ensure(
                restored.VertexBId == b.Id,
                "Restored VertexBId is incorrect.");

            Ensure(
                restored.Route.Count == 2,
                $"Expected 2 intermediate points, " +
                $"actual {restored.Route.Count}.");

            EnsurePoint(
                restored.Route.IntermediatePoints[0],
                p1,
                "First restored bend is incorrect.");

            EnsurePoint(
                restored.Route.IntermediatePoints[1],
                p2,
                "Second restored bend is incorrect.");
        }
        finally
        {
            if (edge is not null)
            {
                _context.Edges.Delete(
                    edge.Id);
            }

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private static void AssertPolylinePoint(
    Polyline polyline,
    int index,
    Point2 expected,
    string message)
    {
        var actual =
            polyline.GetPoint2dAt(
                index);

        const double tolerance =
            1e-6;

        Ensure(
            Math.Abs(actual.X - expected.X) <
                tolerance &&
            Math.Abs(actual.Y - expected.Y) <
                tolerance,
            $"{message} " +
            $"Actual: ({actual.X}, {actual.Y}), " +
            $"Expected: ({expected.X}, {expected.Y}).");
    }

    private static void EnsurePoint(
        Point2 actual,
        Point2 expected,
        string message)
    {
        const double tolerance =
            1e-6;

        Ensure(
            actual.DistanceTo(expected) <
                tolerance,
            $"{message} " +
            $"Actual: ({actual.X}, {actual.Y}), " +
            $"Expected: ({expected.X}, {expected.Y}).");
    }

    private void TestPolylineEdgeRouteUpdate()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1400,
                        1400));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1460,
                        1400));

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id);

            _context.Edges.Add(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var originalObjectId),
                "Original edge is missing from index.");

            var p1 =
                new Point2(
                    1420,
                    1440);

            var p2 =
                new Point2(
                    1440,
                    1440);

            edge.ChangeRoute(
                new EdgeRoute(
                    new[]
                    {
                    p1,
                    p2
                    }));

            _context.Edges.Update(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var updatedObjectId),
                "Updated edge disappeared from index.");

            Ensure(
                updatedObjectId ==
                originalObjectId,
                "Updating route replaced the DWG entity. " +
                "ObjectId must remain unchanged.");

            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var polyline =
                    transaction.GetObject(
                        updatedObjectId,
                        OpenMode.ForRead)
                    as Polyline;

                Ensure(
                    polyline is not null,
                    "Updated edge is not a Polyline.");

                Ensure(
                    polyline.NumberOfVertices == 4,
                    $"Expected 4 vertices after route update, " +
                    $"actual {polyline.NumberOfVertices}.");

                AssertPolylinePoint(
                    polyline,
                    0,
                    a.Position,
                    "Vertex A changed during route update.");

                AssertPolylinePoint(
                    polyline,
                    1,
                    p1,
                    "First bend was not written.");

                AssertPolylinePoint(
                    polyline,
                    2,
                    p2,
                    "Second bend was not written.");

                AssertPolylinePoint(
                    polyline,
                    3,
                    b.Position,
                    "Vertex B changed during route update.");
            }

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Updated edge cannot be restored.");

            Ensure(
                restored.Id == edge.Id,
                "Updated edge lost its Id.");

            Ensure(
                restored.Route.Count == 2,
                "Updated route was not restored.");

            EnsurePoint(
                restored.Route.IntermediatePoints[0],
                p1,
                "First updated bend was not restored.");

            EnsurePoint(
                restored.Route.IntermediatePoints[1],
                p2,
                "Second updated bend was not restored.");

            var sameObjectId = updatedObjectId;

            edge.ChangeRoute(
                EdgeRoute.Straight);

            _context.Edges.Update(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var straightObjectId),
                "Straightened edge disappeared from index.");

            Ensure(
                straightObjectId ==
                sameObjectId,
                "Straightening edge replaced its DWG entity.");

            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                var polyline =
                    transaction.GetObject(
                        straightObjectId,
                        OpenMode.ForRead)
                    as Polyline;

                Ensure(
                    polyline is not null,
                    "Straightened edge is not a Polyline.");

                Ensure(
                    polyline.NumberOfVertices == 2,
                    $"Expected 2 vertices after straightening, " +
                    $"actual {polyline.NumberOfVertices}.");

                AssertPolylinePoint(
                    polyline,
                    0,
                    a.Position,
                    "Vertex A is incorrect after straightening.");

                AssertPolylinePoint(
                    polyline,
                    1,
                    b.Position,
                    "Vertex B is incorrect after straightening.");
            }
        }
        finally
        {
            if (edge is not null)
            {
                _context.Edges.Delete(
                    edge.Id);
            }

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestPolylineSynchronizationPreservesBends()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1500,
                        1500));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1580,
                        1500));

            var p1 =
                new Point2(
                    1520,
                    1540);

            var p2 =
                new Point2(
                    1560,
                    1540);

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        p1,
                        p2
                        }));

            _context.Edges.Add(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var edgeObjectId),
                "Bent edge is missing from index.");

            var newPosition =
                new Point2(
                    1490,
                    1470);

            MoveVertexEntity(
                a.Id,
                newPosition);

            _synchronizer
                .UpdateIncidentEdges(
                    a.Id);

            using var transaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();

            var polyline =
                transaction.GetObject(
                    edgeObjectId,
                    OpenMode.ForRead)
                as Polyline;

            Ensure(
                polyline is not null,
                "Bent edge is not a Polyline.");

            Ensure(
                polyline.NumberOfVertices == 4,
                $"Expected 4 vertices after synchronization, " +
                $"actual {polyline.NumberOfVertices}.");

            AssertPolylinePoint(
                polyline,
                0,
                newPosition,
                "Moved endpoint was not synchronized.");

            AssertPolylinePoint(
                polyline,
                1,
                p1,
                "Synchronization moved the first bend.");

            AssertPolylinePoint(
                polyline,
                2,
                p2,
                "Synchronization moved the second bend.");

            AssertPolylinePoint(
                polyline,
                3,
                b.Position,
                "Synchronization unexpectedly moved Vertex B.");
        }
        finally
        {
            if (edge is not null)
            {
                _context.Edges.Delete(
                    edge.Id);
            }

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestShortestPathUsesPolylineLength()
    {
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

        GraphEdge? longEdge = null;
        GraphEdge? ac = null;
        GraphEdge? cb = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1600,
                        1600));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1700,
                        1600));

            c =
                vertexService.CreateVertex(
                    new Point2(
                        1650,
                        1620));

            longEdge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        new Point2(
                            1600,
                            1700),

                        new Point2(
                            1700,
                            1700)
                        }));

            _context.Edges.Add(
                longEdge);

            ac =
                edgeService.CreateEdge(
                    a.Id,
                    c.Id);

            cb =
                edgeService.CreateEdge(
                    c.Id,
                    b.Id);

            var shortestPathService =
                new ShortestPathApplicationService(
                    _context.Vertices,
                    _context.Edges,
                    new DijkstraShortestPathService(
                        new EdgeLengthCalculator())
                    );

            var result =
                shortestPathService.Find(
                    a.Id,
                    b.Id);

            Ensure(
                result is not null,
                "Shortest path was not found.");

            Ensure(
                result.VertexIds.Count == 3,
                $"Expected A-C-B path with 3 vertices, " +
                $"actual count {result.VertexIds.Count}.");

            Ensure(
                result.VertexIds[0] == a.Id,
                "Shortest path does not start at A.");

            Ensure(
                result.VertexIds[1] == c.Id,
                "Shortest path did not choose C.");

            Ensure(
                result.VertexIds[2] == b.Id,
                "Shortest path does not end at B.");

            Ensure(
                !result.EdgeIds.Contains(
                    longEdge.Id),
                "Shortest path incorrectly used the " +
                "geometrically long polyline edge.");

            Ensure(
                result.EdgeIds.Contains(
                    ac.Id) &&
                result.EdgeIds.Contains(
                    cb.Id),
                "Shortest path does not contain A-C and C-B edges.");
        }
        finally
        {
            if (longEdge is not null)
                _context.Edges.Delete(longEdge.Id);

            if (ac is not null)
                _context.Edges.Delete(ac.Id);

            if (cb is not null)
                _context.Edges.Delete(cb.Id);

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);

            if (c is not null)
                DeleteVertexIfExists(c.Id);
        }
    }

    private void AssertEdgePolyline(
        Guid edgeId,
        params Point2[] expectedPoints)
    {
        Ensure(
            _context.Index.TryGetEdgeObjectId(
                edgeId,
                out var objectId),
            $"Edge '{edgeId}' is missing from index.");

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var polyline =
            transaction.GetObject(
                objectId,
                OpenMode.ForRead)
            as Polyline;

        Ensure(
            polyline is not null,
            $"Edge '{edgeId}' is not represented by Polyline.");

        Ensure(
            !polyline.Closed,
            $"Edge '{edgeId}' polyline is closed.");

        Ensure(
            polyline.NumberOfVertices ==
            expectedPoints.Length,
            $"Edge '{edgeId}' has " +
            $"{polyline.NumberOfVertices} polyline vertices, " +
            $"expected {expectedPoints.Length}.");

        for (var i = 0;
             i < expectedPoints.Length;
             i++)
        {
            AssertPolylinePoint(
                polyline,
                i,
                expectedPoints[i],
                $"Unexpected point {i} of edge '{edgeId}'.");
        }
    }

    private void TestSplitBentPolylineEdge()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;

        GraphEdge? original = null;
        SplitEdgeResult? split = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1800,
                        1800));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        1880,
                        1800));

            var p1 =
                new Point2(
                    1800,
                    1840);

            var p2 =
                new Point2(
                    1840,
                    1840);

            var p3 =
                new Point2(
                    1880,
                    1840);

            var splitPoint =
                new Point2(
                    1860,
                    1840);

            original =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        p1,
                        p2,
                        p3
                        }));

            _context.Edges.Add(
                original);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out var originalObjectId),
                "Original bent edge is missing from index.");

            AssertEdgePolyline(
                original.Id,
                a.Position,
                p1,
                p2,
                p3,
                b.Position);

            var originalLength =
                EdgeRouteGeometry.CalculateLength(
                    a.Position,
                    b.Position,
                    original.Route);

            split =
                splitService.Split(
                    original.Id,
                    splitPoint);

            //
            // Старого GraphEdge больше нет.
            //
            Ensure(
                _context.Edges.Get(
                    original.Id) is null,
                "Original bent edge still exists after split.");

            Ensure(
                !_context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out _),
                "Original bent edge still exists in index.");

            Ensure(
                originalObjectId.IsErased,
                "Original DWG Polyline was not erased.");

            //
            // Новая Vertex.
            //
            EnsurePoint(
                split.NewVertex.Position,
                splitPoint,
                "Split vertex has incorrect position.");

            Ensure(
                _context.Vertices.Get(
                    split.NewVertex.Id) is not null,
                "Split vertex was not persisted.");

            //
            // Проверяем topology.
            //
            Ensure(
                split.EdgeA.VertexAId ==
                a.Id,
                "Edge A-C has incorrect VertexAId.");

            Ensure(
                split.EdgeA.VertexBId ==
                split.NewVertex.Id,
                "Edge A-C has incorrect VertexBId.");

            Ensure(
                split.EdgeB.VertexAId ==
                split.NewVertex.Id,
                "Edge C-B has incorrect VertexAId.");

            Ensure(
                split.EdgeB.VertexBId ==
                b.Id,
                "Edge C-B has incorrect VertexBId.");

            //
            // Проверяем Domain routes.
            //
            Ensure(
                split.EdgeA.Route.Count == 2,
                $"Expected 2 bends in A-C, " +
                $"actual {split.EdgeA.Route.Count}.");

            EnsurePoint(
                split.EdgeA.Route
                    .IntermediatePoints[0],
                p1,
                "A-C lost P1.");

            EnsurePoint(
                split.EdgeA.Route
                    .IntermediatePoints[1],
                p2,
                "A-C lost P2.");

            Ensure(
                split.EdgeB.Route.Count == 1,
                $"Expected 1 bend in C-B, " +
                $"actual {split.EdgeB.Route.Count}.");

            EnsurePoint(
                split.EdgeB.Route
                    .IntermediatePoints[0],
                p3,
                "C-B lost P3.");

            //
            // А теперь главное:
            // проверяем реальные DWG Polyline.
            //
            AssertEdgePolyline(
                split.EdgeA.Id,
                a.Position,
                p1,
                p2,
                splitPoint);

            AssertEdgePolyline(
                split.EdgeB.Id,
                splitPoint,
                p3,
                b.Position);

            //
            // Repository должен восстановить
            // те же routes из Polyline.
            //
            var restoredA =
                _context.Edges.Get(
                    split.EdgeA.Id);

            var restoredB =
                _context.Edges.Get(
                    split.EdgeB.Id);

            Ensure(
                restoredA is not null,
                "Repository cannot restore A-C edge.");

            Ensure(
                restoredB is not null,
                "Repository cannot restore C-B edge.");

            Ensure(
                restoredA.Id ==
                split.EdgeA.Id,
                "Restored A-C edge Id changed.");

            Ensure(
                restoredB.Id ==
                split.EdgeB.Id,
                "Restored C-B edge Id changed.");

            Ensure(
                restoredA.Route.Count == 2,
                "Restored A-C route is incorrect.");

            Ensure(
                restoredB.Route.Count == 1,
                "Restored C-B route is incorrect.");

            //
            // Index relationships.
            //
            var incidentToC =
                _context.Edges
                    .GetByVertex(
                        split.NewVertex.Id)
                    .ToArray();

            Ensure(
                incidentToC.Length == 2,
                $"Split vertex has " +
                $"{incidentToC.Length} incident edges, expected 2.");

            Ensure(
                incidentToC.Any(
                    x => x.Id ==
                         split.EdgeA.Id),
                "A-C is not incident to split vertex.");

            Ensure(
                incidentToC.Any(
                    x => x.Id ==
                         split.EdgeB.Id),
                "C-B is not incident to split vertex.");

            //
            // И длина всей геометрии
            // не должна измениться.
            //
            var leftLength =
                EdgeRouteGeometry.CalculateLength(
                    a.Position,
                    split.NewVertex.Position,
                    split.EdgeA.Route);

            var rightLength =
                EdgeRouteGeometry.CalculateLength(
                    split.NewVertex.Position,
                    b.Position,
                    split.EdgeB.Route);

            Ensure(
                Math.Abs(
                    originalLength -
                    (leftLength + rightLength)) <
                1e-6,
                "Polyline split changed total edge length.");
        }
        finally
        {
            //
            // DeleteVertexIfExists(C) должен
            // каскадно удалить A-C и C-B.
            //
            if (split is not null)
            {
                DeleteVertexIfExists(
                    split.NewVertex.Id);
            }

            //
            // Если тест упал раньше split,
            // исходное ребро ещё может существовать.
            //
            if (original is not null &&
                _context.Edges.Get(
                    original.Id) is not null)
            {
                _context.Edges.Delete(
                    original.Id);
            }

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestAutoBuildSplitBentPolylineEdge()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var buildService =
            new GraphBuildService(
                edgeService,
                _context.Edges);

        var splitService =
            new SplitEdgeService(
                _context.Vertices,
                _context.Edges);

        var executor =
            new GraphBuildStepExecutor(
                _document,
                vertexService,
                splitService,
                buildService);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphVertex? d = null;
        GraphVertex? c = null;

        GraphEdge? original = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        1900,
                        1900));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        2000,
                        1900));

            d =
                vertexService.CreateVertex(
                    new Point2(
                        1950,
                        1980));

            var p1 =
                new Point2(
                    1920,
                    1940);

            var p2 =
                new Point2(
                    1980,
                    1940);

            var splitPoint =
                new Point2(
                    1950,
                    1940);

            original =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        p1,
                        p2
                        }));

            _context.Edges.Add(
                original);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out var originalObjectId),
                "Bent auto-build edge is missing from index.");

            //
            // GRAPHBUILD уже находится в D.
            //
            buildService.AdvanceTo(
                d);

            //
            // Point намеренно неправильный:
            // имитируем Endpoint OSNAP в A.
            //
            // PickPoint — фактический клик
            // по горизонтальному сегменту P1-P2.
            //
            var pick =
                BuildPickResult.FromEdge(
                    new Point3d(
                        a.Position.X,
                        a.Position.Y,
                        0),

                    new Point3d(
                        splitPoint.X,
                        splitPoint.Y,
                        0),

                    originalObjectId,
                    original);

            c =
                executor.Execute(
                    pick);

            //
            // Если executor использовал pick.Point
            // вместо pick.PickPoint,
            // сюда мы либо не дошли бы вообще,
            // либо C оказался бы в неправильном месте.
            //
            EnsurePoint(
                c.Position,
                splitPoint,
                "Auto-build split used Point instead of PickPoint.");

            Ensure(
                buildService.CurrentVertex?.Id ==
                c.Id,
                "Split vertex did not become CurrentVertex.");

            Ensure(
                _context.Edges.Get(
                    original.Id) is null,
                "Original bent edge still exists after auto-build split.");

            Ensure(
                !_context.Index.TryGetEdgeObjectId(
                    original.Id,
                    out _),
                "Original bent edge remains in index.");

            //
            // C должно иметь три ребра:
            //
            // A-C
            // C-B
            // D-C
            //
            var incidentToC =
                _context.Edges
                    .GetByVertex(
                        c.Id)
                    .ToArray();

            Ensure(
                incidentToC.Length == 3,
                $"Auto-build split vertex has " +
                $"{incidentToC.Length} incident edges, expected 3.");

            var edgeAC =
                incidentToC.SingleOrDefault(
                    edge =>
                        edge.IsIncidentTo(a.Id));

            var edgeCB =
                incidentToC.SingleOrDefault(
                    edge =>
                        edge.IsIncidentTo(b.Id));

            var edgeDC =
                incidentToC.SingleOrDefault(
                    edge =>
                        edge.IsIncidentTo(d.Id));

            Ensure(
                edgeAC is not null,
                "Auto-build did not create A-C.");

            Ensure(
                edgeCB is not null,
                "Auto-build did not create C-B.");

            Ensure(
                edgeDC is not null,
                "Auto-build did not create D-C.");

            //
            // Bend distribution.
            //
            Ensure(
                edgeAC.Route.Count == 1,
                $"Expected one bend in A-C, " +
                $"actual {edgeAC.Route.Count}.");

            EnsurePoint(
                edgeAC.Route
                    .IntermediatePoints[0],
                p1,
                "A-C has incorrect bend.");

            Ensure(
                edgeCB.Route.Count == 1,
                $"Expected one bend in C-B, " +
                $"actual {edgeCB.Route.Count}.");

            EnsurePoint(
                edgeCB.Route
                    .IntermediatePoints[0],
                p2,
                "C-B has incorrect bend.");

            Ensure(
                edgeDC.Route.IsStraight,
                "D-C build edge must be straight.");

            //
            // Реальная DWG geometry.
            //
            AssertEdgePolyline(
                edgeAC.Id,
                a.Position,
                p1,
                splitPoint);

            AssertEdgePolyline(
                edgeCB.Id,
                splitPoint,
                p2,
                b.Position);

            AssertEdgePolyline(
                edgeDC.Id,
                d.Position,
                splitPoint);

            //
            // И дополнительно убеждаемся,
            // что A-C не был случайно создан дважды.
            //
            var acCount =
                _context.Edges
                    .GetAll()
                    .Count(
                        edge =>
                            edge.IsIncidentTo(a.Id) &&
                            edge.IsIncidentTo(c.Id));

            Ensure(
                acCount == 1,
                $"Expected exactly one A-C edge, " +
                $"actual {acCount}.");
        }
        finally
        {
            //
            // C удалит все три incident edges.
            //
            if (c is not null)
            {
                DeleteVertexIfExists(
                    c.Id);
            }

            //
            // На случай падения до успешного split.
            //
            if (original is not null &&
                _context.Edges.Get(
                    original.Id) is not null)
            {
                _context.Edges.Delete(
                    original.Id);
            }

            if (d is not null)
                DeleteVertexIfExists(d.Id);

            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void SetEdgePolylinePoint(
        Guid edgeId,
        int vertexIndex,
        Point2 point)
    {
        Ensure(
            _context.Index.TryGetEdgeObjectId(
                edgeId,
                out var objectId),
            $"Edge '{edgeId}' is missing from index.");

        using var transaction =
            _document.Database
                .TransactionManager
                .StartTransaction();

        var polyline =
            transaction.GetObject(
                objectId,
                OpenMode.ForWrite)
            as Polyline;

        Ensure(
            polyline is not null,
            $"Edge '{edgeId}' is not a Polyline.");

        Ensure(
            vertexIndex >= 0 &&
            vertexIndex < polyline.NumberOfVertices,
            $"Polyline vertex index {vertexIndex} is invalid.");

        polyline.SetPointAt(
            vertexIndex,
            new Point2d(
                point.X,
                point.Y));

        transaction.Commit();
    }

    private void TestAddBendPersistsToDwgPolyline()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var edgeService =
            new EdgeService(
                _context.Vertices,
                _context.Edges);

        var addBendService =
            new AddBendService(
                _context.Vertices,
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        2100,
                        2100));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        2200,
                        2100));

            edge =
                edgeService.CreateEdge(
                    a.Id,
                    b.Id);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var originalObjectId),
                "Created edge is missing from index.");

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                b.Position);

            var bend =
                new Point2(
                    2150,
                    2100);

            var result =
                addBendService.Add(
                    edge.Id,
                    bend);

            Ensure(
                result.Edge.Id ==
                edge.Id,
                "AddBend changed EdgeId.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var updatedObjectId),
                "Edge disappeared from index after AddBend.");

            Ensure(
                updatedObjectId ==
                originalObjectId,
                "AddBend replaced the DWG entity.");

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                bend,
                b.Position);

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Repository could not restore edge after AddBend.");

            Ensure(
                restored.Route.Count == 1,
                $"Expected one bend, actual " +
                $"{restored.Route.Count}.");

            EnsurePoint(
                restored.Route
                    .IntermediatePoints[0],
                bend,
                "Restored bend has incorrect position.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestRemoveLastBendMakesEdgeStraight()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        var removeBendService =
            new RemoveBendService(
                _context.Edges);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        2300,
                        2300));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        2400,
                        2300));

            var bend =
                new Point2(
                    2350,
                    2350);

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        bend
                        }));

            _context.Edges.Add(
                edge);

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var originalObjectId),
                "Bent edge is missing from index.");

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                bend,
                b.Position);

            var result =
                removeBendService.Remove(
                    edge.Id,
                    0);

            EnsurePoint(
                result.RemovedPoint,
                bend,
                "RemoveBend returned incorrect point.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    edge.Id,
                    out var updatedObjectId),
                "Edge disappeared after RemoveBend.");

            Ensure(
                updatedObjectId ==
                originalObjectId,
                "RemoveBend replaced the DWG entity.");

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                b.Position);

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Repository could not restore straightened edge.");

            Ensure(
                restored.Route.IsStraight,
                "Edge route is not straight after removing last bend.");

            Ensure(
                restored.Route.Count == 0,
                $"Expected zero bends, actual " +
                $"{restored.Route.Count}.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestPolylineModificationPreservesMovedBend()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        2500,
                        2500));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        2600,
                        2500));

            var p1 =
                new Point2(
                    2530,
                    2540);

            var p2 =
                new Point2(
                    2570,
                    2540);

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        p1,
                        p2
                        }));

            _context.Edges.Add(
                edge);

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                p1,
                p2,
                b.Position);

            var movedP1 =
                new Point2(
                    2530,
                    2580);

            //
            // Polyline:
            //
            // [0] A
            // [1] P1
            // [2] P2
            // [3] B
            //
            SetEdgePolylinePoint(
                edge.Id,
                1,
                movedP1);

            //
            // Имитируем нормализацию,
            // которую watcher выполняет
            // после native grip editing.
            //
            _context.EdgeGeometrySynchronizer
                .UpdateEdge(
                    edge.Id);

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                movedP1,
                p2,
                b.Position);

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Repository could not restore grip-modified edge.");

            Ensure(
                restored.Route.Count == 2,
                $"Expected two bends, actual " +
                $"{restored.Route.Count}.");

            EnsurePoint(
                restored.Route
                    .IntermediatePoints[0],
                movedP1,
                "Moved bend was reverted by synchronization.");

            EnsurePoint(
                restored.Route
                    .IntermediatePoints[1],
                p2,
                "Unmodified bend changed during synchronization.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestPolylineEndpointModificationIsCorrected()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? a = null;
        GraphVertex? b = null;
        GraphEdge? edge = null;

        try
        {
            a =
                vertexService.CreateVertex(
                    new Point2(
                        2700,
                        2700));

            b =
                vertexService.CreateVertex(
                    new Point2(
                        2800,
                        2700));

            var bend =
                new Point2(
                    2750,
                    2750);

            edge =
                GraphEdge.Create(
                    a.Id,
                    b.Id,
                    new EdgeRoute(
                        new[]
                        {
                        bend
                        }));

            _context.Edges.Add(
                edge);

            AssertEdgePolyline(
                edge.Id,
                a.Position,
                bend,
                b.Position);

            var invalidStart =
                new Point2(
                    2650,
                    2650);

            var invalidEnd =
                new Point2(
                    2850,
                    2650);

            //
            // [0] = A endpoint
            //
            SetEdgePolylinePoint(
                edge.Id,
                0,
                invalidStart);

            //
            // [2] = B endpoint
            //
            SetEdgePolylinePoint(
                edge.Id,
                2,
                invalidEnd);

            //
            // До synchronizer действительно
            // должна существовать повреждённая
            // геометрия.
            //
            AssertEdgePolyline(
                edge.Id,
                invalidStart,
                bend,
                invalidEnd);

            _context.EdgeGeometrySynchronizer
                .UpdateEdge(
                    edge.Id);

            //
            // Endpoints восстановлены,
            // bend остался тем же.
            //
            AssertEdgePolyline(
                edge.Id,
                a.Position,
                bend,
                b.Position);

            var restored =
                _context.Edges.Get(
                    edge.Id);

            Ensure(
                restored is not null,
                "Repository could not restore normalized edge.");

            Ensure(
                restored.Route.Count == 1,
                "Endpoint normalization changed bend count.");

            EnsurePoint(
                restored.Route
                    .IntermediatePoints[0],
                bend,
                "Endpoint normalization changed bend position.");
        }
        finally
        {
            if (a is not null)
                DeleteVertexIfExists(a.Id);

            if (b is not null)
                DeleteVertexIfExists(b.Id);
        }
    }

    private void TestVertexAttachmentXRecordRoundTrip()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? vertex = null;

        try
        {
            vertex =
                vertexService.CreateVertex(
                    new Point2(
                        2900,
                        2900));

            var attachment =
                new VertexAttachment(
                    @"Files\report.pdf");

            _context.Attachments.Add(
                vertex.Id,
                attachment);

            //
            // Repository должен прочитать
            // данные обратно именно из DWG.
            //
            var restored =
                _context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            Ensure(
                restored.Length == 1,
                $"Expected one attachment, " +
                $"actual {restored.Length}.");

            Ensure(
                string.Equals(
                    restored[0].Path,
                    attachment.Path,
                    StringComparison.Ordinal),
                $"Unexpected attachment path. " +
                $"Actual: '{restored[0].Path}', " +
                $"expected: '{attachment.Path}'.");

            //
            // Убеждаемся, что attachment
            // не повредил основной Vertex metadata.
            //
            var restoredVertex =
                _context.Vertices.Get(
                    vertex.Id);

            Ensure(
                restoredVertex is not null,
                "Vertex could not be restored after writing attachment.");

            Ensure(
                restoredVertex.Id ==
                vertex.Id,
                "Vertex Id changed after writing attachment.");

            //
            // Проверяем непосредственно структуру DWG:
            // XRecord действительно находится
            // на Vertex entity.
            //
            Ensure(
                _context.Index.TryGetVertexObjectId(
                    vertex.Id,
                    out var objectId),
                "Vertex is missing from index.");

            using var transaction =
                _document.Database
                    .TransactionManager
                    .StartTransaction();

            var entity =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                as Entity;

            Ensure(
                entity is not null,
                "Vertex entity could not be opened.");

            Ensure(
                !entity.ExtensionDictionary.IsNull,
                "Vertex has no extension dictionary.");

            var dictionary =
                transaction.GetObject(
                    entity.ExtensionDictionary,
                    OpenMode.ForRead)
                as DBDictionary;

            Ensure(
                dictionary is not null,
                "Vertex extension dictionary could not be opened.");

            Ensure(
                dictionary.Contains(
                    VertexAttachmentXRecordStore.RecordKey),
                $"'{VertexAttachmentXRecordStore.RecordKey}' " +
                $"XRecord was not created.");
        }
        finally
        {
            if (vertex is not null)
            {
                DeleteVertexIfExists(
                    vertex.Id);
            }
        }
    }

    private void TestVertexAttachmentAddAndDetach()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? vertex = null;

        try
        {
            vertex =
                vertexService.CreateVertex(
                    new Point2(
                        3000,
                        3000));

            var first =
                new VertexAttachment(
                    @"Documents\a.pdf");

            var second =
                new VertexAttachment(
                    @"Images\b.jpg");

            _context.Attachments.Add(
                vertex.Id,
                first);

            _context.Attachments.Add(
                vertex.Id,
                second);

            var afterAdd =
                _context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            Ensure(
                afterAdd.Length == 2,
                $"Expected two attachments, " +
                $"actual {afterAdd.Length}.");

            Ensure(
                afterAdd.Any(
                    x =>
                        x.Path ==
                        first.Path),
                "First attachment was not persisted.");

            Ensure(
                afterAdd.Any(
                    x =>
                        x.Path ==
                        second.Path),
                "Second attachment was not persisted.");

            _context.Attachments.Remove(
                vertex.Id,
                first.Path);

            var afterRemove =
                _context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            Ensure(
                afterRemove.Length == 1,
                $"Expected one attachment after detach, " +
                $"actual {afterRemove.Length}.");

            Ensure(
                afterRemove[0].Path ==
                second.Path,
                "Detach removed the wrong attachment.");

            //
            // Теперь удаляем последний.
            //
            _context.Attachments.Remove(
                vertex.Id,
                second.Path);

            var afterRemoveAll =
                _context.Attachments
                    .GetAll(
                        vertex.Id);

            Ensure(
                afterRemoveAll.Count == 0,
                "Attachments remain after removing all items.");

            //
            // Сам Vertex никуда не делся.
            //
            Ensure(
                _context.Vertices.Get(
                    vertex.Id) is not null,
                "Detaching files deleted or corrupted the vertex.");
        }
        finally
        {
            if (vertex is not null)
            {
                DeleteVertexIfExists(
                    vertex.Id);
            }
        }
    }

    private void TestAttachmentDetachPreservesPhysicalFile()
    {
        var vertexService =
            new VertexService(
                _context.Vertices);

        GraphVertex? vertex = null;

        string? tempDirectory = null;
        string? tempFile = null;

        try
        {
            vertex =
                vertexService.CreateVertex(
                    new Point2(
                        3400,
                        3400));

            tempDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "GraphPluginTests",
                    Guid.NewGuid()
                        .ToString("N"));

            Directory.CreateDirectory(
                tempDirectory);

            tempFile =
                Path.Combine(
                    tempDirectory,
                    "attachment.txt");

            File.WriteAllText(
                tempFile,
                "GraphPlugin attachment test.");

            Ensure(
                File.Exists(tempFile),
                "Test file was not created.");

            //
            // drawingPath = null намеренно:
            // для этого теста нам нужна абсолютная
            // ссылка без зависимости от расположения DWG.
            //
            var attachment =
                _context.AttachmentService.Attach(
                    vertex.Id,
                    tempFile,
                    drawingPath: null);

            var beforeDetach =
                _context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            Ensure(
                beforeDetach.Length == 1,
                $"Expected one attachment before detach, " +
                $"actual {beforeDetach.Length}.");

            Ensure(
                File.Exists(tempFile),
                "Attaching a file unexpectedly deleted it.");

            _context.AttachmentService.Detach(
                vertex.Id,
                attachment.Path);

            var afterDetach =
                _context.Attachments
                    .GetAll(
                        vertex.Id)
                    .ToArray();

            Ensure(
                afterDetach.Length == 0,
                $"Attachment reference remains after detach. " +
                $"Actual count: {afterDetach.Length}.");

            //
            // Главный invariant теста.
            //
            Ensure(
                File.Exists(tempFile),
                "Detaching an attachment deleted the physical file.");

            //
            // Vertex тоже должен остаться.
            //
            Ensure(
                _context.Vertices.Get(
                    vertex.Id) is not null,
                "Detaching an attachment deleted or corrupted the vertex.");
        }
        finally
        {
            if (vertex is not null)
            {
                DeleteVertexIfExists(
                    vertex.Id);
            }

            //
            // Это исключительно cleanup самого теста.
            // Production Detach никогда этого не делает.
            //
            if (tempFile is not null &&
                File.Exists(tempFile))
            {
                File.Delete(
                    tempFile);
            }

            if (tempDirectory is not null &&
                Directory.Exists(tempDirectory))
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);
            }
        }
    }

    public void VerifyCppStyleInteropTest()
    {
        var document = _document;

        var context = _context;

        var store = new CppStyleInteropManifestStore();

        CppStyleInteropManifest manifest;

        using (var transaction =
               document.Database
                   .TransactionManager
                   .StartTransaction())
        {
            
            manifest = store
                .ReadStyleManifest(
                    document.Database,
                    transaction);
        }

        var vertex =
            context.Vertices.Get(
                manifest.VertexId);

        Ensure(
            vertex is not null,
            "C# repository cannot read C++ replacement vertex.");

        Ensure(
            vertex.Style.Shape ==
                VertexShape.Triangle,
            "C++ replacement was not restored as Triangle.");

        Ensure(
            vertex.Style.Color ==
                GraphColor.Red,
            "Triangle color is not Red.");

        Ensure(
            context.Index.TryGetVertexObjectId(
                manifest.VertexId,
                out var currentObjectId),
            "C# GraphEntityIndex does not contain replaced vertex.");

        Ensure(
            !string.Equals(
                currentObjectId.Handle.ToString(),
                manifest.OldHandle,
                StringComparison.OrdinalIgnoreCase),
            "GraphEntityIndex still points to old Circle ObjectId.");

        using (var transaction =
               document.Database
                   .TransactionManager
                   .StartTransaction())
        {
            var entity =
                transaction.GetObject(
                    currentObjectId,
                    OpenMode.ForRead);

            Ensure(
                entity is Polyline,
                "C# index does not point to replacement Triangle Polyline.");
        }

        var attachments =
            context.Attachments
                .GetAll(
                    manifest.VertexId);

        Ensure(
            attachments.Any(
                x => string.Equals(
                    x.Path,
                    manifest.AttachmentPath,
                    StringComparison.OrdinalIgnoreCase)),
            "Attachment was lost during C++ style replacement.");

        _document.Editor.WriteMessage(
                "\n[PASS] C++ style interop test.");
        //
        // Cleanup.
        //
        context.Graph.DeleteVertex(
            manifest.VertexId);

        store.Delete(document.Database);

    }

    public void VerifyCppDeleteUndoTest()
    {
        var manifestStore =
            new CppDeleteUndoTestManifestStore();

        try
        {
            CppDeleteUndoTestManifest manifest;

            //
            // Транзакция нужна ТОЛЬКО для чтения manifest.
            //
            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifest =
                    manifestStore.Read(
                        _document.Database,
                        transaction);

                transaction.Commit();
            }

            var vertexA =
                _context.Vertices.Get(
                    manifest.VertexAId);

            var vertexB =
                _context.Vertices.Get(
                    manifest.VertexBId);

            var vertexC =
                _context.Vertices.Get(
                    manifest.VertexCId);

            Ensure(
                vertexA is not null,
                "Vertex A was not restored after UNDO.");

            Ensure(
                vertexB is not null,
                "Vertex B disappeared.");

            Ensure(
                vertexC is not null,
                "Vertex C disappeared.");

            var edgeAB =
                _context.Edges.Get(
                    manifest.EdgeABId);

            var edgeAC =
                _context.Edges.Get(
                    manifest.EdgeACId);

            var edgeBC =
                _context.Edges.Get(
                    manifest.EdgeBCId);

            Ensure(
                edgeAB is not null,
                "Edge A-B was not restored.");

            Ensure(
                edgeAC is not null,
                "Edge A-C was not restored.");

            Ensure(
                edgeBC is not null,
                "Edge B-C disappeared.");

            Ensure(
                _context.Index.TryGetVertexObjectId(
                    manifest.VertexAId,
                    out var vertexAObjectId),
                "Vertex A is missing from index.");

            Ensure(
                !vertexAObjectId.IsNull &&
                !vertexAObjectId.IsErased,
                "Vertex A ObjectId is invalid.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    manifest.EdgeABId,
                    out var edgeABObjectId),
                "Edge A-B is missing from index.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    manifest.EdgeACId,
                    out var edgeACObjectId),
                "Edge A-C is missing from index.");

            Ensure(
                _context.Index.TryGetEdgeObjectId(
                    manifest.EdgeBCId,
                    out var edgeBCObjectId),
                "Edge B-C is missing from index.");

            Ensure(
                !edgeABObjectId.IsErased,
                "Edge A-B is still erased.");

            Ensure(
                !edgeACObjectId.IsErased,
                "Edge A-C is still erased.");

            Ensure(
                !edgeBCObjectId.IsErased,
                "Edge B-C was unexpectedly erased.");

            var incidentEdges =
                _context.Index
                    .GetIncidentEdgeIds(
                        manifest.VertexAId)
                    .ToHashSet();

            Ensure(
                incidentEdges.Count == 2,
                $"Expected 2 incident edges for A, " +
                $"actual {incidentEdges.Count}.");

            Ensure(
                incidentEdges.Contains(
                    manifest.EdgeABId),
                "A-B is missing from incident index.");

            Ensure(
                incidentEdges.Contains(
                    manifest.EdgeACId),
                "A-C is missing from incident index.");

            Ensure(
                !incidentEdges.Contains(
                    manifest.EdgeBCId),
                "B-C is incorrectly incident to A.");

            _document.Editor.WriteMessage(
                "\n[PASS] C++ cascade delete -> " +
                "UNDO -> C# restore.");

            //
            // Здесь уже НЕТ открытой внешней transaction.
            // Каждый DeleteVertexIfExists может нормально
            // использовать свои repository transactions.
            //
            DeleteVertexIfExists(
                manifest.VertexAId);

            DeleteVertexIfExists(
                manifest.VertexBId);

            DeleteVertexIfExists(
                manifest.VertexCId);

            //
            // Manifest удаляем отдельной transaction
            // и обязательно Commit().
            //
            using (var transaction =
                   _document.Database
                       .TransactionManager
                       .StartTransaction())
            {
                manifestStore.Delete(
                    _document.Database,
                    transaction);

                transaction.Commit();
            }

            _document.Editor.WriteMessage(
                "\nTest objects cleaned up.");
        }
        catch (System.Exception ex)
        {
            _document.Editor.WriteMessage(
                $"\n[FAIL] C++ delete/undo test:\n{ex}");
        }
    }

    private void DeleteVertexIfExists(
    Guid vertexId)
    {
        if (_context.Vertices.Get(vertexId) is null)
        {
            return;
        }

        _context.Graph.DeleteVertex(vertexId);
    }
}