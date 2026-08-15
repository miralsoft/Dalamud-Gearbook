using Gearbook.Core.Filtering;
using Gearbook.Core.Identity;
using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Filtering;

public class FilterEngineTests
{
    private static IReadOnlyList<ReconciledGearset> Sample() =>
    [
        TestData.Entry(1, 1, TestData.WhiteMageId, "Heal", favourite: true, itemLevel: 790, tags: "raid"),
        TestData.Entry(2, 2, TestData.DarkKnightId, "Ultimate", note: "for UWU", itemLevel: 785, tags: "raid"),
        TestData.Entry(3, 3, TestData.DarkKnightId, "Glamour", itemLevel: 640, glamourPlate: 4, tags: "glam"),
        TestData.Entry(4, 4, TestData.BotanistId, "Gathering", itemLevel: 700, barPosition: 0),
        TestData.Entry(5, 5, TestData.CulinarianId, "Cooking", incomplete: true, itemLevel: 690),
    ];

    private static IReadOnlyList<int> Apply(FilterSpec filter) =>
        [.. FilterEngine.Apply(Sample(), filter, TestData.Jobs, TestData.Now).Select(g => g.Record.Id)];

    [Fact]
    public void An_empty_filter_keeps_everything_in_the_game_order()
    {
        Assert.Equal([1, 2, 3, 4, 5], Apply(new FilterSpec()));
    }

    [Fact]
    public void Favourites_only_keeps_the_marked_ones()
    {
        Assert.Equal([1], Apply(new FilterSpec { FavouritesOnly = true }));
    }

    [Fact]
    public void A_role_filter_keeps_only_that_role()
    {
        Assert.Equal([2, 3], Apply(new FilterSpec { Roles = [JobRole.Tank] }));
    }

    [Fact]
    public void Several_roles_are_a_widening_not_a_narrowing()
    {
        Assert.Equal([1, 2, 3], Apply(new FilterSpec { Roles = [JobRole.Tank, JobRole.Healer] }));
    }

    [Fact]
    public void A_category_filter_keeps_only_that_category()
    {
        Assert.Equal([5], Apply(new FilterSpec { Categories = [JobCategory.Crafting] }));
    }

    [Fact]
    public void Several_tags_are_a_narrowing_because_a_set_must_carry_all_of_them()
    {
        Assert.Equal([1, 2], Apply(new FilterSpec { Tags = ["raid"] }));
        Assert.Empty(Apply(new FilterSpec { Tags = ["raid", "glam"] }));
    }

    [Fact]
    public void Tags_are_compared_without_regard_to_case_or_surrounding_space()
    {
        Assert.Equal([3], Apply(new FilterSpec { Tags = ["  GLAM "] }));
    }

    [Fact]
    public void Search_text_reaches_the_name_the_job_the_note_and_the_tags()
    {
        Assert.Equal([2], Apply(new FilterSpec { Text = "ultimate" }));
        Assert.Equal([2, 3], Apply(new FilterSpec { Text = "dark knight" }));
        Assert.Equal([2], Apply(new FilterSpec { Text = "uwu" }));
        Assert.Equal([3], Apply(new FilterSpec { Text = "glam" }));
        Assert.Equal([1], Apply(new FilterSpec { Text = "whm" }));
    }

    [Fact]
    public void Two_search_terms_narrow_rather_than_widen()
    {
        // "dark ultimate" should find the ultimate set of the dark knight, not every dark
        // knight set plus every set called ultimate.
        Assert.Equal([2], Apply(new FilterSpec { Text = "dark ultimate" }));
    }

    [Fact]
    public void The_incomplete_filter_finds_sets_missing_a_piece()
    {
        Assert.Equal([5], Apply(new FilterSpec { Completeness = CompletenessFilter.IncompleteOnly }));
        Assert.Equal([1, 2, 3, 4], Apply(new FilterSpec { Completeness = CompletenessFilter.CompleteOnly }));
    }

    [Fact]
    public void The_glamour_filter_keeps_only_linked_sets()
    {
        Assert.Equal([3], Apply(new FilterSpec { GlamourLinkedOnly = true }));
    }

    [Fact]
    public void A_set_this_plugin_has_never_equipped_counts_as_unused()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 1, TestData.WhiteMageId, "Never worn"),
            TestData.Entry(2, 2, TestData.DarkKnightId, "Worn yesterday", lastUsed: TestData.Now.AddDays(-1)),
            TestData.Entry(3, 3, TestData.BotanistId, "Worn last year", lastUsed: TestData.Now.AddDays(-300)),
        };

        var kept = FilterEngine
            .Apply(gearsets, new FilterSpec { UnusedForDays = 30 }, TestData.Jobs, TestData.Now)
            .Select(g => g.Record.Id);

        Assert.Equal([1, 3], kept);
    }

    [Fact]
    public void Filters_combine_by_narrowing()
    {
        var filter = new FilterSpec
        {
            Roles = [JobRole.Tank],
            Tags = ["raid"],
        };

        Assert.Equal([2], Apply(filter));
    }

    [Fact]
    public void A_job_the_table_does_not_describe_does_not_take_the_list_down()
    {
        var gearsets = new[] { TestData.Entry(1, 1, 9999, "Job from a future patch") };

        var kept = FilterEngine.Apply(gearsets, new FilterSpec(), TestData.Jobs, TestData.Now);

        Assert.Single(kept);
    }

    [Fact]
    public void Sorting_by_name_puts_them_in_alphabetical_order()
    {
        Assert.Equal([5, 4, 3, 1, 2], Apply(new FilterSpec { Sort = GearsetSortOrder.Name }));
    }

    [Fact]
    public void Sorting_by_item_level_puts_the_highest_first()
    {
        Assert.Equal([1, 2, 4, 5, 3], Apply(new FilterSpec { Sort = GearsetSortOrder.ItemLevel }));
    }

    [Fact]
    public void Sorting_by_last_used_puts_sets_never_worn_last_rather_than_first()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 1, TestData.WhiteMageId, "Never"),
            TestData.Entry(2, 2, TestData.DarkKnightId, "Old", lastUsed: TestData.Now.AddDays(-10)),
            TestData.Entry(3, 3, TestData.BotanistId, "Recent", lastUsed: TestData.Now.AddDays(-1)),
        };

        var order = FilterEngine
            .Sort(gearsets, GearsetSortOrder.LastUsed, TestData.Jobs)
            .Select(g => g.Record.Id);

        // A set with no history is not the oldest, it is unknown, so it does not get sorted to
        // the top of a list that claims to be ordered by time.
        Assert.Equal([3, 2, 1], order);
    }

    [Fact]
    public void Every_order_breaks_ties_by_the_game_number_so_the_list_never_reshuffles()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 7, TestData.DarkKnightId, "Same", itemLevel: 700),
            TestData.Entry(2, 3, TestData.DarkKnightId, "Same", itemLevel: 700),
            TestData.Entry(3, 5, TestData.DarkKnightId, "Same", itemLevel: 700),
        };

        foreach (var order in Enum.GetValues<GearsetSortOrder>())
        {
            var first = FilterEngine.Sort(gearsets, order, TestData.Jobs).Select(g => g.Gearset.Slot);
            var second = FilterEngine.Sort(gearsets, order, TestData.Jobs).Select(g => g.Gearset.Slot);

            Assert.Equal(first, second);
            Assert.Equal([3, 5, 7], first);
        }
    }

    [Fact]
    public void Duplicates_are_the_sets_sharing_a_job_and_a_name()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 1, TestData.DarkKnightId, "Dark Knight"),
            TestData.Entry(2, 2, TestData.DarkKnightId, "dark knight"),
            TestData.Entry(3, 3, TestData.DarkKnightId, "Something else"),
            TestData.Entry(4, 4, TestData.WhiteMageId, "Dark Knight"),
        };

        var duplicates = FilterEngine.FindDuplicates(gearsets).Select(g => g.Record.Id);

        // Case does not make two sets distinguishable to a person, so it does not here either.
        // A different job does.
        Assert.Equal([1, 2], duplicates);
    }

    [Fact]
    public void Collected_tags_are_deduplicated_and_sorted()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 1, TestData.DarkKnightId, "A", tags: ["raid", "glam"]),
            TestData.Entry(2, 2, TestData.WhiteMageId, "B", tags: ["Raid", "  ", "alt"]),
        };

        Assert.Equal(["alt", "glam", "raid"], FilterEngine.CollectTags(gearsets));
    }
}

public class FilterSpecTests
{
    private static FilterSpec Full() => new()
    {
        Text = "dark",
        Roles = [JobRole.Tank],
        Categories = [JobCategory.Combat],
        Tags = ["raid"],
        FavouritesOnly = false,
        Completeness = CompletenessFilter.IncompleteOnly,
        GlamourLinkedOnly = true,
        UnusedForDays = 30,
        Sort = GearsetSortOrder.Name,
    };

    [Fact]
    public void An_untouched_filter_says_it_is_everything()
    {
        Assert.True(new FilterSpec().IsEverything);
        Assert.False(new FilterSpec { FavouritesOnly = true }.IsEverything);
    }

    [Fact]
    public void The_favourites_level_keeps_only_the_search_box_and_the_favourite_flag()
    {
        var clamped = Full().AtLevel(FilterLevel.FavouritesOnly);

        Assert.Equal("dark", clamped.Text);
        Assert.True(clamped.FavouritesOnly);
        Assert.Empty(clamped.Roles);
        Assert.Empty(clamped.Tags);
        Assert.False(clamped.GlamourLinkedOnly);
        Assert.Null(clamped.UnusedForDays);
    }

    [Fact]
    public void The_simple_level_keeps_roles_and_categories_but_not_the_rest()
    {
        var clamped = Full().AtLevel(FilterLevel.Simple);

        Assert.Equal([JobRole.Tank], clamped.Roles);
        Assert.Equal([JobCategory.Combat], clamped.Categories);
        Assert.Empty(clamped.Tags);
        Assert.False(clamped.GlamourLinkedOnly);
    }

    [Fact]
    public void Turning_the_panel_down_never_changes_what_is_stored()
    {
        // The promise in the settings help text: choosing a smaller level hides controls, it
        // does not throw away the views and tags somebody already made.
        var original = Full();

        original.AtLevel(FilterLevel.FavouritesOnly);
        original.AtLevel(FilterLevel.Simple);

        Assert.Equal(["raid"], original.Tags);
        Assert.True(original.GlamourLinkedOnly);
        Assert.Equal(30, original.UnusedForDays);
    }

    [Fact]
    public void A_clone_shares_nothing_with_its_original()
    {
        var original = Full();
        var copy = original.Clone();

        copy.Tags.Add("extra");
        copy.Roles.Clear();

        Assert.Equal(["raid"], original.Tags);
        Assert.Equal([JobRole.Tank], original.Roles);
    }
}
