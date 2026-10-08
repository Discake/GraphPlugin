using GraphPlugin.Application.Services;
using GraphPlugin.Domain.Geometry;
using GraphPlugin.Domain.Models;
using GraphPlugin.Tests.Fakes;

namespace GraphPlugin.Tests.Application;

public class VertexAttachmentServiceTests
{
    [Fact]
    public void Attach_AddsAttachmentToVertex()
    {
        var vertices = new FakeVertexRepository();

        var attachments = new FakeVertexAttachmentRepository();

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        var service = new VertexAttachmentService(vertices, attachments, new AttachmentPathResolver());

        var result = service.Attach(vertex.Id, @"C:\Project\Files\report.pdf", @"C:\Project\graph.dwg");

        Assert.Equal(Path.Combine("Files", "report.pdf"), result.Path);

        var stored = attachments.GetAll(vertex.Id);

        Assert.Single(stored);
    }

    [Fact]
    public void Attach_DuplicateAttachment_Throws()
    {
        var vertices = new FakeVertexRepository();

        var attachments = new FakeVertexAttachmentRepository();

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        var service = new VertexAttachmentService(vertices, attachments, new AttachmentPathResolver());

        service.Attach(vertex.Id, @"C:\Project\Files\report.pdf", @"C:\Project\graph.dwg");

        Assert.Throws<InvalidOperationException>(() =>
            service.Attach(vertex.Id, @"C:\PROJECT\FILES\REPORT.PDF", @"C:\Project\graph.dwg")
        );

        Assert.Single(attachments.GetAll(vertex.Id));
    }

    [Fact]
    public void Detach_RemovesOnlySelectedAttachment()
    {
        var vertices = new FakeVertexRepository();

        var attachments = new FakeVertexAttachmentRepository();

        var vertex = GraphVertex.Create(new Point2(0, 0));

        vertices.Add(vertex);

        var service = new VertexAttachmentService(vertices, attachments, new AttachmentPathResolver());

        var first = service.Attach(vertex.Id, @"C:\Project\a.pdf", @"C:\Project\graph.dwg");

        var second = service.Attach(vertex.Id, @"C:\Project\b.jpg", @"C:\Project\graph.dwg");

        service.Detach(vertex.Id, first.Path);

        var remaining = service.GetAll(vertex.Id);

        Assert.Single(remaining);

        Assert.Equal(second.Path, remaining.Single().Path);
    }

    [Fact]
    public void Attach_MissingVertex_Throws()
    {
        var service = new VertexAttachmentService(
            new FakeVertexRepository(),
            new FakeVertexAttachmentRepository(),
            new AttachmentPathResolver()
        );

        Assert.Throws<InvalidOperationException>(() =>
            service.Attach(Guid.NewGuid(), @"C:\Project\a.pdf", @"C:\Project\graph.dwg")
        );
    }
}
