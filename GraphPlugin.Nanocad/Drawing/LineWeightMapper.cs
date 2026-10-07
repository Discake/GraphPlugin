using Teigha.DatabaseServices;

namespace GraphPlugin.NanoCad.Drawing;

public static class LineWeightMapper
{
    private static readonly int[] SupportedWeights =
    {
        0,
        5,
        9,
        13,
        15,
        18,
        20,
        25,
        30,
        35,
        40,
        50,
        53,
        60,
        70,
        80,
        90,
        100,
        106,
        120,
        140,
        158,
        200,
        211
    };

    public static LineWeight ToCadLineWeight(
        double millimeters)
    {
        if (millimeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(millimeters));
        }

        int requested =
            (int)Math.Round(
                millimeters * 100.0);

        int nearest =
            SupportedWeights
                .OrderBy(value =>
                    Math.Abs(value - requested))
                .First();

        return (LineWeight)nearest;
    }
}