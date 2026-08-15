using Gearbook.Core.Editing;
using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Editing;

/// <summary>
/// A mistake in a single edit costs one gearset. The same mistake here costs forty, which is why
/// these rules live in a tested function rather than in the window that offers them.
/// </summary>
public class BulkEditTests
{
    private static IReadOnlyList<GearsetRecord> Records() =>
    [
        TestData.Record(1, TestData.WhiteMageId, "A", slot: 1, tags: ["raid"]),
        TestData.Record(2, TestData.DarkKnightId, "B", slot: 2, tags: ["RAID", "glam"]),
        TestData.Record(3, TestData.BotanistId, "C", slot: 3),
    ];

    private static HashSet<int> Selection(params int[] ids) => [.. ids];

    [Fact]
    public void Adding_a_tag_reaches_only_the_selection()
    {
        var updated = BulkEdit.AddTag(Records(), Selection(1, 3), "alt");

        Assert.Contains("alt", updated[0].Tags);
        Assert.DoesNotContain("alt", updated[1].Tags);
        Assert.Contains("alt", updated[2].Tags);
    }

    [Fact]
    public void Adding_a_tag_a_gearset_already_carries_does_not_duplicate_it()
    {
        var updated = BulkEdit.AddTag(Records(), Selection(1, 2), "raid");

        // The second record spells it in capitals, and the filter treats the two as one tag, so
        // adding it again must not leave the set carrying both spellings.
        Assert.Single(updated[0].Tags);
        Assert.Equal(2, updated[1].Tags.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void An_empty_tag_changes_nothing(string? tag)
    {
        var original = Records();

        Assert.Same(original, BulkEdit.AddTag(original, Selection(1, 2, 3), tag!));
        Assert.Same(original, BulkEdit.RemoveTag(original, Selection(1, 2, 3), tag!));
    }

    [Fact]
    public void Removing_a_tag_ignores_how_it_was_spelled()
    {
        var updated = BulkEdit.RemoveTag(Records(), Selection(1, 2), "raid");

        Assert.Empty(updated[0].Tags);
        Assert.Equal(["glam"], updated[1].Tags);
    }

    [Fact]
    public void Removing_a_tag_nothing_carries_changes_nothing()
    {
        var original = Records();
        var updated = BulkEdit.RemoveTag(original, Selection(3), "raid");

        Assert.Equal(original, updated);
    }

    [Fact]
    public void Marking_a_selection_as_favourite_gives_them_contiguous_bar_positions()
    {
        var updated = BulkEdit.SetFavourite(Records(), Selection(1, 2, 3), favourite: true);

        Assert.All(updated, r => Assert.True(r.IsFavourite));
        Assert.Equal([0, 1, 2], updated.OrderBy(r => r.BarPosition).Select(r => r.BarPosition));
    }

    [Fact]
    public void Unmarking_a_selection_closes_the_gaps_it_leaves()
    {
        var favourites = BulkEdit.SetFavourite(Records(), Selection(1, 2, 3), favourite: true);

        var updated = BulkEdit.SetFavourite(favourites, Selection(2), favourite: false);

        Assert.False(updated.Single(r => r.Id == 2).IsFavourite);
        Assert.Null(updated.Single(r => r.Id == 2).BarPosition);
        Assert.Equal([0, 1], updated.Where(r => r.IsFavourite).OrderBy(r => r.BarPosition).Select(r => r.BarPosition));
    }

    [Fact]
    public void An_empty_selection_leaves_everything_alone()
    {
        var original = Records();

        Assert.Equal(original, BulkEdit.AddTag(original, Selection(), "alt"));
        Assert.Equal(original, BulkEdit.SetFavourite(original, Selection(), favourite: true));
    }

    [Fact]
    public void The_tags_in_a_selection_are_deduplicated_and_sorted()
    {
        Assert.Equal(["glam", "raid"], BulkEdit.TagsInSelection(Records(), Selection(1, 2)));
        Assert.Empty(BulkEdit.TagsInSelection(Records(), Selection(3)));
    }
}
