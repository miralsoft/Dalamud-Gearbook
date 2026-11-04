using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

public class ExternalToolPlacementTests
{
    private static IReadOnlyList<ExternalTool> For(bool inCosmic, params JobCategory[] shown) =>
        ExternalToolPlacement.For(shown.ToHashSet(), inCosmic);

    [Fact]
    public void Combat_sets_alone_bring_only_arsenal()
    {
        Assert.Equal([ExternalTool.Arsenal], For(false, JobCategory.Combat));
    }

    [Fact]
    public void Crafting_sets_bring_artisan_and_arsenal()
    {
        Assert.Equal([ExternalTool.Artisan, ExternalTool.Arsenal], For(false, JobCategory.Crafting));
    }

    [Fact]
    public void Gathering_sets_bring_arsenal()
    {
        Assert.Equal([ExternalTool.Arsenal], For(false, JobCategory.Gathering));
    }

    [Fact]
    public void Cosmic_exploration_is_offered_for_crafting_or_gathering_only_inside_that_content()
    {
        Assert.Equal(
            [ExternalTool.Artisan, ExternalTool.Arsenal, ExternalTool.Cosmic],
            For(true, JobCategory.Crafting));
        Assert.Equal([ExternalTool.Arsenal, ExternalTool.Cosmic], For(true, JobCategory.Gathering));
        Assert.Equal([ExternalTool.Arsenal], For(true, JobCategory.Combat));
    }

    [Fact]
    public void A_mixed_bar_offers_each_tool_once_in_a_fixed_order()
    {
        Assert.Equal(
            [ExternalTool.Artisan, ExternalTool.Arsenal, ExternalTool.Cosmic],
            For(true, JobCategory.Gathering, JobCategory.Combat, JobCategory.Crafting));
    }

    [Fact]
    public void Sets_of_no_known_category_bring_nothing()
    {
        // A set that is neither combat, crafting nor gathering has no target to compare against
        // and no crafting plugin to open, so it must not pull a shortcut onto the bar.
        Assert.Empty(For(true, JobCategory.Unknown));
        Assert.Empty(For(true));
    }

    [Fact]
    public void The_categories_shown_are_read_from_the_job_table()
    {
        var gearsets = new[]
        {
            TestData.Entry(1, 1, TestData.DarkKnightId, "Tank"),
            TestData.Entry(2, 2, TestData.CulinarianId, "Cook"),
            TestData.Entry(3, 3, 9999, "Job from a future patch"),
        };

        var shown = ExternalToolPlacement.CategoriesShown(gearsets, TestData.Jobs);

        Assert.Equal(
            new HashSet<JobCategory> { JobCategory.Combat, JobCategory.Crafting, JobCategory.Unknown },
            shown);
    }
}
