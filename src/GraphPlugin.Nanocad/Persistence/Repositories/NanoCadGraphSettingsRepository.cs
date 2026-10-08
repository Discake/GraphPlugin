using GraphPlugin.Application.Abstractions.Persistence;
using GraphPlugin.Domain.Models;
using GraphPlugin.NanoCad.Persistence.Metadata;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Persistence;

public sealed class NanoCadGraphSettingsRepository
    : IGraphSettingsRepository
{
    private readonly Document _document;

    public NanoCadGraphSettingsRepository(
        Document document)
    {
        _document =
            document ??
            throw new ArgumentNullException(
                nameof(document));
    }

    public GraphSettings Load()
    {
        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var dictionary =
            (DBDictionary)transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForRead);

        if (!dictionary.Contains(
                GraphMetadataKeys.SettingsRecord))
        {
            return GraphSettings.Default;
        }

        var recordId =
            dictionary.GetAt(
                GraphMetadataKeys.SettingsRecord);

        var record =
            (Xrecord)transaction.GetObject(
                recordId,
                OpenMode.ForRead);

        if (record.Data is null)
            return GraphSettings.Default;

        var values =
            record.Data.AsArray();

        if (values.Length < 4)
            return GraphSettings.Default;

        var version =
            Convert.ToInt32(
                values[0].Value);

        if (version !=
            GraphMetadataKeys.CurrentSettingsVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported {GraphMetadataKeys.SettingsRecord} " +
                $"version: {version}. Supported version: " +
                $"{GraphMetadataKeys.CurrentSettingsVersion}.");
        }

        var color =
            (GraphColor)Convert.ToInt32(
                values[1].Value);

        var lineType =
            (EdgeLineType)Convert.ToInt32(
                values[2].Value);

        double lineWeight =
            Convert.ToDouble(
                values[3].Value);

        transaction.Commit();

        return new GraphSettings(
            new EdgeStyle(
                color,
                lineType,
                lineWeight));
    }

    public void Save(GraphSettings settings)
    {
        var database =
            _document.Database;

        using var transaction =
            database.TransactionManager
                .StartTransaction();

        var dictionary =
            (DBDictionary)transaction.GetObject(
                database.NamedObjectsDictionaryId,
                OpenMode.ForWrite);

        var data =
            new ResultBuffer(
                new TypedValue(
                    (int)DxfCode.Int32,
                    GraphMetadataKeys.CurrentSettingsVersion),

                new TypedValue(
                    (int)DxfCode.Int32,
                    (int)settings.EdgeStyle.Color),

                new TypedValue(
                    (int)DxfCode.Int32,
                    (int)settings.EdgeStyle.LineType),

                new TypedValue(
                    (int)DxfCode.Real,
                    settings.EdgeStyle.LineWeightMm));

        if (dictionary.Contains(
                GraphMetadataKeys.SettingsRecord))
        {
            var record =
                (Xrecord)transaction.GetObject(
                    dictionary.GetAt(
                        GraphMetadataKeys.SettingsRecord),
                    OpenMode.ForWrite);

            record.Data = data;
        }
        else
        {
            var record =
                new Xrecord
                {
                    Data = data
                };

            dictionary.SetAt(
                GraphMetadataKeys.SettingsRecord,
                record);

            transaction.AddNewlyCreatedDBObject(
                record,
                true);
        }

        transaction.Commit();
    }
}
