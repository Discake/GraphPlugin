using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Application.Services;

public sealed class VertexAttachmentService
{
    private readonly IVertexRepository _vertices;
    private readonly IVertexAttachmentRepository _attachments;
    private readonly AttachmentPathResolver _pathResolver;

    public VertexAttachmentService(
        IVertexRepository vertices,
        IVertexAttachmentRepository attachments,
        AttachmentPathResolver pathResolver)
    {
        _vertices =
            vertices ??
            throw new ArgumentNullException(
                nameof(vertices));

        _attachments =
            attachments ??
            throw new ArgumentNullException(
                nameof(attachments));

        _pathResolver =
            pathResolver ??
            throw new ArgumentNullException(
                nameof(pathResolver));
    }

    public VertexAttachment Attach(
        Guid vertexId,
        string filePath,
        string? drawingPath)
    {
        EnsureVertexExists(
            vertexId);

        var storedPath =
            _pathResolver.ToStoredPath(
                drawingPath,
                filePath);

        var existing =
            _attachments.GetAll(
                vertexId);

        if (existing.Any(
                attachment =>
                    AreSameStoredPath(
                        attachment.Path,
                        storedPath)))
        {
            throw new InvalidOperationException(
                "This file is already attached to the vertex.");
        }

        var attachment =
            new VertexAttachment(
                storedPath);

        _attachments.Add(
            vertexId,
            attachment);

        return attachment;
    }

    public IReadOnlyCollection<VertexAttachment> GetAll(
        Guid vertexId)
    {
        EnsureVertexExists(
            vertexId);

        return _attachments.GetAll(
            vertexId);
    }

    public string ResolvePath(
        Guid vertexId,
        string storedPath,
        string? drawingPath)
    {
        EnsureVertexExists(
            vertexId);

        var attachment =
            _attachments
                .GetAll(vertexId)
                .FirstOrDefault(
                    x =>
                        AreSameStoredPath(
                            x.Path,
                            storedPath));

        if (attachment is null)
        {
            throw new InvalidOperationException(
                "The attachment does not belong to this vertex.");
        }

        return _pathResolver.ResolvePath(
            drawingPath,
            attachment.Path);
    }

    public void Detach(
        Guid vertexId,
        string storedPath)
    {
        EnsureVertexExists(
            vertexId);

        var existing =
            _attachments
                .GetAll(vertexId)
                .FirstOrDefault(
                    x =>
                        AreSameStoredPath(
                            x.Path,
                            storedPath));

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The attachment does not belong to this vertex.");
        }

        _attachments.Remove(
            vertexId,
            existing.Path);
    }

    private void EnsureVertexExists(
        Guid vertexId)
    {
        if (_vertices.Get(vertexId) is null)
        {
            throw new InvalidOperationException(
                $"Vertex '{vertexId}' does not exist.");
        }
    }

    private static bool AreSameStoredPath(
        string first,
        string second)
    {
        //
        // nanoCAD у нас работает под Windows,
        // поэтому файловые пути сравниваем
        // без учёта регистра.
        //
        return string.Equals(
            Normalize(first),
            Normalize(second),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(
        string path)
    {
        return path
            .Replace(
                Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar)
            .TrimEnd(
                Path.DirectorySeparatorChar);
    }
}