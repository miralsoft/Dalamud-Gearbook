using Gearbook.Core.Model;
using Xunit;

namespace Gearbook.Core.Tests.Model;

/// <summary>
/// The job ids are the game's own (T-07): samurai 34, miner 16.
/// </summary>
public class CurrentGearsetTests
{
    private const uint Samurai = 34;
    private const uint Miner = 16;

    [Fact]
    public void An_index_whose_set_matches_the_actual_job_is_believed()
    {
        Assert.Equal(5, CurrentGearset.Verify(slot: 5, slotJob: Samurai, currentJob: Samurai));
    }

    [Fact]
    public void An_index_left_over_from_another_computer_is_not_believed()
    {
        // The reported case: samurai worn last time on this computer, miner chosen on another
        // one since. The stale index must not mark the samurai set as worn, or switching back to
        // it is refused as "already worn".
        Assert.Null(CurrentGearset.Verify(slot: 5, slotJob: Samurai, currentJob: Miner));
    }

    [Fact]
    public void An_unknown_actual_job_leaves_the_current_set_unknown()
    {
        Assert.Null(CurrentGearset.Verify(slot: 5, slotJob: Samurai, currentJob: null));
        Assert.Null(CurrentGearset.Verify(slot: 5, slotJob: Samurai, currentJob: 0));
    }

    [Fact]
    public void No_index_or_no_set_behind_it_is_unknown()
    {
        Assert.Null(CurrentGearset.Verify(slot: null, slotJob: Samurai, currentJob: Samurai));
        Assert.Null(CurrentGearset.Verify(slot: 5, slotJob: null, currentJob: Samurai));
    }
}
