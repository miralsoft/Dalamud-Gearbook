using Gearbook.Core.Sorting;
using Xunit;

namespace Gearbook.Core.Tests.Sorting;

/// <summary>
/// The bar's width against the screen's.
/// </summary>
/// <remarks>
/// The numbers here are a tile of 40 including its spacing on a work area of 1000 by 600, which
/// makes 25 tiles across and 15 down and keeps the arithmetic checkable by hand.
/// </remarks>
public sealed class BarLayoutTests
{
    private const float Cell = 40f;
    private const float Width = 1000f;
    private const float Height = 600f;

    private static int Columns(int tiles, int wanted) =>
        BarLayout.Columns(tiles, wanted, Cell, Cell, Width, Height);

    [Fact]
    public void A_request_that_fits_is_left_exactly_as_it_is()
    {
        Assert.Equal(8, Columns(tiles: 24, wanted: 8));
    }

    [Fact]
    public void A_single_column_that_fits_stays_a_single_column()
    {
        // Fifteen tiles is exactly the height of the work area, so nothing needs bending. The
        // test guards the boundary rather than the middle, which is where an off-by-one lives.
        Assert.Equal(1, Columns(tiles: 15, wanted: 1));
    }

    [Fact]
    public void A_column_taller_than_the_screen_is_widened_until_it_fits()
    {
        // Ninety in a single column would be ninety rows on a screen that holds fifteen.
        var columns = Columns(tiles: 90, wanted: 1);

        Assert.Equal(6, columns);
        Assert.True(BarLayout.RowsFor(90, columns) <= 15);
    }

    [Fact]
    public void It_widens_no_further_than_the_overflow_requires()
    {
        // Sixteen tiles overflow a fifteen-row screen by exactly one. Two columns is enough, and
        // jumping to the full width of the screen for one tile too many would be a worse answer
        // than the one that was asked for.
        Assert.Equal(2, Columns(tiles: 16, wanted: 1));
    }

    [Fact]
    public void A_row_wider_than_the_screen_is_cut_to_what_the_screen_holds()
    {
        Assert.Equal(25, Columns(tiles: 90, wanted: 100));
    }

    [Fact]
    public void Nothing_is_ever_dropped_when_it_cannot_all_fit()
    {
        // A screen holding 25 by 15 holds 375 tiles. Four hundred cannot fit however it is
        // arranged, and the answer is the widest the screen allows rather than a smaller set of
        // gearsets. The tiles that fall off are visibly missing; a filtered-out gearset is not.
        var columns = Columns(tiles: 400, wanted: 1);

        Assert.Equal(25, columns);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void An_impossible_request_becomes_one_per_row_rather_than_a_division_by_zero(int wanted)
    {
        Assert.Equal(1, Columns(tiles: 10, wanted: wanted));
    }

    [Fact]
    public void Before_anything_has_been_measured_the_wish_is_honoured_untouched()
    {
        // Every caller's first frame: the host has no size to report yet. One frame at the wrong
        // width is invisible, while inventing a limit out of a zero would collapse the bar to a
        // single column and then leave it there.
        Assert.Equal(7, BarLayout.Columns(90, 7, 0f, 0f, 0f, 0f));
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(1, 5, 1)]
    [InlineData(5, 5, 1)]
    [InlineData(6, 5, 2)]
    [InlineData(10, 5, 2)]
    [InlineData(11, 5, 3)]
    public void Rows_are_counted_by_rounding_up(int tiles, int columns, int expected)
    {
        Assert.Equal(expected, BarLayout.RowsFor(tiles, columns));
    }
}
