namespace Gearbook.Core.Sorting;

/// <summary>
/// How many tiles the bar puts in a row once the screen has had its say.
/// </summary>
/// <remarks>
/// The player sets a number of icons per row, and that number is a wish rather than an
/// instruction. One icon per row turns the bar into a column, and ninety gearsets in a column
/// reach past the bottom of the screen; a hundred per row does the same off the right-hand edge.
/// Either way the window grows larger than the game it sits in, and what falls outside cannot be
/// clicked, scrolled to or dragged back, because a locked bar has no title bar to drag.
///
/// So the wish is honoured as far as it fits and then bent: too wide is cut to what the screen
/// holds, and too tall is widened until the height fits. Nothing is ever dropped. A bar that
/// silently stopped showing some gearsets would be worse than one that looks different from what
/// was asked for, because the missing ones are invisible and the changed shape is not.
/// </remarks>
public static class BarLayout
{
    /// <summary>
    /// The number of tiles per row to actually draw.
    /// </summary>
    /// <param name="tiles">How many tiles there are, the view switcher included.</param>
    /// <param name="wanted">The icons per row the player asked for.</param>
    /// <param name="cellWidth">One tile's width, the spacing after it included.</param>
    /// <param name="cellHeight">One tile's height, the spacing under it included.</param>
    /// <param name="availableWidth">The width the window may occupy.</param>
    /// <param name="availableHeight">The height the window may occupy.</param>
    /// <remarks>
    /// Counting with the spacing folded into the cell means the last tile in a row is measured as
    /// though it were followed by a gap it does not have. That errs towards wrapping one tile
    /// early, which costs a few pixels of an edge nobody was using, where the opposite error
    /// costs a tile that hangs off the screen.
    /// </remarks>
    public static int Columns(
        int tiles,
        int wanted,
        float cellWidth,
        float cellHeight,
        float availableWidth,
        float availableHeight)
    {
        var columns = Math.Max(1, wanted);

        // A caller that cannot measure yet, which is every caller on the first frame. Answering
        // with the wish is right: one frame at the wrong width is invisible, and inventing a
        // limit out of a zero would collapse the bar to a single column.
        if (tiles <= 0 || cellWidth <= 0f || cellHeight <= 0f || availableWidth <= 0f || availableHeight <= 0f)
        {
            return columns;
        }

        var fitAcross = Math.Max(1, (int)(availableWidth / cellWidth));
        var fitDown = Math.Max(1, (int)(availableHeight / cellHeight));

        columns = Math.Min(columns, fitAcross);

        if (RowsFor(tiles, columns) > fitDown)
        {
            // Too tall. Widen it to the narrowest arrangement whose height fits, and no further,
            // so a bar that only just overflowed does not jump to the full width of the screen.
            columns = Math.Min(fitAcross, RowsFor(tiles, fitDown));
        }

        return Math.Max(1, columns);
    }

    /// <summary>How many rows a number of tiles needs at a given width.</summary>
    public static int RowsFor(int tiles, int columns) =>
        columns <= 0 ? tiles : (tiles + columns - 1) / columns;
}
