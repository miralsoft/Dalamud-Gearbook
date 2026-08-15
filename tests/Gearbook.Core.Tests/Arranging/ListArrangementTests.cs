using Gearbook.Core.Arranging;
using Xunit;

namespace Gearbook.Core.Tests.Arranging;

/// <summary>
/// Moves for the game's own gearset list.
/// </summary>
/// <remarks>
/// Single letters stand in for gearsets. What matters here is the shape of the arrangement, and
/// a test written with real names would hide that behind strings nobody can compare at a glance.
///
/// Two of these tests apply the moves under both meanings the game's reordering call could have,
/// a swap and a lift-and-close, because being right under both is the property the whole design
/// rests on and it is not something a comment can guarantee.
/// </remarks>
public sealed class ListArrangementTests
{
    [Fact]
    public void A_list_that_already_reads_as_wanted_needs_no_move()
    {
        string[] list = ["a", "b", "c"];

        Assert.Null(ListArrangement.NextMove(list, list));
        Assert.Equal(0, ListArrangement.MovesRemaining(list, list));
    }

    [Fact]
    public void The_first_move_settles_the_first_position_that_is_wrong()
    {
        string[] current = ["a", "c", "b"];
        string[] target = ["a", "b", "c"];

        var move = ListArrangement.NextMove(current, target);

        Assert.NotNull(move);
        Assert.Equal(2, move.Value.FromPosition);
        Assert.Equal(1, move.Value.ToPosition);
    }

    [Fact]
    public void A_list_of_a_different_length_produces_no_move_at_all()
    {
        // What a list changed underneath the caller looks like. Moving anything here would
        // rearrange a list the plan was not made for.
        Assert.Null(ListArrangement.NextMove(["a", "b"], ["a", "b", "c"]));
    }

    [Fact]
    public void A_wanted_entry_that_is_not_there_stops_rather_than_guesses()
    {
        Assert.Null(ListArrangement.NextMove(["a", "b"], ["a", "z"]));
    }

    [Theory]
    [InlineData(new[] { "c", "b", "a" }, new[] { "a", "b", "c" })]
    [InlineData(new[] { "b", "c", "d", "a" }, new[] { "a", "b", "c", "d" })]
    [InlineData(new[] { "e", "d", "c", "b", "a" }, new[] { "a", "b", "c", "d", "e" })]
    [InlineData(new[] { "a", "b", "c" }, new[] { "c", "a", "b" })]
    public void Repeated_moves_reach_the_wanted_order_when_the_game_swaps(string[] start, string[] target)
    {
        Assert.Equal(target, RunToCompletion(start, target, ApplyAsSwap));
    }

    [Theory]
    [InlineData(new[] { "c", "b", "a" }, new[] { "a", "b", "c" })]
    [InlineData(new[] { "b", "c", "d", "a" }, new[] { "a", "b", "c", "d" })]
    [InlineData(new[] { "e", "d", "c", "b", "a" }, new[] { "a", "b", "c", "d", "e" })]
    [InlineData(new[] { "a", "b", "c" }, new[] { "c", "a", "b" })]
    public void Repeated_moves_reach_the_wanted_order_when_the_game_lifts_and_closes(
        string[] start,
        string[] target)
    {
        Assert.Equal(target, RunToCompletion(start, target, ApplyAsLift));
    }

    [Fact]
    public void Two_sets_sharing_a_job_and_a_name_are_interchangeable()
    {
        // They compare equal under every order this offers, so an arrangement that puts either
        // one first is finished. Demanding a particular one would move entries to no visible
        // effect, which is the one thing this must not do to somebody's list.
        string[] current = ["x", "a", "x"];
        string[] target = ["x", "x", "a"];

        var result = RunToCompletion(current, target, ApplyAsLift);

        Assert.Equal(target, result);
    }

    [Theory]
    [InlineData(new[] { "a", "b", "c" }, new[] { "a", "b", "c" }, 0)]
    [InlineData(new[] { "b", "a", "c" }, new[] { "a", "b", "c" }, 1)]
    [InlineData(new[] { "c", "b", "a" }, new[] { "a", "b", "c" }, 2)]
    public void The_count_of_moves_left_is_moves_and_not_differences(
        string[] current,
        string[] target,
        int expected)
    {
        // Two entries in the wrong order are two differing positions and one move. Counting
        // differences would promise twice the work that is actually there.
        Assert.Equal(expected, ListArrangement.MovesRemaining(current, target));
    }

    [Fact]
    public void The_count_matches_what_running_the_moves_actually_costs()
    {
        string[] start = ["e", "d", "c", "b", "a"];
        string[] target = ["a", "b", "c", "d", "e"];

        var predicted = ListArrangement.MovesRemaining(start, target);

        var list = new List<string>(start);
        var actual = 0;

        while (ListArrangement.NextMove(list, target) is { } move)
        {
            ApplyAsLift(list, move);
            actual++;
        }

        Assert.Equal(predicted, actual);
    }

    /// <summary>Applies moves until there are none left, or until it is clear there is a loop.</summary>
    private static List<string> RunToCompletion(
        string[] start,
        string[] target,
        Action<List<string>, ArrangementMove> apply)
    {
        var list = new List<string>(start);

        // A ceiling rather than a while-true. If the moves ever failed to settle a position the
        // test should fail with a wrong list, not hang the suite.
        for (var i = 0; i <= start.Length; i++)
        {
            var move = ListArrangement.NextMove(list, target);
            if (move is null)
            {
                return list;
            }

            apply(list, move.Value);
        }

        return list;
    }

    private static void ApplyAsSwap(List<string> list, ArrangementMove move) =>
        (list[move.ToPosition], list[move.FromPosition]) = (list[move.FromPosition], list[move.ToPosition]);

    private static void ApplyAsLift(List<string> list, ArrangementMove move)
    {
        var moved = list[move.FromPosition];
        list.RemoveAt(move.FromPosition);
        list.Insert(move.ToPosition, moved);
    }
}
