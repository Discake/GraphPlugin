using GraphPlugin.Domain.Models;

using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Drawing;

public sealed class LinetypeManager
{
    private const string DashedName =
        "GRAPH_DASHED";

    private const string DottedName =
        "GRAPH_DOTTED";

    public ObjectId GetOrCreate(
        EdgeLineType lineType,
        Database database,
        Transaction transaction)
    {
        return lineType switch
        {
            EdgeLineType.Continuous =>
                GetExisting(
                    "Continuous",
                    database,
                    transaction),

            EdgeLineType.Dashed =>
                GetOrCreateDashed(
                    database,
                    transaction),

            EdgeLineType.Dotted =>
                GetOrCreateDotted(
                    database,
                    transaction),

            _ => throw new NotSupportedException(
                $"Unsupported line type: {lineType}")
        };
    }

    private static ObjectId GetExisting(
        string name,
        Database database,
        Transaction transaction)
    {
        var table =
            (LinetypeTable)transaction.GetObject(
                database.LinetypeTableId,
                OpenMode.ForRead);

        if (!table.Has(name))
        {
            throw new InvalidOperationException(
                $"Linetype '{name}' was not found.");
        }

        return table[name];
    }

    private static ObjectId GetOrCreateDashed(
        Database database,
        Transaction transaction)
    {
        return GetOrCreatePattern(
            database,
            transaction,
            DashedName,
            "Graph dashed ---- ---- ----",
            dashLength: 6.0,
            gapLength: 3.0);
    }

    private static ObjectId GetOrCreateDotted(
        Database database,
        Transaction transaction)
    {
        return GetOrCreatePattern(
            database,
            transaction,
            DottedName,
            "Graph dotted . . . . .",
            dashLength: 0.0,
            gapLength: 2.0);
    }

    private static ObjectId GetOrCreatePattern(
        Database database,
        Transaction transaction,
        string name,
        string description,
        double dashLength,
        double gapLength)
    {
        var table =
            (LinetypeTable)transaction.GetObject(
                database.LinetypeTableId,
                OpenMode.ForRead);

        if (table.Has(name))
        {
            return table[name];
        }

        table.UpgradeOpen();

        var record =
            new LinetypeTableRecord
            {
                Name = name,
                Comments = description,
                PatternLength =
                    dashLength + gapLength,
                NumDashes = 2
            };

        record.SetDashLengthAt(
            0,
            dashLength);

        record.SetDashLengthAt(
            1,
            -gapLength);

        var id =
            table.Add(record);

        transaction.AddNewlyCreatedDBObject(
            record,
            true);

        return id;
    }
}