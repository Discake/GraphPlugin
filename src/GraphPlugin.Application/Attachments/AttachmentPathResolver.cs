namespace GraphPlugin.Application.Attachments;

public sealed class AttachmentPathResolver
{
    public string ToStoredPath(string? drawingPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        var fullFilePath = Path.GetFullPath(filePath);

        if (string.IsNullOrWhiteSpace(drawingPath))
        {
            return fullFilePath;
        }

        var fullDrawingPath = Path.GetFullPath(drawingPath);

        var drawingDirectory = Path.GetDirectoryName(fullDrawingPath);

        if (string.IsNullOrWhiteSpace(drawingDirectory))
        {
            return fullFilePath;
        }

        var relativePath = Path.GetRelativePath(drawingDirectory, fullFilePath);

        if (IsSafeRelativePath(relativePath))
        {
            return relativePath;
        }

        return fullFilePath;
    }

    public string ResolvePath(string? drawingPath, string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Stored path cannot be empty.", nameof(storedPath));
        }

        if (Path.IsPathRooted(storedPath))
        {
            return Path.GetFullPath(storedPath);
        }

        if (!HasDrawingDirectory(drawingPath))
        {
            throw new InvalidOperationException(
                "Cannot resolve a relative attachment path " + "because the drawing has no file location."
            );
        }

        var drawingDirectory = Path.GetDirectoryName(Path.GetFullPath(drawingPath!))!;

        return Path.GetFullPath(Path.Combine(drawingDirectory, storedPath));
    }

    private static bool HasDrawingDirectory(string? drawingPath)
    {
        if (string.IsNullOrWhiteSpace(drawingPath))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(Path.GetDirectoryName(drawingPath));
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathRooted(path))
        {
            return false;
        }

        if (path.Equals("..", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
