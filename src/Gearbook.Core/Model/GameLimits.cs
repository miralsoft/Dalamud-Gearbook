namespace Gearbook.Core.Model;

/// <summary>
/// Limits the game imposes, in one place.
/// </summary>
/// <remarks>
/// These cannot be derived at run time from anything the plugin can reach, so they are named
/// constants carrying the reason rather than numbers repeated at the places that need them. The
/// value below is the size of the fixed array the gearset module holds, which is also the total
/// the game shows beside its own gearset list.
/// </remarks>
public static class GameLimits
{
    /// <summary>
    /// How many gearsets a character can have. Used to bound the read loop and, because no bar
    /// can ever need more columns than there are gearsets to put in it, as the ceiling on the
    /// bar's column count.
    /// </summary>
    public const int MaxGearsets = 100;
}
