using GraphPlugin.Domain.Models;
using Teigha.Colors;

namespace GraphPlugin.NanoCad.Drawing;

internal static class CadColorMapper
{
    public static Color ToCadColor(GraphColor color)
    {
        short colorIndex = color switch
        {
            GraphColor.Red => 1,
            GraphColor.Green => 3,
            GraphColor.Blue => 5,
            GraphColor.White => 7,
            GraphColor.Black => 7,

            _ => 7
        };

        return Color.FromColorIndex(
            ColorMethod.ByAci,
            colorIndex);
    }
}