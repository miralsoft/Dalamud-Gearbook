using Gearbook.Core.Filtering;
using Gearbook.Core.Views;
using Xunit;

namespace Gearbook.Core.Tests.Views;

public class SavedViewsTests
{
    private static List<SavedView> Views() =>
    [
        new() { Name = "Raid night", Filter = new FilterSpec { FavouritesOnly = true } },
        new() { Name = "Gathering", Filter = new FilterSpec { Text = "botanist" } },
    ];

    [Fact]
    public void A_free_name_is_accepted()
    {
        Assert.Equal(SavedViews.NameProblem.None, SavedViews.CheckName(Views(), "Crafting"));
    }

    [Fact]
    public void An_empty_name_is_refused()
    {
        Assert.Equal(SavedViews.NameProblem.Empty, SavedViews.CheckName(Views(), "   "));
        Assert.Equal(SavedViews.NameProblem.Empty, SavedViews.CheckName(Views(), null));
    }

    [Fact]
    public void A_taken_name_is_refused_whatever_its_case_or_spacing()
    {
        Assert.Equal(SavedViews.NameProblem.AlreadyTaken, SavedViews.CheckName(Views(), "raid night"));
        Assert.Equal(SavedViews.NameProblem.AlreadyTaken, SavedViews.CheckName(Views(), "  Raid night "));
    }

    [Fact]
    public void A_view_may_keep_its_own_name_while_being_renamed()
    {
        var views = Views();

        Assert.Equal(
            SavedViews.NameProblem.None,
            SavedViews.CheckName(views, "Raid Night", renaming: views[0]));
    }

    [Fact]
    public void Finding_a_view_ignores_case_and_surrounding_space()
    {
        var views = Views();

        Assert.Same(views[1], SavedViews.Find(views, " gathering "));
        Assert.Null(SavedViews.Find(views, "nothing"));
        Assert.Null(SavedViews.Find(views, null));
    }

    [Fact]
    public void The_next_view_wraps_round_at_the_end()
    {
        var views = Views();

        Assert.Same(views[1], SavedViews.Next(views, "Raid night"));
        Assert.Same(views[0], SavedViews.Next(views, "Gathering"));
    }

    [Fact]
    public void The_next_view_from_nothing_is_the_first_one()
    {
        var views = Views();

        Assert.Same(views[0], SavedViews.Next(views, null));
        Assert.Same(views[0], SavedViews.Next(views, "a view that was deleted"));
    }

    [Fact]
    public void There_is_no_next_view_when_there_are_none()
    {
        Assert.Null(SavedViews.Next([], "anything"));
    }
}
