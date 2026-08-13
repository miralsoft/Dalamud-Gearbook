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

    /// <summary>Most recently equipped first. Sets this plugin has never equipped go last.</summary>
    LastUsed,
}
