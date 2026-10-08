using GraphPlugin.Application.Services;

namespace GraphPlugin.Tests.Domain;

public class VertexAttachmentTests
{
    [Fact]
    public void ToStoredPath_FileInsideDrawingDirectory_ReturnsRelativePath()
    {
        var resolver = new AttachmentPathResolver();

        var result = resolver.ToStoredPath(@"C:\Project\graph.dwg", @"C:\Project\Documents\report.pdf");

        Assert.Equal(Path.Combine("Documents", "report.pdf"), result);
    }

    [Fact]
    public void ToStoredPath_FileOutsideDrawingDirectory_ReturnsAbsolutePath()
    {
        var resolver = new AttachmentPathResolver();

        var result = resolver.ToStoredPath(@"C:\Project\graph.dwg", @"D:\Archive\report.pdf");

        Assert.True(Path.IsPathRooted(result));

        Assert.Equal(Path.GetFullPath(@"D:\Archive\report.pdf"), result);
    }

    [Fact]
    public void ToStoredPath_UnsavedDrawing_ReturnsAbsolutePath()
    {
        var resolver = new AttachmentPathResolver();

        var result = resolver.ToStoredPath(null, @"C:\Files\report.pdf");

        Assert.Equal(Path.GetFullPath(@"C:\Files\report.pdf"), result);
    }
}
