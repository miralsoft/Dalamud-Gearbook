using Gearbook.Core.Sorting;
using Xunit;

namespace Gearbook.Core.Tests.Sorting;

/// <summary>
/// Putting one role's jobs back to the order they shipped in.
/// </summary>
/// <remarks>
/// The numbers stand in for jobs. 1, 2 and 3 are one role and 10, 11 the other, which is enough
/// to show the property that matters: a reset of one group must not move the other.
/// </remarks>
public sealed class JobOrderEditingTests
{
    private static readonly uint[] Wanted = [1, 2, 3, 10, 11];

    [Fact]
    public void AGroupGoesBackToItsWantedOrder()
    {
        var reset = JobOrderEditing.Reset([3, 2, 1, 10, 11], [1u, 2u, 3u], Wanted);

        Assert.Equal([1u, 2u, 3u, 10u, 11u], reset);
    }

    [Fact]
    public void TheOtherGroupIsNotTouched()
    {
        // The point of the whole method. A reset for the tanks must not quietly reorder the
        // healers, and an assignment would have done exactly that.
        var reset = JobOrderEditing.Reset([3, 2, 1, 11, 10], [1u, 2u, 3u], Wanted);

        Assert.Equal([1u, 2u, 3u, 11u, 10u], reset);
    }

    [Fact]
    public void TheGroupKeepsThePositionsItAlreadyOccupied()
    {
        // Interleaved on purpose. The stored list can hold the roles in any arrangement, because
        // only the order within a role is ever read, so a reset must refill the same places
        // rather than gather the group together.
        var reset = JobOrderEditing.Reset([3, 10, 2, 11, 1], [1u, 2u, 3u], Wanted);

        Assert.Equal([1u, 10u, 2u, 11u, 3u], reset);
    }

    [Fact]
    public void AJobTheWantedOrderDoesNotMentionKeepsAPlace()
    {
        // A reset is not a deletion. The unmentioned job follows the ones that were named.
        var reset = JobOrderEditing.Reset([3, 99, 1], [1u, 3u, 99u], Wanted);

        Assert.Equal([1u, 3u, 99u], reset);
    }

    [Fact]
    public void AnEmptyGroupChangesNothing()
    {
        var order = new uint[] { 3, 2, 1 };

        Assert.Equal(order, JobOrderEditing.Reset(order, [], Wanted));
    }

    [Fact]
    public void AGroupThatIsAlreadyRightIsLeftAsItIs()
    {
        var order = new uint[] { 1, 2, 3, 10, 11 };

        Assert.Equal(order, JobOrderEditing.Reset(order, [1u, 2u, 3u], Wanted));
    }
}
