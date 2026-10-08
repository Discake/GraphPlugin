using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Runtime;

public sealed class GraphEntityIndex
{
    private readonly Dictionary<Guid, ObjectId> _vertices = new();
    private readonly Dictionary<Guid, ObjectId> _edges = new();

    private readonly Dictionary<ObjectId, Guid> _vertexIds = new();
    private readonly Dictionary<ObjectId, Guid> _edgeIds = new();

    private readonly Dictionary<Guid, (Guid VertexAId, Guid VertexBId)> _edgeVertices = new();

    private readonly Dictionary<Guid, HashSet<Guid>> _edgesByVertex = new();

    public int VertexCount => _vertices.Count;

    public int EdgeCount => _edges.Count;

    public void AddVertex(Guid id, ObjectId objectId)
    {
        if (objectId.IsNull)
        {
            throw new ArgumentException("Vertex ObjectId cannot be null.", nameof(objectId));
        }

        if (_vertices.TryGetValue(id, out var existingObjectId))
        {
            if (
                existingObjectId == objectId
                && _vertexIds.TryGetValue(objectId, out var existingVertexId)
                && existingVertexId == id
            )
            {
                return;
            }

            throw new InvalidOperationException(
                $"Vertex '{id}' is already registered " + $"with object '{existingObjectId}'."
            );
        }

        if (_vertexIds.TryGetValue(objectId, out var mappedVertexId))
        {
            throw new InvalidOperationException(
                $"Object '{objectId}' is already registered " + $"as vertex '{mappedVertexId}'."
            );
        }

        _vertices.Add(id, objectId);

        _vertexIds.Add(objectId, id);
    }

    public void AddEdge(Guid edgeId, ObjectId objectId, Guid vertexAId, Guid vertexBId)
    {
        if (objectId.IsNull)
        {
            throw new ArgumentException("Edge ObjectId cannot be null.", nameof(objectId));
        }

        if (_edges.TryGetValue(edgeId, out var existingObjectId))
        {
            if (
                existingObjectId == objectId
                && _edgeIds.TryGetValue(objectId, out var existingEdgeId)
                && existingEdgeId == edgeId
                && _edgeVertices.TryGetValue(edgeId, out var existingVertices)
                && existingVertices == (vertexAId, vertexBId)
            )
            {
                return;
            }

            throw new InvalidOperationException(
                $"Edge '{edgeId}' is already registered " + $"with object '{existingObjectId}'."
            );
        }

        if (_edgeIds.TryGetValue(objectId, out var mappedEdgeId))
        {
            throw new InvalidOperationException(
                $"Object '{objectId}' is already registered " + $"as edge '{mappedEdgeId}'."
            );
        }

        _edges.Add(edgeId, objectId);

        _edgeIds.Add(objectId, edgeId);

        _edgeVertices.Add(edgeId, (vertexAId, vertexBId));

        AddIncidentEdge(vertexAId, edgeId);

        AddIncidentEdge(vertexBId, edgeId);
    }

    public bool TryGetVertexObjectId(Guid id, out ObjectId objectId)
    {
        return _vertices.TryGetValue(id, out objectId);
    }

    public bool TryGetEdgeObjectId(Guid id, out ObjectId objectId)
    {
        return _edges.TryGetValue(id, out objectId);
    }

    public bool TryGetVertexId(ObjectId objectId, out Guid id)
    {
        return _vertexIds.TryGetValue(objectId, out id);
    }

    public bool TryGetEdgeId(ObjectId objectId, out Guid id)
    {
        return _edgeIds.TryGetValue(objectId, out id);
    }

    public void RemoveVertex(Guid vertexId)
    {
        if (_vertices.Remove(vertexId, out var objectId))
        {
            _vertexIds.Remove(objectId);
        }

        _edgesByVertex.Remove(vertexId);
    }

    public void RemoveEdge(Guid edgeId)
    {
        if (_edgeVertices.TryGetValue(edgeId, out var vertices))
        {
            if (_edgesByVertex.TryGetValue(vertices.VertexAId, out var edgesA))
            {
                edgesA.Remove(edgeId);

                if (edgesA.Count == 0)
                {
                    _edgesByVertex.Remove(vertices.VertexAId);
                }
            }

            if (_edgesByVertex.TryGetValue(vertices.VertexBId, out var edgesB))
            {
                edgesB.Remove(edgeId);

                if (edgesB.Count == 0)
                {
                    _edgesByVertex.Remove(vertices.VertexBId);
                }
            }

            _edgeVertices.Remove(edgeId);
        }

        if (_edges.TryGetValue(edgeId, out var objectId))
        {
            _edgeIds.Remove(objectId);

            _edges.Remove(edgeId);
        }
    }

    public void Clear()
    {
        _vertices.Clear();
        _edges.Clear();

        _vertexIds.Clear();
        _edgeIds.Clear();

        _edgeVertices.Clear();
        _edgesByVertex.Clear();
    }

    public void ReplaceVertexObject(Guid vertexId, ObjectId newObjectId)
    {
        if (newObjectId.IsNull)
        {
            throw new ArgumentException("New ObjectId cannot be null.", nameof(newObjectId));
        }

        if (_vertices.TryGetValue(vertexId, out var oldObjectId))
        {
            if (oldObjectId == newObjectId)
            {
                return;
            }

            _vertexIds.Remove(oldObjectId);
        }

        if (_vertexIds.TryGetValue(newObjectId, out var existingVertexId) && existingVertexId != vertexId)
        {
            throw new InvalidOperationException(
                $"Object {newObjectId} is already " + $"registered as vertex {existingVertexId}."
            );
        }

        _vertices[vertexId] = newObjectId;

        _vertexIds[newObjectId] = vertexId;
    }

    public IReadOnlyCollection<Guid> GetIncidentEdgeIds(Guid vertexId)
    {
        if (!_edgesByVertex.TryGetValue(vertexId, out var edges))
        {
            return Array.Empty<Guid>();
        }

        return edges.ToArray();
    }

    public IReadOnlyCollection<Guid> GetEdgeIds()
    {
        return _edges.Keys.ToArray();
    }

    public IReadOnlyCollection<Guid> GetVertexIds()
    {
        return _vertices.Keys.ToArray();
    }

    public IReadOnlyCollection<ObjectId> GetEdgeObjectIds()
    {
        return _edges.Values.ToArray();
    }

    private void AddIncidentEdge(Guid vertexId, Guid edgeId)
    {
        if (!_edgesByVertex.TryGetValue(vertexId, out var edges))
        {
            edges = new HashSet<Guid>();

            _edgesByVertex[vertexId] = edges;
        }

        edges.Add(edgeId);
    }
}
