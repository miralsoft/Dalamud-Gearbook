namespace Gearbook.Core.Filtering;

/// <summary>
/// How a list of gearsets is ordered. Every order breaks ties by the game's own number, so the
/// list never reshuffles between two draws for reasons the player cannot see.
/// </summary>
public enum GearsetSortOrder
{
    /// <summary>The order the game itself shows.</summary>
    Slot = 0,

    Name,
    Job,
    ItemLevel,

    /// <summary>
    /// Grouped by role, in the order the player put the roles in. Tanks, healers, melee, then
    /// the two ranged kinds by default, which is the order the game's own character window uses.
    /// Within a role the jobs stay together and the game's numbering breaks the ties.
    /// </summary>
    Role,

    /// <summary>Most recently equipped first. Sets this plugin has never equipped go last.</summary>
    LastUsed,
}
