using GraphPlugin.Domain.Models;
using GraphPlugin.Nanocad.Persistence.Metadata;
using Teigha.DatabaseServices;

namespace GraphPlugin.Nanocad.Persistence;

public sealed class VertexAttachmentXRecordStore
{
    public const string RecordKey =
        GraphMetadataKeys.VertexAttachmentsRecord;

    private const int CurrentVersion =
        GraphMetadataKeys.CurrentVertexAttachmentsVersion;

    public IReadOnlyCollection<VertexAttachment> Read(
        Entity entity,
        Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        ArgumentNullException.ThrowIfNull(
            transaction);

        if (entity.ExtensionDictionary.IsNull)
        {
            return Array.Empty<VertexAttachment>();
        }

        var dictionary =
            transaction.GetObject(
                entity.ExtensionDictionary,
                OpenMode.ForRead)
            as DBDictionary
            ?? throw new InvalidOperationException(
                "Entity extension dictionary could not be opened.");

        if (!dictionary.Contains(
                RecordKey))
        {
            return Array.Empty<VertexAttachment>();
        }

        var xRecordId =
            dictionary.GetAt(
                RecordKey);

        var xRecord =
            transaction.GetObject(
                xRecordId,
                OpenMode.ForRead)
            as Xrecord
            ?? throw new InvalidOperationException(
                $"'{RecordKey}' is not an XRecord.");

        using var data =
            xRecord.Data;

        if (data is null)
        {
            return Array.Empty<VertexAttachment>();
        }

        var values =
            data.AsArray();

        if (values.Length < 2)
        {
            throw new InvalidOperationException(
                $"'{RecordKey}' contains invalid data.");
        }

        var version =
            ReadInt32(
                values[0],
                "Version");

        if (version != CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported attachment metadata version: " +
                $"{version}.");
        }

        var count =
            ReadInt32(
                values[1],
                "Count");

        if (count < 0)
        {
            throw new InvalidOperationException(
                "Attachment count cannot be negative.");
        }

        if (values.Length !=
            count + 2)
        {
            throw new InvalidOperationException(
                $"Attachment XRecord is inconsistent. " +
                $"Count is {count}, but " +
                $"{values.Length - 2} paths are stored.");
        }

        if (count == 0)
        {
            return Array.Empty<VertexAttachment>();
        }

        var result =
            new List<VertexAttachment>(
                count);

        for (var i = 0;
             i < count;
             i++)
        {
            var path =
                ReadString(
                    values[i + 2],
                    $"Path[{i}]");

            result.Add(
                new VertexAttachment(
                    path));
        }

        return result;
    }

    public void Write(
        Entity entity,
        Transaction transaction,
        IReadOnlyCollection<VertexAttachment> attachments)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        ArgumentNullException.ThrowIfNull(
            transaction);

        ArgumentNullException.ThrowIfNull(
            attachments);

        //
        // Repository должен передавать entity,
        // открытую ForWrite.
        //
        if (entity.ExtensionDictionary.IsNull)
        {
            entity.CreateExtensionDictionary();
        }

        var dictionary =
            transaction.GetObject(
                entity.ExtensionDictionary,
                OpenMode.ForWrite)
            as DBDictionary
            ?? throw new InvalidOperationException(
                "Entity extension dictionary could not be opened.");

        var values =
            new List<TypedValue>(
                attachments.Count + 2)
            {
                new TypedValue(
                    (int)DxfCode.Int32,
                    CurrentVersion),

                new TypedValue(
                    (int)DxfCode.Int32,
                    attachments.Count)
            };

        foreach (var attachment in
                 attachments)
        {
            values.Add(
                new TypedValue(
                    (int)DxfCode.Text,
                    attachment.Path));
        }

        using var buffer =
            new ResultBuffer(
                values.ToArray());

        Xrecord xRecord;

        if (dictionary.Contains(
                RecordKey))
        {
            var xRecordId =
                dictionary.GetAt(
                    RecordKey);

            xRecord =
                transaction.GetObject(
                    xRecordId,
                    OpenMode.ForWrite)
                as Xrecord
                ?? throw new InvalidOperationException(
                    $"'{RecordKey}' is not an XRecord.");
        }
        else
        {
            xRecord =
                new Xrecord();

            dictionary.SetAt(
                RecordKey,
                xRecord);

            transaction
                .AddNewlyCreatedDBObject(
                    xRecord,
                    true);
        }

        xRecord.Data =
            buffer;
    }

    private static int ReadInt32(
        TypedValue value,
        string fieldName)
    {
        if (value.TypeCode !=
            (int)DxfCode.Int32)
        {
            throw new InvalidOperationException(
                $"Attachment field '{fieldName}' " +
                $"has invalid DXF type.");
        }

        return Convert.ToInt32(
            value.Value);
    }

    private static string ReadString(
        TypedValue value,
        string fieldName)
    {
        if (value.TypeCode !=
            (int)DxfCode.Text)
        {
            throw new InvalidOperationException(
                $"Attachment field '{fieldName}' " +
                $"has invalid DXF type.");
        }

        if (value.Value is not string text ||
            string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"Attachment field '{fieldName}' " +
                $"contains an invalid path.");
        }

        return text;
    }
}