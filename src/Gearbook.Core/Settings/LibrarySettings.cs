namespace Gearbook.Core.Settings;

/// <summary>
/// How the library window presents itself, for one character.
/// </summary>
public sealed class LibrarySettings
{
    /// <summary>
    /// Show the game's own gearset number in the list. On by default: it is the only value that
    /// lets somebody find the same set in the game's own window, and without it the two lists
    /// cannot be compared at all.
    /// </summary>
    public bool ShowGameNumber { get; set; } = true;

    /// <summary>
    /// Show records whose gearset is not currently present. On by default, because the
    /// alternative is that a note silently disappears and the player has no way of learning
    /// that it is still there.
    /// </summary>
    public bool ShowOrphans { get; set; } = true;

    /// <summary>Warn where two gearsets share a job and a name.</summary>
    public bool WarnAboutDuplicates { get; set; } = true;
}
