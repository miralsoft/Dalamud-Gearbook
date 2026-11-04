namespace Gearbook.Core.Model;

/// <summary>
/// Decides whether the game's own idea of the current gearset can be believed.
/// </summary>
/// <remarks>
/// <para>
/// The gearset module's current index comes from the gearset data stored on this computer, not
/// from the server. Change job on another computer, or while playing without plugins, and the
/// index here still names the set worn last time on this machine. Trusted as it stood, the bar
/// marked a samurai set as worn while the player stood there as a miner, and refused to switch
/// back to samurai because it was "already worn".
/// </para>
/// <para>
/// The player's actual job comes from the server and is always right, so it is the check: an
/// index whose set belongs to a different job is stale and is treated as unknown. Unknown is the
/// safe direction (S-12): no set is marked and nothing is refused, where a wrong answer refuses a
/// switch the player asked for. A stale index between two sets of the same job cannot be caught
/// this way, because both pass the check.
/// </para>
/// </remarks>
public static class CurrentGearset
{
    /// <summary>
    /// The slot to treat as worn, or null when it cannot be confirmed.
    /// </summary>
    /// <param name="slot">The game's current gearset index, or null if it has none.</param>
    /// <param name="slotJob">The job of the gearset at that index, or null if there is no such set.</param>
    /// <param name="currentJob">The player's actual current job, or null if it is not known yet.</param>
    public static int? Verify(int? slot, uint? slotJob, uint? currentJob)
    {
        if (slot is null || slotJob is null || currentJob is null or 0)
        {
            return null;
        }

        return slotJob == currentJob ? slot : null;
    }
}
