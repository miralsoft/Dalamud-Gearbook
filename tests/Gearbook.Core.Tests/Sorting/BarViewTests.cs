using Gearbook.Core.Identity;
using Gearbook.Core.Model;
using Gearbook.Core.Sorting;
using Xunit;

namespace Gearbook.Core.Tests.Sorting;

public class BarViewTests
{
    private static IReadOnlyList<ReconciledGearset> Sample() =>
    [
        TestData.Entry(1, 1, TestData.DarkKnightId, "Tank set", favourite: true, barPosition: 0),
        TestData.Entry(2, 2, TestData.WhiteMageId, "Healer set"),
        TestData.Entry(3, 3, TestData.CulinarianId, "Cooking", tags: ["glam"]),
        TestData.Entry(4, 4, TestData.BotanistId, "Gathering"),
        TestData.Entry(5, 5, TestData.DarkKnightId, "Glamour set", tags: ["Glam", "fancy"]),
    ];

    private static IReadOnlyList<int> Ids(
        BarViewKind kind,
        JobRole role = JobRole.Tank,
        JobCategory category = JobCategory.Combat,
        string? tag = null) =>
        [.. BarView.Select(Sample(), kind, role, category, tag, TestData.Jobs).Select(g => g.Record.Id)];

    [Fact]
    public void The_favourites_view_holds_only_what_is_marked()
    {
        Assert.Equal([1], Ids(BarViewKind.Favourites));
    }

    [Fact]
    public void The_everything_view_holds_everything_with_the_favourites_first()
    {
        Assert.Equal([1, 2, 3, 4, 5], Ids(BarViewKind.All));
    }

    [Fact]
    public void A_role_view_holds_that_role_whether_or_not_it_is_a_favourite()
    {
        Assert.Equal([1, 5], Ids(BarViewKind.Role, role: JobRole.Tank));
        Assert.Equal([4], Ids(BarViewKind.Role, role: JobRole.Gatherer));
    }

    [Fact]
    public void A_category_view_holds_that_category()
    {
        Assert.Equal([3], Ids(BarViewKind.Category, category: JobCategory.Crafting));
        Assert.Equal([1, 2, 5], Ids(BarViewKind.Category, category: JobCategory.Combat));
    }

    [Fact]
    public void A_tag_view_holds_everything_carrying_that_tag_however_it_was_spelled()
    {
        // The same comparison the library's tag filter uses. Two ideas of what counts as the
        // same tag would be a bug waiting for the first player who capitalises inconsistently.
        Assert.Equal([3, 5], Ids(BarViewKind.Tag, tag: "glam"));
        Assert.Equal([3, 5], Ids(BarViewKind.Tag, tag: "  GLAM "));
    }

    [Fact]
    public void A_tag_nothing_carries_shows_an_empty_bar_rather_than_something_else()
    {
        // Falling back to another view would leave the bar showing something other than what its
        // own switcher says, which is worse than showing nothing.
        Assert.Empty(Ids(BarViewKind.Tag, tag: "raid"));
        Assert.Empty(Ids(BarViewKind.Tag, tag: null));
        Assert.Empty(Ids(BarViewKind.Tag, tag: "  "));
    }

    [Fact]
    public void Only_the_favourites_view_keeps_the_arrangement()
    {
        // Everywhere else the bar holds gearsets that were never arranged, so the arrangement
        // would order a handful of them and leave the rest in an arbitrary tail.
        Assert.True(BarView.UsesArrangement(BarViewKind.Favourites));

        Assert.False(BarView.UsesArrangement(BarViewKind.All));
        Assert.False(BarView.UsesArrangement(BarViewKind.Role));
        Assert.False(BarView.UsesArrangement(BarViewKind.Category));
        Assert.False(BarView.UsesArrangement(BarViewKind.Tag));
    }
}
