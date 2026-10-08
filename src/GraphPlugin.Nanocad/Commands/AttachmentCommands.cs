using System.Diagnostics;
using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Bootstrap;
using GraphPlugin.Nanocad.Runtime;
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.Runtime;

namespace GraphPlugin.Nanocad.Commands;

public sealed class AttachmentCommands
{
    [CommandMethod("GRAPHATTACHFILE")]
    public void GraphAttachFile()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        try
        {
            var vertex = SelectVertex(document, context, "\nSelect vertex to attach file: ");

            if (vertex is null)
                return;

            var fileOptions = new PromptOpenFileOptions("\nSelect file to attach");

            var fileResult = editor.GetFileNameForOpen(fileOptions);

            if (fileResult.Status != PromptStatus.OK)
                return;

            var attachment = context.AttachmentService.Attach(
                vertex.Id,
                fileResult.StringResult,
                GetDrawingPath(document)
            );

            editor.WriteMessage("\nFile attached.");

            editor.WriteMessage($"\nStored path: " + $"{attachment.Path}");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\nCannot attach file: " + $"{exception.Message}");
        }
    }

    [CommandMethod("GRAPHVERTEXFILES")]
    public void GraphVertexFiles()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        try
        {
            var vertex = SelectVertex(document, context, "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments = context.AttachmentService.GetAll(vertex.Id).ToArray();

            editor.WriteMessage($"\nVertex: {vertex.Id}");

            PrintAttachments(document, attachments);
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\nCannot read attached files: " + $"{exception.Message}");
        }
    }

    [CommandMethod("GRAPHOPENFILE")]
    public void GraphOpenFile()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        try
        {
            var vertex = SelectVertex(document, context, "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments = context.AttachmentService.GetAll(vertex.Id).ToArray();

            var attachment = ChooseAttachment(document, attachments, "\nEnter file number to open: ");

            if (attachment is null)
                return;

            var path = context.AttachmentService.ResolvePath(vertex.Id, attachment.Path, GetDrawingPath(document));

            if (!File.Exists(path))
            {
                editor.WriteMessage("\nAttached file was not found.");

                editor.WriteMessage($"\nResolved path: {path}");

                return;
            }

            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

            editor.WriteMessage($"\nOpened: {path}");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\nCannot open attached file: " + $"{exception.Message}");
        }
    }

    [CommandMethod("GRAPHDETACHFILE")]
    public void GraphDetachFile()
    {
        var context = PluginServices.CurrentContext;

        var document = context.Document;

        var editor = document.Editor;

        try
        {
            var vertex = SelectVertex(document, context, "\nSelect vertex: ");

            if (vertex is null)
                return;

            var attachments = context.AttachmentService.GetAll(vertex.Id).ToArray();

            var attachment = ChooseAttachment(document, attachments, "\nEnter file number to detach: ");

            if (attachment is null)
                return;

            context.AttachmentService.Detach(vertex.Id, attachment.Path);

            editor.WriteMessage("\nFile detached from vertex.");

            editor.WriteMessage($"\nRemoved reference: " + $"{attachment.Path}");
        }
        catch (System.Exception exception)
        {
            editor.WriteMessage($"\nCannot detach file: " + $"{exception.Message}");
        }
    }

    private static GraphVertex? SelectVertex(Document document, GraphDocumentContext context, string message)
    {
        var editor = document.Editor;

        while (true)
        {
            var result = editor.GetEntity(new PromptEntityOptions(message));

            if (result.Status != PromptStatus.OK)
                return null;

            if (!context.Index.TryGetVertexId(result.ObjectId, out var vertexId))
            {
                editor.WriteMessage("\nSelected object is not a graph vertex.");

                continue;
            }

            var vertex = context.Vertices.Get(vertexId);

            if (vertex is null)
            {
                editor.WriteMessage("\nGraph vertex could not be restored.");

                continue;
            }

            return vertex;
        }
    }

    private static string? GetDrawingPath(Document document)
    {
        var filename = document.Database.Filename;

        if (string.IsNullOrWhiteSpace(filename))
            return null;

        try
        {
            return Path.GetFullPath(filename);
        }
        catch
        {
            return null;
        }
    }

    private static void PrintAttachments(Document document, IReadOnlyList<VertexAttachment> attachments)
    {
        var editor = document.Editor;

        if (attachments.Count == 0)
        {
            editor.WriteMessage("\nVertex has no attached files.");

            return;
        }

        editor.WriteMessage($"\nAttached files: {attachments.Count}");

        for (var i = 0; i < attachments.Count; i++)
        {
            editor.WriteMessage($"\n  {i + 1}. " + $"{attachments[i].Path}");
        }
    }

    private static VertexAttachment? ChooseAttachment(
        Document document,
        IReadOnlyList<VertexAttachment> attachments,
        string prompt
    )
    {
        if (attachments.Count == 0)
        {
            document.Editor.WriteMessage("\nVertex has no attached files.");

            return null;
        }

        if (attachments.Count == 1)
            return attachments[0];

        PrintAttachments(document, attachments);

        var options = new PromptIntegerOptions(prompt) { LowerLimit = 1, UpperLimit = attachments.Count };

        var result = document.Editor.GetInteger(options);

        if (result.Status != PromptStatus.OK)
            return null;

        return attachments[result.Value - 1];
    }
}
