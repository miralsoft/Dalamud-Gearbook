using Gearbook.Core.Model;

namespace Gearbook.Adapters;

/// <summary>
/// Reads the gearsets out of the game.
/// </summary>
/// <remarks>
/// Everything behind this interface runs on the framework thread and cannot be covered by a
/// test, so the implementation stays as close to a pass-through as it can be. A pass-through
/// with logic in it is untested logic.
/// </remarks>
internal interface IGearsetReader
{
    /// <summary>
    /// Reads every gearset the current character has. Returns an empty list rather than throwing
    /// when the game is not in a state to be read.
    /// </summary>
    IReadOnlyList<GearsetSnapshot> Read();

    /// <summary>The gearset currently worn, by the game's own number, or null.</summary>
    int? CurrentSlot();

    /// <summary>
    /// The content id of the character whose gearsets these are, or null. This is the player's
    /// own character. No other character's identifier is read.
    /// </summary>
    ulong? CharacterContentId();
}
