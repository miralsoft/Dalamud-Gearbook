using Gearbook.Core.Model;
using Gearbook.Core.Sorting;
using Xunit;

namespace Gearbook.Core.Tests.Sorting;

public class BarOrderTests
{
    private static IReadOnlyList<GearsetRecord> Records() =>
    [
        TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, favourite: true, barPosition: 0),
        TestData.Record(2, TestData.DarkKnightId, "B", slot: 2, favourite: true, barPosition: 1),
        TestData.Record(3, TestData.BotanistId, "C", slot: 3, favourite: true, barPosition: 2),
        TestData.Record(4, TestData.CulinarianId, "D", slot: 4),
    ];

    private static IReadOnlyList<int> BarIds(IReadOnlyList<GearsetRecord> records) =>
        [.. records
            .Where(r => r.BarPosition is not null)
            .OrderBy(r => r.BarPosition!.Value)
            .Select(r => r.Id)];

    [Fact]
    public void Adding_to_the_bar_puts_it_at_the_end()
    {
        var updated = BarOrder.SetFavourite(Records(), recordId: 4, favourite: true);

        Assert.Equal([1, 2, 3, 4], BarIds(updated));
    }

    [Fact]
    public void Adding_something_already_on_the_bar_does_not_move_it()
    {
        var updated = BarOrder.SetFavourite(Records(), recordId: 1, favourite: true);

        Assert.Equal([1, 2, 3], BarIds(updated));
    }

    [Fact]
    public void Removing_from_the_bar_closes_the_gap_it_leaves()
    {
        var updated = BarOrder.SetFavourite(Records(), recordId: 2, favourite: false);

        Assert.Equal([1, 3], BarIds(updated));
        Assert.Equal([0, 1], updated.Where(r => r.BarPosition is not null)
            .OrderBy(r => r.BarPosition!.Value)
            .Select(r => r.BarPosition!.Value));
    }

    [Fact]
    public void Normalising_removes_gaps_and_duplicate_positions_while_keeping_the_order()
    {
        // What a configuration restored from an older version can look like.
        IReadOnlyList<GearsetRecord> messy =
        [
            TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, favourite: true, barPosition: 5),
            TestData.Record(2, TestData.DarkKnightId, "B", slot: 2, favourite: true, barPosition: 5),
            TestData.Record(3, TestData.BotanistId, "C", slot: 3, favourite: true, barPosition: 40),
        ];

        var normalised = BarOrder.Normalise(messy);

        Assert.Equal([0, 1, 2], normalised
            .Where(r => r.BarPosition is not null)
            .OrderBy(r => r.BarPosition!.Value)
            .Select(r => r.BarPosition!.Value));

        Assert.Equal([1, 2, 3], BarIds(normalised));
    }

    [Fact]
    public void Moving_an_entry_reorders_the_bar()
    {
        var updated = BarOrder.Move(Records(), recordId: 3, delta: -1);

        Assert.Equal([1, 3, 2], BarIds(updated));
    }

    [Fact]
    public void Moving_past_an_end_stops_there_rather_than_wrapping_round()
    {
        // Dragging the first entry up and having it reappear at the far end is a surprise, and
        // a control that surprises somebody once gets used carefully forever afterwards.
        var up = BarOrder.Move(Records(), recordId: 1, delta: -5);
        var down = BarOrder.Move(Records(), recordId: 3, delta: 5);

        Assert.Equal([1, 2, 3], BarIds(up));
        Assert.Equal([1, 2, 3], BarIds(down));
    }

    [Fact]
    public void Moving_something_that_is_not_on_the_bar_changes_nothing()
    {
        var updated = BarOrder.Move(Records(), recordId: 4, delta: -1);

        Assert.Equal([1, 2, 3], BarIds(updated));
    }

    [Fact]
    public void Only_favourites_reach_the_bar()
    {
        // The merged rule: a favourite is what the bar shows. A record carrying a stale position
        // without the mark, which is what an interrupted edit looks like, does not sneak on.
        var entries = new[]
        {
            TestData.Entry(1, 1, TestData.WhiteMageId, "A", favourite: true, barPosition: 0),
            TestData.Entry(2, 2, TestData.DarkKnightId, "B", favourite: false, barPosition: 1),
        };

        Assert.Equal([1], BarOrder.OnBar(entries).Select(g => g.Record.Id));
    }

    [Fact]
    public void A_favourite_with_no_position_yet_joins_at_the_end()
    {
        // What a set marked from the context menu looks like the instant before normalising.
        IReadOnlyList<GearsetRecord> records =
        [
            TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, favourite: true, barPosition: 0),
            TestData.Record(2, TestData.DarkKnightId, "B", slot: 2, favourite: true),
        ];

        Assert.Equal([1, 2], BarIds(BarOrder.Normalise(records)));
    }

    [Fact]
    public void The_bar_reads_in_position_order_regardless_of_the_game_order()
    {
        var entries = new[]
        {
            TestData.Entry(1, 9, TestData.WhiteMageId, "A", favourite: true, barPosition: 2),
            TestData.Entry(2, 3, TestData.DarkKnightId, "B", favourite: true, barPosition: 0),
            TestData.Entry(3, 5, TestData.BotanistId, "C", favourite: true, barPosition: 1),
            TestData.Entry(4, 1, TestData.CulinarianId, "D"),
        };

        var bar = BarOrder.OnBar(entries).Select(g => g.Record.Id);

        Assert.Equal([2, 3, 1], bar);
    }
}
