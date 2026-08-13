namespace Gearbook.Core.Model;

/// <summary>
/// What a job does, which is the axis players actually filter on. The values are this
/// project's own; they are mapped from the game's job table by the plugin and handed to the
/// core, so that filtering stays testable without a running game.
/// </summary>
public enum JobRole
{
    /// <summary>The role could not be determined from the job table.</summary>
    Unknown = 0,

    Tank,
    Healer,
    MeleeDps,
    PhysicalRangedDps,
    MagicalRangedDps,

    /// <summary>A hand job, one of the eight crafters.</summary>
    Crafter,

    /// <summary>A land job, miner, botanist or fisher.</summary>
    Gatherer,
}
