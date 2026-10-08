namespace GraphPlugin.Domain.Models;

public sealed record VertexAttachment
{
    public string Path { get; }

    public VertexAttachment(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Attachment path cannot be empty.", nameof(path));
        }

        Path = path;
    }
}
